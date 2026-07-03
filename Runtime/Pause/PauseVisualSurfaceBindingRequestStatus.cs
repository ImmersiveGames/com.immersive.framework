using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result status for deriving an explicit ContentAnchor binding request from a Pause visual surface contract.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Pause visual surface binding request status; request-only, no binding execution.")]
    public enum PauseVisualSurfaceBindingRequestStatus
    {
        Unknown = 0,

        SucceededCreated = 10,

        RejectedInvalidContract = 100,
        RejectedInvalidRuntimeContext = 110,
        RejectedMismatchedRuntimeOwner = 120
    }
}
