using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive final/status summary for a Loading operation result.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22H Loading result status primitive.")]
    public enum LoadingResultStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededWithWarnings = 20,
        WaitingForReadiness = 30,
        Failed = 40,
        Canceled = 50
    }
}
