using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Diagnostic reason for failed or no-op PlayerView camera activation operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51C PlayerView camera activation adapter failure/no-op kind.")]
    public enum PlayerViewCameraActivationFailureKind
    {
        None = 0,
        MissingCameraTargetBindingTarget = 1,
        MissingCameraTargetBinding = 2,
        InvalidCameraTargetBinding = 3,
        MissingViewTarget = 4,
        ViewTargetMismatch = 5,
        MissingCameraActivationTarget = 6,
        MissingCamera = 7,
        CameraMismatch = 8,
        TargetRejectedActivation = 9,
        TargetPlayerSlotMismatch = 10,
        MissingExistingActivation = 11,
        UnexpectedException = 12
    }
}
