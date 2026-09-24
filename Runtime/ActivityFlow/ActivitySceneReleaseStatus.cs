using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Aggregate status for Activity-owned scene release execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25D Activity scene release result vocabulary.")]
    internal enum ActivitySceneReleaseStatus
    {
        Unknown = 0,
        NotRequested = 10,
        Succeeded = 20,
        SucceededWithIssues = 30,
        Failed = 40
    }
}
