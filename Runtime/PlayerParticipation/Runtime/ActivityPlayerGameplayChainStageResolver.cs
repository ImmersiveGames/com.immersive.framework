using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Canonical staged resolver for the real P3J/P3K chain. It coordinates existing
    /// authorities and records only work created by the current stage for reverse rollback.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7B staged P3J/P3K Player gameplay chain resolver.")]
    internal sealed class ActivityPlayerGameplayChainStageResolver :
        IActivityPlayerAdmissionStageResolver
    {
        private sealed class SlotRecord
        {
            internal PlayerSlotId Slot;
            internal PlayerSlotRuntimeSnapshot Selection;
            internal bool SelectionCreated;
            internal PlayerActorPreparationSummary Preparation;
            internal bool PreparationCreated;
            internal PlayerGameplayOccupancySummary Occupancy;
            internal bool OccupancyCreated;
            internal PlayerGameplayInputBindingSummary Input;
            internal bool InputCreated;
            internal PlayerGameplayCameraEligibilitySummary Camera;
            internal bool CameraCreated;
            internal PlayerGameplayAdmissionSummary Admission;
            internal bool AdmissionCreated;
        }

        private sealed class ResolutionRecord
        {
            internal readonly List<SlotRecord> Slots = new List<SlotRecord>();
        }

        private readonly IActivityPlayerGameplayStageEndpointSource endpointSource;
        private readonly PlayerGameplayOccupancyRuntimeContext occupancyContext;
        private readonly PlayerGameplayInputBindingRuntimeContext inputContext;
        private readonly PlayerGameplayCameraEligibilityRuntimeContext cameraContext;
        private readonly PlayerGameplayAdmissionRuntimeContext admissionContext;

        internal ActivityPlayerGameplayChainStageResolver(
            IActivityPlayerGameplayStageEndpointSource endpointSource,
            PlayerGameplayOccupancyRuntimeContext occupancyContext,
            PlayerGameplayInputBindingRuntimeContext inputContext,
            PlayerGameplayCameraEligibilityRuntimeContext cameraContext,
            PlayerGameplayAdmissionRuntimeContext admissionContext)
        {
            this.endpointSource = endpointSource ??
                throw new ArgumentNullException(nameof(endpointSource));
            this.occupancyContext = occupancyContext ??
                throw new ArgumentNullException(nameof(occupancyContext));
            this.inputContext = inputContext ??
                throw new ArgumentNullException(nameof(inputContext));
            this.cameraContext = cameraContext ??
                throw new ArgumentNullException(nameof(cameraContext));
            this.admissionContext = admissionContext ??
                throw new ArgumentNullException(nameof(admissionContext));
        }

        public ActivityPlayerAdmissionStageResolution Resolve(
            ActivityAsset activity,
            ActivityPlayerAdmissionStageScope stagedScope,
            string source,
            string reason)
        {
            PlayerParticipationSnapshot participation =
                endpointSource.CreateParticipationSnapshot();
            PlayerActorPreparationSnapshot preparation =
                endpointSource.CreatePreparationSnapshot();
            PlayerGameplayAdmissionSnapshot admission =
                admissionContext.CreateSnapshot();
            var record = new ResolutionRecord();

            if (activity == null || stagedScope == null || !stagedScope.IsValid)
            {
                return ActivityPlayerAdmissionStageResolution.Failed(
                    participation,
                    preparation,
                    admission,
                    record,
                    "Staged Player gameplay resolution requires an Activity and valid staged scope.");
            }

            ActivityPlayerAdmissionEvaluationResult preflight =
                ActivityPlayerAdmissionEvaluator.Evaluate(
                    activity,
                    participation,
                    preparation,
                    admission);

            if (preflight == null)
            {
                return ActivityPlayerAdmissionStageResolution.Failed(
                    participation,
                    preparation,
                    admission,
                    record,
                    "Activity Player admission preflight returned no result.");
            }

            if (preflight.IsFailed || preflight.IsBlocked ||
                preflight.RequirementLevel == PlayerParticipationRequirementLevel.None)
            {
                return new ActivityPlayerAdmissionStageResolution(
                    true,
                    participation,
                    preparation,
                    admission,
                    record,
                    "No resolvable staged Player gameplay work was required.");
            }

            try
            {
                for (int index = 0; index < preflight.Slots.Count; index++)
                {
                ActivityPlayerAdmissionSlotResult projected = preflight.Slots[index];
                var slotRecord = new SlotRecord { Slot = projected.PlayerSlotId };
                record.Slots.Add(slotRecord);

                if (RequiresSelection(preflight.RequirementLevel))
                {
                    if (!endpointSource.TryEnsureSelectedActor(
                            activity,
                            projected.PlayerSlotId,
                            source,
                            reason,
                            out PlayerSlotRuntimeSnapshot currentSelection,
                            out bool selectionCreated,
                            out string selectionIssue))
                    {
                        return Failed(record, participation, selectionIssue);
                    }

                    slotRecord.Selection = currentSelection;
                    slotRecord.SelectionCreated = selectionCreated;
                }

                if (!RequiresPreparation(preflight.RequirementLevel))
                {
                    continue;
                }

                if (!endpointSource.TryEnsurePrepared(
                        activity,
                        stagedScope,
                        projected.PlayerSlotId,
                        source,
                        reason,
                        out PlayerActorPreparationSummary currentPreparation,
                        out bool preparationCreated,
                        out string issue))
                {
                    return Failed(record, participation, issue);
                }

                slotRecord.Preparation = currentPreparation;
                slotRecord.PreparationCreated = preparationCreated;

                if (preflight.RequirementLevel !=
                    PlayerParticipationRequirementLevel.GameplayReady)
                {
                    continue;
                }

                PlayerGameplayOccupancyResult occupancyResult =
                    occupancyContext.TryConfirmOccupancy(
                        currentPreparation,
                        source,
                        reason);
                if (!occupancyResult.Succeeded)
                {
                    return Failed(record, participation, occupancyResult.ToDiagnosticString());
                }

                slotRecord.Occupancy = occupancyResult.CurrentSummary;
                slotRecord.OccupancyCreated =
                    !occupancyResult.PreviousSummary.IsOccupied &&
                    occupancyResult.CurrentSummary.IsOccupied;

                if (!endpointSource.TryResolveGameplayEndpoints(
                        currentPreparation,
                        out LocalPlayerHostAuthoring host,
                        out PlayerActorDeclaration actorDeclaration,
                        out Immersive.Framework.UnityInput.UnityPlayerInputGateAdapter gateAdapter,
                        out PlayerGameplayCameraAuthoring cameraAuthoring,
                        out PlayerGameplayCameraRequiredness cameraRequiredness,
                        out CameraOutputSessionBinding outputSession,
                        out issue))
                {
                    return Failed(record, participation, issue);
                }

                PlayerGameplayInputBindingResult inputResult =
                    inputContext.TryBind(
                        currentPreparation,
                        slotRecord.Occupancy,
                        host,
                        actorDeclaration,
                        gateAdapter,
                        source,
                        reason);
                if (!inputResult.Succeeded)
                {
                    return Failed(record, participation, inputResult.ToDiagnosticString());
                }

                slotRecord.Input = inputResult.CurrentSummary;
                slotRecord.InputCreated =
                    !inputResult.PreviousSummary.IsBound &&
                    inputResult.CurrentSummary.IsBound;

                PlayerGameplayCameraEligibilityResult cameraResult;
                if (cameraAuthoring != null)
                {
                    cameraResult = cameraContext.TryConfirmEligibility(
                        currentPreparation,
                        slotRecord.Occupancy,
                        slotRecord.Input,
                        actorDeclaration,
                        cameraAuthoring,
                        source,
                        reason);
                }
                else if (cameraRequiredness ==
                         PlayerGameplayCameraRequiredness.Optional)
                {
                    cameraResult = cameraContext.TrySkipOptional(
                        currentPreparation,
                        slotRecord.Occupancy,
                        slotRecord.Input,
                        cameraRequiredness,
                        source,
                        reason);
                }
                else
                {
                    return Failed(
                        record,
                        participation,
                        "Required per-Player camera has no explicit camera authoring endpoint.");
                }

                if (!cameraResult.Succeeded)
                {
                    return Failed(record, participation, cameraResult.ToDiagnosticString());
                }

                slotRecord.Camera = cameraResult.CurrentSummary;
                slotRecord.CameraCreated =
                    !cameraResult.PreviousSummary.HasCurrentDecision &&
                    cameraResult.CurrentSummary.HasCurrentDecision;

                PlayerGameplayAdmissionResult admissionResult =
                    admissionContext.TryAdmit(
                        slotRecord.Occupancy,
                        slotRecord.Input,
                        slotRecord.Camera,
                        outputSession,
                        source,
                        reason);
                if (!admissionResult.Succeeded)
                {
                    if (admissionResult.RollbackAttempted &&
                        admissionResult.RollbackSucceeded)
                    {
                        slotRecord.CameraCreated = false;
                        slotRecord.InputCreated = false;
                        slotRecord.OccupancyCreated = false;
                    }
                    else if (admissionResult.CurrentSummary.IsAdmitted)
                    {
                        slotRecord.Admission = admissionResult.CurrentSummary;
                        slotRecord.AdmissionCreated = true;
                    }

                    return Failed(record, participation, admissionResult.ToDiagnosticString());
                }

                slotRecord.Admission = admissionResult.CurrentSummary;
                slotRecord.AdmissionCreated =
                    !admissionResult.PreviousSummary.IsAdmitted &&
                    admissionResult.CurrentSummary.IsAdmitted;
            }

                participation = endpointSource.CreateParticipationSnapshot();
                preparation = endpointSource.CreatePreparationSnapshot();
                admission = admissionContext.CreateSnapshot();
            }
            catch (Exception exception)
            {
                return Failed(
                    record,
                    participation,
                    $"Staged Player gameplay resolver threw '{exception.GetType().Name}'. {exception.Message}");
            }

            return new ActivityPlayerAdmissionStageResolution(
                true,
                participation,
                preparation,
                admission,
                record,
                "Staged Player gameplay chain resolved against current P3J/P3K authorities.");
        }

        public bool TryRollback(
            ActivityPlayerAdmissionStageResolution resolution,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (resolution?.ResolverState is not ResolutionRecord record)
            {
                return true;
            }

            var failures = new List<string>();
            for (int index = record.Slots.Count - 1; index >= 0; index--)
            {
                SlotRecord slot = record.Slots[index];
                bool lowerGameplayChainReleased = false;

                if (slot.AdmissionCreated && slot.Admission.Token.IsValid)
                {
                    PlayerGameplayAdmissionResult released =
                        admissionContext.TryRelease(
                            slot.Slot,
                            slot.Admission.Token,
                            source,
                            reason);
                    if (!released.Succeeded)
                    {
                        failures.Add(released.ToDiagnosticString());
                        continue;
                    }

                    slot.AdmissionCreated = false;
                    slot.CameraCreated = false;
                    slot.InputCreated = false;
                    slot.OccupancyCreated = false;
                    lowerGameplayChainReleased = true;
                }

                if (!lowerGameplayChainReleased)
                {
                    if (slot.CameraCreated && slot.Camera.Token.IsValid)
                    {
                        PlayerGameplayCameraEligibilityResult released =
                            cameraContext.TryRelease(
                                slot.Slot,
                                slot.Camera.Token,
                                source,
                                reason);
                        if (!released.Succeeded)
                        {
                            failures.Add(released.ToDiagnosticString());
                            continue;
                        }

                        slot.CameraCreated = false;
                    }

                    if (slot.InputCreated && slot.Input.Token.IsValid)
                    {
                        PlayerGameplayInputBindingResult released =
                            inputContext.TryRelease(
                                slot.Slot,
                                slot.Input.Token,
                                source,
                                reason);
                        if (!released.Succeeded)
                        {
                            failures.Add(released.ToDiagnosticString());
                            continue;
                        }

                        slot.InputCreated = false;
                    }

                    if (slot.OccupancyCreated && slot.Occupancy.Token.IsValid)
                    {
                        PlayerGameplayOccupancyResult released =
                            occupancyContext.TryReleaseOccupancy(
                                slot.Slot,
                                slot.Occupancy.Token,
                                source,
                                reason);
                        if (!released.Succeeded)
                        {
                            failures.Add(released.ToDiagnosticString());
                            continue;
                        }

                        slot.OccupancyCreated = false;
                    }
                }

                if (slot.PreparationCreated && slot.Preparation.Token.IsValid)
                {
                    if (!endpointSource.TryReleasePreparation(
                            slot.Preparation,
                            source,
                            reason,
                            out string preparationIssue))
                    {
                        failures.Add(preparationIssue);
                        continue;
                    }

                    slot.PreparationCreated = false;
                }

                if (slot.SelectionCreated && slot.Selection.IsValid)
                {
                    if (!endpointSource.TryReleaseSelectedActor(
                            slot.Selection,
                            source,
                            reason,
                            out string selectionIssue))
                    {
                        failures.Add(selectionIssue);
                        continue;
                    }

                    slot.SelectionCreated = false;
                }
            }

            issue = failures.Count == 0
                ? string.Empty
                : string.Join(" | ", failures);
            return failures.Count == 0;
        }

        private ActivityPlayerAdmissionStageResolution Failed(
            ResolutionRecord record,
            PlayerParticipationSnapshot participation,
            string message)
        {
            return ActivityPlayerAdmissionStageResolution.Failed(
                participation,
                endpointSource.CreatePreparationSnapshot(),
                admissionContext.CreateSnapshot(),
                record,
                message);
        }

        private static bool RequiresSelection(
            PlayerParticipationRequirementLevel level)
        {
            return level is
                PlayerParticipationRequirementLevel.SelectedActors or
                PlayerParticipationRequirementLevel.LogicalActorsPrepared or
                PlayerParticipationRequirementLevel.GameplayReady;
        }

        private static bool RequiresPreparation(
            PlayerParticipationRequirementLevel level)
        {
            return level is
                PlayerParticipationRequirementLevel.LogicalActorsPrepared or
                PlayerParticipationRequirementLevel.GameplayReady;
        }
    }
}
