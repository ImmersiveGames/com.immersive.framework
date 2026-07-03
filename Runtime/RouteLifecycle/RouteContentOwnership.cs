using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// API status: Internal. Minimal ownership semantics for content known by the active Route scope.
    /// This is not a release policy and does not authorize additive scene unloading before F6.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route content ownership vocabulary introduced by F3E.")]
    internal enum RouteContentOwnership
    {
        /// <summary>
        /// The Route knows the item, but ownership/release responsibility is not claimed.
        /// </summary>
        Registered = 0,

        /// <summary>
        /// The Route owns the item in the current baseline semantics. F6F can translate this to release intent; physical release remains a later cut concern.
        /// </summary>
        Owned = 1,

        /// <summary>
        /// The item exists only for diagnostics/status and carries no release responsibility.
        /// </summary>
        DiagnosticOnly = 2
    }
}
