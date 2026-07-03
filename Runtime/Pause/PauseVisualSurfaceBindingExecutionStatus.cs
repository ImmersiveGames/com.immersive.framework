using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result vocabulary for explicit Pause visual surface ContentAnchor binding execution.
    /// This is logical binding execution only; it does not instantiate prefabs, move transforms, change InputMode or alter Time.timeScale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10D Pause ContentAnchor binding execution status; no visual materialization or Pause toggle integration.")]
    internal enum PauseVisualSurfaceBindingExecutionStatus
    {
        Unknown = 0,
        SucceededBound = 10,
        FailedMissingRuntimeHost = 100,
        FailedMissingRuntimeContentRuntime = 110,
        FailedInvalidContract = 120,
        FailedInvalidRuntimeContext = 130,
        FailedBindingRequest = 140,
        FailedRuntimeHandleDeclaration = 150,
        FailedBindingExecution = 160
    }
}
