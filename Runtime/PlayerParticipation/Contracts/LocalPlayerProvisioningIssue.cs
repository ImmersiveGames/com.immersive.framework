using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    public sealed class LocalPlayerProvisioningIssue
    {
        internal LocalPlayerProvisioningIssue(
            LocalPlayerProvisioningIssueKind kind,
            bool blocking,
            string source,
            string message)
        {
            Kind = kind;
            Blocking = blocking;
            Source = source.NormalizeTextOrFallback(nameof(LocalPlayerProvisioningIssue));
            Message = message.NormalizeText();
        }

        public LocalPlayerProvisioningIssueKind Kind { get; }
        public bool Blocking { get; }
        public string Source { get; }
        public string Message { get; }

        public override string ToString() => $"{Kind}: {Message}";
    }
}
