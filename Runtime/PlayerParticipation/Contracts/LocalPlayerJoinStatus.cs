using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3G/P3J local Player join result status model.")]
    public enum LocalPlayerJoinStatus
    {
        None = 0,
        SucceededJoined = 1,
        RejectedInvalidRequest = 10,
        RejectedOperationInFlight = 11,
        RejectedRuntimeUnavailable = 12,
        RejectedManagerUnavailable = 13,
        RejectedManagerConfiguration = 14,
        RejectedJoiningClosed = 15,
        RejectedNoAvailableSlot = 16,
        RejectedProvisioningReturnedNull = 17,
        RejectedUnexpectedJoin = 18,
        RejectedCorrelationMismatch = 19,
        RejectedMissingPlayerInput = 20,
        RejectedMissingLocalPlayerHost = 21,
        RejectedForeignOrStaleReservation = 22,
        RejectedInvalidLocalPlayerHost = 23,
        FailedAdmission = 30,
        FailedRollback = 31
    }
}
