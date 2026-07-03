using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Stable identity for a normalized Pause input action.
    /// This is not a Unity Input System action name, action map, device binding or UI object name.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23D Pause Input action identity primitive; no concrete input asset binding.")]
    public readonly struct PauseInputActionId : IFrameworkIdentity, IEquatable<PauseInputActionId>
    {
        private readonly FrameworkIdentityValue _value;

        public PauseInputActionId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public PauseInputActionId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Pause input action id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Pause;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(PauseInputActionId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseInputActionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static PauseInputActionId From(string value)
        {
            return new PauseInputActionId(value);
        }

        public static bool operator ==(PauseInputActionId left, PauseInputActionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseInputActionId left, PauseInputActionId right)
        {
            return !left.Equals(right);
        }
    }
}
