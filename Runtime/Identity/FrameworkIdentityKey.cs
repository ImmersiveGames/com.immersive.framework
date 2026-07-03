using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Identity
{
    /// <summary>
    /// API status: Experimental. Domain-qualified framework identity key.
    /// Use this only as a primitive bridge; prefer domain-specific identity wrappers when a concrete domain exists.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal domain-qualified identity key primitive introduced by F1E.")]
    public readonly struct FrameworkIdentityKey : IEquatable<FrameworkIdentityKey>
    {
        public FrameworkIdentityKey(FrameworkIdentityDomain domain, FrameworkIdentityValue value)
        {
            if (!Enum.IsDefined(typeof(FrameworkIdentityDomain), domain) || domain == FrameworkIdentityDomain.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(domain), domain, "Framework identity domain must be explicit.");
            }

            if (!value.IsValid)
            {
                throw new ArgumentException("Framework identity value must be valid.", nameof(value));
            }

            Domain = domain;
            Value = value;
        }

        public FrameworkIdentityDomain Domain { get; }

        public FrameworkIdentityValue Value { get; }

        public bool IsValid => Domain != FrameworkIdentityDomain.Unspecified && Value.IsValid;

        public string StableText => $"{Domain}:{Value.Value}";

        public bool Equals(FrameworkIdentityKey other)
        {
            return Domain == other.Domain && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkIdentityKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)Domain * 397 ^ Value.GetHashCode();
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static FrameworkIdentityKey From(FrameworkIdentityDomain domain, string value)
        {
            return new FrameworkIdentityKey(domain, new FrameworkIdentityValue(value));
        }

        public static FrameworkIdentityKey From(IFrameworkIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            return new FrameworkIdentityKey(identity.Domain, identity.Value);
        }

        public static bool operator ==(FrameworkIdentityKey left, FrameworkIdentityKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameworkIdentityKey left, FrameworkIdentityKey right)
        {
            return !left.Equals(right);
        }
    }
}
