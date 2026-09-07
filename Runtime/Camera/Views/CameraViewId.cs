using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit identity for one logical Camera observation composition.
    /// It is independent from Camera output and Player identity.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B typed logical Camera View identity.")]
    public readonly struct CameraViewId : IEquatable<CameraViewId>
    {
        public CameraViewId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraViewId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraViewId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CameraViewId left, CameraViewId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CameraViewId left, CameraViewId right)
        {
            return !left.Equals(right);
        }
    }
}
