using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Status for the opt-in Unity InputAction to Pause runtime PlayerInput bridge trigger.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33B opt-in Unity InputAction trigger for the Pause runtime PlayerInput bridge.")]
    public enum PauseInputActionRuntimeBridgeTriggerStatus
    {
        Unknown = 0,
        Succeeded = 10,
        IgnoredBridgeResult = 20,
        FailedBridgeMissing = 100,
        FailedActionEvidence = 110,
        FailedBridgeRequest = 120
    }
}
