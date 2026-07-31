using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Explicit typed snapshot for the active Route scope.
    /// This is state data owned by Route Lifecycle, not a service locator, manager, loader or release executor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route state boundary introduced by F3B; not game-facing API.")]
    internal readonly struct RouteRuntimeState
    {
        public RouteRuntimeState(
            RouteAsset route,
            FrameworkIdentityKey routeIdentity,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteSceneCompositionResult routeSceneCompositionResult,
            RouteContentSet routeContentSet,
            ActivityFlowStartResult activityFlowResult,
            ActivityFlowRuntime activityFlowRuntime,
            string source,
            string reason,
            bool entered)
        {
            Route = route;
            RouteIdentity = routeIdentity;
            SceneLifecycleResult = sceneLifecycleResult;
            RouteSceneCompositionResult = routeSceneCompositionResult;
            RouteContentSet = routeContentSet;
            ActivityFlowResult = activityFlowResult;
            ActivityFlowRuntime = activityFlowRuntime;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Entered = entered;
        }

        public RouteAsset Route { get; }

        public FrameworkIdentityKey RouteIdentity { get; }

        public SceneLifecycleLoadResult SceneLifecycleResult { get; }

        public RouteSceneCompositionResult RouteSceneCompositionResult { get; }

        public RouteContentSet RouteContentSet { get; }

        public ActivityFlowStartResult ActivityFlowResult { get; }

        internal ActivityFlowRuntime ActivityFlowRuntime { get; }

        public ActivityContentSet ActivityContentSet => ActivityFlowResult.ActivityContentSet;

        public ActivityContentLifecycleResult ActivityContentLifecycleResult => ActivityFlowResult.ActivityContentLifecycleResult;

        public ActivityReadinessState ActivityReadinessState => ActivityFlowResult.ActivityReadinessState;

        public string Source { get; }

        public string Reason { get; }

        public bool Entered { get; }

        public ActivityRuntimeState ActivityState => ActivityFlowResult.ActivityState;

        public ActivityAsset CurrentActivity => ActivityState.Activity;

        public bool HasRoute => Route != null;

        public bool HasIdentity => RouteIdentity.IsValid;

        public bool HasRouteContent => RouteContentSet.HasContent;

        public bool HasActiveActivity => ActivityState.IsActive;

        internal ActivityReadinessOccurrence CurrentOccurrence =>
            ActivityFlowRuntime != null ? ActivityFlowRuntime.CurrentOccurrence : default;

        public bool HasActivityContent => ActivityContentSet.HasContent;

        public bool HasActivityContentLifecycle => ActivityContentLifecycleResult.Executed;

        public bool HasActivityReadiness => ActivityReadinessState.IsReady || ActivityReadinessState.IsNone || ActivityReadinessState.IsNotReady;

        public bool IsActivityReady => ActivityReadinessState.IsReady;

        public bool HasActivityIdentity => ActivityState.HasIdentity;

        public string RouteName => Route != null ? Route.RouteName : string.Empty;

        public string CurrentActivityName => ActivityState.ActivityName;

        public string CurrentActivityIdentity => ActivityState.DiagnosticIdentity;

        public string SceneName => SceneLifecycleResult.SceneName;

        public string ScenePath => SceneLifecycleResult.ScenePath;

        public string DiagnosticIdentity => HasIdentity ? RouteIdentity.StableText : string.Empty;

        public static RouteRuntimeState Empty()
        {
            return default;
        }

        public static RouteRuntimeState EnteredWith(
            RouteAsset route,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteSceneCompositionResult routeSceneCompositionResult,
            RouteContentSet routeContentSet,
            ActivityFlowStartResult activityFlowResult,
            string source,
            string reason)
        {
            return EnteredWith(
                route,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                activityFlowResult,
                null,
                source,
                reason);
        }

        public static RouteRuntimeState EnteredWith(
            RouteAsset route,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteSceneCompositionResult routeSceneCompositionResult,
            RouteContentSet routeContentSet,
            ActivityFlowStartResult activityFlowResult,
            ActivityFlowRuntime activityFlowRuntime,
            string source,
            string reason)
        {
            return new RouteRuntimeState(
                route,
                CreateRouteIdentity(route),
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                activityFlowResult,
                activityFlowRuntime,
                source,
                reason,
                true);
        }

        internal RouteRuntimeState WithActivityFlowResult(ActivityFlowStartResult activityFlowResult)
        {
            return new RouteRuntimeState(
                Route,
                RouteIdentity,
                SceneLifecycleResult,
                RouteSceneCompositionResult,
                RouteContentSet,
                activityFlowResult,
                ActivityFlowRuntime,
                Source,
                Reason,
                Entered);
        }

        private static FrameworkIdentityKey CreateRouteIdentity(RouteAsset route)
        {
            if (route == null)
            {
                throw new System.ArgumentNullException(nameof(route));
            }

            if (!route.HasValidRouteId)
            {
                throw new System.ArgumentException("Route identity requires a valid RouteId.", nameof(route));
            }

            return FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, route.RouteId.StableText);
        }
    }
}
