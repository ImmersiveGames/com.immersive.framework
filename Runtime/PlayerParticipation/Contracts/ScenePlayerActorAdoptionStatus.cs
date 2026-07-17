using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B2B typed Scene Logical Player Actor adoption status.")]
    public enum ScenePlayerActorAdoptionStatus
    {
        None = 0,
        SucceededAdopted = 10,
        SucceededAlreadyAdopted = 20,
        SucceededReleased = 30,
        RejectedRuntimeUnavailable = 100,
        RejectedInvalidRequest = 110,
        RejectedSlotNotJoined = 120,
        RejectedSelectionMismatch = 130,
        RejectedHostMismatch = 140,
        RejectedActorMismatch = 150,
        RejectedPreparationConflict = 160,
        RejectedForeignOrStaleAdoption = 170,
        FailedRuntimeContentRegistration = 200,
        FailedActivation = 210,
        FailedRelease = 220,
        FailedRollback = 230
    }
}
