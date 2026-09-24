using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Experimental. Stable code for a structured framework fact.
    /// The value is a diagnostic code, not a user-facing log message.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal structured diagnostics code introduced by F1C.")]
    public readonly struct FrameworkFactCode : IEquatable<FrameworkFactCode>
    {
        private readonly string _value;

        public FrameworkFactCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Framework fact code cannot be null, empty or whitespace.", nameof(value));
            }

            _value = value.Trim();
        }

        public string Value => _value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public bool Equals(FrameworkFactCode other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkFactCode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(FrameworkFactCode left, FrameworkFactCode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameworkFactCode left, FrameworkFactCode right)
        {
            return !left.Equals(right);
        }
    }
}
