using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Opaque Session-scoped reservation evidence. A token is valid only for the
    /// context, Slot and Slot revision that issued it.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F atomic Player Slot reservation token.")]
    public readonly struct PlayerSlotReservationToken : IEquatable<PlayerSlotReservationToken>
    {
        internal PlayerSlotReservationToken(
            string contextId,
            int sequence,
            PlayerSlotId playerSlotId,
            int slotRevision)
        {
            ContextId = contextId ?? string.Empty;
            Sequence = sequence;
            PlayerSlotId = playerSlotId;
            SlotRevision = slotRevision;
        }

        internal string ContextId { get; }

        public int Sequence { get; }

        public PlayerSlotId PlayerSlotId { get; }

        public int SlotRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(ContextId) &&
            Sequence > 0 &&
            PlayerSlotId.IsValid &&
            SlotRevision > 0;

        public string StableText => IsValid
            ? $"player-slot-reservation:{ContextId}:{Sequence}:{PlayerSlotId.StableText}:{SlotRevision}"
            : string.Empty;

        public bool Equals(PlayerSlotReservationToken other)
        {
            return string.Equals(ContextId, other.ContextId, StringComparison.Ordinal) &&
                Sequence == other.Sequence &&
                PlayerSlotId == other.PlayerSlotId &&
                SlotRevision == other.SlotRevision;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSlotReservationToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(ContextId ?? string.Empty);
                hash = (hash * 397) ^ Sequence;
                hash = (hash * 397) ^ PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ SlotRevision;
                return hash;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static bool operator ==(PlayerSlotReservationToken left, PlayerSlotReservationToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerSlotReservationToken left, PlayerSlotReservationToken right)
        {
            return !left.Equals(right);
        }
    }
}
