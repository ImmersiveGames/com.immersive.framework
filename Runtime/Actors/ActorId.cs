using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for an actor known by the framework.
    /// This is not a GameObject name, prefab path, PlayerInput user id, device id or save slot id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A actor identity primitive.")]
    public readonly struct ActorId : IFrameworkIdentity, IEquatable<ActorId>
    {
        private readonly FrameworkIdentityValue _value;

        public ActorId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ActorId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Actor id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Actor;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(ActorId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ActorId From(string value)
        {
            return new ActorId(value);
        }

        public static bool operator ==(ActorId left, ActorId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorId left, ActorId right)
        {
            return !left.Equals(right);
        }
    }
}
