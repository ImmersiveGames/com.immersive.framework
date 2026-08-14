using Immersive.Framework.ActivityFlow;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed partial class RouteLifecycleRuntime
    {
        internal bool TryCreatePendingActivityTransitionPreparationContext(
            out ActivityTransitionPreparationContext context)
        {
            return _activityFlowRuntime
                .TryCreatePendingActivityTransitionPreparationContext(
                    out context);
        }
    }
}
