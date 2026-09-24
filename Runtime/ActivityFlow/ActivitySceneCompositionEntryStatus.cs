using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Per-scene status recorded by an Activity scene composition result entry.
    /// Planned statuses are side-effect-free; loaded/failed statuses are produced by F25C execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25C Activity scene composition entry vocabulary; additive execution, release deferred.")]
    internal enum ActivitySceneCompositionEntryStatus
    {
        Unknown = 0,
        Planned = 10,
        NotExecutionReady = 20,
        Loaded = 30,
        AlreadyLoaded = 40,
        Failed = 50,
        Skipped = 60
    }
}
