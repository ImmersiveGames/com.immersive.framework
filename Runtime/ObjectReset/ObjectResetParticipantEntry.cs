using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Ordered passive entry pairing one Object Reset participant with its descriptor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant entry; no execution side effects.")]
    public readonly struct ObjectResetParticipantEntry : IEquatable<ObjectResetParticipantEntry>
    {
        public ObjectResetParticipantEntry(
            IObjectResetParticipant participant,
            ObjectResetParticipantDescriptor descriptor,
            int sourceIndex)
        {
            Participant = participant ?? throw new ArgumentNullException(nameof(participant), "Object Reset participant entry requires a participant.");

            if (!descriptor.IsValid)
            {
                throw new ArgumentException("Object Reset participant entry requires a valid descriptor.", nameof(descriptor));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Object Reset participant source index cannot be negative.");
            }

            Descriptor = descriptor;
            SourceIndex = sourceIndex;
        }

        public IObjectResetParticipant Participant { get; }

        public ObjectResetParticipantDescriptor Descriptor { get; }

        public int SourceIndex { get; }

        public ObjectResetParticipantId ParticipantId => Descriptor.ParticipantId;

        public ObjectResetTarget Target => Descriptor.Target;

        public ObjectResetParticipantRequiredness Requiredness => Descriptor.Requiredness;

        public bool IsRequired => Descriptor.IsRequired;

        public bool IsOptional => Descriptor.IsOptional;

        public int Order => Descriptor.Order;

        public string DisplayName => Descriptor.DisplayName;

        public bool IsValid => Participant != null && Descriptor.IsValid && SourceIndex >= 0;

        public bool Equals(ObjectResetParticipantEntry other)
        {
            return ReferenceEquals(Participant, other.Participant)
                && Descriptor.Equals(other.Descriptor)
                && SourceIndex == other.SourceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetParticipantEntry other && Equals(other);
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

        public static bool operator ==(ObjectResetParticipantEntry left, ObjectResetParticipantEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetParticipantEntry left, ObjectResetParticipantEntry right)
        {
            return !left.Equals(right);
        }
    }
}
