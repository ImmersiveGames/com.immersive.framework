using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-028-C immutable normalized Camera Output presentation rect.")]
    public readonly struct CameraOutputPresentationRect :
        IEquatable<CameraOutputPresentationRect>
    {
        public CameraOutputPresentationRect(
            float x,
            float y,
            float width,
            float height)
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
            IsFinite(X) &&
            IsFinite(Y) &&
            IsFinite(Width) &&
            IsFinite(Height) &&
            X >= 0f &&
            Y >= 0f &&
            Width > 0f &&
            Height > 0f &&
            X + Width <= 1f &&
            Y + Height <= 1f;

        public bool Equals(CameraOutputPresentationRect other) =>
            X.Equals(other.X) &&
            Y.Equals(other.Y) &&
            Width.Equals(other.Width) &&
            Height.Equals(other.Height);

        public override bool Equals(object obj) =>
            obj is CameraOutputPresentationRect other && Equals(other);

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

        public override string ToString() =>
            $"({X}, {Y}, {Width}, {Height})";

        public static bool operator ==(
            CameraOutputPresentationRect left,
            CameraOutputPresentationRect right) => left.Equals(right);

        public static bool operator !=(
            CameraOutputPresentationRect left,
            CameraOutputPresentationRect right) => !left.Equals(right);

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
