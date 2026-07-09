using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Failure and no-op reasons for explicit Unity PlayerInput bridge operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52B Unity PlayerInput bridge failure/no-op reasons.")]
    public enum UnityPlayerInputBridgeFailureKind
    {
        None = 0,
        MissingPlayerControlBindingTarget = 1,
        MissingPlayerControlBinding = 2,
        InvalidPlayerControlBinding = 3,
        MissingUnityPlayerInputBridgeTarget = 4,
        MissingUnityPlayerInput = 5,
        MissingExpectedPlayerSlot = 6,
        PlayerSlotMismatch = 7,
        TargetRejectedBridge = 8,
        TargetPlayerSlotMismatch = 9,
        MissingExistingBridge = 10,
        UnexpectedException = 99
    }
}
