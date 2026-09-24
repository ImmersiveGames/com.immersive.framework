using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Canonical phase of one non-concurrent Activity transition transaction.
    /// Authority, readiness and previous-Activity finalization remain independent dimensions.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ARCH-A2 Activity transition transaction phase vocabulary.")]
    internal enum ActivityTransitionPhase
    {
        Unknown = 0,
        Idle = 10,
        PreparingTarget = 20,
        ReadyToCommit = 30,
        CommittedTransitioning = 40,
        PreviousExiting = 50,
        TargetEntering = 60,
        PreviousFinalizing = 70,
        Completed = 100,
        FailedBeforeCommit = 110,
        CommittedNotReady = 120,
        CommittedFinalizationFailed = 130
    }
}
