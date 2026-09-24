using System.Threading;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        internal Task<ActivityEntryReadinessWaitResult>
            WaitForActivityEntryReadinessAsync(
                ActivityReadinessOccurrence occurrence,
                CancellationToken cancellationToken)
        {
            return _routeLifecycleRuntime
                .WaitForActivityEntryReadinessAsync(
                    occurrence,
                    cancellationToken);
        }
    }
}
