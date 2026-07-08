using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Diagnostic entry emitted by passive PlayerView topology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49H passive PlayerView topology validation issue.")]
    public readonly struct PlayerViewTopologyIssue : IEquatable<PlayerViewTopologyIssue>
    {
        public PlayerViewTopologyIssue(
            PlayerViewTopologyIssueKind kind,
            string playerSlotIdText,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(PlayerViewTopologyIssueKind), kind) || kind == PlayerViewTopologyIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "PlayerView topology issue kind must be explicit.");
            }

            Kind = kind;
            PlayerSlotIdText = playerSlotIdText.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerViewTopologyIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public PlayerViewTopologyIssueKind Kind { get; }

        public string PlayerSlotIdText { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(PlayerViewTopologyIssue other)
        {
            return Kind == other.Kind
                && string.Equals(PlayerSlotIdText, other.PlayerSlotIdText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerViewTopologyIssue other && Equals(other);
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

        public static PlayerViewTopologyIssue BlockingIssue(
            PlayerViewTopologyIssueKind kind,
            string playerSlotIdText,
            string source,
            string message)
        {
            return new PlayerViewTopologyIssue(kind, playerSlotIdText, source, message, true);
        }

        public static PlayerViewTopologyIssue NonBlockingIssue(
            PlayerViewTopologyIssueKind kind,
            string playerSlotIdText,
            string source,
            string message)
        {
            return new PlayerViewTopologyIssue(kind, playerSlotIdText, source, message, false);
        }
    }
}
