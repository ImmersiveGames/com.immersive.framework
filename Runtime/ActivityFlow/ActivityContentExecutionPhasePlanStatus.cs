using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Diagnostic status for a passive Activity Content Execution phase plan.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10F Activity Content Execution phase plan status contract.")]
    public enum ActivityContentExecutionPhasePlanStatus
    {
        Unknown = 0,
        Planned = 10,
        SkippedNoParticipants = 20,
        RejectedInvalidPhase = 100,
        RejectedMissingActivity = 110,
        RejectedInvalidContext = 120,
        RejectedInvalidParticipants = 130
    }
}
