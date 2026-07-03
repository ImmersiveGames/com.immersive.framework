using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Result status for one Snapshot participant capture or restore operation.
    /// This is not a persistence result and does not imply that any file, PlayerPrefs key or backend record was written.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot participant result status; participant-only diagnostics.")]
    public enum SnapshotParticipantResultStatus
    {
        Unknown = 0,
        Captured = 10,
        Restored = 20,
        Skipped = 30,
        Rejected = 40,
        Failed = 50
    }
}
