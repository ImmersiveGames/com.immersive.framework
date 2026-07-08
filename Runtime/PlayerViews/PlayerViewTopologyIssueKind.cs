using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds emitted by passive PlayerView topology validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49H passive PlayerView topology validation issue kinds.")]
    public enum PlayerViewTopologyIssueKind
    {
        None = 0,
        MissingPlayerTopologyValidation = 10,
        PlayerTopologyIssue = 20,
        DuplicatePlayerViewSlot = 30,
        PlayerViewWithoutPlayerSlotDeclaration = 40,
        PlayerViewWithoutPlayerEntry = 50,
        PlayerViewPlayerEntryStateMismatch = 60,
        BoundPlayerViewWithoutViewBoundOrActiveEntry = 70,
        ActivePlayerViewWithoutViewBoundOrActiveEntry = 80
    }
}
