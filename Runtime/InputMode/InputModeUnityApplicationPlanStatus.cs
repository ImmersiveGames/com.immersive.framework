using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result status for building a side-effect-free Unity Input application plan.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32C InputMode Unity application dry-run plan status.")]
    public enum InputModeUnityApplicationPlanStatus
    {
        Unknown = 0,
        Succeeded = 10,
        FailedApplicationPreview = 20,
        FailedActionMapPreview = 30,
        FailedPreviewMismatch = 40,
        FailedUnsupportedInputMode = 50
    }
}
