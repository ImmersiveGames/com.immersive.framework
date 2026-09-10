using System;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed scope identity for the domain owner publishing camera request intent.
    /// It does not identify a lifetime scope; see CameraRequestLifetimeScopeId.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequestOwnerScopeId : IEquatable<CameraRequestOwnerScopeId>
    {
        public CameraRequestOwnerScopeId(string value)
        {
            Value = value.NormalizeText();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CameraRequestOwnerScopeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CameraRequestOwnerScopeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(CameraRequestOwnerScopeId left, CameraRequestOwnerScopeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CameraRequestOwnerScopeId left, CameraRequestOwnerScopeId right)
        {
            return !left.Equals(right);
        }
    }
}
