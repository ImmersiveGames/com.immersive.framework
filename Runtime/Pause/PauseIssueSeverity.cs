using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Severity of a Pause policy/result issue.
    /// Blocking issues prevent a Pause state change; warnings do not.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B Pause issue severity primitive.")]
    public enum PauseIssueSeverity
    {
        Unknown = 0,
        Info = 10,
        Warning = 20,
        Blocking = 30
    }
}
