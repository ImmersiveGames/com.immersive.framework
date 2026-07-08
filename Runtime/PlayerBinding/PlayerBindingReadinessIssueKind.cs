using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds for passive Player binding readiness summary.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49K passive Player binding readiness issue kinds.")]
    public enum PlayerBindingReadinessIssueKind
    {
        None = 0,
        MissingPlayerTopology = 10,
        MissingPlayerViewTopology = 20,
        MissingPlayerControlTopology = 30,
        PlayerViewTopologyPlayerTopologyMismatch = 40,
        PlayerControlTopologyPlayerTopologyMismatch = 50,
        PlayerTopologyIssue = 60,
        PlayerViewTopologyIssue = 70,
        PlayerControlTopologyIssue = 80,
        NoParticipatingPlayerView = 90,
        NoParticipatingPlayerControl = 100
    }
}
