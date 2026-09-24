
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Stable. Diagnostic issue kind for passive InputMode requests.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum InputModeRequestIssueKind
    {
        None = 0,
        InvalidCurrentState = 10,
        InvalidTargetMode = 20
    }
}
