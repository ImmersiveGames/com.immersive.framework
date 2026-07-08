using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Minimal passive state for Actor readiness.
    /// This is not PlayerEntry state and does not model player suspension.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49B passive Actor readiness state.")]
    public enum ActorReadinessState
    {
        /// <summary>
        /// The Actor exists or is declared, but is not yet ready for view or control.
        /// </summary>
        NotReady = 0,

        /// <summary>
        /// The Actor can be used for view-facing consumers such as camera target or HUD/status data.
        /// </summary>
        ReadyForView = 1,

        /// <summary>
        /// The Actor can receive control according to policy. This implies ReadyForView.
        /// </summary>
        ReadyForControl = 2,

        /// <summary>
        /// Actor initialization failed and must carry an explicit diagnostic reason.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// The readiness cycle has been released and cannot become ready again without a new explicit cycle.
        /// </summary>
        Released = 4
    }
}
