using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-1 typed current Player Slot assignment operation status.")]
    public enum PlayerSlotAssignmentStatus
    {
        None = 0,
        SucceededAssigned = 10,
        SucceededAlreadyAssigned = 20,
        SucceededConfirmed = 30,
        SucceededReleased = 40,
        RejectedInvalidSlot = 100,
        RejectedSlotNotConfigured = 110,
        RejectedSlotNotJoined = 120,
        RejectedInvalidOrigin = 130,
        RejectedUnsupportedOrigin = 140,
        RejectedInvalidOwner = 150,
        RejectedInvalidHostBinding = 160,
        RejectedAssignmentConflict = 170,
        RejectedHostBindingConflict = 180,
        RejectedUnassigned = 190,
        RejectedForeignToken = 200,
        RejectedStaleToken = 210,
        RejectedTokenSlotMismatch = 220
    }
}
