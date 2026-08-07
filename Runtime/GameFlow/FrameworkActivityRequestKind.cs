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
        RejectedByTransitionGate = 6,
        FailedCommittedTargetNotReady = 7,
        FailedCommittedTargetReadinessInvalidated = 8,
        FailedCommittedTargetReadinessCancelled = 9,
        SupersededCommittedTargetByRouteReplacement = 10,
        /// <summary>
        /// Transition Before failed before destination Activity authority advanced.
        /// </summary>
        FailedPreCommitTransition = 11,
        /// <summary>
        /// Destination Activity already committed, but Transition After / reveal did not complete.
        /// </summary>
        FailedCommittedTargetReveal = 12
    }
}
