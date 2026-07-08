using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Diagnostic reason for failed or no-op PlayerView camera-target binding operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51B PlayerView camera-target binding adapter failure/no-op kind.")]
    public enum PlayerViewCameraTargetBindingFailureKind
    {
        None = 0,
        MissingPlayerViewBindingTarget = 1,
        MissingPlayerViewBinding = 2,
        InvalidPlayerViewBinding = 3,
        MissingPlayerViewBehaviour = 4,
        PlayerSlotMismatch = 5,
        MissingViewTarget = 6,
        ViewTargetMismatch = 7,
        MissingCameraTargetBindingTarget = 8,
        TargetRejectedBinding = 9,
        TargetPlayerSlotMismatch = 10,
        MissingExistingBinding = 11,
        UnexpectedException = 12
    }
}
