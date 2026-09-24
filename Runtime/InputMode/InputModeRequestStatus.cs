
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Stable. Deterministic status for a passive InputMode request preview/result.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum InputModeRequestStatus
    {
        Unknown = 0,
        Succeeded = 10,
        IgnoredAlreadyInMode = 20,
        FailedInvalidCurrentState = 30,
        FailedInvalidTargetMode = 40
    }
}
