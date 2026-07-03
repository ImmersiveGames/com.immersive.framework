using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for explicit Pause visual surface materialization.
    /// This does not toggle Pause, change InputMode, alter Time.timeScale or wire into Route/Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Pause visual surface materialization status; explicit visual proof only.")]
    internal enum PauseVisualSurfaceMaterializationStatus
    {
        Unknown = 0,
        SucceededMaterialized = 10,
        FailedMissingRuntimeHost = 100,
        FailedMissingRuntimeContentRuntime = 110,
        FailedInvalidContract = 120,
        FailedInvalidRuntimeContext = 130,
        FailedMissingPhysicalRegistry = 140,
        FailedMissingAnchorTransform = 150,
        FailedBindingRequest = 160,
        FailedMaterializationPipeline = 170
    }
}
