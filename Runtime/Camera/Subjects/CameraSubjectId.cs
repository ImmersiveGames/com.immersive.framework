using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit identity for something that may be observed by Camera presentation.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-A typed Camera Subject identity.")]
    public readonly struct CameraSubjectId : IEquatable<CameraSubjectId>
    {
        public CameraSubjectId(string value) { Value = value.NormalizeText(); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(CameraSubjectId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CameraSubjectId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CameraSubjectId left, CameraSubjectId right) => left.Equals(right);
        public static bool operator !=(CameraSubjectId left, CameraSubjectId right) => !left.Equals(right);
    }
}
