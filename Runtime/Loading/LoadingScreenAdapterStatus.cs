using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Result status for a loading screen adapter action.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22E Loading Screen adapter status boundary; no UI implementation.")]
    public enum LoadingScreenAdapterStatus
    {
        Unknown = 0,
        Succeeded = 10,
        Skipped = 20,
        Failed = 30,
        Rejected = 40
    }
}
