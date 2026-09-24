using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Stable.
    /// Canonical backend-neutral persistence port required by ProgressionSaveRuntime.
    ///
    /// Third-party and custom save-system adapters implement this core contract
    /// without inheriting catalog/manifest maintenance responsibilities.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Stable,
        "ADR018-A certified core Progression Save backend contract. Breaking changes require ADR/migration.")]
    public interface IProgressionSaveStore
    {
        ProgressionSaveBackendId BackendId { get; }

        ProgressionSaveReadResult ReadSlot(ProgressionSaveSlotId slotId);

        ProgressionSaveWriteResult WriteSlot(ProgressionSaveSlotRecord record);

        ProgressionSaveDeleteResult DeleteSlot(ProgressionSaveSlotId slotId);
    }
}
