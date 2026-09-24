using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Opaque evidence for the exact current assignment of one Session Player Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-1 current Player Slot assignment token.")]
    public readonly struct PlayerSlotAssignmentToken :
        IEquatable<PlayerSlotAssignmentToken>
    {
        internal PlayerSlotAssignmentToken(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            int assignmentSequence,
            int assignmentRevision,
            PlayerHostBindingIdentity hostBindingIdentity)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            AssignmentSequence = assignmentSequence;
            AssignmentRevision = assignmentRevision;
            HostBindingIdentity = hostBindingIdentity;
        }

        internal string SessionContextId { get; }

        public PlayerSlotId PlayerSlotId { get; }

        public int AssignmentSequence { get; }

        public int AssignmentRevision { get; }

        public PlayerHostBindingIdentity HostBindingIdentity { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            AssignmentSequence > 0 &&
            AssignmentRevision > 0 &&
            HostBindingIdentity.IsValid;

        public string StableText => IsValid
            ? $"player-slot-assignment:{SessionContextId}:{PlayerSlotId.StableText}:{AssignmentSequence}:{AssignmentRevision}:{HostBindingIdentity.StableText}"
            : string.Empty;

        public bool Equals(PlayerSlotAssignmentToken other)
        {
            return string.Equals(
                    SessionContextId,
                    other.SessionContextId,
                    StringComparison.Ordinal) &&
                PlayerSlotId == other.PlayerSlotId &&
                AssignmentSequence == other.AssignmentSequence &&
                AssignmentRevision == other.AssignmentRevision &&
                HostBindingIdentity == other.HostBindingIdentity;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSlotAssignmentToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(
                    SessionContextId ?? string.Empty);
                hash = hash * 397 ^ PlayerSlotId.GetHashCode();
                hash = hash * 397 ^ AssignmentSequence;
                hash = hash * 397 ^ AssignmentRevision;
                hash = hash * 397 ^ HostBindingIdentity.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            PlayerSlotAssignmentToken left,
            PlayerSlotAssignmentToken right) => left.Equals(right);

        public static bool operator !=(
            PlayerSlotAssignmentToken left,
            PlayerSlotAssignmentToken right) => !left.Equals(right);
    }
}
