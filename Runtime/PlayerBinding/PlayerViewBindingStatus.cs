using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Result status for explicit PlayerView binding adapter operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51A PlayerView binding adapter result status.")]
    public enum PlayerViewBindingStatus
    {
        Failed = 0,
        Succeeded = 1,
        NoOp = 2
    }
}
