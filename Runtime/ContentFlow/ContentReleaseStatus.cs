using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Aggregate release result status. F6F introduces the model without executing release.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release result status vocabulary; execution deferred.")]
    internal enum ContentReleaseStatus
    {
        NotExecuted = 0,
        Succeeded = 10,
        SucceededWithIssues = 20,
        Failed = 30
    }
}
