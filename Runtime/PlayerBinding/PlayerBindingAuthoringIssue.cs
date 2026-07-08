using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Passive issue emitted while validating authored Player binding evidence.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F50A passive Player binding authoring issue.")]
    public readonly struct PlayerBindingAuthoringIssue : IEquatable<PlayerBindingAuthoringIssue>
    {
        public PlayerBindingAuthoringIssue(
            PlayerBindingAuthoringIssueKind kind,
            string objectName,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(PlayerBindingAuthoringIssueKind), kind) || kind == PlayerBindingAuthoringIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Player binding authoring issue kind must be explicit.");
            }

            Kind = kind;
            ObjectName = objectName.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerBindingAuthoringIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public PlayerBindingAuthoringIssueKind Kind { get; }

        public string ObjectName { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(PlayerBindingAuthoringIssue other)
        {
            return Kind == other.Kind
                && string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerBindingAuthoringIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ObjectName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(ObjectName)
                ? Kind.ToString()
                : $"{Kind}:{ObjectName}";
        }

        public static PlayerBindingAuthoringIssue BlockingIssue(
            PlayerBindingAuthoringIssueKind kind,
            string objectName,
            string source,
            string message)
        {
            return new PlayerBindingAuthoringIssue(kind, objectName, source, message, true);
        }

        public static PlayerBindingAuthoringIssue NonBlockingIssue(
            PlayerBindingAuthoringIssueKind kind,
            string objectName,
            string source,
            string message)
        {
            return new PlayerBindingAuthoringIssue(kind, objectName, source, message, false);
        }
    }
}
