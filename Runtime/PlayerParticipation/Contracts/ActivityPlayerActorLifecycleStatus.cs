using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Result status for Activity-owned Logical Player Actor lifecycle execution.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.6 Activity-scoped Logical Player Actor lifecycle result status.")]
    public enum ActivityPlayerActorLifecycleStatus
    {
        None = 0,
        SucceededEntered = 10,
        SucceededEnteredNoParticipants = 20,
        SucceededExited = 30,
        SucceededExitedNoActors = 40,
        RejectedForeignOrStaleActivity = 100,
        FailedInvalidConfiguration = 110,
        FailedProjection = 120,
        FailedRequirement = 130,
        FailedSelection = 140,
        FailedPreparation = 150,
        FailedRelease = 160,
        FailedRollback = 170
    }
}
