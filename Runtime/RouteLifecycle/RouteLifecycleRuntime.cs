using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Loading;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal owner for starting and switching Routes.
    /// It owns the active Route identity and delegates scene loading to Scene Lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed partial class RouteLifecycleRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime = new SceneLifecycleRuntime();
        private readonly ActivityFlowRuntime _activityFlowRuntime;
        private readonly RouteContentRuntime _routeContentRuntime = new RouteContentRuntime();
        private readonly RouteSceneCompositionRuntime _routeSceneCompositionRuntime;
        private readonly ContentReleaseRuntime _contentReleaseRuntime;
        private readonly ContentAnchorDiscoveryRuntime _contentAnchorDiscoveryRuntime = new ContentAnchorDiscoveryRuntime();
        private readonly RuntimeContentRuntime _runtimeContentRuntime;
        private readonly RuntimeContentAnchorBinding _contentAnchorBindingRuntime;
        private readonly CycleResetRuntime _cycleResetRuntime = new CycleResetRuntime();
        private readonly EventBus<RouteEnteredEvent> _routeEnteredEvents = new EventBus<RouteEnteredEvent>();
        private readonly EventBus<RouteExitedEvent> _routeExitedEvents = new EventBus<RouteExitedEvent>();
        private RouteRuntimeState _currentRouteState;
        private ICycleResetParticipantSource _cycleResetParticipantSource = EmptyCycleResetParticipantSource.Instance;

        internal RouteLifecycleRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime)
        {
            _runtimeContentRuntime = runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime));
            _contentAnchorBindingRuntime = contentAnchorBindingRuntime ?? throw new ArgumentNullException(nameof(contentAnchorBindingRuntime));
            _activityFlowRuntime = new ActivityFlowRuntime(_runtimeContentRuntime, _contentAnchorBindingRuntime, _sceneLifecycleRuntime);
            _routeSceneCompositionRuntime = new RouteSceneCompositionRuntime(_sceneLifecycleRuntime);
            _contentReleaseRuntime = new ContentReleaseRuntime(_sceneLifecycleRuntime);
        }

        internal RouteRuntimeState CurrentRouteState => _currentRouteState;

        internal RouteAsset CurrentRoute => _currentRouteState.Route;

        internal RouteContentSet CurrentRouteContentSet => _currentRouteState.RouteContentSet;

        internal ActivityAsset CurrentActivity => _activityFlowRuntime.CurrentActivity;

        internal bool HasActiveRoute => CurrentRoute != null;

        internal bool HasActiveActivity => _activityFlowRuntime.HasActiveActivity;

        internal bool IsRouteActive(RouteAsset route)
        {
            return route != null && ReferenceEquals(CurrentRoute, route);
        }

        internal IEventBinding SubscribeRouteEntered(Action<RouteEnteredEvent> handler)
        {
            return _routeEnteredEvents.Subscribe(handler);
        }

        internal IEventBinding SubscribeRouteExited(Action<RouteExitedEvent> handler)
        {
            return _routeExitedEvents.Subscribe(handler);
        }

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return _activityFlowRuntime.IsActivityActive(activity);
        }

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _activityFlowRuntime.SetActivityContentExecutionParticipantSource(participantSource);
        }

        internal void SetCycleResetParticipantSource(ICycleResetParticipantSource participantSource)
        {
            _cycleResetParticipantSource = participantSource ?? EmptyCycleResetParticipantSource.Instance;
        }

        internal ActivityOperationResult PreviewActivityOperation(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            string source,
            string reason)
        {
            return _activityFlowRuntime.PreviewActivityOperation(
                operationKind,
                previousActivity,
                targetActivity,
                visualMode,
                source,
                reason);
        }

        internal Task<RouteLifecycleStartResult> StartRouteAsync(
            RouteAsset route,
            string source,
            string reason)
        {
            return StartRouteAsync(route, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<RouteLifecycleStartResult> StartRouteAsync(
            RouteAsset route,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (route == null)
            {
                return RouteLifecycleStartResult.Failed("Route is missing.");
            }

            if (!route.HasPrimaryScene)
            {
                return RouteLifecycleStartResult.Failed("Route Primary Scene is missing.");
            }

            var previousRouteState = _currentRouteState;
            var previousRoute = previousRouteState.Route;
            var previousActivity = _activityFlowRuntime.CurrentActivity;
            var startupOperationPreview = PreviewRouteStartupActivityOperation(route, previousActivity, source, reason);
            if (startupOperationPreview.IsBlocked)
            {
                return RouteLifecycleStartResult.Failed(
                    "Route Startup Activity blocked by ActivityOperationPlan. " + startupOperationPreview.ToDiagnosticString());
            }

            var releasePlan = previousRouteState.HasRouteContent
                ? previousRouteState.RouteContentSet.CreateReleasePlan(source, reason)
                : ContentReleasePlan.Empty(
                    FrameworkContentScope.Route,
                    string.Empty,
                    previousRoute != null ? previousRoute.RouteName : string.Empty,
                    source,
                    reason,
                    "No previous Route content is active; release plan is empty.");
            var routeContentPlan = RouteContentMaterializationPlan.FromRoute(route);
            var routeSceneCompositionPlan = RouteSceneCompositionPlan.FromRoute(route, source, reason);
            int activitySceneRouteReleaseCount = _activityFlowRuntime.PreviewActivitySceneReleaseForRouteChangeCount();
            int routeContentReleaseCount = releasePlan.ReleasableCount;
            int routeSceneLoadCount = CountRouteSceneCompositionProgressSteps(routeSceneCompositionPlan);
            int startupActivityProgressCount = startupOperationPreview.IsValid
                ? startupOperationPreview.SceneSideEffectCount
                : 0;
            int routeProgressStepCount = activitySceneRouteReleaseCount
                + routeContentReleaseCount
                + routeSceneLoadCount
                + startupActivityProgressCount;
            int routeProgressStepIndex = 0;

            var activitySceneRouteReleaseProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                progressReporter,
                routeProgressStepIndex,
                activitySceneRouteReleaseCount,
                routeProgressStepCount,
                "RouteTransition",
                "Route transition loading progress.");
            routeProgressStepIndex += activitySceneRouteReleaseCount;

            ActivitySceneReleaseResult activitySceneRouteReleaseResult;
            ActivityFlowStartResult activityRouteExitResult = default;
            if (previousActivity != null)
            {
                activityRouteExitResult = await _activityFlowRuntime.ClearActivityAsync(
                    previousRoute,
                    source,
                    reason,
                    activitySceneRouteReleaseProgressReporter);

                if (!activityRouteExitResult.Completed)
                {
                    return RouteLifecycleStartResult.Failed(activityRouteExitResult.Message);
                }

                activitySceneRouteReleaseResult = activityRouteExitResult.ActivitySceneReleaseResult;
            }
            else
            {
                activitySceneRouteReleaseResult = await _activityFlowRuntime.ReleaseActivityScenesForRouteChangeAsync(
                    source,
                    reason,
                    activitySceneRouteReleaseProgressReporter);
            }

            if (activitySceneRouteReleaseResult.HasBlockingIssues)
            {
                return RouteLifecycleStartResult.Failed(activitySceneRouteReleaseResult.ToDiagnosticString());
            }

            // Activity lifecycle must exit while the previous Route scene is still loaded.
            // Scene-authored Activity receivers can then release scoped camera, audio and input
            // state before their targets and rigs are destroyed by Route scene replacement.
            var routeContentExitResult = _routeContentRuntime.ExitRouteContent(previousRoute, route, source, reason);

            var routeContentReleaseProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                progressReporter,
                routeProgressStepIndex,
                routeContentReleaseCount,
                routeProgressStepCount,
                "RouteTransition",
                "Route transition loading progress.");
            routeProgressStepIndex += routeContentReleaseCount;
            var releaseResult = await _contentReleaseRuntime.ExecuteAsync(releasePlan, routeContentReleaseProgressReporter);
            if (releaseResult.Failed || releaseResult.HasBlockingIssues)
            {
                return RouteLifecycleStartResult.Failed(releaseResult.ToDiagnosticString());
            }

            var routeSceneCompositionProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                progressReporter,
                routeProgressStepIndex,
                routeSceneLoadCount,
                routeProgressStepCount,
                "RouteTransition",
                "Route transition loading progress.");
            routeProgressStepIndex += routeSceneLoadCount;
            var routeSceneCompositionResult = await _routeSceneCompositionRuntime.ExecuteAsync(routeSceneCompositionPlan, routeSceneCompositionProgressReporter);
            if (routeSceneCompositionResult.Failed || routeSceneCompositionResult.HasBlockingIssues)
            {
                return RouteLifecycleStartResult.Failed(routeSceneCompositionResult.ToDiagnosticString());
            }

            var runtimeRouteEnterResult = CreateRouteScopeRoot(route, source, reason);
            var sceneLifecycleResult = routeSceneCompositionResult.PrimarySceneLoadResult;
            var routeContentSet = RouteContentSet.FromSceneCompositionResult(
                route,
                routeContentPlan,
                routeSceneCompositionResult,
                source,
                reason);
            var contentAnchorDiscoveryResult = _contentAnchorDiscoveryRuntime.DiscoverRouteAnchors(
                route,
                routeSceneCompositionResult,
                source,
                reason);

            var routeContentEnterResult = _routeContentRuntime.EnterRouteContent(route, previousRoute, source, reason);

            var startupActivityProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                progressReporter,
                routeProgressStepIndex,
                startupActivityProgressCount,
                routeProgressStepCount,
                "RouteTransition",
                "Route transition loading progress.");
            var startupActivityFlowResult = await _activityFlowRuntime.StartStartupActivityAsync(route, source, reason, startupActivityProgressReporter);
            await FrameworkLoadingProgressReporterUtility.ReportCompletedIfAnyAsync(
                progressReporter,
                "RouteTransition",
                "Route transition loading progress completed.");
            if (!startupActivityFlowResult.Completed)
            {
                return RouteLifecycleStartResult.Failed(startupActivityFlowResult.Message);
            }

            // When the destination Route has no Startup Activity, preserve the real
            // Activity teardown result instead of replacing it with a second empty
            // no-Activity result. This keeps exit lifecycle, scope cleanup and scene
            // release evidence visible in the consolidated Route diagnostics.
            var activityFlowResult = !route.HasStartupActivity && activityRouteExitResult.Completed
                ? activityRouteExitResult
                : startupActivityFlowResult;

            var currentRouteOwner = runtimeRouteEnterResult.Owner;
            var previousRouteOwner = previousRoute != null
                ? CreateRouteOwner(previousRoute)
                : default(RuntimeContentOwner);
            var routeScopeTailRequest = new FrameworkScopeTailOperationRequest(
                currentRouteOwner,
                previousRouteOwner,
                runtimeRouteEnterResult.EnterRootResult,
                runtimeRouteEnterResult.Context,
                _runtimeContentRuntime.RootCount,
                source,
                reason,
                () => _runtimeContentRuntime.RootCount);
            var routeScopeTailResult = FrameworkScopeTailOperationExecutor.Execute(
                routeScopeTailRequest,
                cleanupRequest => CleanupPreviousRouteContentAnchorBindings(previousRoute, route, cleanupRequest.Source, cleanupRequest.Reason),
                removeRequest => RemovePreviousRouteScopeRoot(previousRoute, route, removeRequest.Source, removeRequest.Reason));

            var result = RouteLifecycleStartResult.StartedWith(
                route,
                previousRouteState,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                contentAnchorDiscoveryResult,
                routeContentEnterResult,
                routeContentExitResult,
                releaseResult,
                activityFlowResult,
                source,
                reason,
                routeScopeTailResult.ScopeResult,
                routeScopeTailResult.BindingCleanupResult,
                activitySceneRouteReleaseResult);
            _currentRouteState = result.RouteState;
            PublishRouteTransition(previousRoute, route, source, reason);
            return result;
        }

        private static int CountRouteSceneCompositionProgressSteps(RouteSceneCompositionPlan plan)
        {
            if (!plan.HasRoute)
            {
                return 0;
            }

            int count = plan.PrimaryScene.IsExecutionReady ? 1 : 0;
            for (int i = 0; i < plan.AdditionalScenes.Count; i++)
            {
                if (plan.AdditionalScenes[i].IsExecutionReady)
                {
                    count++;
                }
            }

            return count;
        }

        private ActivityOperationResult PreviewRouteStartupActivityOperation(
            RouteAsset route,
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            if (route == null || !route.HasStartupActivity)
            {
                return ActivityOperationResult.NotRequested(source, reason);
            }

            var startupActivity = route.StartupActivity;
            return _activityFlowRuntime.PreviewActivityOperation(
                ActivityOperationKind.RouteStartup,
                previousActivity,
                startupActivity,
                ResolveActivityTransitionMode(startupActivity),
                source,
                reason);
        }

        private static ActivityVisualTransitionMode ResolveActivityTransitionMode(ActivityAsset activity)
        {
            return activity != null ? activity.VisualTransitionMode : ActivityVisualTransitionMode.Seamless;
        }

        private void PublishRouteTransition(
            RouteAsset previousRoute,
            RouteAsset nextRoute,
            string source,
            string reason)
        {
            if (previousRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                _routeExitedEvents.Publish(new RouteExitedEvent(previousRoute, nextRoute, source, reason));
            }

            if (nextRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                _routeEnteredEvents.Publish(new RouteEnteredEvent(nextRoute, previousRoute, source, reason));
            }
        }

        internal Task<CycleResetResult> RequestRouteCycleResetAsync(
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(CreateRejectedCycleResetResult(
                    CycleResetScope.Route,
                    "Cycle Reset Request failed. No active Route is available.",
                    source,
                    reason));
            }

            var resolvedPolicy = policy.IsValid ? policy : CycleResetPolicy.RouteDefault();
            var request = CycleResetRequest.Route(CurrentRoute, CurrentActivity, resolvedPolicy, source, reason);
            return Task.FromResult(ExecuteCycleResetRequest(request, source, reason));
        }

        internal Task<CycleResetResult> RequestActivityCycleResetAsync(
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(CreateRejectedCycleResetResult(
                    CycleResetScope.Activity,
                    "Cycle Reset Request failed. No active Route is available.",
                    source,
                    reason));
            }

            if (CurrentActivity == null)
            {
                return Task.FromResult(CreateRejectedCycleResetResult(
                    CycleResetScope.Activity,
                    "Cycle Reset Request failed. No active Activity is available.",
                    source,
                    reason));
            }

            var resolvedPolicy = policy.IsValid ? policy : CycleResetPolicy.ActivityDefault();
            var request = CycleResetRequest.Activity(CurrentRoute, CurrentActivity, source, reason);
            if (resolvedPolicy != CycleResetPolicy.ActivityDefault())
            {
                request = new CycleResetRequest(
                    CycleResetScope.Activity,
                    CurrentRoute,
                    CurrentActivity,
                    resolvedPolicy,
                    source,
                    reason);
            }

            return Task.FromResult(ExecuteCycleResetRequest(request, source, reason));
        }

        private CycleResetResult ExecuteCycleResetRequest(CycleResetRequest request, string source, string reason)
        {
            IReadOnlyList<ICycleResetParticipant> participants;
            try
            {
                participants = _cycleResetParticipantSource.ResolveCycleResetParticipants(request);
            }
            catch (Exception exception)
            {
                return CycleResetResult.RejectedInvalidRequest(
                    request,
                    new[]
                    {
                        CycleResetIssue.BlockingIssue(
                            CycleResetIssueKind.ParticipantSourceException,
                            default,
                            request.Scope,
                            $"Cycle Reset participant source threw an exception: {exception.GetType().Name}.")
                    },
                    source,
                    reason,
                    "Cycle Reset Request failed because the participant source threw an exception.");
            }

            var plan = _cycleResetRuntime.CreatePlan(request, participants, source, reason);
            return _cycleResetRuntime.ExecutePlan(plan, source, reason);
        }

        private static CycleResetResult CreateRejectedCycleResetResult(
            CycleResetScope requestedScope,
            string message,
            string source,
            string reason)
        {
            return CycleResetResult.RejectedInvalidRequest(
                default,
                new[]
                {
                    CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.InvalidRequest,
                        default,
                        requestedScope,
                        message)
                },
                source,
                reason,
                message);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(
            ActivityAsset activity,
            string source,
            string reason)
        {
            return StartActivityAsync(activity, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(
            ActivityAsset activity,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.StartActivityAsync(activity, CurrentRoute, source, reason, progressReporter);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            return ClearActivityAsync(source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.ClearActivityAsync(CurrentRoute, source, reason, progressReporter);
        }

        private RuntimeScopeLifecycleResult CreateRouteScopeRoot(RouteAsset route, string source, string reason)
        {
            if (route == null)
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Route, source, reason);
            }

            var owner = CreateRouteOwner(route);
            var enterResult = _runtimeContentRuntime.CreateScopeRoot(owner, source, reason);
            _runtimeContentRuntime.TryCreateScopeContext(owner, source, reason, out var context);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Route,
                owner,
                enterResult,
                null,
                context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private ContentAnchorBindingLifecycleResult CleanupPreviousRouteContentAnchorBindings(RouteAsset previousRoute, RouteAsset nextRoute, string source, string reason)
        {
            var previousOwner = previousRoute != null ? CreateRouteOwner(previousRoute) : default(RuntimeContentOwner);
            var nextOwner = nextRoute != null ? CreateRouteOwner(nextRoute) : default(RuntimeContentOwner);
            return ContentAnchorBindingCleanup.CleanupPreviousRuntimeOwner(
                _contentAnchorBindingRuntime,
                previousOwner,
                nextOwner,
                source,
                reason);
        }

        private RuntimeRootRegistryOperationResult RemovePreviousRouteScopeRoot(RouteAsset previousRoute, RouteAsset nextRoute, string source, string reason)
        {
            if (previousRoute == null || ReferenceEquals(previousRoute, nextRoute))
            {
                throw new InvalidOperationException("Route scope root removal is only valid for a distinct previous Route.");
            }

            var owner = CreateRouteOwner(previousRoute);
            if (nextRoute != null && owner == CreateRouteOwner(nextRoute))
            {
                throw new InvalidOperationException("Route scope root removal is only valid for a distinct previous Route.");
            }

            return _runtimeContentRuntime.RemoveScopeRoot(owner, source, reason);
        }

        private RuntimeScopeLifecycleResult MergeRouteScopeResults(
            RuntimeScopeLifecycleResult enterResult,
            RuntimeScopeLifecycleResult exitResult,
            RouteAsset nextRoute,
            RouteAsset previousRoute,
            string source,
            string reason)
        {
            var owner = nextRoute != null
                ? CreateRouteOwner(nextRoute)
                : previousRoute != null ? CreateRouteOwner(previousRoute) : default(RuntimeContentOwner);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Route,
                owner,
                enterResult.EnterRootResult,
                exitResult.ExitRootResult,
                enterResult.Context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private static RuntimeContentOwner CreateRouteOwner(RouteAsset route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            return RuntimeContentOwner.Route(route.PrimaryScenePath, route.RouteName);
        }
    }
}
