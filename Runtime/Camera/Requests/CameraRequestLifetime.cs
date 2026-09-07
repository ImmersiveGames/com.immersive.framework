using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed lifetime evidence carried by a camera request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequestLifetime
    {
        public CameraRequestLifetime(CameraRequestLifetimeKind kind, string scopeId)
        {
            Kind = kind;
            ScopeId = scopeId.NormalizeText();
        }

        public CameraRequestLifetimeKind Kind { get; }

        public string ScopeId { get; }

        public bool IsValid =>
            Kind != CameraRequestLifetimeKind.Undefined &&
            !string.IsNullOrWhiteSpace(ScopeId);

        public override string ToString()
        {
            return IsValid ? $"{Kind}:{ScopeId}" : "Undefined";
        }
    }
}
