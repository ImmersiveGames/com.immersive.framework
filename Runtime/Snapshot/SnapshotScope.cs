using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Logical lifecycle scope represented by a Snapshot envelope.
    /// This is not a persistence slot, backend partition, scene name or file path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21B Snapshot scope primitive; passive envelope scope only.")]
    public enum SnapshotScope
    {
        Unknown = 0,
        Session = 10,
        Route = 20,
        Activity = 30,
        Local = 40,
        Runtime = 50
    }
}
