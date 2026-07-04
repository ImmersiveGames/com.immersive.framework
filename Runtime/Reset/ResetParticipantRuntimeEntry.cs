using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Internal. Runtime participant invocation entry resolved from ResetRegistry for ResetExecutor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "preview.12C ResetRegistry participant execution entry.")]
    internal readonly struct ResetParticipantRuntimeEntry : IEquatable<ResetParticipantRuntimeEntry>
    {
        internal ResetParticipantRuntimeEntry(
            ResetRegistrationHandle handle,
            ResetParticipantDescriptor descriptor,
            IResetParticipant participant)
        {
            Handle = handle;
            Descriptor = descriptor;
            Participant = participant;
        }

        internal ResetRegistrationHandle Handle { get; }

        internal ResetParticipantDescriptor Descriptor { get; }

        internal IResetParticipant Participant { get; }

        internal bool IsValid => Handle.IsParticipant && Descriptor.IsValid && Participant != null;

        public bool Equals(ResetParticipantRuntimeEntry other)
        {
            return Handle.Equals(other.Handle) && Descriptor.Equals(other.Descriptor) && ReferenceEquals(Participant, other.Participant);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetParticipantRuntimeEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Handle.GetHashCode();
                hashCode = hashCode * 397 ^ Descriptor.GetHashCode();
                hashCode = hashCode * 397 ^ (Participant != null ? Participant.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
