using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "ADR-024 typed public prepared Actor replacement status.")]
    public enum PlayerPreparedActorReplacementStatus
    {
        None = 0,
        SucceededReplacedAndGameplayReady = 10,
        SucceededReplacedGameplayBlocked = 20,
        SucceededReplacedCleanupPending = 30,
        RejectedInvalidRequest = 100,
        RejectedRuntimeUnavailable = 110,
        RejectedStalePublicRevision = 120,
        RejectedNoActiveActivity = 130,
        RejectedUnsupportedProvisioning = 140,
        RejectedPreparedActorUnavailable = 150,
        FailedGameplayRelease = 200,
        FailedBeforeCommit = 210,
        FailedRollback = 220,
        SucceededCommittedGameplayReprojectionFailed = 230
    }
}
