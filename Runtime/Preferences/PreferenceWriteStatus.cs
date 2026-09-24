using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Result status for Preferences write/delete operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences write/delete status.")]
    public enum PreferenceWriteStatus
    {
        Unknown = 0,
        Written = 10,
        Deleted = 20,
        Missing = 30,
        Failed = 40
    }
}
