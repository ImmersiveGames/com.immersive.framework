using System;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Loading;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityFlowRuntime
    {
        internal Task<ActivityFlowStartResult>
            StartStartupActivityWithActivationGateAsync(
                RouteAsset route,
                string source,
                string reason,
                IFrameworkLoadingProgressReporter progressReporter,
                Func<ActivityActivationGateResult> beforeActivation)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route == null)
            {
                return Task.FromResult(
                    ActivityFlowStartResult.Failed(
                        "Route is missing."));
            }

            if (!route.HasStartupActivity)
            {
                return Task.FromResult(
                    ActivityFlowStartResult.Failed(
                        "Route Startup Activity activation gate requires a Startup Activity."));
            }

            if (beforeActivation == null)
            {
                return Task.FromResult(
                    ActivityFlowStartResult.Failed(
                        "Route Startup Activity activation gate callback is missing."));
            }

            SetRouteContext(route);
            ActivityAsset previousActivity = _currentActivityState.Activity;
            ActivityAsset startupActivity = route.StartupActivity;
            ActivityOperationResult operationPreview =
                PreviewActivityOperation(
                    ActivityOperationKind.RouteStartup,
                    previousActivity,
                    startupActivity,
                    ResolveActivityTransitionMode(startupActivity),
                    resolvedSource,
                    resolvedReason);
            if (operationPreview.IsBlocked)
            {
                return Task.FromResult(
                    ActivityFlowStartResult.Failed(
                        "Route Startup Activity blocked by ActivityOperationPlan. " +
                        operationPreview.ToDiagnosticString(),
                        operationPreview));
            }

            return StartActivityCoreAsync(
                startupActivity,
                previousActivity,
                resolvedSource,
                resolvedReason,
                operationPreview,
                progressReporter,
                beforeActivation);
        }

        internal RouteStartupActivityScopeFinalizationResult
            FinalizeRouteStartupPreviousActivityScope(
                ActivityAsset previousActivity,
                ActivityAsset targetActivity,
                string source,
                string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);
            if (previousActivity == null || targetActivity == null)
            {
                return new RouteStartupActivityScopeFinalizationResult(
                    default,
                    null,
                    "Route Startup previous Activity scope finalization requires both Activities.");
            }

            ContentAnchorBindingLifecycleResult bindingCleanup =
                CleanupPreviousActivityContentAnchorBindings(
                    previousActivity,
                    targetActivity,
                    resolvedSource,
                    resolvedReason);
            RuntimeRootRegistryOperationResult scopeRemoval =
                RemovePreviousActivityScopeRoot(
                    previousActivity,
                    targetActivity,
                    resolvedSource,
                    resolvedReason);
            return new RouteStartupActivityScopeFinalizationResult(
                bindingCleanup,
                scopeRemoval,
                bindingCleanup.Succeeded &&
                scopeRemoval != null &&
                !scopeRemoval.Rejected
                    ? "Previous Activity scope finalized after Route Startup Player handoff commit."
                    : "Previous Activity scope finalization failed after Route Startup Player handoff commit.");
        }
    }
}
