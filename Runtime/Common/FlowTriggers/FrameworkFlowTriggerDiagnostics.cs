using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Common.FlowTriggers
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F42 neutral FlowTrigger diagnostics helper; formats caller-owned evidence only.")]
    internal static class FrameworkFlowTriggerDiagnostics
    {
        public static string NormalizeSource(string source, string fallback)
        {
            return source.NormalizeTextOrFallback(fallback);
        }

        public static string NormalizeReason(string reason, string fallback)
        {
            return reason.NormalizeTextOrFallback(fallback);
        }

        public static string BuildSubmissionDiagnostics(FrameworkFlowTriggerSubmission submission)
        {
            var builder = new StringBuilder();
            AppendSubmissionDiagnostics(builder, submission);
            return builder.ToString();
        }

        public static void AppendSubmissionDiagnostics(StringBuilder builder, FrameworkFlowTriggerSubmission submission)
        {
            if (builder == null)
            {
                return;
            }

            AppendField(builder, "phase", submission.Phase);
            AppendField(builder, "outcome", submission.Outcome);
            AppendField(builder, "inFlight", submission.InFlight);
            AppendField(builder, "submitted", submission.Submitted);
            AppendField(builder, "completed", submission.Completed);
            AppendField(builder, "succeeded", submission.Succeeded);
            AppendField(builder, "ignored", submission.Ignored);
            AppendField(builder, "failed", submission.Failed);
            AppendField(builder, "issues", submission.IssueCount);
            AppendField(builder, "blockingIssues", submission.BlockingIssueCount);
            AppendField(builder, "source", submission.Source);
            AppendField(builder, "reason", submission.Reason);

            if (!string.IsNullOrWhiteSpace(submission.Message))
            {
                AppendField(builder, "message", submission.Message);
            }
        }

        public static void AppendField(StringBuilder builder, string name, object value)
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
