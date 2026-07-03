using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Authoring kind for a Pause visual surface contract.
    /// This describes the intended Pause presentation role only; it does not materialize UI or own Pause state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10B Pause visual surface authoring kind; no materialization behavior.")]
    public enum PauseVisualSurfaceKind
    {
        Unknown = 0,

        /// <summary>
        /// A top-level visual overlay root for Pause presentation.
        /// </summary>
        OverlayRoot = 10,

        /// <summary>
        /// A menu root that may be materialized inside an overlay or content anchor in a later cut.
        /// </summary>
        MenuRoot = 20
    }
}
