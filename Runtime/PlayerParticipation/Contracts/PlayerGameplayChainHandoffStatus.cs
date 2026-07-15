using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7D/P3K.7E reversible Player gameplay chain handoff operation status.")]
    public enum PlayerGameplayChainHandoffStatus
    {
        None = 0,
        SucceededReadyToCommit = 5,
        SucceededAlreadyReadyToCommit = 6,
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
        RejectedNotReadyToCommit = 170,

        FailedCurrentChainRelease = 200,
        FailedPreparationHandoff = 210,
        FailedCandidateChain = 220,
        FailedRollback = 230,
        FailedCommit = 235,
        FailedPreviousActorRelease = 240
    }
}
