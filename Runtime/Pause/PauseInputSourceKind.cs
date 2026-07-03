using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Coarse source category for Pause input diagnostics.
    /// It is intentionally device-agnostic and does not select or require a concrete input package.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23D Pause Input source vocabulary; diagnostics only.")]
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
