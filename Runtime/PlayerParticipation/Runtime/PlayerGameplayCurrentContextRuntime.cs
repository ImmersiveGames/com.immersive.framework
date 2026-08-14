using System;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped authority for the current Activity gameplay context. It projects
    /// the already prepared Session physical Player into Activity-owned input, camera
    /// and gameplay admission capabilities; it never stages or replaces physical state.
    /// </summary>
    internal sealed class PlayerGameplayCurrentContextRuntime
    {
        private sealed class ChainEvidence
        {
            internal PlayerGameplayOccupancySummary Occupancy;
            internal PlayerGameplayInputBindingSummary Input;
            internal PlayerGameplayCameraEligibilitySummary Camera;
            internal PlayerGameplayAdmissionSummary Admission;
            internal bool OccupancyCreated;
            internal bool InputCreated;
            internal bool CameraCreated;
            internal bool AdmissionCreated;
            internal bool NestedRollbackAttempted;
            internal bool NestedRollbackSucceeded;
            internal string NestedRollbackMessage;
        }

        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private readonly IPlayerGameplayCurrentContextEndpointSource endpointSource;
        private readonly PlayerGameplayOccupancyRuntimeContext occupancyContext;
        private readonly PlayerGameplayInputBindingRuntimeContext inputContext;
        private readonly PlayerGameplayCameraEligibilityRuntimeContext cameraContext;
        private readonly PlayerGameplayAdmissionRuntimeContext admissionContext;
        private readonly string sessionContextId;

        private PlayerGameplayCurrentContextRuntime(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            IPlayerGameplayCurrentContextEndpointSource endpointSource,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            string sessionContextId)
        {
            this.preparationModule = preparationModule;
            this.endpointSource = endpointSource;
            this.occupancyContext = occupancyContext;
            this.inputContext = inputContext;
            this.cameraContext = cameraContext;
            this.admissionContext = admissionContext;
            this.sessionContextId = sessionContextId;
        }

        internal static bool TryCreate(
            PlayerActorPreparationRuntimeHostModule preparationModule,
            IPlayerGameplayCurrentContextEndpointSource endpointSource,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerGameplayAdmissionRuntimeContext admissionContext,
            out PlayerGameplayCurrentContextRuntime context,
            out string issue)
        {
            context = null;
            issue = string.Empty;
            if (preparationModule == null || !preparationModule.IsReady ||
                endpointSource == null || occupancyContext == null ||
                inputContext == null || cameraContext == null ||
                admissionContext == null ||
                !preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot preparationHost) ||
                preparationHost == null || !preparationHost.IsInitialized ||
                preparationHost.Preparation == null ||
                !preparationHost.Preparation.IsInitialized)
            {
                issue = "Current gameplay context requires ready preparation and gameplay authorities.";
                return false;
            }

            string session = preparationHost.SessionContextId;
            if (string.IsNullOrEmpty(session) ||
                !string.Equals(session, occupancyContext.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, inputContext.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, cameraContext.SessionContextId, StringComparison.Ordinal) ||
                !string.Equals(session, admissionContext.SessionContextId, StringComparison.Ordinal))
            {
                issue = "Current gameplay authorities belong to different or uninitialized Session identities.";
                return false;
            }

            context = new PlayerGameplayCurrentContextRuntime(
                preparationModule, endpointSource, occupancyContext, inputContext,
                cameraContext, admissionContext, session);
            return true;
        }

        internal bool TryEnsureCurrentGameplay(
            PlayerActorPreparationSummary preparation,
            RuntimeContentOwner contextualOwner,
            string source,
            string reason,
            out PlayerGameplayAdmissionSummary admission,
            out bool rollbackAttempted,
            out bool rollbackSucceeded,
            out string rollbackMessage,
            out string issue)
        {
            admission = default;
            rollbackAttempted = false;
            rollbackSucceeded = false;
            rollbackMessage = string.Empty;
            issue = string.Empty;
            if (!contextualOwner.IsValid ||
                contextualOwner.Scope != RuntimeContentScope.Activity ||
                !preparation.IsValid || !preparation.IsPrepared ||
                !preparation.PlayerSlotId.IsValid ||
                !string.Equals(preparation.SessionContextId, sessionContextId, StringComparison.Ordinal))
            {
                issue = "Current gameplay context requires current Activity ownership and exact prepared Session physical evidence.";
                return false;
            }

            PlayerGameplayAdmissionSnapshot snapshot = admissionContext.CreateSnapshot();
            if (snapshot != null && snapshot.TryGetSummary(
                    preparation.PlayerSlotId, out PlayerGameplayAdmissionSummary previous) &&
                previous.IsAdmitted && previous.Owner != contextualOwner)
            {
                PlayerGameplayAdmissionResult released = TryReleaseCurrentGameplay(
                    preparation.PlayerSlotId, previous.Token, source,
                    "reproject-gameplay-context");
                if (!released.Succeeded)
                {
                    issue = released.ToDiagnosticString();
                    return false;
                }
            }

            var chain = new ChainEvidence();
            if (TryBuildChain(preparation, contextualOwner, chain, source, reason, out issue))
            {
                admission = chain.Admission;
                rollbackSucceeded = true;
                return true;
            }

            string buildIssue = issue;
            rollbackAttempted = chain.NestedRollbackAttempted || chain.AdmissionCreated ||
                chain.CameraCreated || chain.InputCreated || chain.OccupancyCreated;
            if (rollbackAttempted)
            {
                bool contextualRollback = TryReleaseContextualChain(
                    chain, source, "ensure-current-gameplay-rollback", out string rollbackIssue);
                rollbackSucceeded = (!chain.NestedRollbackAttempted ||
                    chain.NestedRollbackSucceeded) && contextualRollback;
                rollbackMessage = Join(chain.NestedRollbackMessage, rollbackIssue);
            }

            issue = Join(buildIssue, rollbackMessage);
            return false;
        }

        internal PlayerGameplayAdmissionResult TryReleaseCurrentGameplay(
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken expectedAdmission,
            string source,
            string reason)
        {
            PlayerGameplayAdmissionSnapshot snapshot = admissionContext.CreateSnapshot();
            if (snapshot == null || !snapshot.TryGetSummary(playerSlotId,
                    out PlayerGameplayAdmissionSummary current) ||
                current.Token != expectedAdmission)
            {
                return admissionContext.TryRelease(playerSlotId, expectedAdmission, source, reason);
            }

            PlayerGameplayAdmissionResult released = admissionContext.TryRelease(
                playerSlotId, expectedAdmission, source, reason);
            if (!released.Succeeded)
                return released;

            PlayerGameplayCameraEligibilityResult camera = cameraContext.TryRelease(
                playerSlotId, current.CameraEligibilityToken, source, reason);
            if (!camera.Succeeded)
                return released;

            inputContext.TryRelease(playerSlotId, current.InputBindingToken, source, reason);
            return released;
        }

        internal bool TryGetCurrentAdmission(
            PlayerSlotId playerSlotId,
            out PlayerGameplayAdmissionSummary admission)
        {
            admission = default;
            PlayerGameplayAdmissionSnapshot snapshot = admissionContext.CreateSnapshot();
            return snapshot != null && snapshot.IsInitialized &&
                snapshot.TryGetSummary(playerSlotId, out admission);
        }

        private bool TryBuildChain(
            PlayerActorPreparationSummary preparation,
            RuntimeContentOwner owner,
            ChainEvidence chain,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            PlayerGameplayOccupancyResult occupancy = occupancyContext.TryConfirmOccupancy(
                preparation, source, reason);
            if (!occupancy.Succeeded)
            {
                issue = occupancy.ToDiagnosticString();
                return false;
            }
            chain.Occupancy = occupancy.CurrentSummary;
            chain.OccupancyCreated = !occupancy.PreviousSummary.IsOccupied &&
                occupancy.CurrentSummary.IsOccupied;

            if (!endpointSource.TryResolveGameplayEndpoints(preparation, out LocalPlayerHostAuthoring host,
                    out PlayerActorDeclaration actor, out UnityPlayerInputGateAdapter gate,
                    out PlayerGameplayCameraAuthoring cameraAuthoring,
                    out PlayerGameplayCameraRequiredness cameraRequiredness,
                    out CameraOutputSessionBinding outputSession, out issue))
                return false;

            PlayerGameplayInputBindingResult input = inputContext.TryBind(
                preparation, chain.Occupancy, owner, host, actor, gate, source, reason);
            if (!input.Succeeded)
            {
                chain.NestedRollbackAttempted = input.RollbackAttempted;
                chain.NestedRollbackSucceeded = input.RollbackSucceeded;
                chain.NestedRollbackMessage = input.RollbackMessage;
                issue = input.ToDiagnosticString();
                return false;
            }
            chain.Input = input.CurrentSummary;
            chain.InputCreated = !input.PreviousSummary.IsBound && input.CurrentSummary.IsBound;

            PlayerGameplayCameraEligibilityResult camera = cameraAuthoring != null
                ? cameraContext.TryConfirmEligibility(preparation, chain.Occupancy, chain.Input,
                    outputSession, actor, cameraAuthoring, source, reason)
                : cameraRequiredness == PlayerGameplayCameraRequiredness.Optional
                    ? cameraContext.TrySkipOptional(preparation, chain.Occupancy, chain.Input,
                        outputSession, cameraRequiredness, source, reason)
                    : null;
            if (camera == null || !camera.Succeeded)
            {
                issue = camera == null
                    ? "Required Player camera has no explicit authoring endpoint during current gameplay projection."
                    : camera.ToDiagnosticString();
                return false;
            }
            chain.Camera = camera.CurrentSummary;
            chain.CameraCreated = !camera.PreviousSummary.HasCurrentDecision &&
                camera.CurrentSummary.HasCurrentDecision;

            PlayerGameplayAdmissionResult admission = admissionContext.TryAdmit(
                owner, chain.Occupancy, chain.Input, chain.Camera, source, reason);
            if (!admission.Succeeded)
            {
                chain.NestedRollbackAttempted = admission.RollbackAttempted;
                chain.NestedRollbackSucceeded = admission.RollbackSucceeded;
                chain.NestedRollbackMessage = admission.RollbackIssue;
                if (admission.CurrentSummary.IsAdmitted)
                {
                    chain.Admission = admission.CurrentSummary;
                    chain.AdmissionCreated = true;
                }
                issue = admission.ToDiagnosticString();
                return false;
            }
            chain.Admission = admission.CurrentSummary;
            chain.AdmissionCreated = !admission.PreviousSummary.IsAdmitted &&
                admission.CurrentSummary.IsAdmitted;
            return true;
        }

        private bool TryReleaseContextualChain(
            ChainEvidence chain, string source, string reason, out string issue)
        {
            issue = string.Empty;
            if (chain == null)
                return true;
            if (chain.AdmissionCreated && chain.Admission.Token.IsValid &&
                !admissionContext.TryRelease(chain.Admission.PlayerSlotId, chain.Admission.Token,
                    source, reason).Succeeded)
            {
                issue = "Could not release contextual gameplay admission after projection failure.";
                return false;
            }
            if (chain.CameraCreated && chain.Camera.Token.IsValid &&
                !cameraContext.TryRelease(chain.Camera.PlayerSlotId, chain.Camera.Token,
                    source, reason).Succeeded)
            {
                issue = "Could not release contextual camera evidence after projection failure.";
                return false;
            }
            if (chain.InputCreated && chain.Input.Token.IsValid &&
                !inputContext.TryRelease(chain.Input.PlayerSlotId, chain.Input.Token,
                    source, reason).Succeeded)
            {
                issue = "Could not release contextual input binding after projection failure.";
                return false;
            }
            // Occupancy is Session physical state and intentionally survives contextual rollback.
            return true;
        }

        private static string Join(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left)) return right ?? string.Empty;
            if (string.IsNullOrWhiteSpace(right)) return left;
            return left + " | " + right;
        }
    }
}
