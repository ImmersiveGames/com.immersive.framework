
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kind for InputMode Unity action-map preview.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B InputMode Unity action map preview issue kind.")]
    public enum InputModeUnityActionMapPreviewIssueKind
    {
        None = 0,
        ApplicationPreviewNotSucceeded = 10,
        UnsupportedInputMode = 20,
        MissingActionMapEvidence = 30,
        MissingRequiredActionMap = 40
    }
}
