using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-3 current Logical Player Actor evidence status.")]
    public enum PlayerCurrentActorEvidenceStatus
    {
        None = 0,
        SucceededCurrent = 10,
        NoPreparedActor = 20,
        RejectedInvalidRequest = 100,
        RejectedReleaseFailed = 110,
        RejectedAssignmentDivergence = 120,
        RejectedHostDivergence = 130,
        RejectedSelectionStale = 140,
        RejectedPreparationStale = 150,
        RejectedForeignPreparation = 160,
        RejectedOtherSlotPreparation = 170,
        RejectedOwnerMismatch = 180,
        RejectedRuntimeContentMismatch = 190,
        RejectedPhysicalEvidenceMismatch = 200
    }
}
