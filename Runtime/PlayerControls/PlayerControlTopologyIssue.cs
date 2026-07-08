using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Diagnostic entry emitted by passive PlayerControl topology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49J passive PlayerControl topology validation issue.")]
    public readonly struct PlayerControlTopologyIssue : IEquatable<PlayerControlTopologyIssue>
    {
        public PlayerControlTopologyIssue(
            PlayerControlTopologyIssueKind kind,
            string playerSlotIdText,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(PlayerControlTopologyIssueKind), kind) || kind == PlayerControlTopologyIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "PlayerControl topology issue kind must be explicit.");
            }

            Kind = kind;
            PlayerSlotIdText = playerSlotIdText.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerControlTopologyIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public PlayerControlTopologyIssueKind Kind { get; }

        public string PlayerSlotIdText { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(PlayerControlTopologyIssue other)
        {
            return Kind == other.Kind
                && string.Equals(PlayerSlotIdText, other.PlayerSlotIdText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerControlTopologyIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(PlayerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(PlayerSlotIdText)
                ? Kind.ToString()
                : $"{Kind}:{PlayerSlotIdText}";
        }

        public static PlayerControlTopologyIssue BlockingIssue(
            PlayerControlTopologyIssueKind kind,
            string playerSlotIdText,
            string source,
            string message)
        {
            return new PlayerControlTopologyIssue(kind, playerSlotIdText, source, message, true);
        }

        public static PlayerControlTopologyIssue NonBlockingIssue(
            PlayerControlTopologyIssueKind kind,
            string playerSlotIdText,
            string source,
            string message)
        {
            return new PlayerControlTopologyIssue(kind, playerSlotIdText, source, message, false);
        }
    }
}
