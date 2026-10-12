using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Editor.Diagnostics.GameFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Editor-only scenarios for diagnostic GameFlow fault injection; not product API.")]
    public enum FrameworkGameFlowDiagnosticFaultScenario
    {
        PreparationTokenMismatch = 0,
        OwnerMismatch = 1,
        PreCommitFailure = 2,
        RuntimeUnavailable = 3,
        LoadingRejectedBeforePresentation = 4,
        CommittedTargetNotReady = 5,
        CommittedFinalizationFailure = 6
    }
}
