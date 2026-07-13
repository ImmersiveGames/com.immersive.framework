using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F typed Player participation operation status.")]
    public enum PlayerParticipationOperationStatus
    {
        None = 0,
        Succeeded = 10,
        IgnoredNoChange = 20,
        RejectedInvalidRequest = 30,
        RejectedInvalidState = 40,
        RejectedJoiningClosed = 50,
        RejectedCapacityReached = 60,
        RejectedNoAvailableSlot = 70,
        RejectedForeignOrStaleReservation = 80,
        FailedInvalidConfiguration = 90
    }
}
