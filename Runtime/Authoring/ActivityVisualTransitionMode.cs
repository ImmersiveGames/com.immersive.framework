using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Stable. Activity-level policy for using the session TransitionSurface during Activity requests.
    /// This selects whether Activity Flow asks the Session UIGlobal transition/loading capabilities to wrap an Activity operation; it does not own the surfaces.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable product authoring surface for application/route/activity configuration. Breaking changes require ADR/migration.")]
    public enum ActivityVisualTransitionMode
    {
        /// <summary>
        /// Activity switch/clear/startup runs without TransitionSurface and without the canonical LoadingSurface.
        /// Activity scene load/release may still execute when the operation requires it.
        /// </summary>
        Seamless = 0,

        /// <summary>
        /// Activity switch/clear/startup runs the session TransitionSurface fade before and after the Activity operation, without the canonical LoadingSurface.
        /// Activity scene load/release may still execute when the operation requires it.
        /// </summary>
        Fade = 10,

        /// <summary>
        /// Activity switch/clear/startup runs the session TransitionSurface fade and the canonical LoadingSurface when the Activity operation has scene load/release side-effects.
        /// </summary>
        FadeWithLoading = 20
    }
}
