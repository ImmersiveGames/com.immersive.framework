using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.SessionLifecycle
{
    /// <summary>
    /// API status: Internal. Minimal ownership semantics for content known by the Session scope.
    /// This is not a release policy and does not authorize consumer ownership before later roadmap phases.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Session content ownership vocabulary introduced by F2C.")]
    internal enum SessionContentOwnership
    {
        /// <summary>
        /// The Session knows the item, but ownership/release responsibility is not claimed.
        /// </summary>
        Registered = 0,

        /// <summary>
        /// The Session owns the item and may become responsible for release when release policy exists.
        /// </summary>
        Owned = 1,

        /// <summary>
        /// The item exists only for diagnostics/status and carries no release responsibility.
        /// </summary>
        DiagnosticOnly = 2
    }
}
