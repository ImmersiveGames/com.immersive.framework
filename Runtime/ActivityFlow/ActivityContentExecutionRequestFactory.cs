using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive factory that builds Activity Content Execution phase plans from an ordered participant collection.
    /// It does not discover participants, execute requests, integrate lifecycle or mutate Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10F Activity Content Execution request factory; no execution runtime or Unity side effects.")]
    public static class ActivityContentExecutionRequestFactory
    {
        public static ActivityContentExecutionPhasePlan CreateEnterPlan(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            RuntimeScopeContext context,
            ActivityContentExecutionParticipantCollection participants,
            string source,
            string reason)
        {
            return CreatePlan(
                ActivityContentExecutionPhase.Enter,
                activity,
                previousActivity,
                activity,
                context,
                participants,
                source,
                reason);
        }

        public static ActivityContentExecutionPhasePlan CreateExitPlan(
            ActivityAsset activity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            ActivityContentExecutionParticipantCollection participants,
            string source,
            string reason)
        {
            return CreatePlan(
                ActivityContentExecutionPhase.Exit,
                activity,
                activity,
                nextActivity,
                context,
                participants,
                source,
                reason);
        }

        public static ActivityContentExecutionPhasePlan CreatePlan(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            ActivityContentExecutionParticipantCollection participants,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                return ActivityContentExecutionPhasePlan.RejectedResult(
                    ActivityContentExecutionPhase.Enter,
                    activity,
                    previousActivity,
                    nextActivity,
                    context,
                    ActivityContentExecutionPhasePlanStatus.RejectedInvalidPhase,
                    participants.SnapshotIssues(),
                    source,
                    reason,
                    "Activity content execution phase plan rejected because phase is invalid.");
            }

            if (activity == null)
            {
                return ActivityContentExecutionPhasePlan.RejectedResult(
                    phase,
                    activity,
                    previousActivity,
                    nextActivity,
                    context,
                    ActivityContentExecutionPhasePlanStatus.RejectedMissingActivity,
                    participants.SnapshotIssues(),
                    source,
                    reason,
                    "Activity content execution phase plan rejected because Activity is missing.");
            }

            if (!context.IsValid || context.Scope != RuntimeContentScope.Activity)
            {
                return ActivityContentExecutionPhasePlan.RejectedResult(
                    phase,
                    activity,
                    previousActivity,
                    nextActivity,
                    context,
                    ActivityContentExecutionPhasePlanStatus.RejectedInvalidContext,
                    participants.SnapshotIssues(),
                    source,
                    reason,
                    "Activity content execution phase plan rejected because runtime scope context is not valid Activity scope.");
            }

            ActivityContentExecutionParticipantCollectionIssue[] collectionIssues = participants.SnapshotIssues();
            if (participants is { HasIssues: true, HasParticipants: false })
            {
                return ActivityContentExecutionPhasePlan.RejectedResult(
                    phase,
                    activity,
                    previousActivity,
                    nextActivity,
                    context,
                    ActivityContentExecutionPhasePlanStatus.RejectedInvalidParticipants,
                    collectionIssues,
                    source,
                    reason,
                    "Activity content execution phase plan rejected because participant collection has issues and no valid participants.");
            }

            ActivityContentExecutionParticipantEntry[] entries = participants.SnapshotEntriesForPhase(phase);
            if (entries.Length == 0)
            {
                return ActivityContentExecutionPhasePlan.SkippedNoParticipants(
                    phase,
                    activity,
                    previousActivity,
                    nextActivity,
                    context,
                    collectionIssues,
                    source,
                    reason,
                    "Activity content execution phase plan skipped because no participants support the requested phase.");
            }

            var requests = new List<ActivityContentExecutionRequest>(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                var descriptor = entries[i].Descriptor;
                var request = phase == ActivityContentExecutionPhase.Enter
                    ? ActivityContentExecutionRequest.Enter(
                        activity,
                        previousActivity,
                        context,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        source,
                        reason)
                    : ActivityContentExecutionRequest.Exit(
                        activity,
                        nextActivity,
                        context,
                        descriptor.ContentId,
                        descriptor.Requiredness,
                        source,
                        reason);

                requests.Add(request);
            }

            return ActivityContentExecutionPhasePlan.PlannedResult(
                phase,
                activity,
                previousActivity,
                nextActivity,
                context,
                entries,
                requests,
                collectionIssues,
                source,
                reason,
                "Activity content execution phase plan created from participant collection.");
        }
    }
}
