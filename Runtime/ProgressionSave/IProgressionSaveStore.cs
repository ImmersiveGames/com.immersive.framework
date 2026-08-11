using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental pending ADR018-A certification.
    /// Canonical backend-neutral persistence port required by ProgressionSaveRuntime.
    /// Third-party and custom save-system adapters implement this core contract without
    /// inheriting catalog/manifest authoring responsibilities.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-A core Progression Save backend contract; candidate for Stable after backend-conformance certification.")]
    public interface IProgressionSaveStore
    {
        ProgressionSaveBackendId BackendId { get; }

        ProgressionSaveReadResult ReadSlot(ProgressionSaveSlotId slotId);

        ProgressionSaveWriteResult WriteSlot(ProgressionSaveSlotRecord record);

        ProgressionSaveDeleteResult DeleteSlot(ProgressionSaveSlotId slotId);
    }
}
