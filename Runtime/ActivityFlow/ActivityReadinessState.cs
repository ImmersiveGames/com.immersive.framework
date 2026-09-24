using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal immutable readiness snapshot for the current Activity scope.
    /// Activity content application, synchronous execution and occurrence-scoped readiness
    /// contributions provide separate technical, preparing and terminal-failure evidence.
    /// The complete execution lifecycle result remains owned by ActivityFlowStartResult.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F4D/P3J.6 Activity readiness state with compact execution and authorable contribution evidence.")]
    internal readonly struct ActivityReadinessState
    {
        public ActivityReadinessState(
            ActivityReadinessStatus status,
            ActivityAsset activity,
            ActivityContentSet activityContentSet,
            ActivityContentLifecycleResult activityContentLifecycleResult,
            int blockingIssueCount,
            string source,
            string reason,
            string diagnosticReason)
            : this(
                status,
                activity,
                activityContentSet,
                activityContentLifecycleResult,
                false,
                false,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                blockingIssueCount,
                source,
                reason,
                diagnosticReason)
        {
        }

        public ActivityReadinessState(
            ActivityReadinessStatus status,
            ActivityAsset activity,
            ActivityContentSet activityContentSet,
            ActivityContentLifecycleResult activityContentLifecycleResult,
            bool activityContentExecutionExecuted,
            bool activityContentExecutionBlocksReadiness,
            int activityContentExecutionBlockingIssueCount,
            int blockingIssueCount,
            string source,
            string reason,
            string diagnosticReason)
            : this(
                status,
                activity,
                activityContentSet,
                activityContentLifecycleResult,
                activityContentExecutionExecuted,
                activityContentExecutionBlocksReadiness,
                activityContentExecutionBlockingIssueCount,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                blockingIssueCount,
                source,
                reason,
                diagnosticReason)
        {
        }

        /// <summary>
        /// Compatibility overload for callers that predate explicit completion and release counts.
        /// Any participant not pending or failed is treated as completed; released counts are zero.
        /// </summary>
        public ActivityReadinessState(
            ActivityReadinessStatus status,
            ActivityAsset activity,
            ActivityContentSet activityContentSet,
            ActivityContentLifecycleResult activityContentLifecycleResult,
            bool activityContentExecutionExecuted,
            bool activityContentExecutionBlocksReadiness,
            int activityContentExecutionBlockingIssueCount,
            int requiredCount,
            int optionalCount,
            int requiredPendingCount,
            int requiredFailedCount,
            int optionalPendingCount,
            int optionalFailedCount,
            int blockingIssueCount,
            string source,
            string reason,
            string diagnosticReason)
            : this(
                status,
                activity,
                activityContentSet,
                activityContentLifecycleResult,
                activityContentExecutionExecuted,
                activityContentExecutionBlocksReadiness,
                activityContentExecutionBlockingIssueCount,
                requiredCount,
                optionalCount,
                requiredPendingCount,
                ResolveLegacyCompletedCount(
                    requiredCount,
                    requiredPendingCount,
                    requiredFailedCount,
                    nameof(requiredCount)),
                requiredFailedCount,
                0,
                optionalPendingCount,
                ResolveLegacyCompletedCount(
                    optionalCount,
                    optionalPendingCount,
                    optionalFailedCount,
                    nameof(optionalCount)),
                optionalFailedCount,
                0,
                blockingIssueCount,
                source,
                reason,
                diagnosticReason)
        {
        }

        public ActivityReadinessState(
            ActivityReadinessStatus status,
            ActivityAsset activity,
            ActivityContentSet activityContentSet,
            ActivityContentLifecycleResult activityContentLifecycleResult,
            bool activityContentExecutionExecuted,
            bool activityContentExecutionBlocksReadiness,
            int activityContentExecutionBlockingIssueCount,
            int requiredCount,
            int optionalCount,
            int requiredPendingCount,
            int requiredCompletedCount,
            int requiredFailedCount,
            int requiredReleasedCount,
            int optionalPendingCount,
            int optionalCompletedCount,
            int optionalFailedCount,
            int optionalReleasedCount,
            int blockingIssueCount,
            string source,
            string reason,
            string diagnosticReason)
        {
            ValidateContributionCounts(
                requiredCount,
                requiredPendingCount,
                requiredCompletedCount,
                requiredFailedCount,
                requiredReleasedCount,
                nameof(requiredCount));
            ValidateContributionCounts(
                optionalCount,
                optionalPendingCount,
                optionalCompletedCount,
                optionalFailedCount,
                optionalReleasedCount,
                nameof(optionalCount));

            Status = status;
            Activity = activity;
            ActivityContentSet = activityContentSet;
            ActivityContentLifecycleResult = activityContentLifecycleResult;
            ActivityContentExecutionExecuted = activityContentExecutionExecuted;
            ActivityContentExecutionBlocksReadiness = activityContentExecutionBlocksReadiness;
            ActivityContentExecutionBlockingIssueCount = activityContentExecutionBlockingIssueCount;
            RequiredCount = requiredCount;
            OptionalCount = optionalCount;
            RequiredPendingCount = requiredPendingCount;
            RequiredCompletedCount = requiredCompletedCount;
            RequiredFailedCount = requiredFailedCount;
            RequiredReleasedCount = requiredReleasedCount;
            OptionalPendingCount = optionalPendingCount;
            OptionalCompletedCount = optionalCompletedCount;
            OptionalFailedCount = optionalFailedCount;
            OptionalReleasedCount = optionalReleasedCount;
            BlockingIssueCount = blockingIssueCount;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            DiagnosticReason = diagnosticReason ?? string.Empty;
        }

        public ActivityReadinessStatus Status { get; }
        public ActivityAsset Activity { get; }
        public ActivityContentSet ActivityContentSet { get; }
        public ActivityContentLifecycleResult ActivityContentLifecycleResult { get; }
        public bool ActivityContentExecutionExecuted { get; }
        public bool ActivityContentExecutionBlocksReadiness { get; }
        public int ActivityContentExecutionBlockingIssueCount { get; }
        public int RequiredCount { get; }
        public int OptionalCount { get; }
        public int RequiredPendingCount { get; }
        public int RequiredCompletedCount { get; }
        public int RequiredFailedCount { get; }
        public int RequiredReleasedCount { get; }
        public int OptionalPendingCount { get; }
        public int OptionalCompletedCount { get; }
        public int OptionalFailedCount { get; }
        public int OptionalReleasedCount { get; }
        public int ParticipantCount => RequiredCount + OptionalCount;
        public int PendingCount => RequiredPendingCount + OptionalPendingCount;
        public int CompletedCount => RequiredCompletedCount + OptionalCompletedCount;
        public int FailedCount => RequiredFailedCount + OptionalFailedCount;
        public int ReleasedCount => RequiredReleasedCount + OptionalReleasedCount;
        public int BlockingIssueCount { get; }
        public string Source { get; }
        public string Reason { get; }
        public string DiagnosticReason { get; }
        public bool IsNone => Status == ActivityReadinessStatus.None;
        public bool IsReady =>
            Status == ActivityReadinessStatus.Ready &&
            Activity != null &&
            BlockingIssueCount == 0 &&
            RequiredPendingCount == 0 &&
            RequiredFailedCount == 0 &&
            RequiredReleasedCount == 0;
        public bool IsNotReady => Status == ActivityReadinessStatus.NotReady;
        public bool IsPreparing =>
            IsNotReady &&
            RequiredPendingCount > 0 &&
            !HasTerminalFailure;
        public bool HasTerminalFailure =>
            RequiredFailedCount > 0 ||
            RequiredReleasedCount > 0 ||
            HasBlockingIssues;
        public bool HasActivity => Activity != null;
        public bool HasActivityContent => ActivityContentSet.HasContent;
        public bool HasLifecycleResult => ActivityContentLifecycleResult.Executed;
        public bool HasExecutionResult => ActivityContentExecutionExecuted;
        public bool HasAuthorableReadinessEvidence => ParticipantCount > 0;
        public bool HasBlockingIssues => BlockingIssueCount > 0;
        public string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;
        public string DiagnosticStatus => Status.ToString();

        public static ActivityReadinessState None(
            ActivityRuntimeState activityState,
            ActivityContentApplyResult activityContentResult,
            string source,
            string reason)
        {
            return None(
                activityState,
                activityContentResult,
                false,
                false,
                0,
                source,
                reason);
        }

        public static ActivityReadinessState None(
            ActivityRuntimeState activityState,
            ActivityContentApplyResult activityContentResult,
            bool activityContentExecutionExecuted,
            bool activityContentExecutionBlocksReadiness,
            int activityContentExecutionBlockingIssueCount,
            string source,
            string reason)
        {
            return new ActivityReadinessState(
                ActivityReadinessStatus.None,
                activityState.Activity,
                activityContentResult.ActivityContentSet,
                activityContentResult.LifecycleResult,
                activityContentExecutionExecuted,
                activityContentExecutionBlocksReadiness,
                activityContentExecutionBlockingIssueCount,
                0,
                NormalizeSource(source),
                NormalizeReason(reason),
                "NoActiveActivity");
        }

        public static ActivityReadinessState FromActivityResult(
            ActivityRuntimeState activityState,
            ActivityContentApplyResult activityContentResult,
            string source,
            string reason)
        {
            return FromActivityResult(
                activityState,
                activityContentResult,
                false,
                false,
                0,
                source,
                reason);
        }

        public static ActivityReadinessState FromActivityResult(
            ActivityRuntimeState activityState,
            ActivityContentApplyResult activityContentResult,
            bool activityContentExecutionExecuted,
            bool activityContentExecutionBlocksReadiness,
            int activityContentExecutionBlockingIssueCount,
            string source,
            string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (!activityState.IsActive)
            {
                return None(
                    activityState,
                    activityContentResult,
                    activityContentExecutionExecuted,
                    activityContentExecutionBlocksReadiness,
                    activityContentExecutionBlockingIssueCount,
                    resolvedSource,
                    resolvedReason);
            }

            int blockingIssueCount = 0;
            string diagnosticReason = "BaselineReady";

            if (activityContentResult.MissingActivityCount > 0)
            {
                blockingIssueCount += activityContentResult.MissingActivityCount;
                diagnosticReason = "MissingActivityReference";
            }

            if (activityContentResult.HasLifecycleFailures)
            {
                blockingIssueCount +=
                    activityContentResult.LifecycleResult.EnterFailedReceiverCount +
                    activityContentResult.LifecycleResult.ExitFailedReceiverCount;
                diagnosticReason = "ActivityContentLifecycleFailure";
            }

            if (activityContentExecutionBlocksReadiness)
            {
                blockingIssueCount += activityContentExecutionBlockingIssueCount > 0
                    ? activityContentExecutionBlockingIssueCount
                    : 1;
                diagnosticReason = "ActivityContentExecutionBlockingFailure";
            }

            return new ActivityReadinessState(
                blockingIssueCount == 0
                    ? ActivityReadinessStatus.Ready
                    : ActivityReadinessStatus.NotReady,
                activityState.Activity,
                activityContentResult.ActivityContentSet,
                activityContentResult.LifecycleResult,
                activityContentExecutionExecuted,
                activityContentExecutionBlocksReadiness,
                activityContentExecutionBlockingIssueCount,
                blockingIssueCount,
                resolvedSource,
                resolvedReason,
                diagnosticReason);
        }

        private static int ResolveLegacyCompletedCount(
            int totalCount,
            int pendingCount,
            int failedCount,
            string parameterName)
        {
            ValidateNonNegative(totalCount, parameterName);
            ValidateNonNegative(pendingCount, nameof(pendingCount));
            ValidateNonNegative(failedCount, nameof(failedCount));

            int completedCount = totalCount - pendingCount - failedCount;
            if (completedCount < 0)
            {
                throw new ArgumentException(
                    "Pending and failed participant counts cannot exceed the total count.",
                    parameterName);
            }

            return completedCount;
        }

        private static void ValidateContributionCounts(
            int totalCount,
            int pendingCount,
            int completedCount,
            int failedCount,
            int releasedCount,
            string parameterName)
        {
            ValidateNonNegative(totalCount, parameterName);
            ValidateNonNegative(pendingCount, nameof(pendingCount));
            ValidateNonNegative(completedCount, nameof(completedCount));
            ValidateNonNegative(failedCount, nameof(failedCount));
            ValidateNonNegative(releasedCount, nameof(releasedCount));

            if (pendingCount + completedCount + failedCount + releasedCount != totalCount)
            {
                throw new ArgumentException(
                    "Participant state counts must equal the total participant count.",
                    parameterName);
            }
        }

        private static void ValidateNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Participant counts cannot be negative.");
            }
        }

        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback("Unknown");
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback("None");
        }
    }
}
