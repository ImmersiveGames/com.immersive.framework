using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Severity for passive Player binding diagnostic messages.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49L passive Player binding diagnostic severity.")]
    public enum PlayerBindingDiagnosticSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}
