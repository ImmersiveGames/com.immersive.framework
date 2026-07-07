using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Identifies the lifecycle scope that authored a framework camera rig.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public enum FrameworkCameraScope
    {
        Unknown = 0,
        DefaultFallback = 10,
        Route = 20,
        Activity = 30
    }
}
