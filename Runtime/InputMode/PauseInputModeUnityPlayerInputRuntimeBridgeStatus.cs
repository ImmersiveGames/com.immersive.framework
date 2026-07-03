using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Status for the explicit opt-in Pause runtime to Unity PlayerInput bridge.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33A opt-in Pause runtime PlayerInput wiring status.")]
    public enum PauseInputModeUnityPlayerInputRuntimeBridgeStatus
    {
        Unknown = 0,
        Succeeded = 10,
        IgnoredInputModeRequest = 20,
        FailedRuntimeUnavailable = 100,
        FailedConfiguration = 110,
        FailedPreflight = 120,
        FailedPauseRequest = 130,
        FailedInputModePlayerInputApplication = 140
    }
}
