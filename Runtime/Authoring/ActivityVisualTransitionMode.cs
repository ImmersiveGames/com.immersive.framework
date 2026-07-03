using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Activity-level policy for using the session TransitionSurface during Activity requests.
    /// This selects whether Activity Flow asks the Session UIGlobal transition/loading capabilities to wrap an Activity operation; it does not own the surfaces.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24F Activity visual transition policy; Session UIGlobal remains the surface owner.")]
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
