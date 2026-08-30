using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped orchestration for authorized manual local Player joins.
    /// Slot state remains owned by PlayerParticipationRuntimeContext and physical host creation
    /// remains owned by PlayerInputManager through ILocalPlayerProvisioningBackend.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G/P3J manual local Player reservation, technical-host provisioning, correlation and admission bridge.")]
    internal sealed partial class LocalPlayerProvisioningBridge : IDisposable
    {
        private sealed class PlayerInputReferenceComparer : IEqualityComparer<PlayerInput>
        {
            internal static readonly PlayerInputReferenceComparer Instance = new();

            public bool Equals(PlayerInput x, PlayerInput y) => ReferenceEquals(x, y);

            public int GetHashCode(PlayerInput obj) =>
                obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
        }

        private readonly PlayerParticipationRuntimeContext _participationContext;
        private readonly ILocalPlayerProvisioningBackend _backend;
        private readonly Transform _technicalHostParent;
        private readonly GameObject _expectedLocalPlayerHostPrefab;
        private readonly Dictionary<LocalPlayerJoinOperationId, LocalPlayerJoinCallbackConfirmation>
            _callbackConfirmations = new();
        private readonly Dictionary<PlayerInput, LocalPlayerJoinOperationId>
            _awaitingCallbackConfirmations = new(PlayerInputReferenceComparer.Instance);
        private readonly HashSet<PlayerInput> _admittedPlayers =
            new(PlayerInputReferenceComparer.Instance);

        private PendingLocalPlayerJoin _pendingJoin;

        internal int AdmittedPlayerCount => _admittedPlayers.Count;
        internal bool IsAdmittedPlayer(PlayerInput playerInput)
        {
            return !ReferenceEquals(playerInput, null) &&
                _admittedPlayers.Contains(playerInput);
        }

        private int _operationSequence;
        private bool _disposed;

        internal LocalPlayerProvisioningBridge(
            PlayerParticipationRuntimeContext participationContext,
            ILocalPlayerProvisioningBackend backend,
            Transform technicalHostParent)
            : this(participationContext, backend, technicalHostParent,
                backend != null ? backend.PlayerPrefab : null)
        {
        }

        internal LocalPlayerProvisioningBridge(
            PlayerParticipationRuntimeContext participationContext,
            ILocalPlayerProvisioningBackend backend,
            Transform technicalHostParent,
            GameObject expectedLocalPlayerHostPrefab)
        {
            this._participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
            this._backend = backend ?? throw new ArgumentNullException(nameof(backend));
            this._technicalHostParent = technicalHostParent != null
                ? technicalHostParent
                : throw new ArgumentNullException(nameof(technicalHostParent));
            this._expectedLocalPlayerHostPrefab = expectedLocalPlayerHostPrefab != null
                ? expectedLocalPlayerHostPrefab
                : throw new ArgumentNullException(nameof(expectedLocalPlayerHostPrefab));
            backend.PlayerJoined += HandlePlayerJoined;
        }

        internal bool HasOperationInFlight => _pendingJoin != null;
        internal int AwaitingCallbackConfirmationCount => _awaitingCallbackConfirmations.Count;
        internal LocalPlayerJoinResult LastResult { get; private set; }
        internal LocalPlayerJoinResult LastUnexpectedJoinResult { get; private set; }

        internal static bool TryCreate(
            PlayerParticipationRuntimeContext participationContext,
            LocalPlayerProvisioningAuthoring authoring,
            Transform technicalHostParent,
            out LocalPlayerProvisioningBridge bridge,
            out string issue)
        {
            bridge = null;

            if (participationContext == null)
            {
                issue = "Local Player provisioning requires a Session Player participation runtime context.";
                return false;
            }

            if (authoring == null)
            {
                issue = "Local Player provisioning authoring is missing.";
                return false;
            }

            if (technicalHostParent == null)
            {
                issue = "Local Player provisioning requires an explicit persistent technical-host parent.";
                return false;
            }

            if (authoring.PlayerInputManager == null)
            {
                issue = "Local Player provisioning authoring has no explicit PlayerInputManager.";
                return false;
            }

            if (authoring.LocalPlayerHostPrefab == null)
            {
                issue = "Local Player provisioning authoring has no Local Player Host Prefab.";
                return false;
            }

            bridge = new LocalPlayerProvisioningBridge(
                participationContext,
                new UnityLocalPlayerProvisioningBackend(authoring.PlayerInputManager),
                technicalHostParent,
                authoring.LocalPlayerHostPrefab);
            issue = string.Empty;
            return true;
        }

        internal LocalPlayerJoinResult TryJoin(LocalPlayerJoinRequest request)
        {
            if (_disposed)
            {
                return Complete(CreateRejected(
                    LocalPlayerJoinStatus.RejectedRuntimeUnavailable,
                    default,
                    request,
                    "Local Player join rejected because the provisioning bridge is disposed."));
            }

            if (!request.TryValidate(out string requestIssue))
            {
                return Complete(CreateRejected(
                    LocalPlayerJoinStatus.RejectedInvalidRequest,
                    default,
                    request,
                    requestIssue));
            }

            if (_pendingJoin != null)
            {
                return Complete(CreateRejected(
                    LocalPlayerJoinStatus.RejectedOperationInFlight,
                    _pendingJoin.OperationId,
                    request,
                    "Local Player join rejected because another provisioning operation is in flight.",
                    null,
                    _pendingJoin.ReservationResult != null
                        ? _pendingJoin.ReservationResult.Slot
                        : default));
            }

            if (!TryValidateBackend(out LocalPlayerJoinStatus backendStatus, out string backendIssue))
            {
                return Complete(CreateRejected(
                    backendStatus,
                    default,
                    request,
                    backendIssue));
            }

            PlayerParticipationSnapshot initialSnapshot = _participationContext.CreateSnapshot();

            _operationSequence++;
            if (!LocalPlayerJoinOperationId.TryCreate(
                    initialSnapshot.ContextId,
                    _operationSequence,
                    out LocalPlayerJoinOperationId operationId,
                    out string operationIssue))
            {
                return Complete(CreateRejected(
                    LocalPlayerJoinStatus.RejectedRuntimeUnavailable,
                    default,
                    request,
                    operationIssue));
            }

            PlayerParticipationOperationResult reservationResult =
                _participationContext.TryReserveNextAvailableSlot(
                    request.Source,
                    request.Reason);
            if (reservationResult == null || !reservationResult.Succeeded)
            {
                return Complete(CreateRejected(
                    MapReservationStatus(reservationResult),
                    operationId,
                    request,
                    reservationResult != null
                        ? reservationResult.Message
                        : "Local Player join reservation returned no result.",
                    reservationResult,
                    reservationResult != null ? reservationResult.Slot : default,
                    default));
            }

            _pendingJoin = new PendingLocalPlayerJoin(
                operationId,
                request,
                reservationResult);

            PlayerInput provisionedPlayerInput;
            try
            {
                provisionedPlayerInput = _backend.JoinPlayer(request);
            }
            catch (Exception exception)
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.FailedAdmission,
                    _pendingJoin,
                    null,
                    null,
                    $"PlayerInputManager provisioning threw '{exception.GetType().Name}': {exception.Message}");
            }

            if (UnityObjectReference.IsClrNull(provisionedPlayerInput))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedProvisioningReturnedNull,
                    _pendingJoin,
                    null,
                    _pendingJoin.CallbackPlayerInput,
                    "PlayerInputManager.JoinPlayer returned null.");
            }

            _pendingJoin.RecordDirectResult(provisionedPlayerInput);

            if (UnityObjectReference.IsUnityFakeNull(provisionedPlayerInput))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedMissingPlayerInput,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    "The provisioned PlayerInput evidence was destroyed or became unavailable before admission.");
            }

            if (_pendingJoin.CallbackConfirmation ==
                LocalPlayerJoinCallbackConfirmation.RejectedDifferentPlayerInput)
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedCorrelationMismatch,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    "JoinPlayer return and PlayerInputManager joined callback reference different PlayerInput instances.");
            }

            LocalPlayerHostAuthoring localPlayerHost =
                provisionedPlayerInput.GetComponent<LocalPlayerHostAuthoring>();
            if (localPlayerHost == null)
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    "The provisioned PlayerInput host has no LocalPlayerHostAuthoring.");
            }

            if (!ReferenceEquals(localPlayerHost.PlayerInput, provisionedPlayerInput))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    "LocalPlayerHostAuthoring does not resolve the PlayerInput returned by JoinPlayer.");
            }

            if (!TryAttachHostToSessionLifetime(
                    localPlayerHost,
                    out string sessionLifetimeIssue))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    "Local Player technical host could not enter the Session lifetime. " +
                    sessionLifetimeIssue);
            }

            if (!localPlayerHost.TryStageAdmission(
                    reservationResult.Slot,
                    request.Source,
                    request.Reason,
                    out string hostIssue))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    "Local Player Host admission staging failed. " + hostIssue);
            }

            PlayerParticipationOperationResult commitResult =
                _participationContext.TryMarkJoined(
                    reservationResult.ReservationToken,
                    request.Source,
                    request.Reason);
            if (commitResult == null || !commitResult.Succeeded)
            {
                LocalPlayerJoinStatus failedStatus = commitResult != null &&
                    commitResult.Status ==
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation
                    ? LocalPlayerJoinStatus.RejectedForeignOrStaleReservation
                    : LocalPlayerJoinStatus.FailedAdmission;

                return FailAndRollback(
                    failedStatus,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    commitResult != null
                        ? "Slot admission failed. " + commitResult.Message
                        : "Slot admission returned no result.",
                    commitResult);
            }

            try
            {
                localPlayerHost.CommitStagedAdmission(
                    commitResult.Slot,
                    request.Source,
                    request.Reason);
            }
            catch (Exception exception)
            {
                return FailCommittedJoinAndRollback(
                    LocalPlayerJoinStatus.FailedAdmission,
                    _pendingJoin,
                    provisionedPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    localPlayerHost,
                    commitResult,
                    $"Local Player Host commit threw '{exception.GetType().Name}': {exception.Message}");
            }

            string slotDisplayName =
                commitResult.Slot.Profile.DisplayName.NormalizeTextOrFallback("Player Slot");
            string slotId = commitResult.Slot.PlayerSlotId.Value.Value;
            localPlayerHost.gameObject.name =
                $"{slotDisplayName} [{slotId}] — Local Player Host";

            LocalPlayerJoinCallbackConfirmation callbackConfirmation =
                _pendingJoin.CallbackConfirmation;
            _callbackConfirmations[operationId] = callbackConfirmation;
            _admittedPlayers.Add(provisionedPlayerInput);
            if (callbackConfirmation == LocalPlayerJoinCallbackConfirmation.Pending)
            {
                _awaitingCallbackConfirmations[provisionedPlayerInput] = operationId;
            }

            LocalPlayerJoinResult succeeded = CreateSucceeded(
                operationId,
                request,
                reservationResult,
                commitResult,
                commitResult.Slot,
                provisionedPlayerInput,
                localPlayerHost,
                callbackConfirmation,
                null,
                "Local Player technical host transferred to the persistent FrameworkRuntimeHost and admitted to the reserved Session Slot without an Activity contextual assignment. Logical Actor remains unprepared.");
            _pendingJoin = null;
            return Complete(succeeded);
        }

        internal bool TryGetCallbackConfirmation(
            LocalPlayerJoinOperationId operationId,
            out LocalPlayerJoinCallbackConfirmation confirmation)
        {
            if (!operationId.IsValid)
            {
                confirmation = LocalPlayerJoinCallbackConfirmation.None;
                return false;
            }

            return _callbackConfirmations.TryGetValue(operationId, out confirmation);
        }

        internal LocalPlayerJoinResult RollbackCommittedJoin(
            LocalPlayerJoinResult joinResult,
            string reason,
            bool explicitCallerRollback = false)
        {
            const string source = nameof(LocalPlayerProvisioningBridge);
            string resolvedReason = reason.NormalizeTextOrFallback(
                "committed-join-rollback");
            if (joinResult == null ||
                !joinResult.Succeeded ||
                !joinResult.Slot.PlayerSlotId.IsValid)
            {
                return Complete(CreateRejected(
                    LocalPlayerJoinStatus.FailedRollback,
                    joinResult != null ? joinResult.OperationId : default,
                    joinResult != null ? joinResult.Request : default,
                    "Committed Local Player join rollback requires complete successful join evidence."));
            }

            string hostIssue = string.Empty;
            bool hostRestored =
                joinResult.LocalPlayerHost != null &&
                joinResult.LocalPlayerHost.TryReleaseCommittedAdmission(
                    joinResult.Slot.PlayerSlotId,
                    source,
                    resolvedReason,
                    out hostIssue);
            PlayerParticipationOperationResult slotRollback =
                _participationContext.TryAbandonJoinedSlotAfterAssignmentFailure(
                    joinResult.Slot.PlayerSlotId,
                    source,
                    resolvedReason);

            RejectDistinctPlayers(
                joinResult.PlayerInput,
                null,
                resolvedReason);
            _callbackConfirmations.Remove(joinResult.OperationId);

            bool slotRestored =
                slotRollback != null &&
                slotRollback.Succeeded;
            LocalPlayerJoinStatus status =
                hostRestored && slotRestored
                    ? LocalPlayerJoinStatus.FailedAdmission
                    : LocalPlayerJoinStatus.FailedRollback;
            string message =
                (explicitCallerRollback
                    ? "Committed Local Player join was rolled back explicitly. "
                    : "Committed Local Player join rolled back because physical Host registration failed. ") +
                $"hostReleased='{hostRestored}' " +
                $"slotReleased='{slotRestored}' hostIssue='{hostIssue}'.";
            return Complete(CreateRollbackResult(
                status,
                joinResult.OperationId,
                joinResult.Request,
                joinResult.ReservationResult,
                joinResult.CommitResult,
                slotRollback,
                slotRollback != null && slotRollback.Slot.IsValid
                    ? slotRollback.Slot
                    : joinResult.Slot,
                joinResult.PlayerInput,
                joinResult.LocalPlayerHost,
                joinResult.CallbackConfirmation,
                message,
                LocalPlayerJoinStatus.FailedAdmission));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _backend.PlayerJoined -= HandlePlayerJoined;

            if (_pendingJoin != null)
            {
                LocalPlayerHostAuthoring host = _pendingJoin.DirectPlayerInput != null
                    ? _pendingJoin.DirectPlayerInput.GetComponent<LocalPlayerHostAuthoring>()
                    : null;
                host?.RollbackStagedAdmission(
                    nameof(LocalPlayerProvisioningBridge),
                    "bridge-disposed");
                _participationContext.TryReleaseReservation(
                    _pendingJoin.ReservationToken,
                    nameof(LocalPlayerProvisioningBridge),
                    "bridge-disposed");
                RejectDistinctPlayers(
                    _pendingJoin.DirectPlayerInput,
                    _pendingJoin.CallbackPlayerInput,
                    "bridge-disposed");
                _pendingJoin = null;
            }

            _awaitingCallbackConfirmations.Clear();
            _callbackConfirmations.Clear();
            ReleaseAdmittedPlayers("session-provisioning-runtime-disposed");
        }

        internal bool TryAttachHostToSessionLifetime(
            LocalPlayerHostAuthoring host,
            out string issue)
        {
            issue = string.Empty;
            if (host == null)
            {
                issue = "Local Player Host is missing.";
                return false;
            }

            if (_technicalHostParent == null)
            {
                issue = "Persistent FrameworkRuntimeHost parent is unavailable.";
                return false;
            }

            Transform hostTransform = host.transform;
            if (hostTransform == null)
            {
                issue = "Local Player Host transform is unavailable.";
                return false;
            }

            try
            {
                if (!hostTransform.IsChildOf(_technicalHostParent))
                {
                    hostTransform.SetParent(
                        _technicalHostParent,
                        false);
                }
            }
            catch (Exception exception)
            {
                issue =
                    $"Local Player Host Session parent transfer threw '{exception.GetType().Name}'. {exception.Message}";
                return false;
            }

            if (!hostTransform.IsChildOf(_technicalHostParent) ||
                host.gameObject.scene !=
                    _technicalHostParent.gameObject.scene)
            {
                issue =
                    "Local Player Host did not enter the persistent FrameworkRuntimeHost hierarchy and scene.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryValidateBackend(
            out LocalPlayerJoinStatus status,
            out string issue)
        {
            if (_backend == null || !_backend.IsAvailable)
            {
                status = LocalPlayerJoinStatus.RejectedManagerUnavailable;
                issue = "Local Player join requires an available explicit PlayerInputManager backend.";
                return false;
            }

            if (!_backend.UsesManualJoin)
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager must use Join Players Manually.";
                return false;
            }

            GameObject prefab = _backend.PlayerPrefab;
            if (prefab == null)
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager has no Player Prefab.";
                return false;
            }

            if (!ReferenceEquals(prefab, _expectedLocalPlayerHostPrefab))
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue =
                    $"PlayerInputManager Player Prefab '{prefab.name}' diverges from the Local Player Host Prefab '{_expectedLocalPlayerHostPrefab.name}' materialized during Framework boot.";
                return false;
            }

            PlayerInput prefabPlayerInput = prefab.GetComponent<PlayerInput>();
            if (prefabPlayerInput == null)
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager Player Prefab has no PlayerInput component.";
                return false;
            }

            LocalPlayerHostAuthoring prefabHost =
                prefab.GetComponent<LocalPlayerHostAuthoring>();
            if (prefabHost == null)
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager Player Prefab has no LocalPlayerHostAuthoring.";
                return false;
            }

            if (!ReferenceEquals(prefabHost.PlayerInput, prefabPlayerInput))
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager Player Prefab LocalPlayerHostAuthoring does not resolve its PlayerInput.";
                return false;
            }

            if (!prefabHost.TryValidateConfiguration(out string hostIssue))
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager Player Prefab Local Player Host is invalid. " + hostIssue;
                return false;
            }

            status = LocalPlayerJoinStatus.None;
            issue = string.Empty;
            return true;
        }

        private LocalPlayerJoinResult FailAndRollback(
            LocalPlayerJoinStatus originalStatus,
            PendingLocalPlayerJoin pending,
            PlayerInput directPlayerInput,
            PlayerInput callbackPlayerInput,
            string message,
            PlayerParticipationOperationResult commitResult = null)
        {
            LocalPlayerHostAuthoring host = directPlayerInput != null
                ? directPlayerInput.GetComponent<LocalPlayerHostAuthoring>()
                : null;
            host?.RollbackStagedAdmission(
                pending.Request.Source,
                "local-player-join-rollback");

            PlayerParticipationOperationResult rollbackResult =
                _participationContext.TryReleaseReservation(
                    pending.ReservationToken,
                    pending.Request.Source,
                    "local-player-join-rollback");

            RejectDistinctPlayers(
                directPlayerInput,
                callbackPlayerInput,
                "local-player-join-rejected");

            LocalPlayerJoinStatus finalStatus = rollbackResult != null &&
                rollbackResult.Succeeded
                ? originalStatus
                : LocalPlayerJoinStatus.FailedRollback;

            PlayerSlotRuntimeSnapshot slot = rollbackResult != null &&
                rollbackResult.Slot.IsValid
                ? rollbackResult.Slot
                : pending.ReservationResult.Slot;

            LocalPlayerJoinResult result = CreateRollbackResult(
                finalStatus,
                pending.OperationId,
                pending.Request,
                pending.ReservationResult,
                commitResult,
                rollbackResult,
                slot,
                directPlayerInput,
                host,
                pending.CallbackConfirmation,
                finalStatus == LocalPlayerJoinStatus.FailedRollback
                    ? message + " Reservation rollback also failed."
                    : message,
                originalStatus);
            _pendingJoin = null;
            return Complete(result);
        }

        private LocalPlayerJoinResult FailCommittedJoinAndRollback(
            LocalPlayerJoinStatus originalStatus,
            PendingLocalPlayerJoin pending,
            PlayerInput directPlayerInput,
            PlayerInput callbackPlayerInput,
            LocalPlayerHostAuthoring host,
            PlayerParticipationOperationResult commitResult,
            string message)
        {
            host?.RollbackStagedAdmission(
                pending.Request.Source,
                "manager-assignment-rollback");

            PlayerParticipationOperationResult slotRollback =
                _participationContext.TryAbandonJoinedSlotAfterAssignmentFailure(
                    commitResult.Slot.PlayerSlotId,
                    pending.Request.Source,
                    "manager-assignment-rollback");

            RejectDistinctPlayers(
                directPlayerInput,
                callbackPlayerInput,
                "local-player-join-rejected");

            bool slotRestored = slotRollback != null && slotRollback.Succeeded;
            LocalPlayerJoinStatus finalStatus =
                slotRestored
                    ? originalStatus
                    : LocalPlayerJoinStatus.FailedRollback;
            string finalMessage = finalStatus == LocalPlayerJoinStatus.FailedRollback
                ? message +
                  $" Explicit rollback failed. slotRestored='{slotRestored}'."
                : message;

            LocalPlayerJoinResult result = CreateRollbackResult(
                finalStatus,
                pending.OperationId,
                pending.Request,
                pending.ReservationResult,
                commitResult,
                slotRollback,
                slotRollback != null && slotRollback.Slot.IsValid
                    ? slotRollback.Slot
                    : commitResult.Slot,
                directPlayerInput,
                host,
                pending.CallbackConfirmation,
                finalMessage,
                originalStatus);
            _pendingJoin = null;
            return Complete(result);
        }

        private void HandlePlayerJoined(PlayerInput playerInput)
        {
            if (_disposed)
            {
                return;
            }

            // PlayerInputManager also reports enabled PlayerInput components that already
            // belong to explicitly authored Scene Local Player surfaces. Those callbacks are
            // not manual provisioning requests and remain owned by the scene admission lifecycle.
            if (IsExplicitSceneLocalPlayerInput(playerInput))
            {
                return;
            }

            if (_pendingJoin != null)
            {
                if (!_pendingJoin.TryRecordCallback(playerInput))
                {
                    RejectDistinctPlayers(
                        playerInput,
                        null,
                        "joined-callback-diverged");
                }
                return;
            }

            if (!ReferenceEquals(playerInput, null) &&
                _awaitingCallbackConfirmations.TryGetValue(
                    playerInput,
                    out LocalPlayerJoinOperationId operationId))
            {
                _awaitingCallbackConfirmations.Remove(playerInput);
                _callbackConfirmations[operationId] =
                    LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput;
                if (LastResult != null && LastResult.OperationId == operationId)
                {
                    LastResult = WithCallbackConfirmation(
                        LastResult,
                        LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput);
                }
                return;
            }

            if (!ReferenceEquals(playerInput, null) && _admittedPlayers.Contains(playerInput))
            {
                return;
            }

            LocalPlayerHostAuthoring host = playerInput != null
                ? playerInput.GetComponent<LocalPlayerHostAuthoring>()
                : null;
            LastUnexpectedJoinResult = CreateUnexpectedJoin(
                playerInput,
                host,
                "PlayerInputManager reported a joined Player without an authorized Pending Local Player Join.");

            RejectDistinctPlayers(
                playerInput,
                null,
                "unexpected-player-join");
        }

        private static bool IsExplicitSceneLocalPlayerInput(
            PlayerInput playerInput)
        {
            if (ReferenceEquals(playerInput, null) || playerInput == null)
            {
                return false;
            }

            LocalPlayerHostAuthoring host =
                playerInput.GetComponent<LocalPlayerHostAuthoring>();
            if (host == null ||
                !ReferenceEquals(host.PlayerInput, playerInput))
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene scene =
                playerInput.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                SceneProvidedLocalPlayerAuthoring[] declarations =
                    roots[rootIndex]
                        .GetComponentsInChildren<
                            SceneProvidedLocalPlayerAuthoring>(true);
                for (int declarationIndex = 0;
                     declarationIndex < declarations.Length;
                     declarationIndex++)
                {
                    SceneProvidedLocalPlayerAuthoring declaration =
                        declarations[declarationIndex];
                    if (declaration != null &&
                        ReferenceEquals(
                            declaration.LocalPlayerHost,
                            host))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ReleaseAdmittedPlayers(string reason)
        {
            if (_admittedPlayers.Count == 0)
            {
                return;
            }

            if (!(_backend is IAdmittedLocalPlayerReleaseBackend releaseBackend))
            {
                throw new InvalidOperationException(
                    $"{nameof(LocalPlayerProvisioningBridge)} cannot release admitted Manager-Provisioned " +
                    $"players because backend '{_backend.GetType().FullName}' does not implement " +
                    $"{nameof(IAdmittedLocalPlayerReleaseBackend)}. RejectPlayer is not a teardown fallback.");
            }

            PlayerInput[] players = new PlayerInput[_admittedPlayers.Count];
            _admittedPlayers.CopyTo(players);
            _admittedPlayers.Clear();

            for (int index = 0; index < players.Length; index++)
            {
                PlayerInput playerInput = players[index];
                if (ReferenceEquals(playerInput, null) || playerInput == null)
                {
                    continue;
                }

                releaseBackend.ReleaseAdmittedPlayer(
                    playerInput,
                    nameof(LocalPlayerProvisioningBridge),
                    reason);
            }
        }

        private void RejectDistinctPlayers(
            PlayerInput first,
            PlayerInput second,
            string reason)
        {
            if (!ReferenceEquals(first, null) && first != null)
            {
                _admittedPlayers.Remove(first);
                _awaitingCallbackConfirmations.Remove(first);
                _backend.RejectPlayer(
                    first,
                    nameof(LocalPlayerProvisioningBridge),
                    reason);
            }

            if (!ReferenceEquals(second, null) &&
                second != null &&
                !ReferenceEquals(first, second))
            {
                _admittedPlayers.Remove(second);
                _awaitingCallbackConfirmations.Remove(second);
                _backend.RejectPlayer(
                    second,
                    nameof(LocalPlayerProvisioningBridge),
                    reason);
            }
        }

        private LocalPlayerJoinResult Complete(LocalPlayerJoinResult result)
        {
            LastResult = result;
            return result;
        }

        private static LocalPlayerJoinStatus MapReservationStatus(
            PlayerParticipationOperationResult reservationResult)
        {
            if (reservationResult == null)
            {
                return LocalPlayerJoinStatus.RejectedRuntimeUnavailable;
            }

            return reservationResult.Status switch
            {
                PlayerParticipationOperationStatus.None =>
                    LocalPlayerJoinStatus.FailedAdmission,
                PlayerParticipationOperationStatus.Succeeded =>
                    LocalPlayerJoinStatus.FailedAdmission,
                PlayerParticipationOperationStatus.IgnoredNoChange =>
                    LocalPlayerJoinStatus.FailedAdmission,
                PlayerParticipationOperationStatus.RejectedInvalidRequest =>
                    LocalPlayerJoinStatus.RejectedInvalidRequest,
                PlayerParticipationOperationStatus.RejectedInvalidState =>
                    LocalPlayerJoinStatus.RejectedRuntimeUnavailable,
                PlayerParticipationOperationStatus.RejectedJoiningClosed =>
                    LocalPlayerJoinStatus.RejectedJoiningClosed,
                PlayerParticipationOperationStatus.RejectedNoAvailableSlot =>
                    LocalPlayerJoinStatus.RejectedNoAvailableSlot,
                PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation =>
                    LocalPlayerJoinStatus.RejectedForeignOrStaleReservation,
                PlayerParticipationOperationStatus.FailedInvalidConfiguration =>
                    LocalPlayerJoinStatus.RejectedRuntimeUnavailable,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(reservationResult),
                    reservationResult.Status,
                    "Unknown Player participation reservation status.")
            };
        }

        private static LocalPlayerJoinResult WithCallbackConfirmation(
            LocalPlayerJoinResult result,
            LocalPlayerJoinCallbackConfirmation confirmation)
        {
            return new LocalPlayerJoinResult(
                result.Status,
                result.OperationId,
                result.Request,
                result.ReservationResult,
                result.CommitResult,
                result.RollbackResult,
                result.Slot,
                result.PlayerInput,
                result.LocalPlayerHost,
                result.UnityPlayerIndex,
                confirmation,
                result.Message,
                result.OriginalStatus,
                result.AssignmentResult,
                result.AssignmentRollbackResult);
        }

        private static LocalPlayerJoinResult CreateRejected(
            LocalPlayerJoinStatus status,
            LocalPlayerJoinOperationId operationId,
            LocalPlayerJoinRequest request,
            string message,
            PlayerParticipationOperationResult reservationResult = null,
            PlayerSlotRuntimeSnapshot slot = default,
            LocalPlayerJoinCallbackConfirmation callbackConfirmation =
                LocalPlayerJoinCallbackConfirmation.None)
        {
            return CreateResultCore(
                status, operationId, request, reservationResult, null, null,
                slot, null, null, -1, callbackConfirmation, message);
        }

        private static LocalPlayerJoinResult CreateSucceeded(
            LocalPlayerJoinOperationId operationId,
            LocalPlayerJoinRequest request,
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult commitResult,
            PlayerSlotRuntimeSnapshot slot,
            PlayerInput playerInput,
            LocalPlayerHostAuthoring host,
            LocalPlayerJoinCallbackConfirmation callbackConfirmation,
            PlayerSlotAssignmentResult assignmentResult,
            string message)
        {
            return CreateResultCore(
                LocalPlayerJoinStatus.SucceededJoined, operationId, request,
                reservationResult, commitResult, null, slot, playerInput, host,
                playerInput != null ? playerInput.playerIndex : -1,
                callbackConfirmation, message,
                assignmentResult: assignmentResult);
        }

        private static LocalPlayerJoinResult CreateRollbackResult(
            LocalPlayerJoinStatus status,
            LocalPlayerJoinOperationId operationId,
            LocalPlayerJoinRequest request,
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult commitResult,
            PlayerParticipationOperationResult rollbackResult,
            PlayerSlotRuntimeSnapshot slot,
            PlayerInput playerInput,
            LocalPlayerHostAuthoring host,
            LocalPlayerJoinCallbackConfirmation callbackConfirmation,
            string message,
            LocalPlayerJoinStatus originalStatus,
            PlayerSlotAssignmentResult assignmentResult = null,
            PlayerSlotAssignmentResult assignmentRollbackResult = null)
        {
            return CreateResultCore(
                status, operationId, request, reservationResult, commitResult,
                rollbackResult, slot, playerInput, host,
                playerInput != null ? playerInput.playerIndex : -1,
                callbackConfirmation, message, originalStatus,
                assignmentResult, assignmentRollbackResult);
        }

        private static LocalPlayerJoinResult CreateUnexpectedJoin(
            PlayerInput playerInput,
            LocalPlayerHostAuthoring host,
            string message)
        {
            return CreateResultCore(
                LocalPlayerJoinStatus.RejectedUnexpectedJoin, default, default,
                null, null, null, default, playerInput, host,
                playerInput != null ? playerInput.playerIndex : -1,
                LocalPlayerJoinCallbackConfirmation.RejectedUnexpectedCallback,
                message);
        }

        private static LocalPlayerJoinResult CreateResultCore(
            LocalPlayerJoinStatus status,
            LocalPlayerJoinOperationId operationId,
            LocalPlayerJoinRequest request,
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult commitResult,
            PlayerParticipationOperationResult rollbackResult,
            PlayerSlotRuntimeSnapshot slot,
            PlayerInput playerInput,
            LocalPlayerHostAuthoring localPlayerHost,
            int unityPlayerIndex,
            LocalPlayerJoinCallbackConfirmation callbackConfirmation,
            string message,
            LocalPlayerJoinStatus originalStatus = LocalPlayerJoinStatus.None,
            PlayerSlotAssignmentResult assignmentResult = null,
            PlayerSlotAssignmentResult assignmentRollbackResult = null)
        {
            return new LocalPlayerJoinResult(
                status,
                operationId,
                request,
                reservationResult,
                commitResult,
                rollbackResult,
                slot,
                playerInput,
                localPlayerHost,
                unityPlayerIndex,
                callbackConfirmation,
                message,
                originalStatus,
                assignmentResult,
                assignmentRollbackResult);
        }
    }
}
