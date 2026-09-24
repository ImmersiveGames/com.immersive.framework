using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for one step inside a Loading operation.
    /// This is not a scene path, GameObject name, Transition step id or UI label.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading step identity primitive; no execution owner.")]
    public readonly struct LoadingStepId : IFrameworkIdentity, IEquatable<LoadingStepId>
    {
        private readonly FrameworkIdentityValue _value;

        public LoadingStepId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public LoadingStepId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Loading step id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Loading;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(LoadingStepId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingStepId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LoadingStepId From(string value)
        {
            return new LoadingStepId(value);
        }

        public static bool operator ==(LoadingStepId left, LoadingStepId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingStepId left, LoadingStepId right)
        {
            return !left.Equals(right);
        }
    }
}
