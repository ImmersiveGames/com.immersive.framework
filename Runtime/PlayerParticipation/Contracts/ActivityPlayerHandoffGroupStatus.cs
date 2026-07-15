using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E multi-Slot Activity Player handoff group operation status.")]
    public enum ActivityPlayerHandoffGroupStatus
    {
        None = 0,
        SucceededReadyToCommit = 10,
        SucceededAlreadyReadyToCommit = 11,
        SucceededCommitted = 20,
        SucceededAlreadyCommitted = 21,
        SucceededRolledBack = 30,

        RejectedInvalidRequest = 100,
        RejectedAnotherGroupActive = 110,
        RejectedForeignOrStaleGroup = 120,
        RejectedAdmission = 130,
        RejectedNotReadyToCommit = 140,
        RejectedRollbackNotAvailable = 150,

        FailedSlotBegin = 200,
        FailedRollback = 210,
        FailedCommitValidation = 220,
        FailedCommit = 230,
        FailedCommitCleanup = 240,
        FailedEvidence = 250
    }
}
