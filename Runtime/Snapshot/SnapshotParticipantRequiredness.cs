using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Declares whether a missing or failed Snapshot participant blocks a capture/restore orchestration.
    /// This is orchestration metadata only and does not select a backend, slot or payload format.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot participant requiredness primitive; orchestration metadata only.")]
    public enum SnapshotParticipantRequiredness
    {
        Unknown = 0,
        Required = 10,
        Optional = 20
    }
}
