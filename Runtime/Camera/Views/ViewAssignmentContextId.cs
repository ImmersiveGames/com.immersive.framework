using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>Scoped assignment identity. Blank values are invalid; construction trims text.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-HARDEN-1 typed context identity.")]
    public readonly struct ViewAssignmentContextId : IEquatable<ViewAssignmentContextId>
    {
        public ViewAssignmentContextId(string value) { Value = value.NormalizeText(); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(ViewAssignmentContextId other) => string.Equals(ToString(), other.ToString(), StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ViewAssignmentContextId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(ViewAssignmentContextId left, ViewAssignmentContextId right) => left.Equals(right);
        public static bool operator !=(ViewAssignmentContextId left, ViewAssignmentContextId right) => !left.Equals(right);
    }
}
