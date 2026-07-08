using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Passive vocabulary for player view evidence.
    /// This is not a CameraDirector, input router, control binder or runtime lifecycle coordinator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49G passive PlayerView state vocabulary.")]
    public enum PlayerViewState
    {
        /// <summary>A PlayerView declaration exists for a PlayerSlot.</summary>
        Declared = 0,

        /// <summary>The PlayerView has accepted PlayerEntry evidence in ViewBound or Active state.</summary>
        Bound = 10,

        /// <summary>The PlayerView is eligible to be considered the active view for its PlayerSlot.</summary>
        Active = 20,

        /// <summary>The PlayerView is temporarily suspended and must carry an explicit diagnostic reason.</summary>
        Suspended = 30,

        /// <summary>The PlayerView evidence was released from the current lifecycle.</summary>
        Released = 40
    }
}
