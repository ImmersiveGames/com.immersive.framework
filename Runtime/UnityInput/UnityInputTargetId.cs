using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for a Unity Input target declaration.
    /// This is not a Unity action map name, InputActionAsset path, GameObject name, PlayerInput user id or player actor id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target identity primitive; declaration only.")]
    public readonly struct UnityInputTargetId : IFrameworkIdentity, IEquatable<UnityInputTargetId>
    {
        private readonly FrameworkIdentityValue _value;

        public UnityInputTargetId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public UnityInputTargetId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Unity Input target id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.UnityInput;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(UnityInputTargetId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is UnityInputTargetId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static UnityInputTargetId From(string value)
        {
            return new UnityInputTargetId(value);
        }

        public static bool operator ==(UnityInputTargetId left, UnityInputTargetId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnityInputTargetId left, UnityInputTargetId right)
        {
            return !left.Equals(right);
        }
    }
}
