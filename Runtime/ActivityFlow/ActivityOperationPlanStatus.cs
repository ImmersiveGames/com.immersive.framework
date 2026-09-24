using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Aggregate status for a side-effect-free Activity operation plan.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25E Activity operation plan status; no runtime execution implied.")]
    internal enum ActivityOperationPlanStatus
    {
        Unknown = 0,
        NotRequested = 10,
        Planned = 20,
        PlannedWithIssues = 30,
        Blocked = 40
    }
}
