using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped composition adapter that owns one LocalPlayerProvisioningBridge for the
    /// established FrameworkRuntimeHost lifetime. Slot state remains in the plain C# context and
    /// Unity technical-host creation remains in PlayerInputManager.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G/P3J host-scoped local Player technical-host provisioning composition adapter.")]
    internal sealed partial class LocalPlayerProvisioningRuntimeHostModule : MonoBehaviour
    {
        private FrameworkRuntimeHost _runtimeHost;
        private PlayerParticipationRuntimeContext _participationContext;
        private LocalPlayerProvisioningAuthoring _authoring;
        private LocalPlayerProvisioningBridge _bridge;
        private string _diagnostic = "Local Player provisioning runtime is not initialized.";
        private int _requestCount;

        internal bool IsReady =>
            _runtimeHost != null &&
            _participationContext != null &&
            _authoring != null &&
            _bridge != null;

        internal string Diagnostic => _diagnostic;
        internal LocalPlayerProvisioningAuthoring Authoring => _authoring;
        internal LocalPlayerJoinResult LastJoinResult => _bridge?.LastResult;
        internal int RequestCount => _requestCount;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            LocalPlayerProvisioningAuthoring authoring,
            out LocalPlayerProvisioningRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;

            if (runtimeHost == null)
            {
                issue = "Local Player provisioning requires an explicit FrameworkRuntimeHost.";
                authoring?.ReportRuntimeInitializationFailure(issue);
                return false;
            }

            if (authoring == null)
            {
                issue = "Local Player provisioning requires an explicit authoring component.";
                return false;
            }

            module = runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>();
            if (module == null)
            {
                module = runtimeHost.gameObject.AddComponent<LocalPlayerProvisioningRuntimeHostModule>();
            }

            bool initialized = module.TryInitialize(runtimeHost, authoring, out issue);
            if (!initialized)
            {
                authoring.ReportRuntimeInitializationFailure(issue);
            }

            return initialized;
        }

        internal bool TryInitialize(
            FrameworkRuntimeHost targetRuntimeHost,
            LocalPlayerProvisioningAuthoring targetAuthoring,
            out string issue)
        {
            issue = string.Empty;

            if (IsReady)
            {
                if (ReferenceEquals(_runtimeHost, targetRuntimeHost) &&
                    ReferenceEquals(_authoring, targetAuthoring))
                {
                    return true;
                }

                issue = "Local Player provisioning runtime is already bound to another authoring surface.";
                return false;
            }

            if (targetRuntimeHost == null)
            {
                issue = "FrameworkRuntimeHost is missing.";
                _diagnostic = issue;
                return false;
            }

            if (targetAuthoring == null)
            {
                issue = "Local Player provisioning authoring is missing.";
                _diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext targetParticipationContext))
            {
                issue = "FrameworkRuntimeHost has no initialized Session Player participation context.";
                _diagnostic = issue;
                return false;
            }

            if (!targetAuthoring.TryMaterializeManagerPrefab(out string materializationDiagnostic))
            {
                issue = materializationDiagnostic;
                _diagnostic = issue;
                return false;
            }

            if (!TryValidateRuntimeConfiguration(
                    targetAuthoring,
                    targetParticipationContext,
                    out issue))
            {
                _diagnostic = issue;
                return false;
            }

            PlayerInputManager targetManager = targetAuthoring.PlayerInputManager;
            targetManager.DisableJoining();
            if (targetManager.joiningEnabled)
            {
                issue =
                    $"PlayerInputManager '{targetManager.name}' did not close its technical joining gate during Session initialization.";
                _diagnostic = issue;
                return false;
            }

            if (!LocalPlayerProvisioningBridge.TryCreate(
                    targetParticipationContext,
                    targetAuthoring,
                    targetRuntimeHost.transform,
                    out LocalPlayerProvisioningBridge targetBridge,
                    out issue))
            {
                _diagnostic = issue;
                return false;
            }

            _runtimeHost = targetRuntimeHost;
            _participationContext = targetParticipationContext;
            _authoring = targetAuthoring;
            _bridge = targetBridge;
            _requestCount = 0;
            _diagnostic =
                $"Local Player provisioning runtime is ready. manager='{_authoring.PlayerInputManager.name}' localPlayerHostPrefab='{_authoring.LocalPlayerHostPrefab.name}'. {materializationDiagnostic}";

            try
            {
                _authoring.BindRuntime(this);
            }
            catch (Exception exception)
            {
                _bridge.Dispose();
                _bridge = null;
                _authoring = null;
                _participationContext = null;
                _runtimeHost = null;
                _diagnostic =
                    $"Local Player provisioning authoring rejected Session runtime binding. {exception.Message}";
                issue = _diagnostic;
                return false;
            }

            return true;
        }

        private static bool TryValidateRuntimeConfiguration(
            LocalPlayerProvisioningAuthoring targetAuthoring,
            PlayerParticipationRuntimeContext targetParticipationContext,
            out string issue)
        {
            issue = string.Empty;
            PlayerInputManager manager = targetAuthoring.PlayerInputManager;
            if (manager == null)
            {
                issue = "Local Player provisioning authoring has no explicit PlayerInputManager.";
                return false;
            }

            if (!targetAuthoring.UsesManualJoin)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' must use Join Players Manually. Current join behavior is '{manager.joinBehavior}'.";
                return false;
            }

            if (!targetAuthoring.UsesCSharpJoinNotifications)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' must use Invoke C# Events notifications. Current notification behavior is '{manager.notificationBehavior}'.";
                return false;
            }

            int configuredSlotCount = targetParticipationContext
                .CreateSnapshot()
                .ConfiguredSlotCount;
            if (manager.maxPlayerCount != configuredSlotCount)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' has derived bridge limit '{manager.maxPlayerCount}', but the initialized Player Session has '{configuredSlotCount}' Supported Slots. Update the serialized PlayerInputManager bridge configuration before boot.";
                return false;
            }

            GameObject localPlayerHostPrefab = targetAuthoring.LocalPlayerHostPrefab;
            if (localPlayerHostPrefab == null)
            {
                issue = "Local Player provisioning authoring has no Local Player Host Prefab.";
                return false;
            }

            if (!ReferenceEquals(manager.playerPrefab, localPlayerHostPrefab))
            {
                issue =
                    $"PlayerInputManager '{manager.name}' is not materialized with the authored Local Player Host Prefab '{localPlayerHostPrefab.name}'.";
                return false;
            }

            PlayerInput prefabPlayerInput = localPlayerHostPrefab.GetComponent<PlayerInput>();
            if (prefabPlayerInput == null)
            {
                issue = $"Local Player Host Prefab '{localPlayerHostPrefab.name}' has no PlayerInput component.";
                return false;
            }

            LocalPlayerHostAuthoring prefabHost =
                localPlayerHostPrefab.GetComponent<LocalPlayerHostAuthoring>();
            if (prefabHost == null)
            {
                issue = $"Local Player Host Prefab '{localPlayerHostPrefab.name}' has no LocalPlayerHostAuthoring.";
                return false;
            }

            if (!ReferenceEquals(prefabHost.PlayerInput, prefabPlayerInput))
            {
                issue =
                    $"LocalPlayerHostAuthoring on Local Player Host Prefab '{localPlayerHostPrefab.name}' does not resolve the prefab PlayerInput.";
                return false;
            }

            if (!prefabHost.TryValidateConfiguration(out string hostIssue))
            {
                issue = $"Local Player Host Prefab '{localPlayerHostPrefab.name}' is invalid. {hostIssue}";
                return false;
            }

            return true;
        }

        internal bool IsReadyFor(LocalPlayerProvisioningAuthoring targetAuthoring)
        {
            return IsReady && ReferenceEquals(_authoring, targetAuthoring);
        }

        internal LocalPlayerJoinResult TryJoin(LocalPlayerJoinRequest request)
        {
            if (!IsReady)
            {
                return LocalPlayerJoinResult.RuntimeUnavailable(request, _diagnostic);
            }

            _requestCount++;
            LocalPlayerJoinResult result = _bridge.TryJoin(request);
            _diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Local Player join returned no result.";
            return result ?? LocalPlayerJoinResult.RuntimeUnavailable(
                request,
                "Local Player provisioning bridge returned no result.");
        }

        internal SessionPlayerLeaveResult TryLeave(
            SessionPlayerLeaveRequest request)
        {
            if (!IsReady || _runtimeHost == null)
            {
                return SessionPlayerLeaveResult.RuntimeUnavailable(
                    request,
                    _diagnostic);
            }

            if (!SessionPlayerLeaveRuntimeHostModule.TryAttach(
                    _runtimeHost,
                    out SessionPlayerLeaveRuntimeHostModule leaveRuntime,
                    out string issue))
            {
                _diagnostic =
                    "Session Player Leave runtime could not be composed for the explicit request. " +
                    issue;
                return SessionPlayerLeaveResult.RuntimeUnavailable(
                    request,
                    _diagnostic);
            }

            _requestCount++;
            SessionPlayerLeaveResult result = leaveRuntime.TryLeave(request);
            _diagnostic = result != null
                ? result.ToDiagnosticString()
                : "Session Player Leave returned no result.";
            return result ?? SessionPlayerLeaveResult.RuntimeUnavailable(
                request,
                "Session Player Leave orchestration returned no result.");
        }

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return TryGetActorSelectionRuntime(out IPlayerActorSelectionRuntimePort runtime)
                ? runtime.TrySelectActorProfile(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectActorProfile",
                    request,
                    _diagnostic);
        }

        internal PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            var request = new PlayerActorSelectionRequest(
                playerSlotId,
                null,
                source,
                reason,
                expectedSelectionRevision);
            return TryGetActorSelectionRuntime(out IPlayerActorSelectionRuntimePort runtime)
                ? runtime.TrySelectDefaultActor(
                    playerSlotId,
                    expectedSelectionRevision,
                    source,
                    reason)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "SelectDefaultActor",
                    request,
                    _diagnostic);
        }

        internal PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return TryGetActorSelectionRuntime(out IPlayerActorSelectionRuntimePort runtime)
                ? runtime.TryReplaceActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ReplaceActorSelection",
                    request,
                    _diagnostic);
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return TryGetActorSelectionRuntime(out IPlayerActorSelectionRuntimePort runtime)
                ? runtime.TryClearActorSelection(request)
                : PlayerActorSelectionResult.RuntimeUnavailable(
                    "ClearActorSelection",
                    request,
                    _diagnostic);
        }

        private bool TryGetActorSelectionRuntime(
            out IPlayerActorSelectionRuntimePort runtime)
        {
            runtime = null;
            if (!IsReady || _runtimeHost == null)
            {
                return false;
            }

            runtime = _runtimeHost;
            if (runtime.TryValidatePlayerActorSelectionRuntime(out string issue))
            {
                return true;
            }

            _diagnostic = issue;
            runtime = null;
            return false;
        }

        internal LocalPlayerJoinResult RollbackCommittedJoin(
            LocalPlayerJoinResult joinResult,
            string reason)
        {
            return RollbackCommittedJoin(joinResult, reason, explicitCallerRollback: true);
        }

        internal LocalPlayerJoinResult RollbackCommittedJoin(
            LocalPlayerJoinResult joinResult,
            string reason,
            bool explicitCallerRollback)
        {
            const string source = nameof(LocalPlayerProvisioningRuntimeHostModule);
            if (_bridge == null)
            {
                return LocalPlayerJoinResult.RuntimeUnavailable(
                    joinResult != null ? joinResult.Request : default,
                    "Local Player provisioning bridge is unavailable for committed join rollback.");
            }

            if (joinResult == null || !joinResult.Succeeded ||
                !joinResult.Slot.PlayerSlotId.IsValid ||
                joinResult.LocalPlayerHost == null ||
                joinResult.PlayerInput == null)
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback requires complete successful join and physical Host evidence.");
            }

            if (!_runtimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule preparation))
            {
                return CreateRollbackFailure(
                    joinResult,
                    "FrameworkRuntimeHost has no ready Player Actor preparation authority for committed join rollback.");
            }

            if (preparation.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparationSnapshot) &&
                TryGetActivePreparation(preparationSnapshot, joinResult.Slot.PlayerSlotId,
                    out PlayerActorPreparationSummary activePreparation))
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback is blocked by active Player Actor preparation. " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' preparation='{activePreparation.Token.StableText}'.");
            }

            if (_runtimeHost.TryGetPlayerGameplayRuntimeSnapshot(
                    out PlayerGameplayRuntimeHostSnapshot gameplaySnapshot) &&
                HasActiveGameplayState(gameplaySnapshot, joinResult.Slot.PlayerSlotId))
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback is blocked by active Gameplay ownership, candidate or handoff state. " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' " +
                    $"gameplay='{gameplaySnapshot.ToDiagnosticString()}'.");
            }

            bool retainedEvidence = preparation.TryGetRetainedHostEvidence(
                joinResult.Slot.PlayerSlotId,
                out _);
            PlayerHostEvidenceResult hostEvidenceRelease = retainedEvidence
                ? preparation.ReleaseSessionPhysicalHost(
                    joinResult.Slot.PlayerSlotId,
                    joinResult.LocalPlayerHost,
                    source,
                    reason)
                : null;
            if (hostEvidenceRelease != null && !hostEvidenceRelease.Succeeded)
            {
                return CreateRollbackFailure(
                    joinResult,
                    "Committed Local Player join rollback could not release exact Host evidence. " +
                    hostEvidenceRelease.ToDiagnosticString());
            }

            LocalPlayerJoinResult rollback = _bridge.RollbackCommittedJoin(
                joinResult,
                reason,
                explicitCallerRollback);
            bool bridgeSucceeded = rollback?.RollbackResult != null &&
                                  rollback.RollbackResult.Succeeded;
            if (bridgeSucceeded)
            {
                _diagnostic =
                    $"Committed Local Player join was rolled back explicitly. " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' " +
                    $"hostEvidenceReleased='{(hostEvidenceRelease != null)}'.";
                return rollback;
            }

            PlayerHostEvidenceResult compensation = hostEvidenceRelease != null
                ? preparation.RegisterSessionPhysicalHost(
                    joinResult.Slot.PlayerSlotId,
                    joinResult.LocalPlayerHost,
                    source,
                    "rollback-bridge-failed-compensation")
                : null;
            return CreateRollbackFailure(
                joinResult,
                "Committed Local Player join rollback failed after Host evidence release. " +
                $"hostEvidenceReleased='{(hostEvidenceRelease != null)}' " +
                $"bridgeRollbackStatus='{rollback?.Status}' " +
                $"assignmentReleased='{rollback?.AssignmentRollbackResult?.Succeeded}' " +
                $"hostAdmissionReleased='{(rollback != null && rollback.RollbackResult != null)}' " +
                $"slotReleased='{rollback?.RollbackResult?.Succeeded}' " +
                $"compensationAttempted='{(compensation != null)}' " +
                $"compensationSucceeded='{(compensation?.Succeeded == true)}' " +
                $"bridge='{rollback?.ToDiagnosticString()}' compensation='{compensation?.ToDiagnosticString()}'.");
        }

        private static bool TryGetActivePreparation(
            PlayerActorPreparationRuntimeHostSnapshot snapshot,
            PlayerSlotId slotId,
            out PlayerActorPreparationSummary preparation)
        {
            preparation = default;
            PlayerActorPreparationSnapshot summaries = snapshot?.Preparation;
            if (summaries == null) return false;
            for (int index = 0; index < summaries.Slots.Count; index++)
            {
                PlayerActorPreparationSummary candidate = summaries.Slots[index];
                if (candidate.PlayerSlotId != slotId || !candidate.IsPrepared) continue;
                preparation = candidate;
                return true;
            }

            return false;
        }

        private static bool HasActiveGameplayState(
            PlayerGameplayRuntimeHostSnapshot snapshot,
            PlayerSlotId slotId)
        {
            bool admitted = snapshot?.Admission != null &&
                snapshot.Admission.TryGetSummary(
                    slotId,
                    out PlayerGameplayAdmissionSummary admission) &&
                admission.IsAdmitted;
            bool occupied = snapshot?.Occupancy != null &&
                snapshot.Occupancy.TryGetSummary(
                    slotId,
                    out PlayerGameplayOccupancySummary occupancy) &&
                occupancy.IsOccupied;
            bool inputBound = snapshot?.InputBinding != null &&
                snapshot.InputBinding.TryGetSummary(
                    slotId,
                    out PlayerGameplayInputBindingSummary input) &&
                input.IsBound;
            bool cameraEligible = snapshot?.CameraEligibility != null &&
                snapshot.CameraEligibility.TryGetSummary(
                    slotId,
                    out PlayerGameplayCameraEligibilitySummary camera) &&
                camera.IsEligible;
            return admitted || occupied || inputBound || cameraEligible;
        }

        private static LocalPlayerJoinResult CreateRollbackFailure(
            LocalPlayerJoinResult joinResult,
            string message)
        {
            return new LocalPlayerJoinResult(
                LocalPlayerJoinStatus.FailedRollback,
                joinResult != null ? joinResult.OperationId : default,
                joinResult != null ? joinResult.Request : default,
                joinResult?.ReservationResult,
                joinResult?.CommitResult,
                null,
                joinResult != null ? joinResult.Slot : default,
                joinResult?.PlayerInput,
                joinResult?.LocalPlayerHost,
                joinResult != null ? joinResult.UnityPlayerIndex : -1,
                joinResult != null
                    ? joinResult.CallbackConfirmation
                    : LocalPlayerJoinCallbackConfirmation.None,
                message,
                LocalPlayerJoinStatus.FailedRollback,
                joinResult?.AssignmentResult,
                null);
        }

        internal PlayerParticipationOperationResult TryOpenJoining(
            string source,
            string reason)
        {
            if (_participationContext == null)
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining",
                    source,
                    reason,
                    _diagnostic);
            }

            PlayerParticipationSnapshot before = _participationContext.CreateSnapshot();
            if (before.JoiningOpen)
            {
                PlayerParticipationOperationResult noChangeResult =
                    _participationContext.TryOpenJoining(source, reason);
                _diagnostic = noChangeResult.ToDiagnosticString();
                return noChangeResult;
            }

            PlayerInputManager manager = _authoring != null
                ? _authoring.PlayerInputManager
                : null;
            if (manager == null || !manager.isActiveAndEnabled)
            {
                string message = manager == null
                    ? "PlayerInputManager is missing; logical joining was not opened."
                    : $"PlayerInputManager '{manager.name}' is not active and enabled; logical joining was not opened.";
                var failed = new PlayerParticipationOperationResult(
                    PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                    "OpenJoining",
                    source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningRuntimeHostModule)),
                    reason.NormalizeTextOrFallback("technical-joining-gate-unavailable"),
                    message,
                    before.Revision,
                    before.Revision,
                    default,
                    default,
                    before);
                _diagnostic = failed.ToDiagnosticString();
                return failed;
            }

            manager.EnableJoining();
            if (!manager.joiningEnabled)
            {
                var failed = new PlayerParticipationOperationResult(
                    PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                    "OpenJoining",
                    source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningRuntimeHostModule)),
                    reason.NormalizeTextOrFallback("technical-joining-gate-enable-failed"),
                    $"PlayerInputManager '{manager.name}' did not enable its technical joining gate. " +
                        "Logical joining was not opened.",
                    before.Revision,
                    before.Revision,
                    default,
                    default,
                    before);
                _diagnostic = failed.ToDiagnosticString();
                return failed;
            }

            PlayerParticipationOperationResult result =
                _participationContext.TryOpenJoining(source, reason);
            _diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal PlayerParticipationOperationResult TryCloseJoining(
            string source,
            string reason)
        {
            if (_participationContext == null)
            {
                return PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining",
                    source,
                    reason,
                    _diagnostic);
            }

            PlayerInputManager manager = _authoring != null
                ? _authoring.PlayerInputManager
                : null;
            if (manager != null)
            {
                manager.DisableJoining();
                if (manager.joiningEnabled)
                {
                    PlayerParticipationSnapshot snapshot = _participationContext.CreateSnapshot();
                    var failed = new PlayerParticipationOperationResult(
                        PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                        "CloseJoining",
                        source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningRuntimeHostModule)),
                        reason.NormalizeTextOrFallback("technical-joining-gate-disable-failed"),
                        $"PlayerInputManager '{manager.name}' did not disable its technical joining gate. Logical joining remains unchanged.",
                        snapshot.Revision,
                        snapshot.Revision,
                        default,
                        default,
                        snapshot);
                    _diagnostic = failed.ToDiagnosticString();
                    return failed;
                }
            }

            PlayerParticipationOperationResult result =
                _participationContext.TryCloseJoining(source, reason);
            _diagnostic = result.ToDiagnosticString();
            return result;
        }

        internal bool TryGetSnapshot(out PlayerParticipationSnapshot snapshot)
        {
            if (_participationContext == null)
            {
                snapshot = PlayerParticipationSnapshot.Empty(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    _diagnostic);
                return false;
            }

            snapshot = _participationContext.CreateSnapshot();
            return true;
        }

        internal void SubscribeSessionChanges(Action<PlayerSessionChange> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (_participationContext == null)
            {
                throw new InvalidOperationException(
                    "Player Session change observation requires a live participation context.");
            }

            _participationContext.Changed += listener;
        }

        internal void UnsubscribeSessionChanges(Action<PlayerSessionChange> listener)
        {
            if (listener != null && _participationContext != null)
            {
                _participationContext.Changed -= listener;
            }
        }

        private void OnDestroy()
        {
            if (_authoring != null && _authoring.PlayerInputManager != null)
            {
                _authoring.PlayerInputManager.DisableJoining();
            }

            _bridge?.Dispose();
            _bridge = null;

            if (_authoring != null)
            {
                _authoring.UnbindRuntime(this, "Session Local Player provisioning runtime was released.");
            }

            _authoring = null;
            _participationContext = null;
            _runtimeHost = null;
            _diagnostic = "Session Local Player provisioning runtime was released.";
        }
    }

    /// <summary>
    /// Typed same-host access. A caller must already hold the FrameworkRuntimeHost reference;
    /// this does not introduce a static provisioning registry or service locator.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G/P3J typed FrameworkRuntimeHost access to its local Player provisioning module.")]
    internal static class FrameworkRuntimeHostLocalPlayerProvisioningExtensions
    {
        internal static bool TryGetLocalPlayerProvisioningRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out LocalPlayerProvisioningRuntimeHostModule module)
        {
            module = runtimeHost != null
                ? runtimeHost.GetComponent<LocalPlayerProvisioningRuntimeHostModule>()
                : null;
            return module != null && module.IsReady;
        }
    }
}
