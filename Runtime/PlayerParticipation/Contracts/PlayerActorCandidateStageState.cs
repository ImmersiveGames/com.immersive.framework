using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7C contextual Logical Player Actor candidate staging state.")]
    public enum PlayerActorCandidateStageState
    {
        None = 0,
        StagedInactive = 10,
        Promoting = 20,
        RollbackFailed = 30,
        RolledBack = 40,
        Promoted = 50
    }
}
