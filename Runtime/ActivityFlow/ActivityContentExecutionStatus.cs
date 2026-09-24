using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Result status for one logical Activity Content Execution participant/content item.
    /// Status values are diagnostic contracts only and do not execute physical release, placement or gameplay mutation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10B Activity Content Execution status contract.")]
    public enum ActivityContentExecutionStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededNoOp = 20,
        SkippedNotApplicable = 30,
        SkippedOptional = 40,
        FailedNonBlocking = 100,
        FailedBlocking = 110,
        RejectedInvalidRequest = 200
    }
}
