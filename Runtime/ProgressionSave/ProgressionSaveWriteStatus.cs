using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Stable. Result status for writing one Progression Save slot
    /// through the backend-neutral store port.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "ADR018-A certified Progression Save slot write status primitive.")]
    public enum ProgressionSaveWriteStatus
    {
        Unknown = 0,
        Written = 10,
        Rejected = 20,
        BackendUnavailable = 30,
        Failed = 40
    }
}
