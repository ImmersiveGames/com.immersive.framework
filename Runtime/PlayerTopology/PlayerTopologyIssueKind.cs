using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerTopology
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds emitted by passive PlayerTopology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49F passive PlayerTopology validation issue kinds.")]
    public enum PlayerTopologyIssueKind
    {
        None = 0,
        MissingPlayerSlotSet = 10,
        PlayerSlotSetIssue = 20,
        DuplicatePlayerEntrySlot = 30,
        DuplicatePlayerEntryActor = 40,
        PlayerEntryWithoutPlayerSlotDeclaration = 50,
        PlayerEntryWithoutPlayerSlotOccupancy = 60,
        PlayerEntryActorMismatch = 70,
        PlayerSlotOccupancyWithoutDeclaration = 80,
        PlayerSlotOccupancyWithoutPlayerEntry = 90,
        DuplicateOccupiedActor = 100
    }
}
