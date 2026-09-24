using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed lifetime evidence carried by a camera request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequestLifetime
    {
        public CameraRequestLifetime(CameraRequestLifetimeKind kind, CameraRequestLifetimeScopeId scopeId)
        {
            Kind = kind;
            ScopeId = scopeId;
        }

        public CameraRequestLifetimeKind Kind { get; }

        public CameraRequestLifetimeScopeId ScopeId { get; }

        public bool IsValid =>
            Kind != CameraRequestLifetimeKind.Undefined &&
            ScopeId.IsValid;

        public override string ToString()
        {
            return IsValid ? $"{Kind}:{ScopeId}" : "Undefined";
        }
    }
}
