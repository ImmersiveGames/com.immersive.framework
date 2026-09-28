using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Identity for one exact joined Player lifetime; it is distinct from its Slot.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-038-B exact Player occurrence identity.")]
    public readonly struct PlayerOccurrenceId : IEquatable<PlayerOccurrenceId>
    {
        public PlayerOccurrenceId(string value) { Value = value.NormalizeText(); }
        internal static PlayerOccurrenceId Create(
            string sessionContextId,
            int sequence,
            PlayerSlotId playerSlotId) =>
            !string.IsNullOrWhiteSpace(sessionContextId) && sequence > 0 && playerSlotId.IsValid
                ? new PlayerOccurrenceId($"player-occurrence:{sessionContextId}:{sequence}:{playerSlotId.StableText}")
                : default;
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(PlayerOccurrenceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PlayerOccurrenceId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PlayerOccurrenceId left, PlayerOccurrenceId right) => left.Equals(right);
        public static bool operator !=(PlayerOccurrenceId left, PlayerOccurrenceId right) => !left.Equals(right);
    }
}
