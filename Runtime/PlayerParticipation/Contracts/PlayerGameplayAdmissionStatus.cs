using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.5 gameplay admission operation status.")]
    public enum PlayerGameplayAdmissionStatus
    {
        None = 0,

        SucceededReady = 10,
        SucceededBlockedByInputGate = 11,
        SucceededAlreadyAdmitted = 12,
        SucceededReadinessRefreshed = 13,
        SucceededReleased = 20,
        SucceededAlreadyReleased = 21,

        RejectedInvalidRequest = 100,
        RejectedSessionMismatch = 101,
        RejectedSlotNotConfigured = 102,
        RejectedOccupancyNotReady = 103,
        RejectedInputBindingNotReady = 104,
        RejectedCameraDecisionNotReady = 105,
        RejectedForeignOrStaleOccupancy = 106,
        RejectedForeignOrStaleInputBinding = 107,
        RejectedForeignOrStaleCameraEligibility = 108,
        RejectedSlotAlreadyAdmitted = 109,
        RejectedForeignOrStaleAdmission = 110,

        FailedCameraOutputResolution = 200,
        FailedCameraRequestCreation = 201,
        FailedCameraPublisherCreation = 202,
        FailedCameraPublication = 203,
        FailedCameraRelease = 210,
        FailedCameraEligibilityRelease = 211,
        FailedInputBindingRelease = 212,
        FailedOccupancyRelease = 213,
        FailedAdmissionRollback = 214
    }
}
