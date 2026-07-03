using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.LifecycleOperations
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F45 lifecycle-local content evidence formatter; no content dispatch or policy decisions.")]
    internal static class FrameworkLifecycleContentEvidenceProjection
    {
        public static string BuildDiagnosticString(FrameworkLifecycleContentEvidence evidence)
        {
            var builder = new StringBuilder();
            AppendField(builder, "lifecycleContentStatus", evidence.Status);
            AppendField(builder, "lifecycleContentEnter", evidence.EnterStatus);
            AppendField(builder, "lifecycleContentExit", evidence.ExitStatus);
            AppendField(builder, "lifecycleContentEnterRequests", evidence.EnterRequestCount);
            AppendField(builder, "lifecycleContentExitRequests", evidence.ExitRequestCount);
            AppendField(builder, "lifecycleContentParticipants", evidence.ParticipantCount);
            AppendField(builder, "lifecycleContentParticipantSource", evidence.ParticipantSourceStatus);
            AppendField(builder, "lifecycleContentIssues", evidence.IssueCount);
            AppendField(builder, "lifecycleContentBlockingIssues", evidence.BlockingIssueCount);
            AppendField(builder, "lifecycleContentBlocksReadiness", evidence.BlocksReadiness);
            AppendField(builder, "lifecycleContentHandles", evidence.ContentHandleCount);
            AppendField(builder, "source", evidence.Source);
            AppendField(builder, "reason", evidence.Reason);
            return builder.ToString();
        }

        public static string BuildStageStatus(string status, int requestCount, int blockingIssueCount)
        {
            return $"{status.NormalizeTextOrFallback("Unknown")}:requests={requestCount}:blocking={blockingIssueCount}";
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
