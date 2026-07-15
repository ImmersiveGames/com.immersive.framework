using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B staged Activity Player admission operation status.")]
    public enum ActivityPlayerAdmissionStageStatus
    {
        None = 0,
        SucceededReadyToCommit = 10,
        SucceededCommitted = 20,
        SucceededRolledBack = 30,
        SucceededAlreadyRolledBack = 31,
        RejectedInvalidRequest = 100,
        RejectedAnotherStageActive = 101,
        RejectedForeignOrStaleStage = 102,
        RejectedNotReadyToCommit = 103,
        FailedScopeCreation = 200,
        FailedResolution = 201,
        FailedEvaluation = 202,
        FailedRollback = 203
    }
}
