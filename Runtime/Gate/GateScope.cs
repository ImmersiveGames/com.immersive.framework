
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Stable. Explicit architectural scope evaluated by Gate.
    /// Scopes are framework lifecycle/content domains, not UI tabs, scene hierarchy paths or GameObject names.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum GateScope
    {
        /// <summary>Invalid default value. Do not use for canonical Gate evaluations.</summary>
        Unknown = 0,

        Session = 10,
        Route = 20,
        Activity = 30,
        GameFlow = 40,
        Scene = 50,
        Content = 60,
        Input = 70,
        Interaction = 80,
        Gameplay = 90,
        Pause = 100,
        Transition = 110
    }
}
