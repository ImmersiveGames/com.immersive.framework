using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B2A Scene Local Player Activity lifecycle status vocabulary.")]
    public enum SceneLocalPlayerAdmissionActivityLifecycleStatus
    {
        None = 0,

        SucceededEntered = 10,
        SucceededAlreadyEntered = 20,
        SucceededNoAutomaticPlayers = 30,
        SucceededExited = 40,
        SucceededAlreadyExited = 50,
        SucceededRolledBack = 60,

        RejectedInvalidRequest = 100,
        RejectedForeignOrStaleActivity = 110,
        RejectedActorAdoptionRequired = 120,
        RejectedSelectionConflict = 130,

        FailedAuthoringResolution = 300,
        FailedRequirement = 310,
        FailedAdmission = 320,
        FailedSelection = 330,
        FailedExit = 340,
        FailedRollback = 350
    }
}
