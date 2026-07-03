using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Do not treat this as a stable materialization or contribution contract before F1/F5/F8.
    /// Declares the lifecycle scope that owns a materialized or discovered content handle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public enum FrameworkContentScope
    {
        Unknown = 0,
        Session = 10,
        Route = 20,
        Activity = 30,
        ContentAnchor = 40,
        RuntimeSpawned = 50
    }
}
