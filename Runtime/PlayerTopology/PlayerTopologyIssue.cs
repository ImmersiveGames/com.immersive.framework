using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerTopology
{
    /// <summary>
    /// API status: Experimental. Diagnostic entry emitted by passive PlayerTopology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49F passive PlayerTopology validation issue.")]
    public readonly struct PlayerTopologyIssue : IEquatable<PlayerTopologyIssue>
    {
        public PlayerTopologyIssue(
            PlayerTopologyIssueKind kind,
            string playerSlotIdText,
            string actorIdText,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(PlayerTopologyIssueKind), kind) || kind == PlayerTopologyIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "PlayerTopology issue kind must be explicit.");
            }

            Kind = kind;
            PlayerSlotIdText = playerSlotIdText.NormalizeText();
            ActorIdText = actorIdText.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerTopologyIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public PlayerTopologyIssueKind Kind { get; }

        public string PlayerSlotIdText { get; }

        public string ActorIdText { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(PlayerTopologyIssue other)
        {
            return Kind == other.Kind
                && string.Equals(PlayerSlotIdText, other.PlayerSlotIdText, StringComparison.Ordinal)
                && string.Equals(ActorIdText, other.ActorIdText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerTopologyIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(PlayerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ActorIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(ActorIdText)
                ? $"{Kind}:{PlayerSlotIdText}"
                : $"{Kind}:{PlayerSlotIdText}->{ActorIdText}";
        }

        public static PlayerTopologyIssue BlockingIssue(
            PlayerTopologyIssueKind kind,
            string playerSlotIdText,
            string actorIdText,
            string source,
            string message)
        {
            return new PlayerTopologyIssue(kind, playerSlotIdText, actorIdText, source, message, true);
        }

        public static PlayerTopologyIssue NonBlockingIssue(
            PlayerTopologyIssueKind kind,
            string playerSlotIdText,
            string actorIdText,
            string source,
            string message)
        {
            return new PlayerTopologyIssue(kind, playerSlotIdText, actorIdText, source, message, false);
        }
    }
}
