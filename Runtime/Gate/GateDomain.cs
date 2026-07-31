
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Stable. Explicit admission domain evaluated by Gate.
    /// The domain describes what kind of operation is being admitted, not which object executes it.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum GateDomain
    {
        /// <summary>Invalid default value. Do not use for canonical Gate evaluations.</summary>
        Unknown = 0,

        LifecycleRequest = 10,
        SceneTransition = 20,
        ContentRequest = 30,
        InputAcceptance = 40,
        InteractionAcceptance = 50,
        GameplayAction = 60,
        PauseRequest = 70,
        TransitionOperation = 80,
        UiNavigation = 90,
        SystemRequest = 100
    }
}
