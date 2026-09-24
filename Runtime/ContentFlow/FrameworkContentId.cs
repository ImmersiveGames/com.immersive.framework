using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Typed content id local to a content owner/scope/kind identity.
    /// This is not a Unity asset path, scene name or display label.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal typed content id introduced by F1F.")]
    public readonly struct FrameworkContentId : IFrameworkIdentity, IEquatable<FrameworkContentId>
    {
        private readonly FrameworkIdentityValue _value;

        public FrameworkContentId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public FrameworkContentId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Framework content id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Content;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(FrameworkContentId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkContentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static FrameworkContentId From(string value)
        {
            return new FrameworkContentId(value);
        }

        public static bool operator ==(FrameworkContentId left, FrameworkContentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameworkContentId left, FrameworkContentId right)
        {
            return !left.Equals(right);
        }
    }
}
