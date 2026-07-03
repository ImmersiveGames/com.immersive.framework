using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result status for the explicit PlayerInput application wrapper that activates input before selecting action maps.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32E Unity PlayerInput application status.")]
    public enum InputModeUnityPlayerInputApplicationStatus
    {
        Unknown = 0,
        Succeeded = 10,
        FailedInvalidPlan = 20,
        FailedMissingPlayerInput = 30,
        FailedMissingActionAsset = 40,
        FailedMissingActionMap = 50,
        FailedPlayerInputActivation = 60,
        FailedPlayerInputAdapter = 70,
        FailedUnsupportedOperation = 80
    }
}
