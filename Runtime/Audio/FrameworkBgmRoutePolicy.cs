using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Defines the complete BGM intent published by a Route.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "BGM-ROUTE-POLICY-1 Route BGM intent policy.")]
    public enum FrameworkBgmRoutePolicy
    {
        /// <summary>
        /// Play the Route cue. A cue is required.
        /// </summary>
        PlayOwn = 0,

        /// <summary>
        /// Publish no request and preserve the confirmed presentation.
        /// </summary>
        PreserveCurrent = 1,

        /// <summary>
        /// Explicitly request silence/stop.
        /// </summary>
        Silence = 2
    }
}
