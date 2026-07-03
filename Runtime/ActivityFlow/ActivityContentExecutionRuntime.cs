using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Executes an already-built Activity Content Execution phase plan and aggregates participant results.
    /// It does not discover participants, integrate ActivityFlow lifecycle, mutate Unity objects, materialize content, reset objects or call gameplay systems directly.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10G Activity Content Execution runtime executor; executes supplied participants only and has no Unity side effects.")]
    public sealed class ActivityContentExecutionRuntime
    {
        public ActivityContentExecutionAggregateResult ExecutePhasePlan(
            ActivityContentExecutionPhasePlan plan,
            string source,
            string reason)
        {
            string sourceText = source.NormalizeText();
            string reasonText = reason.NormalizeText();
            var phase = ResolvePhase(plan.Phase);

            if (plan.Phase == ActivityContentExecutionPhase.Unknown)
            {
                return ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                    phase,
                    plan.Activity,
                    plan.PreviousActivity,
                    plan.NextActivity,
                    sourceText,
                    reasonText,
                    "Activity content execution runtime rejected the phase plan because the plan phase is invalid.");
            }

            if (plan.Rejected)
            {
                return ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                    phase,
                    plan.Activity,
                    plan.PreviousActivity,
                    plan.NextActivity,
                    sourceText,
                    reasonText,
                    "Activity content execution runtime rejected the phase plan because the plan was already rejected.");
            }

            if (plan.Skipped || !plan.HasRequests)
            {
                return ActivityContentExecutionAggregateResult.SkippedNoContent(
                    phase,
                    plan.Activity,
                    plan.PreviousActivity,
                    plan.NextActivity,
                    sourceText,
                    reasonText,
                    "Activity content participant execution runtime skipped the phase plan because it has no participant execution requests.");
            }

            if (!plan.Planned)
            {
                return ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                    phase,
                    plan.Activity,
                    plan.PreviousActivity,
                    plan.NextActivity,
                    sourceText,
                    reasonText,
                    "Activity content execution runtime rejected the phase plan because it is not planned.");
            }

            var results = new List<ActivityContentExecutionResult>(plan.RequestCount);
            IReadOnlyList<ActivityContentExecutionRequest> requests = plan.Requests;
            foreach (var request in requests)
            {
                if (!request.IsValid)
                {
                    return ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                        phase,
                        plan.Activity,
                        plan.PreviousActivity,
                        plan.NextActivity,
                        sourceText,
                        reasonText,
                        "Activity content execution runtime rejected the phase plan because one request is invalid.");
                }

                if (!TryGetEntry(plan, request, out var entry))
                {
                    results.Add(CreateFailure(
                        request,
                        sourceText,
                        reasonText,
                        "Activity content execution runtime could not find a participant entry for the request."));
                    continue;
                }

                if (entry.Participant == null)
                {
                    results.Add(CreateFailure(
                        request,
                        sourceText,
                        reasonText,
                        "Activity content execution runtime found a null participant for the request."));
                    continue;
                }

                if (!entry.SupportsPhase(request.Phase))
                {
                    results.Add(ActivityContentExecutionResult.SkippedResult(
                        request,
                        ActivityContentExecutionStatus.SkippedNotApplicable,
                        sourceText,
                        reasonText,
                        "Activity content execution participant does not support the requested phase."));
                    continue;
                }

                try
                {
                    var result = entry.Participant.ExecuteActivityContent(request);
                    if (!IsValidResultForRequest(result, request, out string invalidResultMessage))
                    {
                        results.Add(CreateFailure(
                            request,
                            sourceText,
                            reasonText,
                            invalidResultMessage));
                        continue;
                    }

                    results.Add(result);
                }
                catch (Exception exception)
                {
                    results.Add(CreateFailure(
                        request,
                        sourceText,
                        reasonText,
                        $"Activity content execution participant threw an exception: {exception.GetType().Name}."));
                }
            }

            return ActivityContentExecutionAggregateResult.FromResults(
                phase,
                plan.Activity,
                plan.PreviousActivity,
                plan.NextActivity,
                results,
                sourceText,
                reasonText,
                "Activity content participant execution runtime executed the supplied phase plan.");
        }

        private static bool TryGetEntry(
            ActivityContentExecutionPhasePlan plan,
            ActivityContentExecutionRequest request,
            out ActivityContentExecutionParticipantEntry entry)
        {
            IReadOnlyList<ActivityContentExecutionParticipantEntry> entries = plan.Entries;
            foreach (var candidate in entries)
            {
                if (candidate.ContentId.Equals(request.ContentId))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private static ActivityContentExecutionResult CreateFailure(
            ActivityContentExecutionRequest request,
            string source,
            string reason,
            string message)
        {
            return request.IsRequired
                ? ActivityContentExecutionResult.BlockingFailure(request, 1, source, reason, message)
                : ActivityContentExecutionResult.NonBlockingFailure(request, 1, source, reason, message);
        }

        private static bool IsValidResultForRequest(
            ActivityContentExecutionResult result,
            ActivityContentExecutionRequest request,
            out string message)
        {
            if (!result.Request.IsValid)
            {
                message = "Activity content execution participant returned an invalid result request.";
                return false;
            }

            if (result.Status == ActivityContentExecutionStatus.Unknown)
            {
                message = "Activity content execution participant returned an unknown result status.";
                return false;
            }

            if (result.Phase != request.Phase)
            {
                message = "Activity content execution participant returned a result for a different phase.";
                return false;
            }

            if (!ReferenceEquals(result.Request.Activity, request.Activity))
            {
                message = "Activity content execution participant returned a result for a different Activity.";
                return false;
            }

            if (!ReferenceEquals(result.Request.PreviousActivity, request.PreviousActivity))
            {
                message = "Activity content execution participant returned a result for a different previous Activity.";
                return false;
            }

            if (!ReferenceEquals(result.Request.NextActivity, request.NextActivity))
            {
                message = "Activity content execution participant returned a result for a different next Activity.";
                return false;
            }

            if (!result.Request.ContentId.Equals(request.ContentId))
            {
                message = "Activity content execution participant returned a result for a different content id.";
                return false;
            }

            if (!result.Request.Owner.Equals(request.Owner))
            {
                message = "Activity content execution participant returned a result for a different runtime owner.";
                return false;
            }

            if (result.Requiredness != request.Requiredness)
            {
                message = "Activity content execution participant returned a result with different requiredness.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static ActivityContentExecutionPhase ResolvePhase(ActivityContentExecutionPhase phase)
        {
            return phase == ActivityContentExecutionPhase.Unknown
                ? ActivityContentExecutionPhase.Enter
                : phase;
        }
    }
}
