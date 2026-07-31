
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit target source contract for authoring/runtime components that can provide camera follow/look-at targets.
    /// Implementations must not silently fall back to Camera.main, object names or hierarchy paths.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public interface ICameraTargetSource
    {
        CameraTargetSourceKind TargetSourceKind { get; }

        CameraTargetResolveResult ResolveCameraTargets(
            CameraTargetRequirement followRequirement,
            CameraTargetRequirement lookAtRequirement);
    }
}
