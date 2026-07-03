using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Aggregate status for a Route scene composition result.
    /// F6E records result vocabulary used by Route scene composition execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6E route scene composition result vocabulary; release is deferred.")]
    internal enum RouteSceneCompositionStatus
    {
        Unknown = 0,
        NotExecuted = 10,
        Succeeded = 20,
        SucceededWithIssues = 30,
        Failed = 40
    }
}
