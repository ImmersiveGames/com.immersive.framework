using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Result status for explicit PlayerControl binding adapter operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52A PlayerControl binding adapter result status.")]
    public enum PlayerControlBindingStatus
    {
        Failed = 0,
        Succeeded = 1,
        NoOp = 2
    }
}
