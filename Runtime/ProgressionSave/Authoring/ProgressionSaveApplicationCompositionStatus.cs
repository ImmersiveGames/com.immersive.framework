using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-C Progression Save application composition status.")]
    public enum ProgressionSaveApplicationCompositionStatus
    {
        Unknown = 0,
        Disabled = 10,
        Ready = 20,
        Rejected = 30
    }
}
