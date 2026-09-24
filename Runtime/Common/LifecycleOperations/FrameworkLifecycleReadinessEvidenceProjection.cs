using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F45 lifecycle-local readiness evidence formatter; no readiness policy decisions.")]
    internal static class FrameworkLifecycleReadinessEvidenceProjection
    {
        public static string BuildDiagnosticString(FrameworkLifecycleReadinessEvidence evidence)
        {
            var builder = new StringBuilder();
            AppendField(builder, "lifecycleReadiness", evidence.Status);
            AppendField(builder, "lifecycleReadinessReason", evidence.Reason);
            AppendField(builder, "lifecycleReadinessIssues", evidence.IssueCount);
            AppendField(builder, "lifecycleReadinessBlockedByContent", evidence.BlockedByContent);
            AppendField(builder, "source", evidence.Source);
            AppendField(builder, "reason", evidence.RequestReason);
            return builder.ToString();
        }

        public static string BuildStageStatus(FrameworkLifecycleReadinessEvidence evidence)
        {
            return $"{evidence.Status}:issues={evidence.IssueCount}:blockedByContent={evidence.BlockedByContent}";
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
