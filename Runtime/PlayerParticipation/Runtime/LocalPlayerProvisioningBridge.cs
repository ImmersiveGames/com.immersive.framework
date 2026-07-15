using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
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
    internal sealed class LocalPlayerProvisioningBridge : IDisposable
    {
        private sealed class PlayerInputReferenceComparer : IEqualityComparer<PlayerInput>
        {
            internal static readonly PlayerInputReferenceComparer Instance = new();

            public bool Equals(PlayerInput x, PlayerInput y) => ReferenceEquals(x, y);

            public int GetHashCode(PlayerInput obj) =>
                obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
        }

        private readonly PlayerParticipationRuntimeContext participationContext;
        private readonly ILocalPlayerProvisioningBackend backend;
        private readonly Transform technicalHostParent;
        private readonly Dictionary<LocalPlayerJoinOperationId, LocalPlayerJoinCallbackConfirmation>
            callbackConfirmations = new();
        private readonly Dictionary<PlayerInput, LocalPlayerJoinOperationId>
            awaitingCallbackConfirmations = new(PlayerInputReferenceComparer.Instance);
        private readonly HashSet<PlayerInput> admittedPlayers =
            new(PlayerInputReferenceComparer.Instance);

        private PendingLocalPlayerJoin pendingJoin;
        private int operationSequence;
        private bool disposed;

        internal LocalPlayerProvisioningBridge(
            PlayerParticipationRuntimeContext participationContext,
            ILocalPlayerProvisioningBackend backend,
            Transform technicalHostParent)
        {
            this.participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            this.technicalHostParent = technicalHostParent != null
                ? technicalHostParent
                : throw new ArgumentNullException(nameof(technicalHostParent));
            backend.PlayerJoined += HandlePlayerJoined;
        }

        internal bool HasOperationInFlight => pendingJoin != null;
        internal int AwaitingCallbackConfirmationCount => awaitingCallbackConfirmations.Count;
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

            bridge = new LocalPlayerProvisioningBridge(
                participationContext,
                new UnityLocalPlayerProvisioningBackend(authoring.PlayerInputManager),
                technicalHostParent);
            issue = string.Empty;
            return true;
        }

        internal LocalPlayerJoinResult TryJoin(LocalPlayerJoinRequest request)
        {
            if (disposed)
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

            if (pendingJoin != null)
            {
                return Complete(CreateRejected(
                    LocalPlayerJoinStatus.RejectedOperationInFlight,
                    pendingJoin.OperationId,
                    request,
                    "Local Player join rejected because another provisioning operation is in flight.",
                    null,
                    pendingJoin.ReservationResult != null
                        ? pendingJoin.ReservationResult.Slot
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

            PlayerParticipationSnapshot initialSnapshot = participationContext.CreateSnapshot();
            int technicalMax = backend.TechnicalMaxPlayerCount;
            if (technicalMax > 0 && initialSnapshot.DynamicCapacity > technicalMax)
            {
                return Complete(CreateRejected(
                    LocalPlayerJoinStatus.RejectedManagerConfiguration,
                    default,
                    request,
                    $"Session dynamic capacity '{initialSnapshot.DynamicCapacity}' exceeds PlayerInputManager technical max player count '{technicalMax}'."));
            }

            operationSequence++;
            if (!LocalPlayerJoinOperationId.TryCreate(
                    initialSnapshot.ContextId,
                    operationSequence,
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
                participationContext.TryReserveNextAvailableSlot(
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

            pendingJoin = new PendingLocalPlayerJoin(
                operationId,
                request,
                reservationResult);

            PlayerInput provisionedPlayerInput;
            try
            {
                provisionedPlayerInput = backend.JoinPlayer(request);
            }
            catch (Exception exception)
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.FailedAdmission,
                    pendingJoin,
                    null,
                    null,
                    $"PlayerInputManager provisioning threw '{exception.GetType().Name}': {exception.Message}");
            }

            if (UnityObjectReference.IsClrNull(provisionedPlayerInput))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedProvisioningReturnedNull,
                    pendingJoin,
                    null,
                    pendingJoin.CallbackPlayerInput,
                    "PlayerInputManager.JoinPlayer returned null.");
            }

            pendingJoin.RecordDirectResult(provisionedPlayerInput);

            if (UnityObjectReference.IsUnityFakeNull(provisionedPlayerInput))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedMissingPlayerInput,
                    pendingJoin,
                    provisionedPlayerInput,
                    pendingJoin.CallbackPlayerInput,
                    "The provisioned PlayerInput evidence was destroyed or became unavailable before admission.");
            }

            if (pendingJoin.CallbackConfirmation ==
                LocalPlayerJoinCallbackConfirmation.RejectedDifferentPlayerInput)
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedCorrelationMismatch,
                    pendingJoin,
                    provisionedPlayerInput,
                    pendingJoin.CallbackPlayerInput,
                    "JoinPlayer return and PlayerInputManager joined callback reference different PlayerInput instances.");
            }

            LocalPlayerHostAuthoring localPlayerHost =
                provisionedPlayerInput.GetComponent<LocalPlayerHostAuthoring>();
            if (localPlayerHost == null)
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost,
                    pendingJoin,
                    provisionedPlayerInput,
                    pendingJoin.CallbackPlayerInput,
                    "The provisioned PlayerInput host has no LocalPlayerHostAuthoring.");
            }

            if (!ReferenceEquals(localPlayerHost.PlayerInput, provisionedPlayerInput))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost,
                    pendingJoin,
                    provisionedPlayerInput,
                    pendingJoin.CallbackPlayerInput,
                    "LocalPlayerHostAuthoring does not resolve the PlayerInput returned by JoinPlayer.");
            }

            if (!TryAttachHostToSessionLifetime(
                    localPlayerHost,
                    out string sessionLifetimeIssue))
            {
                return FailAndRollback(
                    LocalPlayerJoinStatus.RejectedInvalidLocalPlayerHost,
                    pendingJoin,
                    provisionedPlayerInput,
                    pendingJoin.CallbackPlayerInput,
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
                    pendingJoin,
                    provisionedPlayerInput,
                    pendingJoin.CallbackPlayerInput,
                    "Local Player Host admission staging failed. " + hostIssue);
            }

            PlayerParticipationOperationResult commitResult =
                participationContext.TryMarkJoined(
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
                    pendingJoin,
                    provisionedPlayerInput,
                    pendingJoin.CallbackPlayerInput,
                    commitResult != null
                        ? "Slot admission failed. " + commitResult.Message
                        : "Slot admission returned no result.",
                    commitResult);
            }

            localPlayerHost.CommitStagedAdmission(
                commitResult.Slot,
                request.Source,
                request.Reason);

            LocalPlayerJoinCallbackConfirmation callbackConfirmation =
                pendingJoin.CallbackConfirmation;
            callbackConfirmations[operationId] = callbackConfirmation;
            admittedPlayers.Add(provisionedPlayerInput);
            if (callbackConfirmation == LocalPlayerJoinCallbackConfirmation.Pending)
            {
                awaitingCallbackConfirmations[provisionedPlayerInput] = operationId;
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
                "Local Player technical host transferred to the persistent FrameworkRuntimeHost and admitted to the reserved Session Slot. Logical Actor remains unprepared.");
            pendingJoin = null;
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

            return callbackConfirmations.TryGetValue(operationId, out confirmation);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            backend.PlayerJoined -= HandlePlayerJoined;

            if (pendingJoin != null)
            {
                LocalPlayerHostAuthoring host = pendingJoin.DirectPlayerInput != null
                    ? pendingJoin.DirectPlayerInput.GetComponent<LocalPlayerHostAuthoring>()
                    : null;
                host?.RollbackStagedAdmission(
                    nameof(LocalPlayerProvisioningBridge),
                    "bridge-disposed");
                participationContext.TryReleaseReservation(
                    pendingJoin.ReservationToken,
                    nameof(LocalPlayerProvisioningBridge),
                    "bridge-disposed");
                RejectDistinctPlayers(
                    pendingJoin.DirectPlayerInput,
                    pendingJoin.CallbackPlayerInput,
                    "bridge-disposed");
                pendingJoin = null;
            }

            awaitingCallbackConfirmations.Clear();
            callbackConfirmations.Clear();
            admittedPlayers.Clear();
        }

        private bool TryAttachHostToSessionLifetime(
            LocalPlayerHostAuthoring host,
            out string issue)
        {
            issue = string.Empty;
            if (host == null)
            {
                issue = "Local Player Host is missing.";
                return false;
            }

            if (technicalHostParent == null)
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
                if (!hostTransform.IsChildOf(technicalHostParent))
                {
                    hostTransform.SetParent(
                        technicalHostParent,
                        false);
                }
            }
            catch (Exception exception)
            {
                issue =
                    $"Local Player Host Session parent transfer threw '{exception.GetType().Name}'. {exception.Message}";
                return false;
            }

            if (!hostTransform.IsChildOf(technicalHostParent) ||
                host.gameObject.scene !=
                    technicalHostParent.gameObject.scene)
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
            if (backend == null || !backend.IsAvailable)
            {
                status = LocalPlayerJoinStatus.RejectedManagerUnavailable;
                issue = "Local Player join requires an available explicit PlayerInputManager backend.";
                return false;
            }

            if (!backend.UsesManualJoin)
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager must use Join Players Manually.";
                return false;
            }

            GameObject prefab = backend.PlayerPrefab;
            if (prefab == null)
            {
                status = LocalPlayerJoinStatus.RejectedManagerConfiguration;
                issue = "PlayerInputManager has no Player Prefab.";
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

            int technicalMax = backend.TechnicalMaxPlayerCount;
            if (technicalMax > 0 && backend.CurrentPlayerCount >= technicalMax)
            {
                status = LocalPlayerJoinStatus.RejectedCapacityReached;
                issue = $"PlayerInputManager technical max player count '{technicalMax}' is reached.";
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
                participationContext.TryReleaseReservation(
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
            pendingJoin = null;
            return Complete(result);
        }

        private void HandlePlayerJoined(PlayerInput playerInput)
        {
            if (disposed)
            {
                return;
            }

            if (pendingJoin != null)
            {
                if (!pendingJoin.TryRecordCallback(playerInput))
                {
                    RejectDistinctPlayers(
                        playerInput,
                        null,
                        "joined-callback-diverged");
                }
                return;
            }

            if (!ReferenceEquals(playerInput, null) &&
                awaitingCallbackConfirmations.TryGetValue(
                    playerInput,
                    out LocalPlayerJoinOperationId operationId))
            {
                awaitingCallbackConfirmations.Remove(playerInput);
                callbackConfirmations[operationId] =
                    LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput;
                if (LastResult != null && LastResult.OperationId == operationId)
                {
                    LastResult = WithCallbackConfirmation(
                        LastResult,
                        LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput);
                }
                return;
            }

            if (!ReferenceEquals(playerInput, null) && admittedPlayers.Contains(playerInput))
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

        private void RejectDistinctPlayers(
            PlayerInput first,
            PlayerInput second,
            string reason)
        {
            if (!ReferenceEquals(first, null) && first != null)
            {
                admittedPlayers.Remove(first);
                awaitingCallbackConfirmations.Remove(first);
                backend.RejectPlayer(
                    first,
                    nameof(LocalPlayerProvisioningBridge),
                    reason);
            }

            if (!ReferenceEquals(second, null) &&
                second != null &&
                !ReferenceEquals(first, second))
            {
                admittedPlayers.Remove(second);
                awaitingCallbackConfirmations.Remove(second);
                backend.RejectPlayer(
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
                PlayerParticipationOperationStatus.RejectedCapacityReached =>
                    LocalPlayerJoinStatus.RejectedCapacityReached,
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
                result.OriginalStatus);
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
            string message)
        {
            return CreateResultCore(
                LocalPlayerJoinStatus.SucceededJoined, operationId, request,
                reservationResult, commitResult, null, slot, playerInput, host,
                playerInput != null ? playerInput.playerIndex : -1,
                callbackConfirmation, message);
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
            LocalPlayerJoinStatus originalStatus)
        {
            return CreateResultCore(
                status, operationId, request, reservationResult, commitResult,
                rollbackResult, slot, playerInput, host,
                playerInput != null ? playerInput.playerIndex : -1,
                callbackConfirmation, message, originalStatus);
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
            LocalPlayerJoinStatus originalStatus = LocalPlayerJoinStatus.None)
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
                originalStatus);
        }
    }
}
