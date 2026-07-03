using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result status for the first explicit Unity PlayerInput adapter side effect.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32D Unity PlayerInput adapter application status.")]
    public enum InputModeUnityPlayerInputAdapterStatus
    {
        Unknown = 0,
        Succeeded = 10,
        FailedInvalidPlan = 20,
        FailedMissingPlayerInput = 30,
        FailedMissingActionAsset = 40,
        FailedMissingActionMap = 50,
        FailedUnsupportedOperation = 60,
        FailedUnityInputException = 70
    }
}
