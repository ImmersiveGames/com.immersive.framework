using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Ordered passive entry pairing one Cycle Reset participant with its descriptor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset participant entry; no execution side effects.")]
    public readonly struct CycleResetParticipantEntry : IEquatable<CycleResetParticipantEntry>
    {
        public CycleResetParticipantEntry(
            ICycleResetParticipant participant,
            CycleResetParticipantDescriptor descriptor,
            int sourceIndex)
        {
            Participant = participant ?? throw new ArgumentNullException(nameof(participant), "Cycle Reset participant entry requires a participant.");

            if (!descriptor.IsValid)
            {
                throw new ArgumentException("Cycle Reset participant entry requires a valid descriptor.", nameof(descriptor));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Cycle Reset participant source index cannot be negative.");
            }

            Descriptor = descriptor;
            SourceIndex = sourceIndex;
        }

        public ICycleResetParticipant Participant { get; }

        public CycleResetParticipantDescriptor Descriptor { get; }

        public int SourceIndex { get; }

        public CycleResetParticipantId ParticipantId => Descriptor.ParticipantId;

        public CycleResetScope ParticipantScope => Descriptor.ParticipantScope;

        public CycleResetParticipantRequiredness Requiredness => Descriptor.Requiredness;

        public bool IsRequired => Descriptor.IsRequired;

        public bool IsOptional => Descriptor.IsOptional;

        public int Order => Descriptor.Order;

        public string DisplayName => Descriptor.DisplayName;

        public bool IsValid => Participant != null && Descriptor.IsValid && SourceIndex >= 0;

        public bool Equals(CycleResetParticipantEntry other)
        {
            return ReferenceEquals(Participant, other.Participant)
                && Descriptor.Equals(other.Descriptor)
                && SourceIndex == other.SourceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetParticipantEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Participant != null ? Participant.GetHashCode() : 0;
                hashCode = hashCode * 397 ^ Descriptor.GetHashCode();
                hashCode = hashCode * 397 ^ SourceIndex;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"sourceIndex='{SourceIndex}' {Descriptor.ToDiagnosticString()}";
        }

        public static bool operator ==(CycleResetParticipantEntry left, CycleResetParticipantEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetParticipantEntry left, CycleResetParticipantEntry right)
        {
            return !left.Equals(right);
        }
    }
}
