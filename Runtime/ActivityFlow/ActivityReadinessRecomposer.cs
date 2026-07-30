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
            if (!technicalBaseline.HasActivity ||
                !technicalBaseline.IsReady ||
                authorableContribution.IsSatisfied)
            {
                return technicalBaseline;
            }

            return new ActivityReadinessState(
                ActivityReadinessStatus.NotReady,
                technicalBaseline.Activity,
                technicalBaseline.ActivityContentSet,
                technicalBaseline.ActivityContentLifecycleResult,
                technicalBaseline.ActivityContentExecutionExecuted,
                technicalBaseline.ActivityContentExecutionBlocksReadiness,
                technicalBaseline.ActivityContentExecutionBlockingIssueCount,
                technicalBaseline.BlockingIssueCount + 1,
                source.NormalizeTextOrFallback("Unknown"),
                reason.NormalizeTextOrFallback("None"),
                NormalizeDiagnosticReason(authorableContribution.Reason));
        }

        private static string NormalizeDiagnosticReason(string reason)
        {
            return reason.NormalizeTextOrFallback(
                "AuthorableReadinessBlocked");
        }
    }
}
