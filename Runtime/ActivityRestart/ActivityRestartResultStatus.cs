using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Experimental. Aggregate status for Activity Restart via Object Reset Group.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F40A Activity Restart aggregate status.")]
    public enum ActivityRestartResultStatus
    {
        Unknown = 0,
        Succeeded = 1,
        CompletedWithWarnings = 2,
        RejectedAlreadyInFlight = 3,
        RejectedRuntimeUnavailable = 4,
        RejectedNoActiveActivity = 5,
        RejectedTargetMismatch = 6,
        ResetExecutionFailed = 7,
        ActivityClearFailed = 8,
        ActivityReenterFailed = 9
    }
}
