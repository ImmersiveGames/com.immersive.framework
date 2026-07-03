using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Aggregate status for a logical Activity Content Execution batch.
    /// This status summarizes passive per-content execution results only; it does not execute content or mutate Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Activity Content Execution aggregate status contract.")]
    public enum ActivityContentExecutionAggregateStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededNoOp = 20,
        SucceededWithNonBlockingIssues = 30,
        SkippedNoContent = 40,
        FailedNonBlocking = 100,
        FailedBlocking = 110,
        RejectedInvalidResults = 200
    }
}
