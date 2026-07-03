using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kind for side-effect-free Unity Input application plans.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32C InputMode Unity application dry-run plan issue kind.")]
    public enum InputModeUnityApplicationPlanIssueKind
    {
        None = 0,
        ApplicationPreviewNotSucceeded = 10,
        ActionMapPreviewNotSucceeded = 20,
        PreviewModeMismatch = 30,
        UnsupportedInputMode = 40
    }
}
