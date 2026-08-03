using System.Threading;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed partial class RouteLifecycleRuntime
    {
        internal Task<ActivityEntryReadinessWaitResult>
            WaitForActivityEntryReadinessAsync(
                ActivityReadinessOccurrence occurrence,
                CancellationToken cancellationToken)
        {
            return _activityFlowRuntime
                .WaitForActivityEntryReadinessAsync(
                    occurrence,
                    cancellationToken);
        }
    }
}
