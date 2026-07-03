using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Severity for a side-effect-free Activity operation planning issue.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation issue severity; planning diagnostics only.")]
    internal enum ActivityOperationIssueSeverity
    {
        None = 0,
        Warning = 10,
        Blocking = 20
    }
}
