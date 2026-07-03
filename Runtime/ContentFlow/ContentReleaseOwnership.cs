using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Internal release ownership vocabulary used by content release planning.
    /// It mirrors lifecycle ownership semantics without exposing route/activity implementation details.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release planning ownership vocabulary; not game-facing API.")]
    internal enum ContentReleaseOwnership
    {
        Registered = 0,
        Owned = 1,
        DiagnosticOnly = 2
    }
}
