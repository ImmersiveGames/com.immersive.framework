using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7G same-Route Activity Player lifecycle admission operation status.")]
    public enum ActivityPlayerLifecycleAdmissionStatus
    {
        None = 0,

        SucceededNotRequired = 10,
        SucceededReadyToCommit = 20,
        SucceededAlreadyReadyToCommit = 21,
        SucceededTransitionAuthorized = 30,
        SucceededAlreadyTransitionAuthorized = 31,
        SucceededCommitted = 40,
        SucceededAlreadyCommitted = 41,
        SucceededCommitCleanupPending = 42,
        SucceededLifecycleCompleted = 50,
        SucceededRolledBack = 60,
        SucceededAlreadyRolledBack = 61,

        RejectedInvalidRequest = 100,
        RejectedUnsupportedFlow = 110,
        RejectedRuntimeUnavailable = 120,
        RejectedCurrentGameplayNotReady = 130,
        RejectedForeignOrStaleTransaction = 140,
        RejectedInvalidState = 150,
        RejectedRollbackNotAvailable = 160,

        FailedScopePreparation = 200,
        FailedCandidateStaging = 210,
        FailedGroupBegin = 220,
        FailedTransitionAuthorization = 230,
        FailedCommit = 240,
        FailedCommitCleanup = 250,
        FailedRollback = 260,
        FailedLifecycleAdoption = 270,
        FailedGameplayRelease = 280
    }
}
