using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Aggregate status for a ResetExecutor run.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12C ResetExecutor aggregate status.")]
    public enum ResetExecutionStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededNoSubjects = 20,
        Failed = 30,
        FailedNoSubjects = 40,
        RejectedInvalidRequest = 50
    }
}
