using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Aggregate status for an Activity scene composition result.
    /// F25C introduces additive execution while Activity content release remains deferred.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25C Activity scene composition result vocabulary; additive execution, release deferred.")]
    internal enum ActivitySceneCompositionStatus
    {
        Unknown = 0,
        NotRequested = 10,
        Planned = 20,
        PlannedWithIssues = 30,
        Succeeded = 40,
        SucceededWithIssues = 50,
        Failed = 60
    }
}
