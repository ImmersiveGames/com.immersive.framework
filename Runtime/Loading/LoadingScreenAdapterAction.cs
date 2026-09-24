using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Adapter-facing action for a loading screen presentation update.
    /// This is a visual adapter action only; it does not execute LoadingOperation, SceneLifecycle, Transition or effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22E Loading Screen adapter action boundary; no UI implementation.")]
    public enum LoadingScreenAdapterAction
    {
        Unknown = 0,
        Show = 10,
        Update = 20,
        Hide = 30
    }
}
