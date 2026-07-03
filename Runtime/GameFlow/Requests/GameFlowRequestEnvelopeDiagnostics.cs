using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F48 GameFlow request envelope diagnostic string projection.")]
    internal static class GameFlowRequestEnvelopeDiagnostics
    {
        public static string BuildDiagnosticString(GameFlowRequestEnvelope envelope)
        {
            var builder = new StringBuilder();
            AppendField(builder, "gameFlowEnvelopeKind", envelope.OperationKindText);
            AppendField(builder, "gameFlowEnvelopeAdmission", envelope.AdmissionText);
            AppendField(builder, "gameFlowEnvelopeSource", envelope.Source);
            AppendField(builder, "gameFlowEnvelopeReason", envelope.Reason);
            AppendField(builder, "gameFlowEnvelopeTargetRoute", envelope.TargetRoute);
            AppendField(builder, "gameFlowEnvelopePreviousRoute", envelope.PreviousRoute);
            AppendField(builder, "gameFlowEnvelopeTargetActivity", envelope.TargetActivity);
            AppendField(builder, "gameFlowEnvelopePreviousActivity", envelope.PreviousActivity);
            AppendField(builder, "gameFlowEnvelopeTransitionStatus", envelope.TransitionStatus);
            AppendField(builder, "gameFlowEnvelopeLoadingStatus", envelope.LoadingStatus);
            AppendField(builder, "gameFlowEnvelopeValidationMode", envelope.ValidationMode);
            AppendField(builder, "gameFlowEnvelopeDomainStatus", envelope.DomainStatus);
            AppendField(builder, "gameFlowEnvelopeLifecycleOperationKind", envelope.LifecycleOperationKind);
            AppendField(builder, "gameFlowEnvelopeLifecycleStages", envelope.LifecycleStageCount);
            AppendField(builder, "gameFlowEnvelopeLifecycleBlockingIssues", envelope.LifecycleBlockingIssueCount);
            AppendField(builder, "gameFlowEnvelopeLifecycleFailedStages", envelope.LifecycleFailedStageCount);
            AppendField(builder, "gameFlowEnvelopeLifecycleSkippedStages", envelope.LifecycleSkippedStageCount);
            AppendField(builder, "gameFlowEnvelopeContentStatus", envelope.LifecycleContentStatus);
            AppendField(builder, "gameFlowEnvelopeContentBlockingIssues", envelope.LifecycleContentBlockingIssueCount);
            AppendField(builder, "gameFlowEnvelopeContentHandles", envelope.LifecycleContentHandleCount);
            AppendField(builder, "gameFlowEnvelopeReadiness", envelope.LifecycleReadiness);
            AppendField(builder, "gameFlowEnvelopeReadinessIssues", envelope.LifecycleReadinessIssueCount);
            AppendField(builder, "gameFlowEnvelopeLoadingAdapterEvidenceCount", envelope.LoadingAdapterEvidenceCount);
            AppendField(builder, "gameFlowEnvelopeLoadingAdapterBlockingIssues", envelope.LoadingAdapterBlockingIssueCount);
            return builder.ToString();
        }

        private static void AppendField(StringBuilder builder, string name, object value)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(name.NormalizeTextOrFallback("field"));
            builder.Append("='");
            builder.Append((value == null ? string.Empty : value.ToString()).ToDiagnosticText());
            builder.Append("'");
        }
    }
}
