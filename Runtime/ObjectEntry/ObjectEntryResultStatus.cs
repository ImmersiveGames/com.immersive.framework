using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Aggregate status for object entry contract results.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry result status introduced by F13A.")]
    public enum ObjectEntryResultStatus
    {
        Unknown = 0,
        Accepted = 10,
        AcceptedWithWarnings = 20,
        Rejected = 30
    }
}
