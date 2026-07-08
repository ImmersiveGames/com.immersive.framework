using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerControls
{
    /// <summary>
    /// API status: Experimental. Passive PlayerControl state vocabulary.
    /// This is not an input router, movement enabler, action map switcher or gameplay controller.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49I passive PlayerControl state vocabulary.")]
    public enum PlayerControlState
    {
        /// <summary>Stable PlayerSlot control intent is known.</summary>
        Declared = 0,

        /// <summary>Control evidence is bound to an active PlayerEntry, but no gameplay control is activated here.</summary>
        Bound = 10,

        /// <summary>Control evidence is active for diagnostics. This does not enable movement or input routing.</summary>
        Active = 20,

        /// <summary>Control evidence is temporarily suspended and must carry an explicit reason.</summary>
        Suspended = 30,

        /// <summary>Control evidence is released from the current lifecycle.</summary>
        Released = 40
    }
}
