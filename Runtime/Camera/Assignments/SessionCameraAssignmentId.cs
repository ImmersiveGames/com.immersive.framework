using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>Stable identity of a Session-owned camera policy assignment.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-038-B Session Camera Assignment identity.")]
    public readonly struct SessionCameraAssignmentId : IEquatable<SessionCameraAssignmentId>
    {
        public SessionCameraAssignmentId(string value) { Value = value.NormalizeText(); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(SessionCameraAssignmentId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SessionCameraAssignmentId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(SessionCameraAssignmentId left, SessionCameraAssignmentId right) => left.Equals(right);
        public static bool operator !=(SessionCameraAssignmentId left, SessionCameraAssignmentId right) => !left.Equals(right);
    }
}
