using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.2 effective runtime Player Slot occupancy operation status.")]
    public enum PlayerGameplayOccupancyStatus
    {
        None = 0,

        SucceededOccupied = 10,
        SucceededReleased = 20,
        SucceededAlreadyOccupied = 30,
        SucceededAlreadyReleased = 40,

        RejectedInvalidRequest = 100,
        RejectedSessionMismatch = 110,
        RejectedSlotNotConfigured = 120,
        RejectedPreparationNotReady = 130,
        RejectedForeignOrStalePreparation = 140,
        RejectedSlotAlreadyOccupied = 150,
        RejectedPreparationAlreadyOccupied = 155,
        RejectedForeignOrStaleOccupancy = 160
    }
}
