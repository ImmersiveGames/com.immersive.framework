using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.Loading;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityFlowRuntime
    {
        internal Task<ActivityFlowStartResult>
            StartActivityWithActivationGateAsync(
                ActivityAsset activity,
                RouteAsset route,
                string source,
                string reason,
                IFrameworkLoadingProgressReporter progressReporter,
                Func<ActivityActivationGateResult> beforeActivation)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (activity == null)
            {
                return Task.FromResult(
                    ActivityFlowStartResult.Failed(
                        "Activity is missing."));
            }

            if (route != null)
            {
                SetRouteContext(route);
            }

            return StartActivityCoreAsync(
                activity,
                _currentActivityState.Activity,
                resolvedSource,
                resolvedReason,
                progressReporter: progressReporter,
                beforeActivation: beforeActivation);
        }

        private async Task<string> RollbackPreparedTargetScenesAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            var issues = new List<string>();
            ActivitySceneReleaseResult release =
                await ReleasePreviousActivityScenesAsync(
                    targetActivity,
                    source,
                    reason,
                    NoOpFrameworkLoadingProgressReporter.Instance);
            if (release.HasBlockingIssues)
            {
                issues.Add(release.ToDiagnosticString());
            }

            return issues.Count == 0
                ? string.Empty
                : " Target Activity scene rollback failed. " +
                  string.Join(" | ", issues);
        }
    }
}
