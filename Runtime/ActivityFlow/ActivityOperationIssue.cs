using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable diagnostic issue produced by an Activity operation plan.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation issue; side-effect-free diagnostic record.")]
    internal readonly struct ActivityOperationIssue
    {
        public ActivityOperationIssue(
            ActivityOperationIssueKind kind,
            ActivityOperationIssueSeverity severity,
            string message)
        {
            Kind = kind;
            Severity = severity;
            Message = Normalize(message);
        }

        public ActivityOperationIssueKind Kind { get; }

        public ActivityOperationIssueSeverity Severity { get; }

        public string Message { get; }

        public bool IsBlocking => Severity == ActivityOperationIssueSeverity.Blocking;

        public bool IsWarning => Severity == ActivityOperationIssueSeverity.Warning;

        public bool HasIssue => Kind != ActivityOperationIssueKind.Unknown
            && Severity != ActivityOperationIssueSeverity.None;

        public string ToDiagnosticString()
        {
            return $"kind='{Kind}' severity='{Severity}' blocking='{IsBlocking}' message='{Message}'";
        }

        public static ActivityOperationIssue Blocking(ActivityOperationIssueKind kind, string message)
        {
            return new ActivityOperationIssue(kind, ActivityOperationIssueSeverity.Blocking, message);
        }

        public static ActivityOperationIssue Warning(ActivityOperationIssueKind kind, string message)
        {
            return new ActivityOperationIssue(kind, ActivityOperationIssueSeverity.Warning, message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
