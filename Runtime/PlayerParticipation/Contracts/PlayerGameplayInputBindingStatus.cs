using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.3 typed gameplay input binding operation status.")]
    public enum PlayerGameplayInputBindingStatus
    {
        None = 0,

        SucceededBound = 10,
        SucceededReleased = 20,
        SucceededAlreadyBound = 30,
        SucceededAlreadyReleased = 40,
        SucceededAvailabilityRefreshed = 50,

        RejectedInvalidRequest = 100,
        RejectedSessionMismatch = 110,
        RejectedSlotNotConfigured = 120,
        RejectedPreparationNotReady = 130,
        RejectedOccupancyNotReady = 140,
        RejectedForeignOrStaleOccupancy = 150,
        RejectedHostMismatch = 160,
        RejectedActorMismatch = 170,
        RejectedPlayerInputMismatch = 180,
        RejectedMissingActionAsset = 190,
        RejectedMissingActionMap = 200,
        RejectedGateAdapterMismatch = 210,
        RejectedSlotAlreadyBound = 220,
        RejectedPlayerInputAlreadyBound = 230,
        RejectedForeignOrStaleBinding = 240,

        FailedActionMapActivation = 300,
        FailedRollback = 310,
        FailedRelease = 320
    }
}
