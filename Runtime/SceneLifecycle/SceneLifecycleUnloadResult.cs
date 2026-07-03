using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Immutable result for one scene unload operation.
    /// This is diagnostics data for release execution, not a global scene service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6G scene unload result used by route content release execution.")]
    internal readonly struct SceneLifecycleUnloadResult
    {
        public SceneLifecycleUnloadResult(
            bool completed,
            bool unloaded,
            bool skipped,
            string message,
            string sceneName,
            string scenePath)
        {
            Completed = completed;
            Unloaded = unloaded;
            Skipped = skipped;
            Message = message ?? string.Empty;
            SceneName = sceneName ?? string.Empty;
            ScenePath = scenePath ?? string.Empty;
        }

        public bool Completed { get; }

        public bool Unloaded { get; }

        public bool Skipped { get; }

        public string Message { get; }

        public string SceneName { get; }

        public string ScenePath { get; }

        public static SceneLifecycleUnloadResult UnloadedScene(
            string sceneName,
            string scenePath)
        {
            return new SceneLifecycleUnloadResult(
                true,
                true,
                false,
                $"Scene Lifecycle unloaded Scene '{sceneName}'.",
                sceneName,
                scenePath);
        }

        public static SceneLifecycleUnloadResult SkippedScene(
            string sceneName,
            string scenePath,
            string message)
        {
            return new SceneLifecycleUnloadResult(
                true,
                false,
                true,
                message,
                sceneName,
                scenePath);
        }

        public static SceneLifecycleUnloadResult Failed(string message)
        {
            return new SceneLifecycleUnloadResult(
                false,
                false,
                false,
                message,
                string.Empty,
                string.Empty);
        }
    }
}
