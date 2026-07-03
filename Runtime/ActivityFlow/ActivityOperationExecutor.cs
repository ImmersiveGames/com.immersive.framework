using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// F25F validation-only executor facade.
    /// Later cuts will move Activity transition/loading/release/load/state sequencing here.
    /// This cut intentionally produces only a result preview and performs no side effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25F Activity operation executor facade; validation-only, no side effects.")]
    internal sealed class ActivityOperationExecutor
    {
        internal ActivityOperationResult Preview(ActivityOperationPlan plan)
        {
            return ActivityOperationResult.FromPlan(plan);
        }
    }
}
