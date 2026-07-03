using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for explicit composite lifecycle release of ContentAnchor materialized Unity content.
    /// This status is not Route/Activity auto-release and does not select any consumer.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-S explicit composite lifecycle release status; no Route/Activity auto-release wiring.")]
    internal enum UnityContentAnchorCompositeLifecycleReleaseStatus
    {
        Unknown = 0,
        SucceededReleasedAll = 10,
        SucceededNoRequests = 20,
        FailedInvalidPlan = 100,
        FailedMissingRuntimeHost = 110,
        FailedMissingRuntimeContentRuntime = 120,
        FailedLifecycleRequestRelease = 130,
        FailedPhysicalRelease = 140,
        FailedLogicalRelease = 150,
        FailedBindingCleanup = 160,
        FailedLifecycleMarkReleased = 170,
        FailedPartialRelease = 180
    }
}
