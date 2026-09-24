using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// ADR-020 terminal Session Player Leave status. Success means all required typed
    /// pre-commit release evidence was accepted, Session-scoped associations were cleared,
    /// and the exact Leaving occurrence committed its Slot to Available.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-020 exact Session Player Leave terminal cleanup and commit status.")]
    internal enum SessionPlayerLeaveTerminalStatus
    {
        None = 0,
        SucceededCommitted = 10,
        SucceededAlreadyCommitted = 20,
        RejectedInvalidRequest = 100,
        RejectedLeaveCorrelation = 110,
        RejectedActivityReleaseEvidence = 120,
        RejectedProvisioningReleaseEvidence = 130,
        RejectedProvisioningMode = 140,
        RejectedAssignmentCorrelation = 150,
        RejectedForeignOrStalePostCommit = 160,
        FailedAssignmentRelease = 200,
        FailedActorSelectionCleanup = 210,
        FailedCommit = 220,
        FailedInvariant = 230
    }
}
