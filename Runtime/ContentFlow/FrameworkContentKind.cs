using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Declares the concrete materialization family represented by a content handle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public enum FrameworkContentKind
    {
        Unknown = 0,
        Scene = 10,
        Prefab = 20,
        SceneAuthored = 30,
        RuntimeRoot = 40,
        ContentAnchorSlot = 50,
        RuntimeSpawned = 60
    }
}
