namespace Immersive.Framework.GameFlow.Diagnostics
{
    internal enum GameFlowDiagnosticFaultCheckpoint
    {
        None = 0,
        CurrentPreparationTokenValidation = 1,
        CurrentOwnershipValidation = 2,
        BeforeCandidateStaging = 3,
        LifecycleRuntimeAvailability = 4,
        BeforeLoadingPresentation = 5,
        AfterCommitBeforeTargetReadiness = 6,
        AfterCandidateOwnershipBeforePreviousCleanup = 7
    }
}
