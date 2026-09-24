using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Authoring policy for capability blockers active during Route/Activity transitions.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F37 Transition Gate policy; no Pause or TimeScale ownership.")]
    public enum TransitionGateMode
    {
        /// <summary>Transition is visual only and does not create Transition Gate blockers.</summary>
        None = 0,

        /// <summary>Blocks concurrent framework lifecycle/gameflow requests during the transition window.</summary>
        LifecycleRequestsOnly = 10,

        /// <summary>Blocks lifecycle requests plus input and interaction acceptance during the transition window.</summary>
        InputAndInteraction = 20,

        /// <summary>Blocks lifecycle requests, input acceptance, interaction acceptance and gameplay actions during the transition window.</summary>
        InputInteractionAndGameplay = 30
    }
}
