using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Result status for Activity-owned Logical Player Actor lifecycle execution.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-M07-10 Activity-scoped Logical Player Actor lifecycle status with readiness reconcile.")]
    public enum ActivityPlayerActorLifecycleStatus
    {
        None = 0,
        SucceededEntered = 10,
        SucceededEnteredNoParticipants = 20,
        SucceededEnteredPreparing = 21,
        SucceededReconciledPreparing = 22,
        SucceededReconciledReady = 23,
        SucceededExited = 30,
        SucceededExitedNoActors = 40,
        RejectedForeignOrStaleActivity = 100,
        FailedInvalidConfiguration = 110,
        FailedProjection = 120,
        FailedRequirement = 130,
        FailedSelection = 140,
        FailedPreparation = 150,
        FailedReconcile = 155,
        FailedRelease = 160,
        FailedRollback = 170
    }
}
