using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed partial class RouteLifecycleRuntime
    {
        internal int PreviewRouteLoadingProgressStepCount(
            RouteAsset route,
            string source,
            string reason)
        {
            if (route == null || !route.HasPrimaryScene)
            {
                return 0;
            }

            RouteRuntimeState previousRouteState = _currentRouteState;
            RouteAsset previousRoute = previousRouteState.Route;
            ActivityAsset previousActivity = _activityFlowRuntime.CurrentActivity;
            ActivityOperationResult startupOperationPreview =
                PreviewRouteStartupActivityOperation(
                    route,
                    previousActivity,
                    source,
                    reason);
            if (startupOperationPreview.IsBlocked)
            {
                return 0;
            }

            ContentReleasePlan releasePlan = previousRouteState.HasRouteContent
                ? previousRouteState.RouteContentSet.CreateReleasePlan(
                    source,
                    reason)
                : ContentReleasePlan.Empty(
                    FrameworkContentScope.Route,
                    string.Empty,
                    previousRoute != null
                        ? previousRoute.RouteName
                        : string.Empty,
                    source,
                    reason,
                    "No previous Route content is active; release plan is empty.");
            RouteSceneCompositionPlan sceneCompositionPlan =
                RouteSceneCompositionPlan.FromRoute(route, source, reason);

            int activitySceneReleaseCount =
                _activityFlowRuntime
                    .PreviewActivitySceneReleaseForRouteChangeCount();
            int routeContentReleaseCount = releasePlan.ReleasableCount;
            int routeSceneLoadCount =
                CountRouteSceneCompositionProgressSteps(
                    sceneCompositionPlan);
            int startupActivityProgressCount =
                startupOperationPreview.IsValid
                    ? startupOperationPreview.SceneSideEffectCount
                    : 0;

            return checked(
                activitySceneReleaseCount +
                routeContentReleaseCount +
                routeSceneLoadCount +
                startupActivityProgressCount);
        }

        internal int PreviewActivityLoadingProgressStepCount(
            ActivityAsset activity,
            string source,
            string reason)
        {
            if (CurrentRoute == null || activity == null)
            {
                return 0;
            }

            ActivityAsset previousActivity = CurrentActivity;
            ActivityOperationKind operationKind = previousActivity != null
                ? ActivityOperationKind.Switch
                : ActivityOperationKind.Start;
            ActivityOperationResult preview =
                _activityFlowRuntime.PreviewActivityOperation(
                    operationKind,
                    previousActivity,
                    activity,
                    activity.VisualTransitionMode,
                    source,
                    reason);

            return preview.IsValid && !preview.IsBlocked
                ? preview.SceneSideEffectCount
                : 0;
        }
    }
}
