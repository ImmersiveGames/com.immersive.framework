using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-044 consumer gameplay-availability block operation status.")]
    public enum PlayerGameplayAvailabilityBlockStatus
    {
        None = 0,
        SucceededBlocked = 10,
        SucceededReleased = 20,
        RejectedRuntimeUnavailable = 100,
        RejectedInvalidRequest = 110,
        RejectedPlayerNotJoined = 120,
        RejectedForeignOrStaleToken = 130,
        FailedInputProjection = 200
    }
}
