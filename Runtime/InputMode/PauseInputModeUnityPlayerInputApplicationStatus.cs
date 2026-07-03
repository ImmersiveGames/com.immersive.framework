using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Status for applying a completed Pause result to an explicit Unity PlayerInput through InputMode.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32G Pause result to explicit Unity PlayerInput application status.")]
    public enum PauseInputModeUnityPlayerInputApplicationStatus
    {
        Unknown = 0,
        Succeeded = 10,
        IgnoredInputModeRequest = 20,
        FailedPauseResultNotCompleted = 30,
        FailedInputModePlayerInputApplication = 40
    }
}
