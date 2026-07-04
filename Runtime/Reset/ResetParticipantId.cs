using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Functional identity for one participant under a ResetSubject.
    /// Participant ids are unique only within their owning subject.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset participant identity.")]
    public readonly struct ResetParticipantId : IFrameworkIdentity, IEquatable<ResetParticipantId>
    {
        private readonly FrameworkIdentityValue _value;

        public ResetParticipantId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ResetParticipantId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Reset participant id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ObjectReset;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(ResetParticipantId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetParticipantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return IsValid ? StableText : string.Empty;
        }

        public static ResetParticipantId From(string value)
        {
            return new ResetParticipantId(value);
        }

        public static bool operator ==(ResetParticipantId left, ResetParticipantId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResetParticipantId left, ResetParticipantId right)
        {
            return !left.Equals(right);
        }
    }
}
