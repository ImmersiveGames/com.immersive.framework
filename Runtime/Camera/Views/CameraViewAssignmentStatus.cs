using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CAMERA-026-B logical Camera View Assignment operation status.")]
    public enum CameraViewAssignmentStatus
    {
        None = 0,
        SucceededAssigned = 10,
        SucceededAlreadyAssigned = 20,
        SucceededReleased = 30,
        SucceededAlreadyReleased = 40,
        SucceededReconciled = 50,
        SucceededOwnerReleased = 60,
        RejectedInvalidRequest = 100,
        RejectedViewUnavailable = 110,
        RejectedSubjectUnavailable = 120,
        RejectedAssignmentConflict = 130,
        RejectedForeignOrStaleToken = 140,
        RejectedAvailabilityContextMismatch = 150,
        RejectedStaleAvailabilitySnapshot = 160
    }
}
