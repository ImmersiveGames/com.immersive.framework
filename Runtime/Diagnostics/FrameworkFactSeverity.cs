using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Experimental. Severity for structured framework facts.
    /// This is diagnostics metadata and does not replace human log levels.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal structured diagnostics severity introduced by F1C.")]
    public enum FrameworkFactSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Fatal = 3
    }
}
