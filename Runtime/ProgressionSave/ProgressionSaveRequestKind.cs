using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Supported logical request kinds for the Progression Save runtime path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save runtime request kind primitive.")]
    public enum ProgressionSaveRequestKind
    {
        Unknown = 0,
        Save = 10,
        Load = 20,
        Delete = 30
    }
}
