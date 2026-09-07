
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Shared status for camera authoring operations and target resolution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public enum CameraOperationStatus
    {
        NotRun = 0,
        Succeeded = 10,
        SucceededWithWarnings = 20,
        Blocked = 30
    }
}
