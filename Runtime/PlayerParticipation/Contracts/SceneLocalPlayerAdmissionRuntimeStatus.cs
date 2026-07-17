using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Outcome vocabulary for the Scene Local Player host/Slot admission transaction.
    /// Logical Actor preparation and gameplay readiness are intentionally outside P3M4B1.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B1 Scene Local Player host and Slot admission transaction status.")]
    public enum SceneLocalPlayerAdmissionRuntimeStatus
    {
        None = 0,

        SucceededAdmitted = 10,
        SucceededAlreadyAdmitted = 20,
        SucceededReleased = 30,
        SucceededAlreadyReleased = 40,

        RejectedInvalidRequest = 100,
        RejectedRuntimeUnavailable = 110,
        RejectedSlotOrderMismatch = 120,
        RejectedConflict = 130,
        RejectedForeignOrStaleToken = 140,
        RejectedCapacityReached = 150,
        RejectedSlotUnavailable = 160,
        RejectedDependentState = 170,

        FailedReservation = 300,
        FailedReservationRollback = 310,
        FailedHostStage = 320,
        FailedSlotCommit = 330,
        FailedHostCommit = 340,
        FailedReleaseBegin = 350,
        FailedHostRelease = 360,
        FailedReleaseCommit = 370,
        FailedCompensation = 380,
        FailedInvariant = 390
    }
}
