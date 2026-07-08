using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Diagnostic reason for failed or no-op PlayerView binding operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51A PlayerView binding adapter failure/no-op kind.")]
    public enum PlayerViewBindingFailureKind
    {
        None = 0,
        MissingReadinessSummary = 1,
        ViewBindingNotReady = 2,
        MissingPlayerView = 3,
        InvalidPlayerView = 4,
        PlayerViewNotInReadinessTopology = 5,
        PlayerViewNotActive = 6,
        PlayerViewNotEligible = 7,
        MissingBindingTarget = 8,
        TargetRejectedBinding = 9,
        TargetPlayerSlotMismatch = 10,
        MissingExistingBinding = 11,
        UnexpectedException = 12
    }
}
