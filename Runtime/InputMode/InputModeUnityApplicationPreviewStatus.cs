using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result status for previewing how a logical InputMode request maps to official Unity Input evidence.
    /// This is not action-map switching and does not mutate PlayerInput.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32A InputMode Unity application preview status.")]
    public enum InputModeUnityApplicationPreviewStatus
    {
        Unknown = 0,
        Succeeded = 10,
        FailedInputModeRequest = 20,
        FailedUnsupportedMode = 30,
        FailedTargetEvidence = 40,
        FailedPlayerActorEvidence = 50,
        FailedLocalPlayerProvisioning = 60
    }
}
