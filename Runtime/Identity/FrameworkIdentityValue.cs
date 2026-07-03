using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Identity
{
    /// <summary>
    /// API status: Experimental. Validated identity payload used by typed framework identity wrappers.
    /// It is not a label and should not carry human-facing display text.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal validated identity value primitive introduced by F1E.")]
    public readonly struct FrameworkIdentityValue : IEquatable<FrameworkIdentityValue>
    {
        private readonly string _value;

        public FrameworkIdentityValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Framework identity value cannot be null, empty or whitespace.", nameof(value));
            }

            _value = value.NormalizeText();
        }

        public string Value => _value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public bool Equals(FrameworkIdentityValue other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkIdentityValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static FrameworkIdentityValue From(string value)
        {
            return new FrameworkIdentityValue(value);
        }

        public static bool operator ==(FrameworkIdentityValue left, FrameworkIdentityValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameworkIdentityValue left, FrameworkIdentityValue right)
        {
            return !left.Equals(right);
        }
    }
}
