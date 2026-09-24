using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Result status for the F25E Activity operation planning baseline.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation result status; execution is deferred.")]
    internal enum ActivityOperationResultStatus
    {
        Unknown = 0,
        NotRequested = 10,
        Planned = 20,
        Blocked = 30
    }
}
