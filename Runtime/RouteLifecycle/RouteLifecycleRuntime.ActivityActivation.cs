using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Loading;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed partial class RouteLifecycleRuntime
    {
        internal Task<ActivityFlowStartResult>
            StartActivityWithActivationGateAsync(
                ActivityAsset activity,
                string source,
                string reason,
                IFrameworkLoadingProgressReporter progressReporter,
                Func<ActivityActivationGateResult> beforeActivation)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(
                    ActivityFlowStartResult.Failed(
                        "No active Route is available."));
            }

            return _activityFlowRuntime
                .StartActivityWithActivationGateAsync(
                    activity,
                    CurrentRoute,
                    source,
                    reason,
                    progressReporter,
                    beforeActivation);
        }
    }
}
