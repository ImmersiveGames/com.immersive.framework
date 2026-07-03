using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Minimal outcome classification for runtime route requests.
    /// This is intentionally small; richer transition policies are added only when needed.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal enum FrameworkRouteRequestKind
    {
        Succeeded = 0,
        FailedInvalidConfig = 1,
        IgnoredAlreadyActive = 2,
        IgnoredAlreadyInFlight = 3,
        FailedRuntimeUnavailable = 4,
        RejectedByTransitionGate = 5
    }
}
