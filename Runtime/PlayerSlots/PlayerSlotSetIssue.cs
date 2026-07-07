using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Diagnostic entry emitted by PlayerSlot validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot validation issue.")]
    public readonly struct PlayerSlotSetIssue : IEquatable<PlayerSlotSetIssue>
    {
        public PlayerSlotSetIssue(
            PlayerSlotSetIssueKind kind,
            string playerSlotIdText,
            string occupiedActorIdText,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(PlayerSlotSetIssueKind), kind) || kind == PlayerSlotSetIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "PlayerSlot issue kind must be explicit.");
            }

            Kind = kind;
            PlayerSlotIdText = playerSlotIdText.NormalizeText();
            OccupiedActorIdText = occupiedActorIdText.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerSlotSetIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public PlayerSlotSetIssueKind Kind { get; }

        public string PlayerSlotIdText { get; }

        public string OccupiedActorIdText { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(PlayerSlotSetIssue other)
        {
            return Kind == other.Kind
                && string.Equals(PlayerSlotIdText, other.PlayerSlotIdText, StringComparison.Ordinal)
                && string.Equals(OccupiedActorIdText, other.OccupiedActorIdText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSlotSetIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(PlayerSlotIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(OccupiedActorIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(OccupiedActorIdText)
                ? $"{Kind}:{PlayerSlotIdText}"
                : $"{Kind}:{PlayerSlotIdText}->{OccupiedActorIdText}";
        }

        internal static PlayerSlotSetIssue BlockingIssue(
            PlayerSlotSetIssueKind kind,
            string playerSlotIdText,
            string occupiedActorIdText,
            string source,
            string message)
        {
            return new PlayerSlotSetIssue(kind, playerSlotIdText, occupiedActorIdText, source, message, true);
        }

        internal static PlayerSlotSetIssue NonBlockingIssue(
            PlayerSlotSetIssueKind kind,
            string playerSlotIdText,
            string occupiedActorIdText,
            string source,
            string message)
        {
            return new PlayerSlotSetIssue(kind, playerSlotIdText, occupiedActorIdText, source, message, false);
        }
    }
}
