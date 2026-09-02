using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Official FrameworkRuntimeHost-scoped composition for P3K.2-P3K.7E.
    /// Domain state remains in the plain C# authorities; this component owns only
    /// their explicit Session lifetime and typed cross-authority wiring.
    /// </summary>
    [DisallowMultipleComponent]
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7F official Session Player gameplay authority composition.")]
    internal sealed partial class PlayerGameplayRuntimeHostModule : MonoBehaviour
    {
        private FrameworkRuntimeHost _runtimeHost;
        private PlayerParticipationRuntimeContext _participationContext;
        private PlayerActorPreparationRuntimeHostModule _preparationModule;
        private PlayerGameplayOccupancyRuntimeContext _occupancyContext;
        private PlayerGameplayInputBindingRuntimeContext _inputContext;
        private PlayerGameplayCameraEligibilityRuntimeContext _cameraContext;
        private PlayerGameplayAdmissionRuntimeContext _admissionContext;
        private PlayerGameplayCurrentContextRuntime _currentGameplayContext;
        private IActivityPlayerLifecycleAdmissionRuntime _activityRelocationContext;
        private PlayerGameplayRuntimeOperationStatus _lastOperationStatus;
        private string _diagnostic =
            "Player gameplay runtime is not initialized.";
        private bool _shuttingDown;

        internal bool IsReady =>
            _runtimeHost != null &&
            _participationContext != null &&
            _preparationModule != null &&
            _occupancyContext != null &&
            _inputContext != null &&
            _cameraContext != null &&
            _admissionContext != null &&
            _currentGameplayContext != null;

        internal string Diagnostic => _diagnostic;

        internal static bool TryAttach(
            FrameworkRuntimeHost runtimeHost,
            out PlayerGameplayRuntimeHostModule module,
            out string issue)
        {
            module = null;
            issue = string.Empty;

            if (runtimeHost == null)
            {
                issue =
                    "Player gameplay runtime requires an explicit FrameworkRuntimeHost.";
                return false;
            }

            module = runtimeHost.GetComponent<PlayerGameplayRuntimeHostModule>();
            if (module == null)
            {
                module =
                    runtimeHost.gameObject.AddComponent<PlayerGameplayRuntimeHostModule>();
            }

            return module.TryInitialize(runtimeHost, out issue);
        }

        internal bool TryInitialize(
            FrameworkRuntimeHost targetRuntimeHost,
            out string issue)
        {
            issue = string.Empty;

            if (IsReady)
            {
                if (ReferenceEquals(_runtimeHost, targetRuntimeHost))
                {
                    return true;
                }

                issue =
                    "Player gameplay runtime is already bound to another FrameworkRuntimeHost.";
                return false;
            }

            if (targetRuntimeHost == null)
            {
                issue = "FrameworkRuntimeHost is missing.";
                _diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerParticipationRuntime(
                    out PlayerParticipationRuntimeContext targetParticipation))
            {
                issue =
                    "FrameworkRuntimeHost has no initialized Player participation authority.";
                _diagnostic = issue;
                return false;
            }

            if (!targetRuntimeHost.TryGetPlayerActorPreparationRuntime(
                    out PlayerActorPreparationRuntimeHostModule targetPreparation))
            {
                issue =
                    "FrameworkRuntimeHost has no ready P3J Player Actor preparation module.";
                _diagnostic = issue;
                return false;
            }

            if (!targetPreparation.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparationHost) ||
                preparationHost == null ||
                !preparationHost.IsInitialized ||
                preparationHost.Preparation == null ||
                !preparationHost.Preparation.IsInitialized)
            {
                issue =
                    "Player gameplay runtime requires an initialized P3J preparation snapshot.";
                _diagnostic = issue;
                return false;
            }

            if (!PlayerGameplayOccupancyRuntimeContext.TryCreate(
                    preparationHost.Preparation,
                    out PlayerGameplayOccupancyRuntimeContext targetOccupancy,
                    out issue))
            {
                _diagnostic = "P3K.2 composition failed. " + issue;
                issue = _diagnostic;
                return false;
            }

            if (!PlayerGameplayInputBindingRuntimeContext.TryCreate(
                    targetPreparation,
                    targetOccupancy,
                    out PlayerGameplayInputBindingRuntimeContext targetInput,
                    out issue))
            {
                _diagnostic = "P3K.3 composition failed. " + issue;
                issue = _diagnostic;
                return false;
            }

            if (!PlayerGameplayCameraEligibilityRuntimeContext.TryCreate(
                    targetOccupancy,
                    targetInput,
                    out PlayerGameplayCameraEligibilityRuntimeContext targetCamera,
                    out issue))
            {
                _diagnostic = "P3K.4 composition failed. " + issue;
                issue = _diagnostic;
                return false;
            }

            if (!PlayerGameplayAdmissionRuntimeContext.TryCreate(
                    targetOccupancy,
                    targetInput,
                    targetCamera,
                    out PlayerGameplayAdmissionRuntimeContext targetAdmission,
                    out issue))
            {
                _diagnostic = "P3K.5 composition failed. " + issue;
                issue = _diagnostic;
                return false;
            }

            var endpointSource =
                new HostScopedPlayerGameplayChainEndpointSource(
                    targetRuntimeHost,
                    targetPreparation);

            if (!PlayerGameplayCurrentContextRuntime.TryCreate(
                    targetPreparation,
                    endpointSource,
                    targetOccupancy,
                    targetInput,
                    targetCamera,
                    targetAdmission,
                    out PlayerGameplayCurrentContextRuntime targetCurrentGameplay,
                    out issue))
            {
                _diagnostic = "Current gameplay context composition failed. " + issue;
                issue = _diagnostic;
                return false;
            }

            _runtimeHost = targetRuntimeHost;
            _participationContext = targetParticipation;
            _preparationModule = targetPreparation;
            _occupancyContext = targetOccupancy;
            _inputContext = targetInput;
            _cameraContext = targetCamera;
            _admissionContext = targetAdmission;
            _currentGameplayContext = targetCurrentGameplay;
            _activityRelocationContext =
                new ActivityPlayerRelocationContextRuntime(targetPreparation);
            targetRuntimeHost.SetActivityPlayerLifecycleAdmissionRuntime(
                _activityRelocationContext);

            _lastOperationStatus =
                PlayerGameplayRuntimeOperationStatus.None;
            _diagnostic =
                $"Player gameplay runtime is ready. session='{preparationHost.SessionContextId}' " +
                $"slots='{preparationHost.Preparation.ConfiguredSlotCount}'.";
            return true;
        }

        internal PlayerGameplayRuntimeOperationResult TryEnsureCurrentGameplay(
            PlayerSlotId playerSlotId,
            RuntimeContentOwner contextualOwner,
            string source,
            string reason)
        {
            const string operation = "EnsureCurrentGameplay";
            PlayerGameplayAdmissionSummary previous =
                GetAdmissionOrDefault(playerSlotId);

            if (!IsReady)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedRuntimeUnavailable,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    _diagnostic);
            }

            if (!playerSlotId.IsValid || !contextualOwner.IsValid ||
                contextualOwner.Scope != RuntimeContentScope.Activity)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Current gameplay creation requires a valid Player Slot and Activity contextual owner.");
            }

            if (!_preparationModule.TryGetCurrentPreparation(
                    playerSlotId,
                    out PlayerActorPreparationSummary preparation,
                    out string preparationIssue) ||
                !preparation.IsPrepared)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    preparationIssue);
            }

            bool succeeded = _currentGameplayContext.TryEnsureCurrentGameplay(
                preparation,
                contextualOwner,
                source,
                reason,
                out PlayerGameplayAdmissionSummary current,
                out bool rollbackAttempted,
                out bool rollbackSucceeded,
                out string rollbackMessage,
                out string issue);

            if (!succeeded)
            {
                PlayerGameplayRuntimeOperationStatus failure =
                    rollbackAttempted && !rollbackSucceeded
                        ? PlayerGameplayRuntimeOperationStatus.FailedChainRollback
                        : PlayerGameplayRuntimeOperationStatus.FailedChainBuild;
                return Result(
                    failure,
                    operation,
                    playerSlotId,
                    previous,
                    GetAdmissionOrDefault(playerSlotId),
                    rollbackAttempted,
                    rollbackSucceeded,
                    rollbackMessage,
                    issue);
            }

            bool alreadyAdmitted = previous.IsAdmitted &&
                previous.Owner == contextualOwner;
            PlayerGameplayRuntimeOperationStatus successStatus =
                current.GameplayReady
                    ? alreadyAdmitted
                        ? PlayerGameplayRuntimeOperationStatus.SucceededAlreadyReady
                        : PlayerGameplayRuntimeOperationStatus.SucceededReady
                    : alreadyAdmitted
                        ? PlayerGameplayRuntimeOperationStatus.SucceededAlreadyBlockedByInputGate
                        : PlayerGameplayRuntimeOperationStatus.SucceededBlockedByInputGate;
            string successMessage = current.GameplayReady
                ? alreadyAdmitted
                    ? "Current Player gameplay chain is already authoritative and GameplayReady."
                    : "Current Player gameplay chain became authoritative and GameplayReady."
                : alreadyAdmitted
                    ? "Current Player gameplay chain is already authoritative but blocked by the input Gate."
                    : "Current Player gameplay chain became authoritative but is blocked by the input Gate.";

            return Result(
                successStatus,
                operation,
                playerSlotId,
                previous,
                current,
                false,
                true,
                string.Empty,
                successMessage);
        }

        internal PlayerGameplayRuntimeOperationResult TryReleaseCurrentGameplay(
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken expectedAdmission,
            string source,
            string reason)
        {
            const string operation = "ReleaseCurrentGameplay";
            PlayerGameplayAdmissionSummary previous =
                GetAdmissionOrDefault(playerSlotId);

            if (!IsReady)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedRuntimeUnavailable,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    _diagnostic);
            }

            if (!playerSlotId.IsValid ||
                !expectedAdmission.IsValid ||
                expectedAdmission.PlayerSlotId != playerSlotId)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Gameplay release requires a valid Slot and exact admission token.");
            }

            if (!previous.IsAdmitted ||
                previous.Token != expectedAdmission)
            {
                return Result(
                    PlayerGameplayRuntimeOperationStatus
                        .RejectedForeignOrStaleAdmission,
                    operation,
                    playerSlotId,
                    previous,
                    previous,
                    false,
                    false,
                    string.Empty,
                    "Expected gameplay admission token is foreign or stale.");
            }

            PlayerGameplayAdmissionResult release =
                _currentGameplayContext.TryReleaseCurrentGameplay(
                    playerSlotId,
                    expectedAdmission,
                    source,
                    reason);
            PlayerGameplayAdmissionSummary current =
                GetAdmissionOrDefault(playerSlotId);

            return release.Succeeded
                ? Result(
                    PlayerGameplayRuntimeOperationStatus.SucceededReleased,
                    operation,
                    playerSlotId,
                    previous,
                    current,
                    false,
                    true,
                    string.Empty,
                    release.Message)
                : Result(
                    PlayerGameplayRuntimeOperationStatus.FailedRelease,
                    operation,
                    playerSlotId,
                    previous,
                    current,
                    false,
                    false,
                    string.Empty,
                    release.ToDiagnosticString());
        }

        internal bool TryReleaseCurrentOccupancyForPreparation(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!IsReady || !playerSlotId.IsValid || !expectedPreparation.IsValid ||
                expectedPreparation.PlayerSlotId != playerSlotId)
            {
                issue =
                    "Gameplay occupancy release requires a ready runtime plus exact Slot and prepared Actor evidence.";
                return false;
            }

            if (!_occupancyContext.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayOccupancySummary occupancy) ||
                !occupancy.IsOccupied)
            {
                issue =
                    "Prepared Actor replacement requires the current prepared Actor to own gameplay occupancy before physical replacement.";
                return false;
            }

            if (occupancy.PreparationToken != expectedPreparation)
            {
                issue =
                    "Current gameplay occupancy belongs to another prepared Actor occurrence.";
                return false;
            }

            PlayerGameplayOccupancyResult release =
                _occupancyContext.TryReleaseOccupancy(
                    playerSlotId,
                    occupancy.Token,
                    source,
                    reason);
            if (release == null || !release.Succeeded)
            {
                issue = release != null
                    ? release.ToDiagnosticString()
                    : "Gameplay occupancy release returned no result.";
                return false;
            }

            return true;
        }

        internal bool TryGetCurrentAdmission(
            PlayerSlotId playerSlotId,
            out PlayerGameplayAdmissionSummary admission)
        {
            admission = default;
            return IsReady &&
                _currentGameplayContext.TryGetCurrentAdmission(
                    playerSlotId,
                out admission);
        }

        internal bool TryGetCurrentInputBinding(
            PlayerSlotId playerSlotId,
            out PlayerGameplayInputBindingSummary binding,
            out PlayerGameplayInputBindingResult confirmation)
        {
            binding = default;
            confirmation = null;
            return _inputContext != null &&
                _inputContext.TryGetCurrentInputBinding(
                    playerSlotId,
                    out binding,
                    out confirmation);
        }

        internal bool TryGetRetainedInputBinding(
            PlayerSlotId playerSlotId,
            out PlayerGameplayInputBindingSummary binding)
        {
            binding = default;
            return _inputContext != null &&
                _inputContext.TryGetRetainedInputBinding(
                    playerSlotId,
                    out binding);
        }

        internal PlayerGameplayInputBindingResult ConfirmCurrentInputBinding(
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken expectedBinding,
            string source,
            string reason)
        {
            return _inputContext != null
                ? _inputContext.ConfirmCurrentInputBinding(
                    playerSlotId,
                    expectedBinding,
                    source,
                    reason)
                : null;
        }

        internal PlayerGameplayInputBindingResult RefreshInputAvailability(
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken expectedBinding,
            string source,
            string reason)
        {
            return _inputContext != null
                ? _inputContext.TryRefreshAvailability(
                    playerSlotId,
                    expectedBinding,
                    source,
                    reason)
                : null;
        }

        internal PlayerGameplayInputBindingResult ReleaseInputBinding(
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken expectedBinding,
            string source,
            string reason)
        {
            if (_admissionContext != null &&
                _admissionContext.CreateSnapshot().TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary admission) &&
                admission.IsAdmitted)
            {
                PlayerGameplayAdmissionResult admissionRelease =
                    _admissionContext.TryRelease(
                        playerSlotId,
                        admission.Token,
                        source,
                        $"{reason}; input-release");
                if (!admissionRelease.Succeeded)
                {
                    return null;
                }
            }

            return _inputContext != null
                ? _inputContext.TryRelease(
                    playerSlotId,
                    expectedBinding,
                    source,
                    reason)
                : null;
        }

        internal bool TryGetSnapshot(
            out PlayerGameplayRuntimeHostSnapshot snapshot)
        {
            snapshot = CreateSnapshot();
            return snapshot.IsInitialized;
        }

        private PlayerGameplayRuntimeHostSnapshot CreateSnapshot()
        {
            if (!IsReady)
            {
                return PlayerGameplayRuntimeHostSnapshot.Unavailable(
                    _diagnostic);
            }

            return new PlayerGameplayRuntimeHostSnapshot(
                true,
                _occupancyContext.SessionContextId,
                _occupancyContext.CreateSnapshot(),
                _inputContext.CreateSnapshot(),
                _cameraContext.CreateSnapshot(),
                _admissionContext.CreateSnapshot(),
                _lastOperationStatus,
                _diagnostic);
        }

        private PlayerGameplayAdmissionSummary GetAdmissionOrDefault(
            PlayerSlotId playerSlotId)
        {
            if (_admissionContext != null)
            {
                PlayerGameplayAdmissionSnapshot snapshot =
                    _admissionContext.CreateSnapshot();
                if (snapshot != null &&
                    snapshot.TryGetSummary(
                        playerSlotId,
                        out PlayerGameplayAdmissionSummary admission))
                {
                    return admission;
                }
            }

            return default;
        }

        private PlayerGameplayRuntimeOperationResult Result(
            PlayerGameplayRuntimeOperationStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary previous,
            PlayerGameplayAdmissionSummary current,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            string message)
        {
            _lastOperationStatus = status;
            _diagnostic = message ?? string.Empty;
            return new PlayerGameplayRuntimeOperationResult(
                status,
                operation,
                playerSlotId,
                previous,
                current,
                rollbackAttempted,
                rollbackSucceeded,
                rollbackMessage,
                CreateSnapshot(),
                message);
        }

        private void OnDestroy()
        {
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            if (_admissionContext != null &&
                _currentGameplayContext != null)
            {
                PlayerGameplayAdmissionSnapshot snapshot =
                    _admissionContext.CreateSnapshot();
                for (int index = snapshot.Slots.Count - 1;
                     index >= 0;
                     index--)
                {
                    PlayerGameplayAdmissionSummary admission =
                        snapshot.Slots[index];
                    if (!admission.IsAdmitted ||
                        !admission.Token.IsValid)
                    {
                        continue;
                    }

                    _currentGameplayContext.TryReleaseCurrentGameplay(
                        admission.PlayerSlotId,
                        admission.Token,
                        nameof(PlayerGameplayRuntimeHostModule),
                        "runtime-host-shutdown");
                }
            }

            _currentGameplayContext = null;
            if (_runtimeHost != null)
            {
                _runtimeHost.SetActivityPlayerLifecycleAdmissionRuntime(null);
            }
            _activityRelocationContext = null;
            _admissionContext = null;
            _cameraContext = null;
            _inputContext = null;
            _occupancyContext = null;
            _preparationModule = null;
            _participationContext = null;
            _runtimeHost = null;
            _diagnostic = "Player gameplay runtime was released.";
        }
    }

    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7F typed same-host access to official Player gameplay composition.")]
    internal static class FrameworkRuntimeHostPlayerGameplayExtensions
    {
        internal static bool TryGetPlayerGameplayRuntime(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerGameplayRuntimeHostModule module)
        {
            module = runtimeHost != null
                ? runtimeHost.GetComponent<PlayerGameplayRuntimeHostModule>()
                : null;
            return module != null && module.IsReady;
        }

        internal static bool TryGetPlayerGameplayRuntimeSnapshot(
            this FrameworkRuntimeHost runtimeHost,
            out PlayerGameplayRuntimeHostSnapshot snapshot)
        {
            if (runtimeHost == null)
            {
                snapshot =
                    PlayerGameplayRuntimeHostSnapshot.Unavailable(
                        "FrameworkRuntimeHost is missing.");
                return false;
            }

            PlayerGameplayRuntimeHostModule module =
                runtimeHost.GetComponent<PlayerGameplayRuntimeHostModule>();
            if (module == null)
            {
                snapshot =
                    PlayerGameplayRuntimeHostSnapshot.Unavailable(
                        "FrameworkRuntimeHost has no Player gameplay runtime module.");
                return false;
            }

            return module.TryGetSnapshot(out snapshot);
        }
    }
}
