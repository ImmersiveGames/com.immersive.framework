using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Gate
{
    /// <summary>
    /// API status: Experimental. Explicit architectural scope evaluated by Gate.
    /// Scopes are framework lifecycle/content domains, not UI tabs, scene hierarchy paths or GameObject names.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F17B Gate scope primitive; admission scope only, no runtime owner.")]
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
