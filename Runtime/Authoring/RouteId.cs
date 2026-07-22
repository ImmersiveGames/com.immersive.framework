using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Authoring
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Stable functional identity for an authored Route.")]
    public readonly struct RouteId : IFrameworkIdentity, IEquatable<RouteId>
    {
        private readonly FrameworkIdentityValue _value;

        public RouteId(string value)
        {
            if (!IsValidText(value))
            {
                throw new ArgumentException("Route id must use lowercase alphanumeric segments separated by dots or hyphens.", nameof(value));
            }

            _value = FrameworkIdentityValue.From(value);
        }

        public RouteId(FrameworkIdentityValue value)
        {
            if (!value.IsValid || !IsValidText(value.Value))
            {
                throw new ArgumentException("Route id must be valid and canonical.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Route;
        public FrameworkIdentityValue Value => _value;
        public bool IsValid => _value.IsValid && IsValidText(_value.Value);
        public string StableText => IsValid ? _value.Value : string.Empty;

        public bool Equals(RouteId other) => _value.Equals(other._value);
        public override bool Equals(object obj) => obj is RouteId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => StableText;

        public static RouteId From(string value) => new RouteId(value);
        public static bool IsValidText(string value) => AuthoringIdentityText.IsValid(value);
        public static bool operator ==(RouteId left, RouteId right) => left.Equals(right);
        public static bool operator !=(RouteId left, RouteId right) => !left.Equals(right);
    }
}
