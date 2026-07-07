using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds for PlayerSlot declaration and occupancy validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot validation issue kinds.")]
    public enum PlayerSlotSetIssueKind
    {
        None = 0,
        InvalidPlayerSlotId = 10,
        InvalidDeclaration = 20,
        DuplicatePlayerSlotId = 30,
        MissingOccupiedActor = 40,
        InvalidOccupiedActorId = 50,
        DuplicatePlayerSlotOccupancy = 60,
        ConflictingOccupiedActorSources = 70
    }
}
