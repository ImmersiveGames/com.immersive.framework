using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Result status for explicit PlayerView camera activation operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51C PlayerView camera activation adapter result status.")]
    public enum PlayerViewCameraActivationStatus
    {
        Failed = 0,
        Succeeded = 1,
        NoOp = 2
    }
}
