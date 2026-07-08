using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Result status for explicit PlayerView camera-target binding operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51B PlayerView camera-target binding adapter result status.")]
    public enum PlayerViewCameraTargetBindingStatus
    {
        Failed = 0,
        Succeeded = 1,
        NoOp = 2
    }
}
