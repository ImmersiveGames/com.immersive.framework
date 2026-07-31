
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Describes the presentation behavior configured by a materialized camera rig.
    /// This is authoring intent only and never selects a runtime camera.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public enum CameraRigPresentationIntent
    {
        Undefined = 0,
        Follow = 10
    }
}
