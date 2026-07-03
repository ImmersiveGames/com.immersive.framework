using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Result status for writing Progression Save slots or manifest data through the store port.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save write status primitive.")]
    public enum ProgressionSaveWriteStatus
    {
        Unknown = 0,
        Written = 10,
        Rejected = 20,
        BackendUnavailable = 30,
        Failed = 40
    }
}
