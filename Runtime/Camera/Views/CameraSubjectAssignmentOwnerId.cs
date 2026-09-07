using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>Explicit identity for the scope that owns one or more Subject assignments.</summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B Camera Subject Assignment owner identity.")]
    public readonly struct CameraSubjectAssignmentOwnerId :
        IEquatable<CameraSubjectAssignmentOwnerId>
    {
        public CameraSubjectAssignmentOwnerId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraSubjectAssignmentOwnerId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraSubjectAssignmentOwnerId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(
            CameraSubjectAssignmentOwnerId left,
            CameraSubjectAssignmentOwnerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CameraSubjectAssignmentOwnerId left,
            CameraSubjectAssignmentOwnerId right)
        {
            return !left.Equals(right);
        }
    }
}
