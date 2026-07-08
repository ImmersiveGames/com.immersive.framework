using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Message kinds emitted by passive Player binding diagnostic reporting.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49L passive Player binding diagnostic message kinds.")]
    public enum PlayerBindingDiagnosticMessageKind
    {
        None = 0,
        MissingReadinessSummary = 10,
        SummaryAccepted = 20,
        ReadyForViewBinding = 30,
        ReadyForControlBinding = 40,
        ReadyForFullBinding = 50,
        ViewBindingNotReady = 60,
        ControlBindingNotReady = 70,
        FullBindingNotReady = 80,
        ReadinessIssue = 90,
        NoParticipatingPlayerView = 100,
        NoParticipatingPlayerControl = 110,
        PassiveBoundary = 120
    }
}
