using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Canonical role for an explicitly declared Unity Input integration target.
    /// This enum does not represent Unity action maps, controls, devices, player actors, PlayerInputManager state or gameplay commands.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target role vocabulary; declaration only.")]
    public enum UnityInputTargetRole
    {
        Unknown = 0,

        /// <summary>
        /// Integration target responsible for global UI and Pause intent continuity.
        /// It keeps PauseToggle, Cancel and UI navigation conceptually separate from gameplay commands. A future adapter may validate UI input components here.
        /// </summary>
        GlobalUiPause = 10,

        /// <summary>
        /// Integration target that may later expose Unity PlayerInput evidence for gameplay commands after Activity/Player ownership is explicit.
        /// </summary>
        GameplayCommands = 20
    }
}
