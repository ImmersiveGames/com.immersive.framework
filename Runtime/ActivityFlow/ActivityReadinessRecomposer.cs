using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    internal static class ActivityReadinessRecomposer
    {
        internal static ActivityReadinessState Recompute(
            ActivityReadinessState technicalBaseline,
            ActivityReadinessAuthorableContribution authorableContribution,
            string source,
            string reason)
        {
            if (!technicalBaseline.HasActivity || technicalBaseline.IsNone)
            {
                return technicalBaseline;
            }

            int blockingIssueCount =
                technicalBaseline.BlockingIssueCount +
                authorableContribution.TerminalBlockingIssueCount;
            bool isReady =
                technicalBaseline.IsReady &&
                authorableContribution.IsSatisfied;

            return new ActivityReadinessState(
                isReady
                    ? ActivityReadinessStatus.Ready
                    : ActivityReadinessStatus.NotReady,
                technicalBaseline.Activity,
                technicalBaseline.ActivityContentSet,
                technicalBaseline.ActivityContentLifecycleResult,
                technicalBaseline.ActivityContentExecutionExecuted,
                technicalBaseline.ActivityContentExecutionBlocksReadiness,
                technicalBaseline.ActivityContentExecutionBlockingIssueCount,
                authorableContribution.RequiredCount,
                authorableContribution.OptionalCount,
                authorableContribution.RequiredPendingCount,
                authorableContribution.RequiredFailedCount,
                authorableContribution.OptionalPendingCount,
                authorableContribution.OptionalFailedCount,
                blockingIssueCount,
                source.NormalizeTextOrFallback("Unknown"),
                reason.NormalizeTextOrFallback("None"),
                ResolveDiagnosticReason(
                    technicalBaseline,
                    authorableContribution,
                    isReady));
        }

        private static string ResolveDiagnosticReason(
            ActivityReadinessState technicalBaseline,
            ActivityReadinessAuthorableContribution authorableContribution,
            bool isReady)
        {
            if (!technicalBaseline.IsReady)
            {
                return technicalBaseline.DiagnosticReason.NormalizeTextOrFallback(
                    "TechnicalReadinessBlocked");
            }

            if (authorableContribution.HasTerminalFailure)
            {
                return authorableContribution.Reason.NormalizeTextOrFallback(
                    "RequiredParticipantFailed");
            }

            if (authorableContribution.RequiredPendingCount > 0)
            {
                return authorableContribution.Reason.NormalizeTextOrFallback(
                    "Preparing");
            }

            if (isReady && authorableContribution.ParticipantCount > 0)
            {
                return authorableContribution.Reason.NormalizeTextOrFallback(
                    "Ready");
            }

            return technicalBaseline.DiagnosticReason.NormalizeTextOrFallback(
                "BaselineReady");
        }
    }
}
