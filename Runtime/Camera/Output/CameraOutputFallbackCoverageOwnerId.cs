using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>Typed identity for one explicit owner of temporary Fallback coverage on an Output.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    public readonly struct CameraOutputFallbackCoverageOwnerId : IEquatable<CameraOutputFallbackCoverageOwnerId>
    {
        public CameraOutputFallbackCoverageOwnerId(string value) { Value = value.NormalizeText(); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(CameraOutputFallbackCoverageOwnerId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CameraOutputFallbackCoverageOwnerId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CameraOutputFallbackCoverageOwnerId left, CameraOutputFallbackCoverageOwnerId right) => left.Equals(right);
        public static bool operator !=(CameraOutputFallbackCoverageOwnerId left, CameraOutputFallbackCoverageOwnerId right) => !left.Equals(right);
    }
}
