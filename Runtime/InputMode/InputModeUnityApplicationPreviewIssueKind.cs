using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kinds for F32A InputMode Unity application preview.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32A InputMode Unity application preview issue kinds.")]
    public enum InputModeUnityApplicationPreviewIssueKind
    {
        None = 0,
        InputModeRequestNotSucceeded = 10,
        UnsupportedInputMode = 20,
        MissingRequiredUnityInputTarget = 30,
        InvalidUnityInputTargetEvidence = 40,
        MissingRequiredPlayerActor = 50,
        InvalidPlayerActorEvidence = 60,
        MissingRequiredLocalPlayerProvisioning = 70,
        InvalidLocalPlayerProvisioning = 80
    }
}
