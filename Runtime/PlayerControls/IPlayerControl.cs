using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Passive PlayerControl contract.
    /// It exposes control intent/evidence for a PlayerSlot without owning PlayerInputManager,
    /// InputAction routing, movement, gameplay control or ControlBinding lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49I passive PlayerControl contract.")]
    public interface IPlayerControl
    {
        PlayerSlotId PlayerSlotId { get; }

        PlayerControlState State { get; }

        bool HasPlayerEntryEvidence { get; }

        PlayerEntryState PlayerEntryState { get; }

        bool IsPlayerEntryReadyForControl { get; }

        bool HasControlTarget { get; }

        string ControlTargetName { get; }

        string InputSourceId { get; }

        bool IsEligibleForBoundControl { get; }

        bool IsEligibleForActiveControl { get; }

        PlayerControlSnapshot CreateSnapshot();
    }
}
