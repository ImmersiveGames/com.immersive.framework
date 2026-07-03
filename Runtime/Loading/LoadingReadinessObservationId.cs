using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for one Loading readiness observation.
    /// This is not an Activity id, Transition id, scene path, UI label or gameplay object id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22G Loading readiness observation identity primitive; observation only.")]
    public readonly struct LoadingReadinessObservationId : IFrameworkIdentity, IEquatable<LoadingReadinessObservationId>
    {
        private readonly FrameworkIdentityValue _value;

        public LoadingReadinessObservationId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public LoadingReadinessObservationId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Loading readiness observation id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Loading;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(LoadingReadinessObservationId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingReadinessObservationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LoadingReadinessObservationId From(string value)
        {
            return new LoadingReadinessObservationId(value);
        }

        public static bool operator ==(LoadingReadinessObservationId left, LoadingReadinessObservationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingReadinessObservationId left, LoadingReadinessObservationId right)
        {
            return !left.Equals(right);
        }
    }
}
