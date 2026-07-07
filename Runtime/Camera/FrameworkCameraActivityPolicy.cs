using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Defines how Activity camera authoring interacts with Route camera fallback.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public enum FrameworkCameraActivityPolicy
    {
        UseOwnOrRoute = 0,
        UseOwnOrRetainActivityUntilRouteExit = 1,
        UseRoute = 2
    }
}
