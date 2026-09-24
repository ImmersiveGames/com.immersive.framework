using System;
using Immersive.Framework.Identity;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Stable identity for a normalized Pause input action.
    /// This is not a Unity Input System action name, action map, device binding or UI object name.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
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
