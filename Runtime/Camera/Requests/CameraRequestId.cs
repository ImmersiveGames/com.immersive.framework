using System;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Stable identity for one admitted camera request.
    /// </summary>
    public readonly struct CameraRequestId : IEquatable<CameraRequestId>
    {
        public CameraRequestId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraRequestId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraRequestId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CameraRequestId left, CameraRequestId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CameraRequestId left, CameraRequestId right)
        {
            return !left.Equals(right);
        }
    }
}
