using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Severity for Object Reset structured diagnostics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14H Object Reset issue severity primitive for logical foundation diagnostics.")]
    public enum ObjectResetIssueSeverity
    {
        Unknown = 0,
        Info = 10,
        Warning = 20,
        Error = 30
    }
}
