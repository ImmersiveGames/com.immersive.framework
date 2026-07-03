using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal enum FrameworkActivityRequestKind
    {
        Succeeded = 0,
        IgnoredAlreadyActive = 1,
        IgnoredAlreadyInFlight = 2,
        IgnoredNoActiveActivity = 3,
        FailedInvalidConfig = 4,
        FailedRuntimeUnavailable = 5,
        RejectedByTransitionGate = 6
    }
}
