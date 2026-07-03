using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Local runtime participant contract for Snapshot capture and restore.
    /// Implementations own their local state conversion. The framework core validates descriptors/results and later orchestration code may aggregate them.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot participant contract; no backend, discovery or orchestration runtime.")]
    public interface ISnapshotParticipant
    {
        /// <summary>
        /// Returns passive identity, owner, schema, requiredness and ordering data.
        /// This method must not save files, read PlayerPrefs, choose a progression slot, load scenes, instantiate objects or mutate gameplay state.
        /// </summary>
        SnapshotParticipantDescriptor GetSnapshotDescriptor();

        /// <summary>
        /// Captures this participant local state into a backend-agnostic envelope.
        /// This method must not persist the envelope. Persistence belongs to Preferences/Progression Save ports in later cuts.
        /// </summary>
        SnapshotParticipantCaptureResult CaptureSnapshot(SnapshotCaptureContext context);

        /// <summary>
        /// Restores this participant local state from a provided backend-agnostic envelope.
        /// This method must not load the envelope from storage. Loading belongs to Progression Save ports in later cuts.
        /// </summary>
        SnapshotParticipantRestoreResult RestoreSnapshot(SnapshotRestoreContext context);
    }
}
