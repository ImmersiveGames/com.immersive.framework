using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed scope identity for the lifecycle scope that keeps a camera request eligible.
    /// It does not identify a request owner; see CameraRequestOwnerScopeId.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequestLifetimeScopeId : IEquatable<CameraRequestLifetimeScopeId>
    {
        public CameraRequestLifetimeScopeId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraRequestLifetimeScopeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraRequestLifetimeScopeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CameraRequestLifetimeScopeId left, CameraRequestLifetimeScopeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CameraRequestLifetimeScopeId left, CameraRequestLifetimeScopeId right)
        {
            return !left.Equals(right);
        }
    }
}
