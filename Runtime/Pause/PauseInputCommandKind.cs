
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Device-agnostic command intent for Pause input.
    /// This enum does not model Unity Input System phases, action maps or concrete controls.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum PauseInputCommandKind
    {
        Unknown = 0,
        TogglePause = 10,
        Pause = 20,
        Resume = 30,
        NavigateUp = 100,
        NavigateDown = 110,
        NavigateLeft = 120,
        NavigateRight = 130,
        Submit = 140,
        Cancel = 150,
        Back = 160,
        OpenSettings = 200,
        CloseSettings = 210
    }
}
