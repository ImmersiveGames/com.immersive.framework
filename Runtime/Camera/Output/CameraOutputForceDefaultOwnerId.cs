using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed identity for one explicit system/output-presentation owner currently asserting
    /// Force Default on a Camera Output. It is independent from camera request arbitration:
    /// it is not a CameraRequestOwner, not a lifecycle owner and not a CameraOutputId.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public readonly struct CameraOutputForceDefaultOwnerId : IEquatable<CameraOutputForceDefaultOwnerId>
    {
        public CameraOutputForceDefaultOwnerId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraOutputForceDefaultOwnerId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraOutputForceDefaultOwnerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CameraOutputForceDefaultOwnerId left, CameraOutputForceDefaultOwnerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CameraOutputForceDefaultOwnerId left, CameraOutputForceDefaultOwnerId right)
        {
            return !left.Equals(right);
        }
    }
}
