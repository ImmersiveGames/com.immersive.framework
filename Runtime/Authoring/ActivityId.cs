using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Authoring
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Stable functional identity for an authored Activity.")]
    public readonly struct ActivityId : IFrameworkIdentity, IEquatable<ActivityId>
    {
        private readonly FrameworkIdentityValue _value;

        public ActivityId(string value)
        {
            if (!IsValidText(value))
            {
                throw new ArgumentException("Activity id must use lowercase alphanumeric segments separated by dots or hyphens.", nameof(value));
            }

            _value = FrameworkIdentityValue.From(value);
        }

        public ActivityId(FrameworkIdentityValue value)
        {
            if (!value.IsValid || !IsValidText(value.Value))
            {
                throw new ArgumentException("Activity id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Activity;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid && IsValidText(_value.Value);

        public string StableText => IsValid ? _value.Value : string.Empty;

        public bool Equals(ActivityId other) => _value.Equals(other._value);

        public override bool Equals(object obj) => obj is ActivityId other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => StableText;

        public static ActivityId From(string value) => new ActivityId(value);

        public static bool IsValidText(string value) => AuthoringIdentityText.IsValid(value);

        public static bool operator ==(ActivityId left, ActivityId right) => left.Equals(right);

        public static bool operator !=(ActivityId left, ActivityId right) => !left.Equals(right);
    }
}
