using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E multi-Slot Activity Player handoff group state.")]
    public enum ActivityPlayerHandoffGroupState
    {
        None = 0,
        Beginning = 10,
        ReadyToCommit = 20,
        Committing = 30,
        Committed = 40,
        RollingBack = 50,
        RolledBack = 60,
        RollbackFailed = 70,
        CommitCleanupFailed = 80
    }
}
