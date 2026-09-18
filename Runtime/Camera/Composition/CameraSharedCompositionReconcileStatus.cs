using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit outcome of one Camera composition reconciliation pass. Zero currently
    /// assigned Subjects is a valid dormant state (SucceededAwaitingSubjects), not a failure.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-029-B composition-owned membership reconciliation status.")]
    public enum CameraSharedCompositionReconcileStatus
    {
        None = 0,
        SucceededApplied = 10,
        SucceededNoChange = 20,
        SucceededAwaitingSubjects = 30,
        SucceededStopped = 40,
        RejectedForeignAvailabilityContext = 100,
        RejectedStaleAvailabilitySnapshot = 110,
        BlockedInvalidMembership = 120,
        BlockedInvalidComposer = 130,
        BlockedMembershipFailure = 140,
        BlockedProjectionFailure = 150,
        BlockedPresentationFailure = 160
    }
}
