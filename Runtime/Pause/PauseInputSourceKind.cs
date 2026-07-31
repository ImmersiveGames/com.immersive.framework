
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Coarse source category for Pause input diagnostics.
    /// It is intentionally device-agnostic and does not select or require a concrete input package.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum PauseInputSourceKind
    {
        Unknown = 0,
        Keyboard = 10,
        Mouse = 20,
        Gamepad = 30,
        Touch = 40,
        XR = 50,
        UI = 60,
        External = 70,
        Synthetic = 80
    }
}
