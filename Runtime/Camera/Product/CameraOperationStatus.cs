
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Shared status for camera authoring operations and target resolution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public enum CameraOperationStatus
    {
        NotRun = 0,
        Succeeded = 10,
        SucceededWithWarnings = 20,
        Blocked = 30
    }
}
