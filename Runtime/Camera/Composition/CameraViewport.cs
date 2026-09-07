using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>Normalized physical viewport owned by Camera composition.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-H explicit normalized Camera viewport.")]
    public readonly struct CameraViewport : IEquatable<CameraViewport>
    {
        public CameraViewport(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public bool IsValid =>
            IsFinite(X) && IsFinite(Y) && IsFinite(Width) && IsFinite(Height) &&
            X >= 0f && Y >= 0f && Width > 0f && Height > 0f &&
            X + Width <= 1f && Y + Height <= 1f;

        public Rect ToRect() => new Rect(X, Y, Width, Height);

        public bool Equals(CameraViewport other) =>
            X.Equals(other.X) && Y.Equals(other.Y) &&
            Width.Equals(other.Width) && Height.Equals(other.Height);

        public override bool Equals(object obj) => obj is CameraViewport other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ Width.GetHashCode();
                return (hash * 397) ^ Height.GetHashCode();
            }
        }
        public static bool operator ==(CameraViewport left, CameraViewport right) => left.Equals(right);
        public static bool operator !=(CameraViewport left, CameraViewport right) => !left.Equals(right);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
