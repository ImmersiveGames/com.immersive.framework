using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Diagnostic status for resolving Activity Content Execution participants from an explicit source.
    /// This status describes source resolution only; it does not execute participants, discover scene objects globally or imply physical materialization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10K Activity Content Execution participant source boundary status; no Unity side effects.")]
    public enum ActivityContentExecutionParticipantSourceStatus
    {
        Unknown = 0,
        None = 5,
        Succeeded = 10,
        SucceededNoParticipants = 20,
        SucceededWithIssues = 30,
        RejectedInvalidRequest = 100,
        Failed = 110,
        FailedException = 120,
    }
}
