using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. High-level runtime request result status mapped from the active Progression Save store.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save runtime request result status primitive.")]
    public enum ProgressionSaveRequestStatus
    {
        Unknown = 0,
        Saved = 10,
        Loaded = 20,
        Deleted = 30,
        Missing = 40,
        Rejected = 50,
        BackendUnavailable = 60,
        Corrupt = 70,
        Failed = 80
    }
}
