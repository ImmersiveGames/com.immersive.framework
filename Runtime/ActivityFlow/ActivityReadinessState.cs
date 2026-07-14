using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal immutable readiness snapshot for the current Activity scope.
    /// Activity content application and required execution participants contribute compact blocking evidence.
    /// The complete execution lifecycle result remains owned by ActivityFlowStartResult.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F4D/P3J.6 Activity readiness state with compact required execution-participant evidence.")]
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
        {
            Status = status;
            Activity = activity;
            ActivityContentSet = activityContentSet;
            ActivityContentLifecycleResult = activityContentLifecycleResult;
            ActivityContentExecutionExecuted = activityContentExecutionExecuted;
            ActivityContentExecutionBlocksReadiness = activityContentExecutionBlocksReadiness;
            ActivityContentExecutionBlockingIssueCount = activityContentExecutionBlockingIssueCount;
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
        public int BlockingIssueCount { get; }
        public string Source { get; }
        public string Reason { get; }
        public string DiagnosticReason { get; }
        public bool IsNone => Status == ActivityReadinessStatus.None;
        public bool IsReady => Status == ActivityReadinessStatus.Ready && Activity != null && BlockingIssueCount == 0;
        public bool IsNotReady => Status == ActivityReadinessStatus.NotReady;
        public bool HasActivity => Activity != null;
        public bool HasActivityContent => ActivityContentSet.HasContent;
        public bool HasLifecycleResult => ActivityContentLifecycleResult.Executed;
        public bool HasExecutionResult => ActivityContentExecutionExecuted;
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
