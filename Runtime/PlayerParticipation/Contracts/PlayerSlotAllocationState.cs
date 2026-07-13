using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Mutable Session allocation state for one configured Player Slot.
    /// Actor selection, Activity projection and occupancy remain separate facts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F Session Player Slot allocation state.")]
    public enum PlayerSlotAllocationState
    {
        Unavailable = 0,
        Available = 10,
        Reserved = 20,
        Joined = 30,
        Leaving = 40
    }
}
