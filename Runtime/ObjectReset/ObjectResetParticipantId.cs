using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Functional identity for one Object Reset participant.
    /// This must not be derived from GameObject names, scene paths or hierarchy paths.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C typed Object Reset participant identity; no GameObject-name fallback.")]
    public readonly struct ObjectResetParticipantId : IFrameworkIdentity, IEquatable<ObjectResetParticipantId>
    {
        private readonly FrameworkIdentityValue _value;

        public ObjectResetParticipantId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ObjectResetParticipantId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Object Reset participant id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ObjectReset;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(ObjectResetParticipantId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetParticipantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ObjectResetParticipantId From(string value)
        {
            return new ObjectResetParticipantId(value);
        }

        public static bool operator ==(ObjectResetParticipantId left, ObjectResetParticipantId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetParticipantId left, ObjectResetParticipantId right)
        {
            return !left.Equals(right);
        }
    }
}
