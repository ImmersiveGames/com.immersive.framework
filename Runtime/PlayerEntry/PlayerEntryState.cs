using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerEntry
{
    /// <summary>
    /// API status: Experimental. Passive vocabulary for the PlayerEntry chain.
    /// This is not a coordinator and does not perform Unity PlayerInput join, spawn, view binding or movement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49D passive PlayerEntry state vocabulary.")]
    public enum PlayerEntryState
    {
        /// <summary>Stable PlayerSlot/Actor identities are known by the entry model.</summary>
        Configured = 0,

        /// <summary>Local input/join evidence was observed by a future boundary. This state is passive here.</summary>
        Joined = 10,

        /// <summary>The PlayerSlot has an assigned Actor identity. This does not mean the Actor object exists.</summary>
        Assigned = 20,

        /// <summary>The Actor runtime object exists. This does not mean it is initialized.</summary>
        Instantiated = 30,

        /// <summary>The Actor is ready for view-facing consumers according to Actor readiness evidence.</summary>
        ActorReady = 40,

        /// <summary>A PlayerView is bound. PlayerView ownership is still a future cut.</summary>
        ViewBound = 50,

        /// <summary>The PlayerEntry is active. Control binding remains a future cut.</summary>
        Active = 60,

        /// <summary>The PlayerEntry is temporarily suspended and must carry an explicit suspension reason.</summary>
        Suspended = 70,

        /// <summary>The PlayerEntry is released from the current lifecycle.</summary>
        Released = 80
    }
}
