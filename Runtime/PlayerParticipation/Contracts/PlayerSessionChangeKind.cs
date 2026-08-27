using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Semantic Session participation state dimension changed by an
    /// authoritative committed mutation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-08 scoped Player Session change kind.")]
    public enum PlayerSessionChangeKind
    {
        JoiningChanged = 10,
        SlotAllocationChanged = 20,
        ActorSelectionChanged = 30
    }
}
