using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Result status for explicit Unity PlayerInput bridge operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52B Unity PlayerInput bridge operation status.")]
    public enum UnityPlayerInputBridgeStatus
    {
        Failed = 0,
        Succeeded = 1,
        NoOp = 2
    }
}
