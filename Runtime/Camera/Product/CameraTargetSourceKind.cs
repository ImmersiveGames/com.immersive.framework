
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declares where a camera target source is expected to come from.
    /// Sources must be explicit; this contract does not permit Camera.main or hierarchy-name fallback.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public enum CameraTargetSourceKind
    {
        None = 0,
        ExplicitTransform = 10,
        PlayerComposer = 20,
        PlayerSlot = 30,
        Route = 40,
        Activity = 50,
        PlayerGroup = 60
    }
}
