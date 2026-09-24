using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Adapter-facing category of a Transition Effect.
    /// The kind describes what an effect represents; it does not execute visuals or choose an implementation package.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B Transition Effect kind primitive; no concrete visual implementation.")]
    public enum TransitionEffectKind
    {
        /// <summary>Invalid default value. Do not use for canonical effect requests.</summary>
        Unknown = 0,

        Fade = 10,
        Curtain = 20,
        Blackout = 30,
        LoadingScreen = 40,
        LoadingProgress = 50,
        ProgressReport = 60,
        Custom = 100
    }
}
