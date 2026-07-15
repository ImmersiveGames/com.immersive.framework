using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7D/P3K.7E reversible Player gameplay chain handoff state.")]
    public enum PlayerGameplayChainHandoffState
    {
        None = 0,
        CurrentChainReleased = 10,
        CandidatePreparationCurrent = 20,
        CandidateChainReady = 30,
        Committed = 40,
        RolledBack = 50,
        RollbackFailed = 60,
        CommitFailed = 65,
        CommitCleanupFailed = 70
    }
}
