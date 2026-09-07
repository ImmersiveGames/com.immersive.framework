using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit identity of the scoped producer responsible for Subject availability.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A scoped Camera Subject availability owner identity.")]
    public readonly struct CameraSubjectAvailabilityOwnerId : IEquatable<CameraSubjectAvailabilityOwnerId>
    {
        public CameraSubjectAvailabilityOwnerId(string value) { Value = value.NormalizeText(); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(CameraSubjectAvailabilityOwnerId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CameraSubjectAvailabilityOwnerId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CameraSubjectAvailabilityOwnerId left, CameraSubjectAvailabilityOwnerId right) => left.Equals(right);
        public static bool operator !=(CameraSubjectAvailabilityOwnerId left, CameraSubjectAvailabilityOwnerId right) => !left.Equals(right);
    }
}
