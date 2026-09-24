using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Activity Player lifecycle admission transaction state.")]
    public enum ActivityPlayerLifecycleAdmissionState
    {
        None = 0,
        NotRequired = 10,
        Preparing = 20,
        ReadyToCommit = 30,
        TransitionAuthorized = 40,
        Committing = 50,
        CommittedAwaitingLifecycle = 60,
        CommitCleanupPending = 70,
        Completed = 80,
        RollingBack = 90,
        RolledBack = 100,
        Failed = 200
    }
}
