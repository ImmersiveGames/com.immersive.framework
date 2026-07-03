using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Defines how a Unity PlayerInput gate adapter suppresses gameplay input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F38 Unity PlayerInput Gate Adapter block mode.")]
    public enum UnityPlayerInputGateBlockMode
    {
        Unknown = 0,
        DisableActionMap = 10,
        DeactivatePlayerInput = 20
    }
}
