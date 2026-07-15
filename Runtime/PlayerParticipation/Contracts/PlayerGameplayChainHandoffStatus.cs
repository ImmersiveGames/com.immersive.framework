using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7D reversible Player gameplay chain handoff operation status.")]
    public enum PlayerGameplayChainHandoffStatus
    {
        None = 0,
        SucceededCommitted = 10,
        SucceededAlreadyCommitted = 11,
        SucceededRolledBack = 20,

        RejectedInvalidRequest = 100,
        RejectedRuntimeUnavailable = 110,
        RejectedForeignOrStaleCandidate = 120,
        RejectedForeignOrStaleAdmission = 130,
        RejectedHandoffAlreadyActive = 140,
        RejectedForeignOrStaleHandoff = 150,
        RejectedRollbackNotAvailable = 160,

        FailedCurrentChainRelease = 200,
        FailedPreparationHandoff = 210,
        FailedCandidateChain = 220,
        FailedRollback = 230,
        FailedPreviousActorRelease = 240
    }
}
