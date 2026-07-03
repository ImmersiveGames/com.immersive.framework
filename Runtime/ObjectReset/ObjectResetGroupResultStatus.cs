using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Aggregate status for a multi-target Object Reset Group request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F39A Object Reset Group aggregate status.")]
    public enum ObjectResetGroupResultStatus
    {
        Unknown = 0,
        Succeeded = 1,
        SucceededNoTargets = 2,
        CompletedWithWarnings = 3,
        Failed = 4,
        RejectedInvalidRequest = 5,
        RejectedRuntimeUnavailable = 6,
        RejectedRuntimeContextUnavailable = 7,
        RejectedAlreadyInFlight = 8
    }
}
