using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Device-agnostic command intent for Pause input.
    /// This enum does not model Unity Input System phases, action maps or concrete controls.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23D Pause Input command vocabulary; no concrete input binding.")]
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
