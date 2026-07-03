using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Canonical framework input posture vocabulary.
    /// This is not a Unity action map name, PlayerInput state, device pairing state or gameplay command binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A InputMode posture vocabulary; no Unity Input behavior.")]
    public enum InputModeKind
    {
        Unknown = 0,

        /// <summary>
        /// Gameplay command posture. Later adapters may allow gameplay commands to drive gameplay while this mode is active.
        /// </summary>
        Gameplay = 10,

        /// <summary>
        /// Pause overlay posture. Global UI/Pause continuity remains conceptually active while gameplay commands are suppressed by later adapters.
        /// </summary>
        PauseOverlay = 20,

        /// <summary>
        /// Front-end/menu posture reserved for non-gameplay menu surfaces.
        /// </summary>
        FrontendMenu = 30,

        /// <summary>
        /// Hard input suppression posture reserved for transition/loading/exceptional lock scenarios.
        /// </summary>
        InputLocked = 40
    }
}
