using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-1 canonical current Player Slot assignment origin.")]
    public enum PlayerSlotAssignmentOrigin
    {
        None = 0,
        ManagerProvisioned = 10,
        SceneProvided = 20,
        SessionPersistent = 30
    }
}
