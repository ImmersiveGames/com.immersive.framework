using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Stable identity for one logical Pause request.
    /// This is not an input action name, UI object name, menu id or Time.timeScale policy id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B Pause request identity primitive; passive request identity only.")]
    public readonly struct PauseRequestId : IFrameworkIdentity, IEquatable<PauseRequestId>
    {
        private readonly FrameworkIdentityValue _value;

        public PauseRequestId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public PauseRequestId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Pause request id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Pause;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(PauseRequestId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseRequestId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static PauseRequestId From(string value)
        {
            return new PauseRequestId(value);
        }

        public static bool operator ==(PauseRequestId left, PauseRequestId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseRequestId left, PauseRequestId right)
        {
            return !left.Equals(right);
        }
    }
}
