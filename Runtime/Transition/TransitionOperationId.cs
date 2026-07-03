using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Stable identity for one logical Transition operation.
    /// This is not a scene name, route name, activity name, GameObject name or visual effect id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B Transition operation identity primitive; passive operation identity only.")]
    public readonly struct TransitionOperationId : IFrameworkIdentity, IEquatable<TransitionOperationId>
    {
        private readonly FrameworkIdentityValue _value;

        public TransitionOperationId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public TransitionOperationId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Transition operation id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Transition;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(TransitionOperationId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionOperationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static TransitionOperationId From(string value)
        {
            return new TransitionOperationId(value);
        }

        public static bool operator ==(TransitionOperationId left, TransitionOperationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionOperationId left, TransitionOperationId right)
        {
            return !left.Equals(right);
        }
    }
}
