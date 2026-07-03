using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Minimal immutable result for a Scene Lifecycle primary scene load.
    /// This is diagnostics data, not a global scene service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct SceneLifecycleLoadResult
    {
        public SceneLifecycleLoadResult(
            bool loaded,
            string message,
            string sceneName,
            string scenePath,
            bool alreadyLoaded,
            string loadMode)
        {
            Loaded = loaded;
            Message = message ?? string.Empty;
            SceneName = sceneName ?? string.Empty;
            ScenePath = scenePath ?? string.Empty;
            AlreadyLoaded = alreadyLoaded;
            LoadMode = loadMode ?? string.Empty;
        }

        public bool Loaded { get; }

        public string Message { get; }

        public string SceneName { get; }

        public string ScenePath { get; }

        public bool AlreadyLoaded { get; }

        public string LoadMode { get; }

        public static SceneLifecycleLoadResult Failed(string message)
        {
            return new SceneLifecycleLoadResult(false, message, string.Empty, string.Empty, false, string.Empty);
        }

        public static SceneLifecycleLoadResult LoadedPrimaryScene(
            string sceneName,
            string scenePath,
            bool alreadyLoaded,
            string loadMode)
        {
            return new SceneLifecycleLoadResult(
                true,
                $"Scene Lifecycle resolved Primary Scene '{sceneName}' and set it active. alreadyLoaded='{alreadyLoaded}'. loadMode='{loadMode}'.",
                sceneName,
                scenePath,
                alreadyLoaded,
                loadMode);
        }

        public static SceneLifecycleLoadResult LoadedAdditiveScene(
            string sceneName,
            string scenePath,
            bool alreadyLoaded,
            string loadMode)
        {
            return new SceneLifecycleLoadResult(
                true,
                $"Scene Lifecycle resolved Additive Scene '{sceneName}'. alreadyLoaded='{alreadyLoaded}'. loadMode='{loadMode}'.",
                sceneName,
                scenePath,
                alreadyLoaded,
                loadMode);
        }
    }
}
