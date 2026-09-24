using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Physical state of one contextual Logical Player Actor instance.
    /// Session preparation authority is introduced separately by P3J.4.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.3 physical Player Actor materialization state vocabulary.")]
    public enum PlayerActorMaterializationState
    {
        Unknown = 0,
        StagedInactive = 10,
        Active = 20,
        ReleaseRequested = 30,
        Released = 40,
        ReleaseFailed = 50
    }
}
