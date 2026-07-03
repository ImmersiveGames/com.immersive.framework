using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Replaceable storage port for Progression Save.
    /// Concrete JSON, cloud, encrypted or premium plugin backends must implement this port without changing framework consumers.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save backend port; no concrete backend.")]
    public interface IProgressionSaveStore
    {
        ProgressionSaveBackendId BackendId { get; }

        ProgressionSaveManifestReadResult ReadManifest();

        ProgressionSaveManifestWriteResult WriteManifest(ProgressionSaveManifest manifest);

        bool ContainsSlot(ProgressionSaveSlotId slotId);

        ProgressionSaveReadResult ReadSlot(ProgressionSaveSlotId slotId);

        ProgressionSaveWriteResult WriteSlot(ProgressionSaveSlotRecord record);

        ProgressionSaveDeleteResult DeleteSlot(ProgressionSaveSlotId slotId);
    }
}
