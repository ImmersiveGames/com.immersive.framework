
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Authoring kind for a Pause visual surface contract.
    /// This describes the intended Pause presentation role only; it does not materialize UI or own Pause state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
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
