using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Result status for reading Progression Save slots or manifest data through the store port.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save read status primitive.")]
    public enum ProgressionSaveReadStatus
    {
        Unknown = 0,
        Missing = 10,
        Found = 20,
        Rejected = 30,
        Corrupt = 40,
        BackendUnavailable = 50,
        Failed = 60
    }
}
