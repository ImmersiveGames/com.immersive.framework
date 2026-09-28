using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>Stable identity of reusable camera behavior configuration.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-038-B reusable Camera Definition identity.")]
    public readonly struct CameraDefinitionId : IEquatable<CameraDefinitionId>
    {
        public CameraDefinitionId(string value) { Value = value.NormalizeText(); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(CameraDefinitionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CameraDefinitionId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CameraDefinitionId left, CameraDefinitionId right) => left.Equals(right);
        public static bool operator !=(CameraDefinitionId left, CameraDefinitionId right) => !left.Equals(right);
    }
}
