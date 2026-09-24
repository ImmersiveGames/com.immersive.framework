using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Stable. Result status for backend-neutral Progression Save
    /// read operations. The core store uses it for slot reads; optional catalog
    /// capabilities may reuse the same explicit status vocabulary.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Stable,
        "ADR018-A certified Progression Save read status primitive.")]
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
