using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Audio
{
    /// <summary>
    /// API status: Experimental. Defines which explicit BGM intent an Activity publishes.
    /// Absence of an Activity binding publishes no BGM intent and therefore preserves the confirmed
    /// sticky presentation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "BGM-CONTINUITY-1 Activity BGM intent policy.")]
    public enum FrameworkBgmActivityPolicy
    {
        /// <summary>
        /// Play the Activity cue when authored; otherwise explicitly request the current Route cue
        /// when one is authored. If neither exists, publish no request.
        /// </summary>
        UseOwnOrRoute = 0,

        /// <summary>
        /// Play the Activity cue when authored; otherwise publish no request and preserve the
        /// current confirmed presentation. Route exit does not stop that presentation.
        /// </summary>
        UseOwnOrPreserveCurrent = 1,

        /// <summary>
        /// Explicitly request the current Route cue. If the Route has no cue, publish no request.
        /// </summary>
        UseRoute = 2,

        /// <summary>
        /// Explicitly request silence/stop.
        /// </summary>
        Silence = 3
    }
}
