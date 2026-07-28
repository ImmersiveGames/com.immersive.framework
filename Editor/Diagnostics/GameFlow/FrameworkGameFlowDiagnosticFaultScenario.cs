namespace Immersive.Framework.Editor.Diagnostics.GameFlow
{
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
