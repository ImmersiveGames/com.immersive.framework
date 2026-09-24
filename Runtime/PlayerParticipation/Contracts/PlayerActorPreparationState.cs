using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Stable Session evidence for one Player Slot logical Actor preparation state.
    /// Physical staging details remain in PlayerActorMaterializationState.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.4 Session Logical Player Actor preparation state vocabulary.")]
    public enum PlayerActorPreparationState
    {
        None = 0,
        Unprepared = 10,
        Prepared = 20,
        ReleaseFailed = 30
    }
}
