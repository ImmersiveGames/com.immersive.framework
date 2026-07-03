using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Adapter-facing contract for presenting canonical LoadingOperation state on a loading screen.
    /// Implementations may drive concrete UI, but they must not execute SceneLifecycle, Transition, TransitionEffects, readiness mutation or LoadingOperation orchestration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22E Loading Screen adapter boundary; no built-in UI/prefab implementation.")]
    public interface ILoadingScreenAdapter
    {
        /// <summary>Human-readable adapter name for diagnostics.</summary>
        string AdapterName { get; }

        /// <summary>Returns true when this adapter can present the supplied loading operation.</summary>
        bool Supports(LoadingOperation operation);

        /// <summary>Shows the loading screen for a canonical Loading presentation.</summary>
        LoadingScreenAdapterResult Show(LoadingScreenPresentation presentation);

        /// <summary>Updates the loading screen from canonical Loading progress.</summary>
        LoadingScreenAdapterResult Update(LoadingScreenPresentation presentation);

        /// <summary>Hides the loading screen for a canonical Loading presentation.</summary>
        LoadingScreenAdapterResult Hide(LoadingScreenPresentation presentation);
    }
}
