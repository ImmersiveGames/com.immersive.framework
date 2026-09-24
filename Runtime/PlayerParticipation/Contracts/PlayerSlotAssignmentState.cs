using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-1 canonical current Player Slot assignment state.")]
    public enum PlayerSlotAssignmentState
    {
        Unassigned = 0,
        Assigned = 10
    }
}
