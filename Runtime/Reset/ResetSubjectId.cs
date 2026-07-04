using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Functional identity for one reset subject.
    /// This identity is owned by the Reset module and is not an ObjectEntry identity.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset Subject identity; independent from ObjectEntry.")]
    public readonly struct ResetSubjectId : IFrameworkIdentity, IEquatable<ResetSubjectId>
    {
        private readonly FrameworkIdentityValue _value;

        public ResetSubjectId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ResetSubjectId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Reset subject id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ObjectReset;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(ResetSubjectId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetSubjectId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return IsValid ? StableText : string.Empty;
        }

        public static ResetSubjectId From(string value)
        {
            return new ResetSubjectId(value);
        }

        public static bool operator ==(ResetSubjectId left, ResetSubjectId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResetSubjectId left, ResetSubjectId right)
        {
            return !left.Equals(right);
        }
    }
}
