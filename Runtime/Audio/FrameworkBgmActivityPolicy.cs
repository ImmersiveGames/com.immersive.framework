using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Defines how Activity BGM authoring interacts with Route BGM fallback.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F47C optional framework-owned BGM adapter.")]
    public enum FrameworkBgmActivityPolicy
    {
        UseOwnOrRoute = 0,
        UseOwnOrRetainActivityUntilRouteExit = 1,
        UseRoute = 2,
        Silence = 3
    }
}
