using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Structured validator issue or non-blocking diagnostic emitted by F5G.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Structured local contribution validation issue introduced by F5G.")]
    internal readonly struct LocalContributionValidationIssue
    {
        public LocalContributionValidationIssue(
            LocalContributionValidationIssueKind kind,
            string message,
            bool blocking,
            string identityText = null,
            string diagnosticLabel = null)
        {
            Kind = kind;
            Message = message.NormalizeTextOrFallback("Local contribution validation issue.");
            Blocking = blocking;
            IdentityText = identityText.NormalizeText();
            DiagnosticLabel = diagnosticLabel.NormalizeText();
        }

        public LocalContributionValidationIssueKind Kind { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public string IdentityText { get; }

        public string DiagnosticLabel { get; }

        public string Severity => Blocking ? "Error" : "Info";

        public string ToDiagnosticString()
        {
            string identity = string.IsNullOrWhiteSpace(IdentityText) ? string.Empty : $" identity='{FormatValue(IdentityText)}'";
            string label = string.IsNullOrWhiteSpace(DiagnosticLabel) ? string.Empty : $" label='{FormatValue(DiagnosticLabel)}'";
            return $"kind='{Kind}' severity='{Severity}'{identity}{label} message='{FormatValue(Message)}'";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }
    }
}
