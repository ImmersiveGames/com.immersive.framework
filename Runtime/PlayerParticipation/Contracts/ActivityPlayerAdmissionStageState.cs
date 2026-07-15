using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B staged Activity Player admission transaction state.")]
    public enum ActivityPlayerAdmissionStageState
    {
        None = 0,
        Staging = 10,
        ReadyToCommit = 20,
        Committed = 30,
        RolledBack = 40,
        RollbackFailed = 50,
        Failed = 60
    }
}
