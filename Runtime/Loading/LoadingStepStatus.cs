using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive status for one Loading step observation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading step status primitive.")]
    public enum LoadingStepStatus
    {
        Unknown = 0,
        Pending = 10,
        Running = 20,
        Completed = 30,
        Failed = 40,
        Skipped = 50,
        Canceled = 60
    }
}
