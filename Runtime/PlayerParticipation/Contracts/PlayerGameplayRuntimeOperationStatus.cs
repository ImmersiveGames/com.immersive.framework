using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7F host-scoped Player gameplay runtime operation status.")]
    public enum PlayerGameplayRuntimeOperationStatus
    {
        None = 0,

        SucceededReady = 10,
        SucceededAlreadyReady = 20,
        SucceededBlockedByInputGate = 25,
        SucceededAlreadyBlockedByInputGate = 26,
        SucceededReleased = 30,
        SucceededAlreadyReleased = 40,

        RejectedRuntimeUnavailable = 100,
        RejectedInvalidRequest = 110,
        RejectedForeignOrStaleAdmission = 120,

        FailedChainBuild = 200,
        FailedChainRollback = 210,
        FailedRelease = 220
    }
}
