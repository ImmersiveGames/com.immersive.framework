using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Opaque handle for deterministic unregister of reset subjects and participants.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset registration handle.")]
    public readonly struct ResetRegistrationHandle : IEquatable<ResetRegistrationHandle>
    {
        public ResetRegistrationHandle(ResetRegistrationKind kind, int value)
        {
            if (!Enum.IsDefined(typeof(ResetRegistrationKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Reset registration kind must be defined.");
            }

            Kind = kind;
            Value = value;
        }

        public ResetRegistrationKind Kind { get; }

        public int Value { get; }

        public bool IsValid => Kind != ResetRegistrationKind.Unknown && Value > 0;

        public bool IsSubject => Kind == ResetRegistrationKind.Subject && IsValid;

        public bool IsParticipant => Kind == ResetRegistrationKind.Participant && IsValid;

        public bool Equals(ResetRegistrationHandle other)
        {
            return Kind == other.Kind && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ResetRegistrationHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)Kind * 397 ^ Value;
            }
        }

        public override string ToString()
        {
            return IsValid ? $"{Kind}:{Value}" : "<invalid>";
        }

        public static bool operator ==(ResetRegistrationHandle left, ResetRegistrationHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResetRegistrationHandle left, ResetRegistrationHandle right)
        {
            return !left.Equals(right);
        }
    }
}
