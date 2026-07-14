using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Stable identity for one reusable Actor product Profile.
    /// This is not a concrete runtime ActorId, prefab path, asset name or PlayerSlotId.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3H immutable Actor Profile identity primitive.")]
    public readonly struct ActorProfileId : IFrameworkIdentity, IEquatable<ActorProfileId>
    {
        private readonly FrameworkIdentityValue _value;

        public ActorProfileId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ActorProfileId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Actor Profile id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ActorProfile;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(ActorProfileId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorProfileId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ActorProfileId From(string value)
        {
            return new ActorProfileId(value);
        }

        public static bool operator ==(ActorProfileId left, ActorProfileId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorProfileId left, ActorProfileId right)
        {
            return !left.Equals(right);
        }
    }
}
