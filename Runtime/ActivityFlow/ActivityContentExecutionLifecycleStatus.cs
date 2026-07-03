using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Aggregated diagnostic status for Activity Content Execution integration at Activity lifecycle level.
    /// It reports only logical execution state; it does not imply physical materialization, placement or gameplay execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10I Activity Content Execution lifecycle integration status; diagnostics only and no Unity side effects.")]
    public enum ActivityContentExecutionLifecycleStatus
    {
        Unknown = 0,
        None = 5,
        Succeeded = 10,
        SucceededNoContent = 20,
        SucceededWithNonBlockingIssues = 30,
        FailedBlocking = 40,
        FailedNonBlocking = 50,
        RejectedInvalidContext = 60,
        RejectedParticipantSource = 70,
    }
}
