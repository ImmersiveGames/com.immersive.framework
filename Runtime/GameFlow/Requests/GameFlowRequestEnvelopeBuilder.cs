using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common.LifecycleOperations;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F48 GameFlow request envelope builder; copies diagnostics and performs no side effects.")]
    internal static class GameFlowRequestEnvelopeBuilder
    {
        public static GameFlowRequestEnvelope BuildRoute(
            FrameworkRouteRequestResult result,
            string previousRoute,
            string targetRoute,
            string transitionStatus,
            string loadingStatus,
            FrameworkLifecycleOperationEvidence lifecycleOperation,
            FrameworkLifecycleContentEvidence lifecycleContent,
            FrameworkLifecycleReadinessEvidence lifecycleReadiness,
            int loadingAdapterEvidenceCount,
            int loadingAdapterEvidenceAppliedCount,
            int loadingAdapterEvidenceSkippedCount,
            int loadingAdapterEvidenceFailedCount,
            int loadingAdapterBlockingIssueCount)
        {
            return new GameFlowRequestEnvelope(
                GameFlowRequestOperationKind.Route,
                ResolveRouteAdmission(result.Kind),
                result.Source,
                result.Reason,
                targetRoute,
                previousRoute,
                "<none>",
                "<none>",
                transitionStatus,
                loadingStatus,
                "Unknown",
                result.Kind.ToString(),
                lifecycleOperation.OperationKindText,
                lifecycleOperation.StageCount,
                lifecycleOperation.BlockingIssueCount,
                lifecycleOperation.FailedStageCount,
                lifecycleOperation.SkippedStageCount,
                lifecycleContent.Status,
                lifecycleContent.BlockingIssueCount,
                lifecycleContent.ContentHandleCount,
                lifecycleReadiness.Status,
                lifecycleReadiness.Reason,
                lifecycleReadiness.IssueCount,
                loadingAdapterEvidenceCount,
                loadingAdapterEvidenceAppliedCount,
                loadingAdapterEvidenceSkippedCount,
                loadingAdapterEvidenceFailedCount,
                loadingAdapterBlockingIssueCount);
        }

        public static GameFlowRequestEnvelope BuildActivity(
            FrameworkActivityRequestResult result,
            string previousActivity,
            string targetActivity,
            string transitionStatus,
            string loadingStatus,
            FrameworkLifecycleOperationEvidence lifecycleOperation,
            FrameworkLifecycleContentEvidence lifecycleContent,
            FrameworkLifecycleReadinessEvidence lifecycleReadiness,
            int loadingAdapterEvidenceCount,
            int loadingAdapterEvidenceAppliedCount,
            int loadingAdapterEvidenceSkippedCount,
            int loadingAdapterEvidenceFailedCount,
            int loadingAdapterBlockingIssueCount)
        {
            return new GameFlowRequestEnvelope(
                result.OperationKind,
                ResolveActivityAdmission(result.Kind),
                result.Source,
                result.Reason,
                "<none>",
                "<none>",
                targetActivity,
                previousActivity,
                transitionStatus,
                loadingStatus,
                "Unknown",
                result.Kind.ToString(),
                lifecycleOperation.OperationKindText,
                lifecycleOperation.StageCount,
                lifecycleOperation.BlockingIssueCount,
                lifecycleOperation.FailedStageCount,
                lifecycleOperation.SkippedStageCount,
                lifecycleContent.Status,
                lifecycleContent.BlockingIssueCount,
                lifecycleContent.ContentHandleCount,
                lifecycleReadiness.Status,
                lifecycleReadiness.Reason,
                lifecycleReadiness.IssueCount,
                loadingAdapterEvidenceCount,
                loadingAdapterEvidenceAppliedCount,
                loadingAdapterEvidenceSkippedCount,
                loadingAdapterEvidenceFailedCount,
                loadingAdapterBlockingIssueCount);
        }

        private static GameFlowRequestAdmission ResolveRouteAdmission(FrameworkRouteRequestKind kind)
        {
            return kind == FrameworkRouteRequestKind.Succeeded
                ? GameFlowRequestAdmission.Accepted
                : GameFlowRequestAdmission.Rejected;
        }

        private static GameFlowRequestAdmission ResolveActivityAdmission(FrameworkActivityRequestKind kind)
        {
            return kind == FrameworkActivityRequestKind.Succeeded
                ? GameFlowRequestAdmission.Accepted
                : GameFlowRequestAdmission.Rejected;
        }
    }
}
