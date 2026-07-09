using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Diagnostic reason for failed or no-op PlayerControl binding operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52A PlayerControl binding adapter failure/no-op kind.")]
    public enum PlayerControlBindingFailureKind
    {
        None = 0,
        MissingReadinessSummary = 1,
        ControlBindingNotReady = 2,
        MissingPlayerControl = 3,
        InvalidPlayerControl = 4,
        PlayerControlNotInReadinessTopology = 5,
        PlayerControlNotActive = 6,
        PlayerControlNotEligible = 7,
        MissingBindingTarget = 8,
        TargetRejectedBinding = 9,
        TargetPlayerSlotMismatch = 10,
        MissingExistingBinding = 11,
        UnexpectedException = 12
    }
}
