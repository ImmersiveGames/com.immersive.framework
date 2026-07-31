using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed identity for one concrete camera output or viewport.
    /// It does not discover, create or select an output.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public readonly struct CameraOutputId : IEquatable<CameraOutputId>
    {
        public CameraOutputId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static CameraOutputId Main => new CameraOutputId("camera.output.main");

        public bool Equals(CameraOutputId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraOutputId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CameraOutputId left, CameraOutputId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CameraOutputId left, CameraOutputId right)
        {
            return !left.Equals(right);
        }
    }
}
