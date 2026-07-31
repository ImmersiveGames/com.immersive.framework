
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Severity of a Pause policy/result issue.
    /// Blocking issues prevent a Pause state change; warnings do not.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum PauseIssueSeverity
    {
        Unknown = 0,
        Info = 10,
        Warning = 20,
        Blocking = 30
    }
}
