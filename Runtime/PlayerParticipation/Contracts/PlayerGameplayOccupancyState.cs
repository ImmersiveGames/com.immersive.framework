using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.2 effective runtime Player Slot occupancy state.")]
    public enum PlayerGameplayOccupancyState
    {
        None = 0,
        Vacant = 10,
        Occupied = 20
    }
}
