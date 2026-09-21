using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Stable authored identity for one reusable Camera Presentation definition.
    /// It does not identify a live runtime occurrence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-032-B stable authored Camera Presentation identity.")]
    public readonly struct CameraPresentationId : IEquatable<CameraPresentationId>
    {
        public CameraPresentationId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraPresentationId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CameraPresentationId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(
            CameraPresentationId left,
            CameraPresentationId right) =>
            left.Equals(right);

        public static bool operator !=(
            CameraPresentationId left,
            CameraPresentationId right) =>
            !left.Equals(right);
    }
}
