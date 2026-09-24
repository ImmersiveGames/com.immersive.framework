using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Experimental. Explicit id for local content inside its owning local identity scope.
    /// This value must be authored or provided explicitly; it must not be inferred from object names or paths.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Explicit local content id introduced by F5B.")]
    public readonly struct LocalContentId : IFrameworkIdentity, IEquatable<LocalContentId>
    {
        private readonly FrameworkIdentityValue _value;

        public LocalContentId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public LocalContentId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Local content id must be explicit and valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Local;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(LocalContentId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalContentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LocalContentId From(string value)
        {
            return new LocalContentId(value);
        }

        public static bool operator ==(LocalContentId left, LocalContentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalContentId left, LocalContentId right)
        {
            return !left.Equals(right);
        }
    }
}
