using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. One structured local contribution discovery issue.
    /// F5D treats all emitted issues as blocking authoring errors; optional/required policy is deferred.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Structured local contribution discovery issue introduced by F5D.")]
    internal readonly struct LocalContributionDiscoveryIssue
    {
        public LocalContributionDiscoveryIssue(
            LocalContributionDiscoveryIssueKind kind,
            string message,
            string identityText = null,
            string sceneName = null,
            string objectName = null)
        {
            Kind = kind;
            Message = message.NormalizeTextOrFallback("Local contribution discovery issue.");
            IdentityText = identityText.NormalizeText();
            SceneName = sceneName.NormalizeText();
            ObjectName = objectName.NormalizeText();
        }

        public LocalContributionDiscoveryIssueKind Kind { get; }

        public string Message { get; }

        public string IdentityText { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string ToDiagnosticString()
        {
            string identity = string.IsNullOrWhiteSpace(IdentityText) ? string.Empty : $" identity='{FormatValue(IdentityText)}'";
            string scene = string.IsNullOrWhiteSpace(SceneName) ? string.Empty : $" scene='{FormatValue(SceneName)}'";
            string obj = string.IsNullOrWhiteSpace(ObjectName) ? string.Empty : $" object='{FormatValue(ObjectName)}'";
            return $"kind='{Kind}'{identity}{scene}{obj} message='{FormatValue(Message)}'";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\'");
        }
    }
}
