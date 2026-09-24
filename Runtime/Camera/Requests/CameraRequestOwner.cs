using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed publisher identity for a camera request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequestOwner
    {
        public CameraRequestOwner(CameraRequestOwnerKind kind, CameraRequestOwnerScopeId ownerScopeId)
        {
            Kind = kind;
            OwnerScopeId = ownerScopeId;
        }

        public CameraRequestOwnerKind Kind { get; }

        public CameraRequestOwnerScopeId OwnerScopeId { get; }

        public bool IsValid =>
            Kind != CameraRequestOwnerKind.Undefined &&
            OwnerScopeId.IsValid;

        public override string ToString()
        {
            return IsValid ? $"{Kind}:{OwnerScopeId}" : "Undefined";
        }
    }
}
