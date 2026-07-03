using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Diagnostic issue kind for passive InputMode requests.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A InputMode request issue vocabulary.")]
    public enum InputModeRequestIssueKind
    {
        None = 0,
        InvalidCurrentState = 10,
        InvalidTargetMode = 20
    }
}
