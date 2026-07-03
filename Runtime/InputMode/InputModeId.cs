using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for an InputMode posture.
    /// This is not a Unity Input System action-map string.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A InputMode identity primitive; no action-map behavior.")]
    public readonly struct InputModeId : IFrameworkIdentity, IEquatable<InputModeId>
    {
        private readonly FrameworkIdentityValue _value;

        public InputModeId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public InputModeId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("InputMode id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.InputMode;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(InputModeId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is InputModeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static InputModeId From(string value)
        {
            return new InputModeId(value);
        }

        public static bool operator ==(InputModeId left, InputModeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InputModeId left, InputModeId right)
        {
            return !left.Equals(right);
        }
    }
}
