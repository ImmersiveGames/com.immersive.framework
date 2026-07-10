using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// API status: Experimental. Defines whether an Activity owns an explicit output or leaves Route output unchanged.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F46B framework-owned camera ownership skeleton.")]
    public enum FrameworkCameraActivityPolicy
    {
        UseOwn = 0,
        UseRoute = 1
    }
}
