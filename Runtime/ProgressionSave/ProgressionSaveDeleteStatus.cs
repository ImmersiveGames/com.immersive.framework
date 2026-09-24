using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Stable. Result status for deleting Progression Save slot data
    /// through the backend-neutral store port.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Stable,
        "ADR018-A certified Progression Save delete status primitive.")]
    public enum ProgressionSaveDeleteStatus
    {
        Unknown = 0,
        Missing = 10,
        Deleted = 20,
        Rejected = 30,
        BackendUnavailable = 40,
        Failed = 50
    }
}
