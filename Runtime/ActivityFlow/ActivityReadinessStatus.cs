using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal readiness status for the active Activity scope.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal Activity readiness status introduced by F4D.")]
    internal enum ActivityReadinessStatus
    {
        None = 0,
        Ready = 1,
        NotReady = 2
    }
}
