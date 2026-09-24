using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Coarse payload representation carried by a Snapshot envelope.
    /// This is not a backend selection, file extension or canonical JSON declaration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21B Snapshot payload format primitive; backend-agnostic.")]
    public enum SnapshotPayloadFormat
    {
        Unknown = 0,
        Empty = 10,
        Binary = 20,
        Text = 30,
        Structured = 40
    }
}
