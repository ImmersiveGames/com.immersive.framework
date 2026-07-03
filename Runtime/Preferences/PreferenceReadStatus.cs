using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Result status for Preferences reads.
    /// Missing keys, type mismatch and failures are explicit; there is no silent default fallback.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences read status; no silent fallback.")]
    public enum PreferenceReadStatus
    {
        Unknown = 0,
        Found = 10,
        Missing = 20,
        TypeMismatch = 30,
        Failed = 40
    }
}
