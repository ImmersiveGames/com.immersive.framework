using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Ordered passive entry pairing one Activity Content Execution participant with its descriptor.
    /// The entry does not discover, execute, materialize, reset or mutate Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Activity Content Execution participant entry; no execution runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionParticipantEntry : IEquatable<ActivityContentExecutionParticipantEntry>
    {
        public ActivityContentExecutionParticipantEntry(
            IActivityContentExecutionParticipant participant,
            ActivityContentExecutionParticipantDescriptor descriptor,
            int sourceIndex)
        {
            Participant = participant ?? throw new ArgumentNullException(nameof(participant), "Activity content execution participant entry requires a participant.");

            if (!descriptor.IsValid)
            {
                throw new ArgumentException("Activity content execution participant entry requires a valid descriptor.", nameof(descriptor));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Activity content execution participant source index cannot be negative.");
            }

            Descriptor = descriptor;
            SourceIndex = sourceIndex;
        }

        public IActivityContentExecutionParticipant Participant { get; }

        public ActivityContentExecutionParticipantDescriptor Descriptor { get; }

        public int SourceIndex { get; }

        public RuntimeContentId ContentId => Descriptor.ContentId;

        public ActivityContentExecutionRequiredness Requiredness => Descriptor.Requiredness;

        public int Order => Descriptor.Order;

        public bool IsValid => Participant != null && Descriptor.IsValid && SourceIndex >= 0;

        public bool SupportsPhase(ActivityContentExecutionPhase phase)
        {
            return Descriptor.SupportsPhase(phase);
        }

        public bool Equals(ActivityContentExecutionParticipantEntry other)
        {
            return ReferenceEquals(Participant, other.Participant)
                && Descriptor.Equals(other.Descriptor)
                && SourceIndex == other.SourceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantEntry other && Equals(other);
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

        public static bool operator ==(ActivityContentExecutionParticipantEntry left, ActivityContentExecutionParticipantEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActivityContentExecutionParticipantEntry left, ActivityContentExecutionParticipantEntry right)
        {
            return !left.Equals(right);
        }
    }
}
