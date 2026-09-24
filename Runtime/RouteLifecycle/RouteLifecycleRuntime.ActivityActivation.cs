using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Loading;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed partial class RouteLifecycleRuntime
    {
        internal async Task<ActivityFlowStartResult>
            StartActivityWithActivationGateAsync(
                ActivityAsset activity,
                string source,
                string reason,
                IFrameworkLoadingProgressReporter progressReporter,
                Func<ActivityActivationGateResult> beforeActivation)
        {
            if (CurrentRoute == null)
            {
                return ActivityFlowStartResult.Failed(
                    "No active Route is available.");
            }

            ActivityFlowStartResult result = await _activityFlowRuntime
                .StartActivityWithActivationGateAsync(
                    activity,
                    CurrentRoute,
                    source,
                    reason,
                    progressReporter,
                    beforeActivation);
            UpdateCurrentActivityProjection(result);
            return result;
        }
    }
}
