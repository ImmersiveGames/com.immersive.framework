using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        private IActivityPlayerGameplayLifecycleRuntime
            gameplayLifecycleRuntime;

        internal void SetActivityPlayerGameplayLifecycleRuntime(
            IActivityPlayerGameplayLifecycleRuntime runtime)
        {
            gameplayLifecycleRuntime = runtime;
        }

        private ActivityContentExecutionResult
            ExecuteGameplayReadyAdoptionEnter(
                ActivityContentExecutionRequest request,
                ActivityAsset activity,
                RuntimeContentOwner owner,
                List<PlayerSlotRuntimeSnapshot> projectedSlots)
        {
            if (gameplayLifecycleRuntime == null)
            {
                string issue =
                    "GameplayReady Activity enter requires the official P3K.7G lifecycle adoption runtime.";
                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedRequirement,
                    activity,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots,
                    issue);
                return Blocking(
                    request,
                    "activity-player-actor-gameplay-ready-runtime-missing",
                    issue);
            }

            if (!gameplayLifecycleRuntime.TryAdoptCommittedTarget(
                    request,
                    projectedSlots,
                    out IReadOnlyList<
                        ActivityPlayerGameplayAdoptedSlot> adopted,
                    out string adoptionIssue))
            {
                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedRequirement,
                    activity,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots,
                    adoptionIssue);
                return Blocking(
                    request,
                    "activity-player-actor-gameplay-ready-adoption-failed",
                    adoptionIssue);
            }

            if (adopted == null ||
                adopted.Count != projectedSlots.Count)
            {
                string issue =
                    "GameplayReady adoption returned an invalid Slot count.";
                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedRequirement,
                    activity,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots,
                    issue);
                return Blocking(
                    request,
                    "activity-player-actor-gameplay-ready-adoption-count",
                    issue);
            }

            var prepared =
                new List<PreparedSlotRecord>(adopted.Count);
            var evidence =
                new ActivityPlayerActorSlotLifecycleSnapshot[
                    adopted.Count];

            for (int index = 0;
                 index < adopted.Count;
                 index++)
            {
                ActivityPlayerGameplayAdoptedSlot slot =
                    adopted[index];
                if (!slot.IsValid ||
                    slot.PlayerSlotId !=
                        projectedSlots[index].PlayerSlotId)
                {
                    string issue =
                        $"GameplayReady adoption Slot order or evidence is invalid at index '{index}'.";
                    lastSnapshot = FailureSnapshot(
                        ActivityPlayerActorLifecycleStatus.FailedRequirement,
                        activity,
                        owner,
                        PlayerParticipationRequirementLevel.GameplayReady,
                        projectedSlots,
                        issue);
                    return Blocking(
                        request,
                        "activity-player-actor-gameplay-ready-adoption-invalid",
                        issue);
                }

                prepared.Add(
                    new PreparedSlotRecord(
                        slot.PlayerSlotId,
                        slot.PreparationToken,
                        false));
                evidence[index] =
                    new ActivityPlayerActorSlotLifecycleSnapshot(
                        slot.PlayerSlotId,
                        true,
                        slot.ActorProfileId,
                        false,
                        slot.PreparationToken,
                        false,
                        false,
                        PlayerActorPreparationStatus
                            .SucceededAlreadyPrepared,
                        slot.Message);
            }

            activeRecord = new ActiveActivityRecord(
                activity,
                owner,
                PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots.Count,
                projectedSlots.Count,
                prepared);
            lastSnapshot =
                new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus
                        .SucceededEntered,
                    activity.ActivityName,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots.Count,
                    projectedSlots.Count,
                    prepared.Count,
                    0,
                    0,
                    evidence,
                    "Activity Player Actor lifecycle adopted the committed P3J/P3K GameplayReady handoff.");
            return ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-gameplay-ready-adopted",
                lastSnapshot.ToDiagnosticString());
        }

        private bool TryExecuteCommittedGameplayHandoffExit(
            ActivityContentExecutionRequest request,
            out ActivityContentExecutionResult result)
        {
            result = default;
            if (gameplayLifecycleRuntime == null)
            {
                return false;
            }

            if (!gameplayLifecycleRuntime
                    .TryHandleCommittedPreviousExit(
                        request,
                        out bool handled,
                        out string issue))
            {
                result = Blocking(
                    request,
                    "activity-player-actor-handoff-exit-failed",
                    issue);
                return true;
            }

            if (!handled)
            {
                return false;
            }

            var evidence =
                new ActivityPlayerActorSlotLifecycleSnapshot[
                    activeRecord.PreparedSlots.Count];
            for (int index = 0;
                 index < activeRecord.PreparedSlots.Count;
                 index++)
            {
                PreparedSlotRecord prepared =
                    activeRecord.PreparedSlots[index];
                evidence[index] =
                    new ActivityPlayerActorSlotLifecycleSnapshot(
                        prepared.PlayerSlotId,
                        true,
                        default,
                        false,
                        prepared.Token,
                        prepared.CreatedByEnter,
                        true,
                        PlayerActorPreparationStatus
                            .SucceededAlreadyReleased,
                        "Previous Actor and gameplay chain were released by the committed P3K.7E handoff.");
            }

            PlayerParticipationRequirementLevel requirementLevel =
                activeRecord.RequirementLevel;
            int projectedSlotCount =
                activeRecord.ProjectedSlotCount;
            int selectedCount =
                activeRecord.SelectedCount;
            int preparedCount =
                activeRecord.PreparedSlots.Count;
            activeRecord = null;

            lastSnapshot =
                new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus
                        .SucceededExited,
                    request.Activity.ActivityName,
                    request.Owner,
                    requirementLevel,
                    projectedSlotCount,
                    selectedCount,
                    preparedCount,
                    preparedCount,
                    0,
                    evidence,
                    "Previous Activity Player Actor lifecycle exit was superseded by the committed P3K.7E handoff.");
            result = ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-exit-superseded-by-handoff",
                lastSnapshot.ToDiagnosticString());
            return true;
        }

        private bool TryReleaseGameplayBeforePreparedActor(
            PreparedSlotRecord prepared,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (gameplayLifecycleRuntime == null)
            {
                if (activeRecord != null &&
                    activeRecord.RequirementLevel ==
                        PlayerParticipationRequirementLevel
                            .GameplayReady)
                {
                    issue =
                        "GameplayReady Actor exit cannot release P3J before the official gameplay lifecycle runtime is available.";
                    return false;
                }

                return true;
            }

            return gameplayLifecycleRuntime
                .TryReleaseGameplayBeforeActor(
                    prepared.PlayerSlotId,
                    prepared.Token,
                    source,
                    reason,
                    out _,
                    out issue);
        }
    }
}
