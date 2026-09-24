using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Stable identity for one admitted camera request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
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
