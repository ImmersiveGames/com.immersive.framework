using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Severity for passive Loading issues.
    /// Issues report operation/readiness problems; they do not execute fallback behavior or mutate lifecycle state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22H Loading issue severity primitive; reporting only.")]
    public enum LoadingIssueSeverity
    {
        Unknown = 0,
        Info = 10,
        Warning = 20,
        Error = 30,
        Blocking = 40
    }
}
