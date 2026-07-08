using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds emitted by passive Player binding authoring validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F50A passive Player binding authoring validation issue kinds.")]
    public enum PlayerBindingAuthoringIssueKind
    {
        None = 0,
        MissingValidationRoot = 1,
        MissingPlayerSlotDeclaration = 2,
        MissingPlayerSlotOccupancy = 3,
        MissingActorReadinessBehaviour = 4,
        MissingPlayerEntryBehaviour = 5,
        MissingPlayerViewBehaviour = 6,
        MissingPlayerControlBehaviour = 7,
        PlayerSlotDeclarationIssue = 8,
        PlayerSlotOccupancyIssue = 9,
        PlayerSlotSetIssue = 10,
        PlayerEntrySnapshotFailure = 11,
        PlayerViewSnapshotFailure = 12,
        PlayerControlSnapshotFailure = 13,
        PlayerTopologyIssue = 14,
        PlayerViewTopologyIssue = 15,
        PlayerControlTopologyIssue = 16,
        BindingReadinessIssue = 17,
        BindingDiagnosticError = 18
    }
}
