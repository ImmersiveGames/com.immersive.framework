using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F48 passive GameFlow request diagnostics shell; does not execute lifecycle behavior.")]
    internal readonly struct GameFlowRequestEnvelope
    {
        public GameFlowRequestEnvelope(
            GameFlowRequestOperationKind operationKind,
            GameFlowRequestAdmission admission,
            string source,
            string reason,
            string targetRoute,
            string previousRoute,
            string targetActivity,
            string previousActivity,
            string transitionStatus,
            string loadingStatus,
            string validationMode,
            string domainStatus,
            string lifecycleOperationKind,
            int lifecycleStageCount,
            int lifecycleBlockingIssueCount,
            int lifecycleFailedStageCount,
            int lifecycleSkippedStageCount,
            string lifecycleContentStatus,
            int lifecycleContentBlockingIssueCount,
            int lifecycleContentHandleCount,
            string lifecycleReadiness,
            string lifecycleReadinessReason,
            int lifecycleReadinessIssueCount,
            int loadingAdapterEvidenceCount,
            int loadingAdapterEvidenceAppliedCount,
            int loadingAdapterEvidenceSkippedCount,
            int loadingAdapterEvidenceFailedCount,
            int loadingAdapterBlockingIssueCount)
        {
            OperationKind = operationKind;
            Admission = admission;
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
            TargetRoute = targetRoute.NormalizeTextOrFallback("<none>");
            PreviousRoute = previousRoute.NormalizeTextOrFallback("<none>");
            TargetActivity = targetActivity.NormalizeTextOrFallback("<none>");
            PreviousActivity = previousActivity.NormalizeTextOrFallback("<none>");
            TransitionStatus = transitionStatus.NormalizeTextOrFallback("Unknown");
            LoadingStatus = loadingStatus.NormalizeTextOrFallback("Unknown");
            ValidationMode = validationMode.NormalizeTextOrFallback("Unknown");
            DomainStatus = domainStatus.NormalizeTextOrFallback("Unknown");
            LifecycleOperationKind = lifecycleOperationKind.NormalizeTextOrFallback("Unknown");
            LifecycleStageCount = ClampNonNegative(lifecycleStageCount);
            LifecycleBlockingIssueCount = ClampNonNegative(lifecycleBlockingIssueCount);
            LifecycleFailedStageCount = ClampNonNegative(lifecycleFailedStageCount);
            LifecycleSkippedStageCount = ClampNonNegative(lifecycleSkippedStageCount);
            LifecycleContentStatus = lifecycleContentStatus.NormalizeTextOrFallback("Unknown");
            LifecycleContentBlockingIssueCount = ClampNonNegative(lifecycleContentBlockingIssueCount);
            LifecycleContentHandleCount = ClampNonNegative(lifecycleContentHandleCount);
            LifecycleReadiness = lifecycleReadiness.NormalizeTextOrFallback("Unknown");
            LifecycleReadinessReason = lifecycleReadinessReason.NormalizeTextOrFallback("None");
            LifecycleReadinessIssueCount = ClampNonNegative(lifecycleReadinessIssueCount);
            LoadingAdapterEvidenceCount = ClampNonNegative(loadingAdapterEvidenceCount);
            LoadingAdapterEvidenceAppliedCount = ClampNonNegative(loadingAdapterEvidenceAppliedCount);
            LoadingAdapterEvidenceSkippedCount = ClampNonNegative(loadingAdapterEvidenceSkippedCount);
            LoadingAdapterEvidenceFailedCount = ClampNonNegative(loadingAdapterEvidenceFailedCount);
            LoadingAdapterBlockingIssueCount = ClampNonNegative(loadingAdapterBlockingIssueCount);
        }

        public GameFlowRequestOperationKind OperationKind { get; }

        public GameFlowRequestAdmission Admission { get; }

        public string Source { get; }

        public string Reason { get; }

        public string TargetRoute { get; }

        public string PreviousRoute { get; }

        public string TargetActivity { get; }

        public string PreviousActivity { get; }

        public string TransitionStatus { get; }

        public string LoadingStatus { get; }

        public string ValidationMode { get; }

        public string DomainStatus { get; }

        public string LifecycleOperationKind { get; }

        public int LifecycleStageCount { get; }

        public int LifecycleBlockingIssueCount { get; }

        public int LifecycleFailedStageCount { get; }

        public int LifecycleSkippedStageCount { get; }

        public string LifecycleContentStatus { get; }

        public int LifecycleContentBlockingIssueCount { get; }

        public int LifecycleContentHandleCount { get; }

        public string LifecycleReadiness { get; }

        public string LifecycleReadinessReason { get; }

        public int LifecycleReadinessIssueCount { get; }

        public int LoadingAdapterEvidenceCount { get; }

        public int LoadingAdapterEvidenceAppliedCount { get; }

        public int LoadingAdapterEvidenceSkippedCount { get; }

        public int LoadingAdapterEvidenceFailedCount { get; }

        public int LoadingAdapterBlockingIssueCount { get; }

        public string OperationKindText => OperationKind.ToString();

        public string AdmissionText => Admission.ToString();

        public string DiagnosticText => GameFlowRequestEnvelopeDiagnostics.BuildDiagnosticString(this);

        private static int ClampNonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
