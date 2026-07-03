using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive aggregate result for a logical Activity Content Execution phase.
    /// It combines per-content execution results for readiness and diagnostics only; it does not discover participants, execute adapters, mutate GameObjects, reset objects or release physical resources.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Activity Content Execution aggregate result contract; no execution runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionAggregateResult : IEquatable<ActivityContentExecutionAggregateResult>
    {
        private readonly ActivityContentExecutionResult[] _results;

        private ActivityContentExecutionAggregateResult(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            IReadOnlyList<ActivityContentExecutionResult> results,
            ActivityContentExecutionAggregateStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Activity content execution aggregate phase must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ActivityContentExecutionAggregateStatus), status)
                || status == ActivityContentExecutionAggregateStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution aggregate status must be explicit.");
            }

            Phase = phase;
            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Status = status;
            
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();

            if (results == null || results.Count == 0)
            {
                _results = Array.Empty<ActivityContentExecutionResult>();
            }
            else
            {
                _results = new ActivityContentExecutionResult[results.Count];
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    if (result.Phase != phase)
                    {
                        throw new ArgumentException("All Activity content execution results in an aggregate must use the aggregate phase.", nameof(results));
                    }

                    _results[i] = result;
                }
            }
        }

        private ActivityContentExecutionPhase Phase { get; }

        private ActivityAsset Activity { get; }

        private ActivityAsset PreviousActivity { get; } 

        private ActivityAsset NextActivity { get; }

        public ActivityContentExecutionAggregateStatus Status { get; }

        private IReadOnlyList<ActivityContentExecutionResult> Results => _results ?? Array.Empty<ActivityContentExecutionResult>();

        private string Source { get; }

        private string Reason { get; }

        private string Message { get; }

        private string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        private string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        private string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public int ResultCount => _results?.Length ?? 0;

        public int RequiredCount => CountByRequiredness(ActivityContentExecutionRequiredness.Required);

        public int OptionalCount => CountByRequiredness(ActivityContentExecutionRequiredness.Optional);

        private int SucceededCount => CountSucceeded();

        private int SkippedCount => CountSkipped();

        private int FailedCount => CountFailed();

        public int BlockingIssueCount => SumBlockingIssues();

        public int NonBlockingIssueCount => SumNonBlockingIssues();

        private bool HasBlockingIssues => BlockingIssueCount > 0
            || Status == ActivityContentExecutionAggregateStatus.FailedBlocking;

        public bool HasNonBlockingIssues => NonBlockingIssueCount > 0
            || Status == ActivityContentExecutionAggregateStatus.FailedNonBlocking
            || Status == ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues;

        public bool BlocksReadiness => HasBlockingIssues;

        public bool Succeeded => Status is ActivityContentExecutionAggregateStatus.Succeeded or ActivityContentExecutionAggregateStatus.SucceededNoOp or ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues;

        public bool Skipped => Status == ActivityContentExecutionAggregateStatus.SkippedNoContent;

        public bool Failed => Status is ActivityContentExecutionAggregateStatus.FailedNonBlocking or ActivityContentExecutionAggregateStatus.FailedBlocking or ActivityContentExecutionAggregateStatus.RejectedInvalidResults;

        public bool Equals(ActivityContentExecutionAggregateResult other)
        {
            if (Phase != other.Phase
                || !ReferenceEquals(Activity, other.Activity)
                || !ReferenceEquals(PreviousActivity, other.PreviousActivity)
                || !ReferenceEquals(NextActivity, other.NextActivity)
                || Status != other.Status
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || ResultCount != other.ResultCount)
            {
                return false;
            }

            for (int i = 0; i < ResultCount; i++)
            {
                if (!Results[i].Equals(other.Results[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionAggregateResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Phase;
                hashCode = hashCode * 397 ^ (Activity != null ? Activity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                for (int i = 0; i < ResultCount; i++)
                {
                    hashCode = hashCode * 397 ^ Results[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string activityText = ActivityName.ToDiagnosticText();
            string previousText = PreviousActivityName.ToDiagnosticText();
            string nextText = NextActivityName.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            var builder = new StringBuilder();
            builder.Append($"Activity Content Participant Execution Aggregate phase='{Phase}' activity='{activityText}' previousActivity='{previousText}' nextActivity='{nextText}' status='{Status}' results='{ResultCount}' required='{RequiredCount}' optional='{OptionalCount}' succeeded='{SucceededCount}' skipped='{SkippedCount}' failed='{FailedCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' blocksReadiness='{BlocksReadiness}' source='{sourceText}' reason='{reasonText}' message='{messageText}' details=[");
            for (int i = 0; i < Results.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Results[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static ActivityContentExecutionAggregateResult FromResults(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            IReadOnlyList<ActivityContentExecutionResult> results,
            string source,
            string reason,
            string message)
        {
            var status = DetermineStatus(results);
            return new ActivityContentExecutionAggregateResult(
                phase,
                activity,
                previousActivity,
                nextActivity,
                results,
                status,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? BuildDefaultMessage(status) : message);
        }

        public static ActivityContentExecutionAggregateResult SkippedNoContent(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionAggregateResult(
                phase,
                activity,
                previousActivity,
                nextActivity,
                Array.Empty<ActivityContentExecutionResult>(),
                ActivityContentExecutionAggregateStatus.SkippedNoContent,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content participant execution aggregate skipped because no participant results were provided." : message);
        }

        public static ActivityContentExecutionAggregateResult RejectedInvalidResults(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionAggregateResult(
                phase,
                activity,
                previousActivity,
                nextActivity,
                Array.Empty<ActivityContentExecutionResult>(),
                ActivityContentExecutionAggregateStatus.RejectedInvalidResults,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution aggregate rejected invalid results." : message);
        }

        private static ActivityContentExecutionAggregateStatus DetermineStatus(IReadOnlyList<ActivityContentExecutionResult> candidateResults)
        {
            if (candidateResults == null || candidateResults.Count == 0)
            {
                return ActivityContentExecutionAggregateStatus.SkippedNoContent;
            }

            int succeeded = 0;
            int skipped = 0;
            int failed = 0;
            int blockingIssues = 0;
            int nonBlockingIssues = 0;

            foreach (var result in candidateResults)
            {
                if (result.HasBlockingIssues)
                {
                    blockingIssues += result.BlockingIssueCount > 0 ? result.BlockingIssueCount : 1;
                }

                if (result.HasNonBlockingIssues)
                {
                    nonBlockingIssues += result.NonBlockingIssueCount > 0 ? result.NonBlockingIssueCount : 1;
                }

                if (result.Failed)
                {
                    failed++;
                }

                if (result.Skipped)
                {
                    skipped++;
                }

                if (result.Succeeded)
                {
                    succeeded++;
                }
            }

            if (blockingIssues > 0)
            {
                return ActivityContentExecutionAggregateStatus.FailedBlocking;
            }

            if (failed > 0)
            {
                return ActivityContentExecutionAggregateStatus.FailedNonBlocking;
            }

            if (nonBlockingIssues > 0)
            {
                return ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues;
            }

            if (succeeded == 0 && skipped == candidateResults.Count)
            {
                return ActivityContentExecutionAggregateStatus.SkippedNoContent;
            }

            if (succeeded == 0)
            {
                return ActivityContentExecutionAggregateStatus.SucceededNoOp;
            }

            return ActivityContentExecutionAggregateStatus.Succeeded;
        }

        private static string BuildDefaultMessage(ActivityContentExecutionAggregateStatus status)
        {
            return status switch
            {
                ActivityContentExecutionAggregateStatus.Succeeded => "Activity content participant execution aggregate succeeded.",
                ActivityContentExecutionAggregateStatus.SucceededNoOp => "Activity content participant execution aggregate completed with no operation.",
                ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues => "Activity content participant execution aggregate succeeded with non-blocking issues.",
                ActivityContentExecutionAggregateStatus.SkippedNoContent => "Activity content participant execution aggregate skipped because no participant results were provided.",
                ActivityContentExecutionAggregateStatus.FailedNonBlocking => "Activity content participant execution aggregate failed without blocking readiness.",
                ActivityContentExecutionAggregateStatus.FailedBlocking => "Activity content participant execution aggregate failed and blocks readiness.",
                ActivityContentExecutionAggregateStatus.RejectedInvalidResults => "Activity content participant execution aggregate rejected invalid results.",
                _ => "Activity content participant execution aggregate completed."
            };
        }

        private int CountByRequiredness(ActivityContentExecutionRequiredness requiredness)
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            return _results.Count(t => t.Requiredness == requiredness);
        }

        private int CountSucceeded()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            return _results.Count(t => t.Succeeded);
        }

        private int CountSkipped()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            return _results.Count(t => t.Skipped);
        }

        private int CountFailed()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            return _results.Count(t => t.Failed);
        }

        private int SumBlockingIssues()
        {
            return FrameworkIssueCounting.Sum(_results, t => t.BlockingIssueCount);
        }

        private int SumNonBlockingIssues()
        {
            return FrameworkIssueCounting.Sum(_results, t => t.NonBlockingIssueCount);
        }
    }
}
