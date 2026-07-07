using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Describes the role that produced the currently selected framework camera rig.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public enum FrameworkCameraRigRole
    {
        Unknown = 0,
        DefaultFallback = 10,
        Route = 20,
        Activity = 30,
        RetainedActivity = 40
    }
}
