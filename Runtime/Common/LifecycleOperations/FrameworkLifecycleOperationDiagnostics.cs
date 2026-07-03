using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 lifecycle-local operation evidence diagnostics formatter.")]
    internal static class FrameworkLifecycleOperationDiagnostics
    {
        public static string BuildDiagnosticString(FrameworkLifecycleOperationEvidence evidence)
        {
            var builder = new StringBuilder();
            AppendField(builder, "lifecycleOperationKind", evidence.OperationKindText);
            AppendField(builder, "lifecycleOperationStages", evidence.StageCount);
            AppendField(builder, "lifecycleOperationBlockingIssues", evidence.BlockingIssueCount);
            AppendField(builder, "lifecycleOperationIssues", evidence.IssueCount);
            AppendField(builder, "lifecycleOperationSideEffects", evidence.SideEffectCount);
            AppendField(builder, "lifecycleOperationFailedStages", evidence.FailedStageCount);
            AppendField(builder, "lifecycleOperationSkippedStages", evidence.SkippedStageCount);
            AppendField(builder, "lifecycleOperationStageNames", evidence.StageNamesText);
            AppendField(builder, "lifecycleOperationStageStatuses", evidence.StageStatusesText);
            AppendField(builder, "source", evidence.Source);
            AppendField(builder, "reason", evidence.Reason);
            return builder.ToString();
        }

        public static string BuildStageDiagnosticString(FrameworkLifecycleOperationStageEvidence evidence)
        {
            var builder = new StringBuilder();
            AppendField(builder, "stage", evidence.StageText);
            AppendField(builder, "status", evidence.StatusText);
            AppendField(builder, "issues", evidence.IssueCount);
            AppendField(builder, "blockingIssues", evidence.BlockingIssueCount);
            AppendField(builder, "sideEffectsApplied", evidence.SideEffectsApplied);
            AppendField(builder, "failed", evidence.Failed);
            AppendField(builder, "skipped", evidence.Skipped);
            AppendField(builder, "source", evidence.Source);
            AppendField(builder, "reason", evidence.Reason);
            if (!string.IsNullOrWhiteSpace(evidence.Message))
            {
                AppendField(builder, "message", evidence.Message);
            }

            if (!string.IsNullOrWhiteSpace(evidence.OriginalEvidenceText))
            {
                AppendField(builder, "originalEvidence", evidence.OriginalEvidenceText);
            }

            return builder.ToString();
        }

        public static void AppendSeparated(StringBuilder builder, string value)
        {
            if (builder == null)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append(value.NormalizeTextOrFallback("None").ToDiagnosticText());
        }

        private static void AppendField(StringBuilder builder, string name, object value)
        {
            if (builder == null)
            {
                return;
            }

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
