using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Severity for Reset module diagnostic issues.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset issue severity.")]
    public enum ResetIssueSeverity
    {
        Unknown = 0,
        Info = 10,
        Warning = 20,
        Error = 30
    }
}
