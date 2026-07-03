using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit stable id for an authored Content Anchor.
    /// This is not a GameObject name, hierarchy path, scene name, scene path or display label.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor id primitive introduced by F7B.")]
    public readonly struct ContentAnchorId : IFrameworkIdentity, IEquatable<ContentAnchorId>
    {
        private readonly FrameworkIdentityValue _value;

        public ContentAnchorId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ContentAnchorId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Content Anchor id must be explicit and valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ContentAnchor;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(ContentAnchorId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ContentAnchorId From(string value)
        {
            return new ContentAnchorId(value);
        }

        public static bool operator ==(ContentAnchorId left, ContentAnchorId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorId left, ContentAnchorId right)
        {
            return !left.Equals(right);
        }
    }
}
