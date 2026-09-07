using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed publisher identity for a camera request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public readonly struct CameraRequestOwner
    {
        public CameraRequestOwner(CameraRequestOwnerKind kind, string logicalOwnerId)
        {
            Kind = kind;
            LogicalOwnerId = logicalOwnerId.NormalizeText();
        }

        public CameraRequestOwnerKind Kind { get; }

        public string LogicalOwnerId { get; }

        public bool IsValid =>
            Kind != CameraRequestOwnerKind.Undefined &&
            !string.IsNullOrWhiteSpace(LogicalOwnerId);

        public override string ToString()
        {
            return IsValid ? $"{Kind}:{LogicalOwnerId}" : "Undefined";
        }
    }
}
