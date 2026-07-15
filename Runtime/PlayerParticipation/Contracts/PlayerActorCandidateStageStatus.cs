using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7C contextual Logical Player Actor candidate staging operation status.")]
    public enum PlayerActorCandidateStageStatus
    {
        None = 0,
        SucceededStaged = 10,
        SucceededAlreadyStaged = 11,
        SucceededRolledBack = 20,
        SucceededAlreadyRolledBack = 21,

        RejectedRuntimeUnavailable = 100,
        RejectedInvalidRequest = 110,
        RejectedSlotNotConfigured = 120,
        RejectedSlotNotJoined = 130,
        RejectedActorSelectionMissing = 140,
        RejectedHostUnavailable = 150,
        RejectedTargetOwnerMatchesCurrent = 160,
        RejectedAnotherCandidateActive = 170,
        RejectedForeignOrStaleCandidate = 180,

        FailedMaterialization = 200,
        FailedRollback = 210
    }
}
