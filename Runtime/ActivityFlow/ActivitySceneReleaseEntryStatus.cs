using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Per-scene status recorded by Activity-owned scene release execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25D Activity scene release entry vocabulary.")]
    internal enum ActivitySceneReleaseEntryStatus
    {
        Unknown = 0,
        Unloaded = 10,
        SkippedKeepOnActivityChange = 20,
        SkippedNotLoaded = 30,
        Failed = 40
    }
}
