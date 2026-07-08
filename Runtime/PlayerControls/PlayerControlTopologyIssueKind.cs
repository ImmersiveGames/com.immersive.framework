using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds emitted by passive PlayerControl topology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49J passive PlayerControl topology validation issue kinds.")]
    public enum PlayerControlTopologyIssueKind
    {
        None = 0,
        MissingPlayerTopologyValidation = 10,
        PlayerTopologyIssue = 20,
        DuplicatePlayerControlSlot = 30,
        PlayerControlWithoutPlayerSlotDeclaration = 40,
        PlayerControlWithoutPlayerEntry = 50,
        PlayerControlPlayerEntryEvidenceMismatch = 60,
        BoundPlayerControlWithoutActiveEntry = 70,
        ActivePlayerControlWithoutActiveReadyEntry = 80
    }
}
