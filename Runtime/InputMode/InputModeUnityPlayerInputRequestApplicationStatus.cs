using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. End-to-end status for applying an InputMode request to an explicit Unity PlayerInput instance.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32F InputMode request to explicit Unity PlayerInput application status.")]
    public enum InputModeUnityPlayerInputRequestApplicationStatus
    {
        Unknown = 0,
        Succeeded = 10,
        IgnoredInputModeRequest = 20,
        FailedInputModeRequest = 30,
        FailedUnityApplicationPreview = 40,
        FailedActionMapPreview = 50,
        FailedApplicationPlan = 60,
        FailedPlayerInputApplication = 70
    }
}
