
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result status for previewing an InputMode action-map binding against Unity action map evidence.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B InputMode Unity action map preview status.")]
    public enum InputModeUnityActionMapPreviewStatus
    {
        Unknown = 0,
        Succeeded = 10,
        FailedApplicationPreview = 20,
        FailedUnsupportedMode = 30,
        FailedActionMapEvidence = 40
    }
}
