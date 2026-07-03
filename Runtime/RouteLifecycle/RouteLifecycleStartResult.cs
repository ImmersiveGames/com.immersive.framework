using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal immutable result for starting a Route.
    /// This is diagnostics data for route lifecycle ownership, not a global route service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct RouteLifecycleStartResult
    {
        public RouteLifecycleStartResult(
            bool started,
            string message,
            RouteAsset route,
            RouteAsset previousRoute,
            RouteExitResult routeExitResult,
            RouteRuntimeState routeState,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteSceneCompositionResult routeSceneCompositionResult,
            RouteContentSet routeContentSet,
            ContentAnchorDiscoveryResult contentAnchorDiscoveryResult,
            RouteContentLifecycleDispatchResult routeContentEnterResult,
            RouteContentLifecycleDispatchResult routeContentExitResult,
            ContentReleaseResult contentReleaseResult,
            ActivityFlowStartResult activityFlowResult,
            RuntimeScopeLifecycleResult runtimeRouteScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult routeContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult),
            ActivitySceneReleaseResult activitySceneRouteReleaseResult = default(ActivitySceneReleaseResult))
        {
            Started = started;
            Message = message ?? string.Empty;
            Route = route;
            PreviousRoute = previousRoute;
            RouteExitResult = routeExitResult;
            RouteState = routeState;
            SceneLifecycleResult = sceneLifecycleResult;
            RouteSceneCompositionResult = routeSceneCompositionResult;
            RouteContentSet = routeContentSet;
            ContentAnchorDiscoveryResult = contentAnchorDiscoveryResult;
            RouteContentEnterResult = routeContentEnterResult;
            RouteContentExitResult = routeContentExitResult;
            ContentReleaseResult = contentReleaseResult;
            ActivityFlowResult = activityFlowResult;
            ActivitySceneRouteReleaseResult = activitySceneRouteReleaseResult;
            RuntimeRouteScopeResult = runtimeRouteScopeResult;
            RouteContentAnchorBindingCleanupResult = routeContentAnchorBindingCleanupResult;
        }

        public bool Started { get; }

        public string Message { get; }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }

        public bool ReplacedPreviousRoute => PreviousRoute != null;

        public RouteExitResult RouteExitResult { get; }

        public bool HasRouteExitResult => RouteExitResult.HasRoute;

        public RouteRuntimeState RouteState { get; }

        public SceneLifecycleLoadResult SceneLifecycleResult { get; }

        public RouteSceneCompositionResult RouteSceneCompositionResult { get; }

        public RouteContentSet RouteContentSet { get; }

        public ContentAnchorDiscoveryResult ContentAnchorDiscoveryResult { get; }

        public ContentAnchorSet ContentAnchorSet => ContentAnchorDiscoveryResult.AnchorSet;

        public RouteContentLifecycleDispatchResult RouteContentEnterResult { get; }

        public RouteContentLifecycleDispatchResult RouteContentExitResult { get; }

        public ContentReleaseResult ContentReleaseResult { get; }

        public ActivityFlowStartResult ActivityFlowResult { get; }

        public ActivitySceneReleaseResult ActivitySceneRouteReleaseResult { get; }

        public RuntimeScopeLifecycleResult RuntimeRouteScopeResult { get; }

        public ContentAnchorBindingLifecycleResult RouteContentAnchorBindingCleanupResult { get; }

        public bool HasRuntimeRouteScope => RuntimeRouteScopeResult.Executed;

        public static RouteLifecycleStartResult Failed(string message)
        {
            return new RouteLifecycleStartResult(false, message, null, null, default, default, default, default, default, default, default, default, default, default);
        }

        public static RouteLifecycleStartResult StartedWith(
            RouteAsset route,
            RouteRuntimeState previousRouteState,
            SceneLifecycleLoadResult sceneLifecycleResult,
            RouteSceneCompositionResult routeSceneCompositionResult,
            RouteContentSet routeContentSet,
            ContentAnchorDiscoveryResult contentAnchorDiscoveryResult,
            RouteContentLifecycleDispatchResult routeContentEnterResult,
            RouteContentLifecycleDispatchResult routeContentExitResult,
            ContentReleaseResult contentReleaseResult,
            ActivityFlowStartResult activityFlowResult,
            string source,
            string reason,
            RuntimeScopeLifecycleResult runtimeRouteScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult routeContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult),
            ActivitySceneReleaseResult activitySceneRouteReleaseResult = default(ActivitySceneReleaseResult))
        {
            var previousRoute = previousRouteState.Route;
            var routeExitResult = RouteExitResult.Exited(previousRouteState, route, source, reason);
            var routeState = RouteRuntimeState.EnteredWith(
                route,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                contentAnchorDiscoveryResult,
                activityFlowResult,
                source,
                reason);
            string routeStateMessage = routeState.HasIdentity ? $" routeIdentity='{routeState.DiagnosticIdentity}'." : string.Empty;
            string routeExitMessage = routeExitResult.HasRoute ? $" routeExit='{routeExitResult.DiagnosticStatus}'." : string.Empty;
            string routeContentExitMessage = routeContentExitResult.Executed
                ? $" routeContentExit='{routeContentExitResult.DiagnosticStatus}' routeContentExitBindings='{routeContentExitResult.BindingCount}' routeContentExitReceivers='{routeContentExitResult.ReceiverCount}' routeContentExitFailed='{routeContentExitResult.FailedReceiverCount}'."
                : string.Empty;
            string releaseMessage = contentReleaseResult.Completed || contentReleaseResult.NotExecuted
                ? $" routeRelease='{contentReleaseResult.Status}' routeReleaseReleased='{contentReleaseResult.ReleasedCount}' routeReleaseSkipped='{contentReleaseResult.SkippedCount}' routeReleaseFailed='{contentReleaseResult.FailedCount}' routeReleaseBlockingIssues='{contentReleaseResult.BlockingIssueCount}'."
                : string.Empty;
            string activitySceneRouteReleaseMessage = activitySceneRouteReleaseResult.Executed
                ? $" routeActivitySceneRelease='{activitySceneRouteReleaseResult.DiagnosticStatus}' routeActivitySceneReleaseScenes='{activitySceneRouteReleaseResult.SceneCount}' routeActivitySceneReleaseReleased='{activitySceneRouteReleaseResult.ReleasedSceneCount}' routeActivitySceneReleaseSkipped='{activitySceneRouteReleaseResult.SkippedSceneCount}' routeActivitySceneReleaseFailed='{activitySceneRouteReleaseResult.FailedSceneCount}' routeActivitySceneReleaseBlockingIssues='{activitySceneRouteReleaseResult.BlockingIssueCount}'."
                : string.Empty;
            string routeContentMessage = routeContentSet.HasContent ? $" {routeContentSet.DiagnosticMessage}" : string.Empty;
            string contentAnchorMessage = !string.IsNullOrWhiteSpace(contentAnchorDiscoveryResult.Message)
                ? $" {contentAnchorDiscoveryResult.DiagnosticMessage}"
                : string.Empty;
            string compositionMessage = !string.IsNullOrWhiteSpace(routeSceneCompositionResult.Message) ? $" {routeSceneCompositionResult.Message}" : string.Empty;
            string routeContentEnterMessage = routeContentEnterResult.Executed
                ? $" routeContentEnter='{routeContentEnterResult.DiagnosticStatus}' routeContentEnterBindings='{routeContentEnterResult.BindingCount}' routeContentEnterReceivers='{routeContentEnterResult.ReceiverCount}' routeContentEnterFailed='{routeContentEnterResult.FailedReceiverCount}'."
                : string.Empty;
            string runtimeRouteMessage = runtimeRouteScopeResult.Executed
                ? $" runtimeRouteScope='{runtimeRouteScopeResult.DiagnosticStatus}' runtimeRouteRootEnter='{runtimeRouteScopeResult.EnterStatus}' runtimeRouteRootExit='{runtimeRouteScopeResult.ExitStatus}' runtimeRouteContext='{runtimeRouteScopeResult.ContextStatus}' runtimeRootCount='{runtimeRouteScopeResult.RootCount}'."
                : string.Empty;
            string routeBindingCleanupMessage = BindingCleanupMessage("routeContentAnchorBindingCleanup", routeContentAnchorBindingCleanupResult);
            string activityMessage = !string.IsNullOrWhiteSpace(activityFlowResult.Message) ? $" {activityFlowResult.Message}" : string.Empty;
            string message = previousRoute != null
                ? $"Route Lifecycle switched from Route '{previousRoute.RouteName}' to Route '{route.RouteName}'.{routeStateMessage}{routeExitMessage}{routeContentExitMessage}{releaseMessage}{activitySceneRouteReleaseMessage} {sceneLifecycleResult.Message}{compositionMessage}{routeContentMessage}{contentAnchorMessage}{routeContentEnterMessage}{routeBindingCleanupMessage}{runtimeRouteMessage}{activityMessage}"
                : $"Route Lifecycle started Route '{route.RouteName}'.{routeStateMessage}{routeExitMessage}{routeContentExitMessage}{releaseMessage}{activitySceneRouteReleaseMessage} {sceneLifecycleResult.Message}{compositionMessage}{routeContentMessage}{contentAnchorMessage}{routeContentEnterMessage}{routeBindingCleanupMessage}{runtimeRouteMessage}{activityMessage}";

            return new RouteLifecycleStartResult(
                true,
                message,
                route,
                previousRoute,
                routeExitResult,
                routeState,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                contentAnchorDiscoveryResult,
                routeContentEnterResult,
                routeContentExitResult,
                contentReleaseResult,
                activityFlowResult,
                runtimeRouteScopeResult,
                routeContentAnchorBindingCleanupResult,
                activitySceneRouteReleaseResult);
        }

        private static string BindingCleanupMessage(string fieldPrefix, ContentAnchorBindingLifecycleResult result)
        {
            if (!result.Executed)
            {
                return string.Empty;
            }

            return $" {fieldPrefix}='{result.DiagnosticStatus}' {fieldPrefix}Removed='{result.RemovedCount}' {fieldPrefix}Before='{result.BindingCountBefore}' {fieldPrefix}After='{result.BindingCountAfter}'.";
        }
    }
}
