
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3G.2 local Player join result status model.")]
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
        RejectedCapacityReached = 16,
        RejectedNoAvailableSlot = 17,
        RejectedProvisioningReturnedNull = 18,
        RejectedUnexpectedJoin = 19,
        RejectedCorrelationMismatch = 20,
        RejectedMissingPlayerInput = 21,
        RejectedMissingPlayerActorDeclaration = 22,
        RejectedForeignOrStaleReservation = 23,
        FailedAdmission = 30,
        FailedRollback = 31
    }
}
