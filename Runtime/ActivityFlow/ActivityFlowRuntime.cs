using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Loading;
using Immersive.Framework.Common;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for Activity entry and exit identity.
    /// It owns the active Activity runtime state for the current application runtime and emits canonical lifecycle events.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed partial class ActivityFlowRuntime
    {
        private readonly ActivityContentRuntime _activityContentRuntime = new ActivityContentRuntime();
        private readonly ContentAnchorDiscoveryRuntime _contentAnchorDiscoveryRuntime = new ContentAnchorDiscoveryRuntime();
        private readonly ActivityContentExecutionRuntime _activityContentExecutionRuntime = new ActivityContentExecutionRuntime();
        private readonly ActivitySceneCompositionRuntime _activitySceneCompositionRuntime;
        private readonly ActivityOperationPlanner _activityOperationPlanner;
        private readonly ActivityOperationExecutor _activityOperationExecutor = new ActivityOperationExecutor();
        private IActivityContentExecutionParticipantSource _activityContentExecutionParticipantSource;
        private readonly RuntimeContentRuntime _runtimeContentRuntime;
        private readonly RuntimeContentAnchorBinding _contentAnchorBindingRuntime;
        private readonly EventBus<ActivityEnteredEvent> _activityEnteredEvents = new EventBus<ActivityEnteredEvent>();
        private readonly EventBus<ActivityExitedEvent> _activityExitedEvents = new EventBus<ActivityExitedEvent>();
        private readonly IEventBinding _activityContentEnteredBinding;
        private readonly IEventBinding _activityContentExitedBinding;
        private RouteAsset _currentRoute;
        private string _currentRouteInstanceId = string.Empty;
        private int _routeInstanceSequence;
        private ActivityRuntimeState _currentActivityState;

        internal ActivityFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime,
            SceneLifecycleRuntime sceneLifecycleRuntime)
            : this(runtimeContentRuntime, contentAnchorBindingRuntime, sceneLifecycleRuntime, EmptyActivityContentExecutionParticipantSource.Instance)
        {
        }

        internal ActivityFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime,
            SceneLifecycleRuntime sceneLifecycleRuntime,
            IActivityContentExecutionParticipantSource activityContentExecutionParticipantSource)
        {
            _runtimeContentRuntime = runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime));
            _contentAnchorBindingRuntime = contentAnchorBindingRuntime ?? throw new ArgumentNullException(nameof(contentAnchorBindingRuntime));
            _activitySceneCompositionRuntime = new ActivitySceneCompositionRuntime(sceneLifecycleRuntime ?? throw new ArgumentNullException(nameof(sceneLifecycleRuntime)));
            _activityOperationPlanner = new ActivityOperationPlanner(_activitySceneCompositionRuntime);
            _activityContentExecutionParticipantSource = activityContentExecutionParticipantSource ?? EmptyActivityContentExecutionParticipantSource.Instance;
            _currentActivityState = ActivityRuntimeState.Empty();
            _activityContentEnteredBinding = _activityEnteredEvents.Subscribe(_activityContentRuntime.HandleActivityEntered);
            _activityContentExitedBinding = _activityExitedEvents.Subscribe(_activityContentRuntime.HandleActivityExited);
        }

        internal ActivityAsset CurrentActivity => _currentActivityState.Activity;

        internal bool HasActiveActivity => _currentActivityState.IsActive;

        internal int PreviewActivitySceneReleaseForRouteChangeCount()
        {
            return _activitySceneCompositionRuntime.PreviewReleaseForRouteChangeCount();
        }

        internal int PreviewActivitySceneReleaseForActivityChangeCount(ActivityAsset activity)
        {
            return _activitySceneCompositionRuntime.PreviewReleaseForActivityChangeCount(activity);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseActivityScenesForRouteChangeAsync(
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return _activitySceneCompositionRuntime.ReleaseForRouteChangeAsync(source, reason, progressReporter);
        }

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return activity != null && ReferenceEquals(_currentActivityState.Activity, activity);
        }

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _activityContentExecutionParticipantSource = participantSource ?? EmptyActivityContentExecutionParticipantSource.Instance;
        }

        internal IEventBinding SubscribeActivityEntered(Action<ActivityEnteredEvent> handler)
        {
            return _activityEnteredEvents.Subscribe(handler);
        }

        internal IEventBinding SubscribeActivityExited(Action<ActivityExitedEvent> handler)
        {
            return _activityExitedEvents.Subscribe(handler);
        }

        internal ActivityOperationResult PreviewActivityOperation(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            string source,
            string reason)
        {
            var plan = _activityOperationPlanner.CreatePlan(
                operationKind,
                previousActivity,
                targetActivity,
                visualMode,
                NormalizeSource(source),
                NormalizeReason(reason));

            return _activityOperationExecutor.Preview(plan);
        }

        internal Task<ActivityFlowStartResult> StartStartupActivityAsync(RouteAsset route, string source, string reason)
        {
            return StartStartupActivityAsync(route, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivityFlowStartResult> StartStartupActivityAsync(
            RouteAsset route,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Route is missing."));
            }

            SetRouteContext(route);
            var previousActivity = _currentActivityState.Activity;
            if (!route.HasStartupActivity)
            {
                var operationResult = ActivityOperationResult.NotRequested(resolvedSource, resolvedReason);
                _currentActivityState = ActivityRuntimeState.None(previousActivity, resolvedSource, resolvedReason);
                var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null, resolvedSource, resolvedReason);
                var executionResult = ExecuteActivityContentLifecycle(previousActivity, null, resolvedSource, resolvedReason);
                if (previousActivity == null)
                {
                    var bindingCleanupResult = CleanupPreviousActivityContentAnchorBindings(previousActivity, null, resolvedSource, resolvedReason);
                    var runtimeScopeResult = RuntimeScopeLifecycleResult.None(RuntimeContentScope.Activity, resolvedSource, resolvedReason);
                    return Task.FromResult(ActivityFlowStartResult.SkippedNoStartupActivity(
                        _currentActivityState,
                        previousActivity,
                        contentResult,
                        runtimeScopeResult,
                        bindingCleanupResult,
                        ActivityContentAnchorDiscoveryResult.Empty(null, resolvedSource, resolvedReason, "No startup Activity is active; Activity Content Anchor discovery was skipped."),
                        executionResult,
                        activityOperationResult: operationResult,
                        activitySceneLedgerSnapshot: CreateActivitySceneLedgerSnapshot()));
                }

                var activityScopeTailRequest = new FrameworkScopeTailOperationRequest(
                    default(RuntimeContentOwner),
                    CreateActivityOwner(previousActivity),
                    null,
                    default(RuntimeScopeContext),
                    _runtimeContentRuntime.RootCount,
                    resolvedSource,
                    resolvedReason,
                    () => _runtimeContentRuntime.RootCount);
                var activityScopeTailResult = FrameworkScopeTailOperationExecutor.Execute(
                    activityScopeTailRequest,
                    cleanupRequest => CleanupPreviousActivityContentAnchorBindings(previousActivity, null, cleanupRequest.Source, cleanupRequest.Reason),
                    removeRequest => RemovePreviousActivityScopeRoot(previousActivity, null, removeRequest.Source, removeRequest.Reason));
                return Task.FromResult(ActivityFlowStartResult.SkippedNoStartupActivity(
                    _currentActivityState,
                    previousActivity,
                    contentResult,
                    activityScopeTailResult.ScopeResult,
                    activityScopeTailResult.BindingCleanupResult,
                    ActivityContentAnchorDiscoveryResult.Empty(null, resolvedSource, resolvedReason, "No startup Activity is active; Activity Content Anchor discovery was skipped."),
                    executionResult,
                    activityOperationResult: operationResult,
                    activitySceneLedgerSnapshot: CreateActivitySceneLedgerSnapshot()));
            }

            var startupActivity = route.StartupActivity;
            var operationPreview = PreviewActivityOperation(
                ActivityOperationKind.RouteStartup,
                previousActivity,
                startupActivity,
                ResolveActivityTransitionMode(startupActivity),
                resolvedSource,
                resolvedReason);
            if (operationPreview.IsBlocked)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed(
                    "Route Startup Activity blocked by ActivityOperationPlan. " + operationPreview.ToDiagnosticString(),
                    operationPreview));
            }

            return StartActivityCoreAsync(
                startupActivity,
                previousActivity,
                resolvedSource,
                resolvedReason,
                operationPreview,
                progressReporter);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity, string source, string reason)
        {
            return StartActivityAsync(activity, _currentRoute, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(
            ActivityAsset activity,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return StartActivityAsync(activity, _currentRoute, source, reason, progressReporter);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity, RouteAsset route, string source, string reason)
        {
            return StartActivityAsync(activity, route, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(
            ActivityAsset activity,
            RouteAsset route,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (activity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity is missing."));
            }

            if (route != null)
            {
                SetRouteContext(route);
            }

            return StartActivityCoreAsync(activity, _currentActivityState.Activity, resolvedSource, resolvedReason, progressReporter: progressReporter);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            return ClearActivityAsync(_currentRoute, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return ClearActivityAsync(_currentRoute, source, reason, progressReporter);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(RouteAsset route, string source, string reason)
        {
            return ClearActivityAsync(route, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<ActivityFlowStartResult> ClearActivityAsync(
            RouteAsset route,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route != null)
            {
                SetRouteContext(route);
            }

            var previousActivity = _currentActivityState.Activity;
            if (previousActivity == null)
            {
                return ActivityFlowStartResult.Failed("Activity Flow cannot clear Activity because no Activity is active.");
            }

            _currentActivityState = ActivityRuntimeState.None(previousActivity, resolvedSource, resolvedReason);
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null, resolvedSource, resolvedReason);
            var executionResult = ExecuteActivityContentLifecycle(previousActivity, null, resolvedSource, resolvedReason);
            var sceneCompositionResult = CreateActivitySceneCompositionResult(null, resolvedSource, resolvedReason);
            int releaseCount = PreviewActivitySceneReleaseForActivityChangeCount(previousActivity);
            var activityScopeTailRequest = new FrameworkScopeTailOperationRequest(
                default(RuntimeContentOwner),
                CreateActivityOwner(previousActivity),
                null,
                default(RuntimeScopeContext),
                _runtimeContentRuntime.RootCount,
                resolvedSource,
                resolvedReason,
                () => _runtimeContentRuntime.RootCount);
            var activityScopeTailResult = FrameworkScopeTailOperationExecutor.Execute(
                activityScopeTailRequest,
                cleanupRequest => CleanupPreviousActivityContentAnchorBindings(previousActivity, null, cleanupRequest.Source, cleanupRequest.Reason),
                removeRequest => RemovePreviousActivityScopeRoot(previousActivity, null, removeRequest.Source, removeRequest.Reason));
            var sceneReleaseProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                progressReporter,
                0,
                releaseCount,
                releaseCount,
                "ActivityTransition",
                "Activity transition loading progress.");
            var sceneReleaseResult = await ReleasePreviousActivityScenesAsync(previousActivity, resolvedSource, resolvedReason, sceneReleaseProgressReporter);
            await FrameworkLoadingProgressReporterUtility.ReportCompletedIfAnyAsync(
                progressReporter,
                "ActivityTransition",
                "Activity transition loading progress completed.");
            return ActivityFlowStartResult.ClearedByRequest(
                _currentActivityState,
                previousActivity,
                contentResult,
                activityScopeTailResult.ScopeResult,
                activityScopeTailResult.BindingCleanupResult,
                ActivityContentAnchorDiscoveryResult.Empty(null, resolvedSource, resolvedReason, "Activity was cleared; Activity Content Anchor discovery was skipped."),
                executionResult,
                sceneCompositionResult,
                sceneReleaseResult,
                activitySceneLedgerSnapshot: CreateActivitySceneLedgerSnapshot());
        }

        private async Task<ActivityFlowStartResult> StartActivityCoreAsync(
            ActivityAsset nextActivity,
            ActivityAsset previousActivity,
            string source,
            string reason,
            ActivityOperationResult activityOperationResult = default(ActivityOperationResult),
            IFrameworkLoadingProgressReporter progressReporter = null,
            Func<ActivityActivationGateResult> beforeActivation = null)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (nextActivity == null)
            {
                return ActivityFlowStartResult.Failed("Activity is missing.");
            }

            if (ReferenceEquals(previousActivity, nextActivity))
            {
                if (!_currentActivityState.IsActive)
                {
                    _currentActivityState = ActivityRuntimeState.ActiveWith(nextActivity, previousActivity, resolvedSource, resolvedReason);
                }

                return ActivityFlowStartResult.KeptCurrentActivity(_currentActivityState);
            }

            var runtimeEnterResult = CreateActivityScopeRoot(nextActivity, resolvedSource, resolvedReason);
            var activityScopeTailRequest = new FrameworkScopeTailOperationRequest(
                runtimeEnterResult.Owner,
                previousActivity != null ? CreateActivityOwner(previousActivity) : default(RuntimeContentOwner),
                runtimeEnterResult.EnterRootResult,
                runtimeEnterResult.Context,
                _runtimeContentRuntime.RootCount,
                resolvedSource,
                resolvedReason,
                () => _runtimeContentRuntime.RootCount);
            var resolvedProgressReporter = progressReporter ?? NoOpFrameworkLoadingProgressReporter.Instance;
            var operationForProgress = ResolveActivityOperationForProgress(
                activityOperationResult,
                previousActivity,
                nextActivity,
                resolvedSource,
                resolvedReason);
            int loadProgressCount = CountActivityOperationSceneSideEffects(operationForProgress, ActivityOperationSceneAction.Load);
            int releaseProgressCount = CountActivityOperationSceneSideEffects(operationForProgress, ActivityOperationSceneAction.Release);
            int totalProgressCount = loadProgressCount + releaseProgressCount;
            var sceneCompositionProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                resolvedProgressReporter,
                0,
                loadProgressCount,
                totalProgressCount,
                "ActivityTransition",
                "Activity transition loading progress.");
            var sceneCompositionResult = await ExecuteActivitySceneCompositionAsync(nextActivity, resolvedSource, resolvedReason, sceneCompositionProgressReporter);
            if (sceneCompositionResult.HasBlockingIssues)
            {
                string targetSceneRollbackIssue =
                    await RollbackPreparedTargetScenesAsync(
                        nextActivity,
                        resolvedSource,
                        "activity-scene-composition-failure");
                if (beforeActivation == null)
                {
                    RemovePreviousActivityScopeRoot(nextActivity, previousActivity, resolvedSource, resolvedReason);
                }
                _currentActivityState = previousActivity != null
                    ? ActivityRuntimeState.ActiveWith(previousActivity, nextActivity, resolvedSource, resolvedReason)
                    : ActivityRuntimeState.None(nextActivity, resolvedSource, resolvedReason);
                return ActivityFlowStartResult.Failed(
                    sceneCompositionResult.ToDiagnosticString() + targetSceneRollbackIssue,
                    activityOperationResult);
            }

            ActivityActivationGateResult activationGate = beforeActivation != null
                ? beforeActivation()
                : ActivityActivationGateResult.Allowed(
                    resolvedSource,
                    resolvedReason,
                    "Activity activation gate approved target publication.");
            if (!activationGate.CanActivate)
            {
                string targetSceneRollbackIssue =
                    await RollbackPreparedTargetScenesAsync(
                        nextActivity,
                        resolvedSource,
                        "activity-activation-gate-blocked");
                _currentActivityState = previousActivity != null
                    ? ActivityRuntimeState.ActiveWith(previousActivity, nextActivity, resolvedSource, resolvedReason)
                    : ActivityRuntimeState.None(nextActivity, resolvedSource, resolvedReason);
                return ActivityFlowStartResult.Failed(
                    "Activity activation gate blocked target Activity publication. " +
                    activationGate.ToDiagnosticString() + targetSceneRollbackIssue,
                    activityOperationResult);
            }

            _currentActivityState = ActivityRuntimeState.ActiveWith(
                nextActivity,
                previousActivity,
                resolvedSource,
                resolvedReason);
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, nextActivity, resolvedSource, resolvedReason);
            var activityContentAnchorDiscoveryResult = _contentAnchorDiscoveryRuntime.DiscoverActivityAnchors(
                nextActivity,
                _currentRoute,
                _activitySceneCompositionRuntime.CreateActivityContentDiscoveryScope(nextActivity),
                resolvedSource,
                resolvedReason);
            var executionResult = ExecuteActivityContentLifecycle(previousActivity, nextActivity, resolvedSource, resolvedReason);
            var activityScopeTailResult = FrameworkScopeTailOperationExecutor.Execute(
                activityScopeTailRequest,
                cleanupRequest => CleanupPreviousActivityContentAnchorBindings(previousActivity, nextActivity, cleanupRequest.Source, cleanupRequest.Reason),
                removeRequest => RemovePreviousActivityScopeRoot(previousActivity, nextActivity, removeRequest.Source, removeRequest.Reason));
            var sceneReleaseProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                resolvedProgressReporter,
                loadProgressCount,
                releaseProgressCount,
                totalProgressCount,
                "ActivityTransition",
                "Activity transition loading progress.");
            var sceneReleaseResult = await ReleasePreviousActivityScenesAsync(previousActivity, resolvedSource, resolvedReason, sceneReleaseProgressReporter);
            await FrameworkLoadingProgressReporterUtility.ReportCompletedIfAnyAsync(
                resolvedProgressReporter,
                "ActivityTransition",
                "Activity transition loading progress completed.");

            return ActivityFlowStartResult.StartedWith(
                _currentActivityState,
                previousActivity,
                contentResult,
                activityScopeTailResult.ScopeResult,
                activityScopeTailResult.BindingCleanupResult,
                activityContentAnchorDiscoveryResult,
                executionResult,
                sceneCompositionResult,
                sceneReleaseResult,
                activityOperationResult,
                CreateActivitySceneLedgerSnapshot());
        }


        private ActivityOperationResult ResolveActivityOperationForProgress(
            ActivityOperationResult activityOperationResult,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason)
        {
            if (activityOperationResult.IsValid)
            {
                return activityOperationResult;
            }

            var operationKind = previousActivity == null
                ? ActivityOperationKind.Start
                : ActivityOperationKind.Switch;
            return PreviewActivityOperation(
                operationKind,
                previousActivity,
                nextActivity,
                ResolveActivityTransitionMode(nextActivity),
                source,
                reason);
        }

        private static int CountActivityOperationSceneSideEffects(
            ActivityOperationResult activityOperationResult,
            ActivityOperationSceneAction action)
        {
            if (!activityOperationResult.IsValid)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<ActivityOperationPlanSceneEntry> scenes = activityOperationResult.Plan.Scenes;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].Action == action && scenes[i].IsSceneSideEffect)
                {
                    count++;
                }
            }

            return count;
        }

        private static ActivityVisualTransitionMode ResolveActivityTransitionMode(ActivityAsset activity)
        {
            return activity != null ? activity.VisualTransitionMode : ActivityVisualTransitionMode.Seamless;
        }

        private Task<ActivitySceneCompositionResult> ExecuteActivitySceneCompositionAsync(
            ActivityAsset activity,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            var plan = ActivitySceneCompositionPlan.FromActivity(activity, source, reason);
            return _activitySceneCompositionRuntime.ExecuteAsync(plan, progressReporter);
        }

        private Task<ActivitySceneReleaseResult> ReleasePreviousActivityScenesAsync(
            ActivityAsset previousActivity,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return _activitySceneCompositionRuntime.ReleaseOnActivityChangeAsync(previousActivity, source, reason, progressReporter);
        }

        private static ActivitySceneCompositionResult CreateActivitySceneCompositionResult(
            ActivityAsset activity,
            string source,
            string reason)
        {
            var plan = ActivitySceneCompositionPlan.FromActivity(activity, source, reason);
            return ActivitySceneCompositionResult.FromPlan(plan, source, reason);
        }

        private ActivityContentExecutionLifecycleResult ExecuteActivityContentLifecycle(
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (previousActivity == null && nextActivity == null)
            {
                return ActivityContentExecutionLifecycleResult.None(
                    resolvedSource,
                    resolvedReason,
                    "Activity content execution lifecycle skipped because there is no previous or next Activity.");
            }

            var participantSourceRequest = new ActivityContentExecutionParticipantSourceRequest(
                _currentRoute,
                previousActivity,
                nextActivity,
                resolvedSource,
                resolvedReason);
            var participantSourceResult = ResolveActivityContentExecutionParticipants(participantSourceRequest);
            var participants = participantSourceResult.Collection;
            var enterPlan = default(ActivityContentExecutionPhasePlan);
            var enterResult = default(ActivityContentExecutionAggregateResult);
            var exitPlan = default(ActivityContentExecutionPhasePlan);
            var exitResult = default(ActivityContentExecutionAggregateResult);

            if (previousActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                if (TryCreateActivityScopeContext(previousActivity, resolvedSource, resolvedReason, out var exitContext))
                {
                    exitPlan = ActivityContentExecutionRequestFactory.CreateExitPlan(
                        previousActivity,
                        nextActivity,
                        exitContext,
                        participants,
                        resolvedSource,
                        resolvedReason);
                    exitResult = _activityContentExecutionRuntime.ExecutePhasePlan(exitPlan, resolvedSource, resolvedReason);
                }
                else
                {
                    exitResult = ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                        ActivityContentExecutionPhase.Exit,
                        previousActivity,
                        previousActivity,
                        nextActivity,
                        resolvedSource,
                        resolvedReason,
                        "Activity content execution exit phase rejected because previous Activity runtime scope context is not available.");
                }
            }

            if (nextActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                if (TryCreateActivityScopeContext(nextActivity, resolvedSource, resolvedReason, out var enterContext))
                {
                    enterPlan = ActivityContentExecutionRequestFactory.CreateEnterPlan(
                        nextActivity,
                        previousActivity,
                        enterContext,
                        participants,
                        resolvedSource,
                        resolvedReason);
                    enterResult = _activityContentExecutionRuntime.ExecutePhasePlan(enterPlan, resolvedSource, resolvedReason);
                }
                else
                {
                    enterResult = ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                        ActivityContentExecutionPhase.Enter,
                        nextActivity,
                        previousActivity,
                        nextActivity,
                        resolvedSource,
                        resolvedReason,
                        "Activity content execution enter phase rejected because next Activity runtime scope context is not available.");
                }
            }

            var activity = nextActivity != null ? nextActivity : previousActivity;
            return ActivityContentExecutionLifecycleResult.FromResults(
                activity,
                previousActivity,
                nextActivity,
                participantSourceResult,
                participants,
                enterPlan,
                enterResult,
                exitPlan,
                exitResult,
                resolvedSource,
                resolvedReason,
                "Activity content execution lifecycle integrated with ActivityFlow using the currently resolved participant collection.");
        }

        private ActivityContentExecutionParticipantSourceResult ResolveActivityContentExecutionParticipants(
            ActivityContentExecutionParticipantSourceRequest request)
        {
            if (!request.IsValid)
            {
                return ActivityContentExecutionParticipantSourceResult.RejectedInvalidRequest(
                    request,
                    request.Source,
                    request.Reason,
                    "Activity content execution participant source request rejected because no Activity transition is available.");
            }

            try
            {
                var result = _activityContentExecutionParticipantSource.ResolveActivityContentExecutionParticipants(request);
                if (!result.Executed)
                {
                    return ActivityContentExecutionParticipantSourceResult.FailedResult(
                        request,
                        request.Source,
                        request.Reason,
                        "Activity content execution participant source returned a non-executed result for an executable lifecycle request.");
                }

                return result;
            }
            catch (Exception exception)
            {
                return ActivityContentExecutionParticipantSourceResult.FailedException(
                    request,
                    exception,
                    request.Source,
                    request.Reason);
            }
        }

        private ActivitySceneLedgerSnapshot CreateActivitySceneLedgerSnapshot()
        {
            return new ActivitySceneLedgerSnapshot(
                _activitySceneCompositionRuntime.LedgerEntryCount,
                _activitySceneCompositionRuntime.LedgerLoadedCount,
                _activitySceneCompositionRuntime.LedgerReleasedCount,
                _activitySceneCompositionRuntime.LedgerStaleCount);
        }

        private void SetRouteContext(RouteAsset route)
        {
            if (route == null)
            {
                return;
            }

            if (!ReferenceEquals(_currentRoute, route))
            {
                _routeInstanceSequence++;
                _currentRoute = route;
                _currentRouteInstanceId = CreateRouteInstanceId(route, _routeInstanceSequence);
            }

            _activitySceneCompositionRuntime.SetRouteContext(_currentRoute, _currentRouteInstanceId);
        }

        private static string CreateRouteInstanceId(RouteAsset route, int sequence)
        {
            string routeName = route != null && !string.IsNullOrWhiteSpace(route.RouteName)
                ? route.RouteName.Trim()
                : "Route";
            return $"route:{sequence}:{routeName}";
        }

        private bool TryCreateActivityScopeContext(
            ActivityAsset activity,
            string source,
            string reason,
            out RuntimeScopeContext context)
        {
            if (activity == null)
            {
                context = default(RuntimeScopeContext);
                return false;
            }

            return _runtimeContentRuntime.TryCreateScopeContext(CreateActivityOwner(activity), source, reason, out context);
        }

        private ActivityContentApplyResult ApplyActivityContentThroughLifecycleEvents(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            var discoveryScope = _activitySceneCompositionRuntime.CreateActivityContentDiscoveryScope(previousActivity, nextActivity);
            _activityContentRuntime.SetRouteScope(_currentRoute);
            _activityContentRuntime.SetDiscoveryScope(discoveryScope);
            _activityContentRuntime.ClearLastApplyResult();
            PublishActivityTransition(previousActivity, nextActivity, resolvedSource, resolvedReason);

            if (_activityContentRuntime.HasLastApplyResult)
            {
                return _activityContentRuntime.LastApplyResult;
            }

            // No lifecycle event is emitted when both previous and next Activity are absent.
            // The content owner still needs to enforce the scene-authored no-active-Activity state.
            return _activityContentRuntime.ApplyActiveActivity(nextActivity);
        }

        private void PublishActivityTransition(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            if (previousActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                _activityExitedEvents.Publish(new ActivityExitedEvent(previousActivity, nextActivity, source, reason));
            }

            if (nextActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                _activityEnteredEvents.Publish(new ActivityEnteredEvent(nextActivity, previousActivity, source, reason));
            }
        }

        private RuntimeScopeLifecycleResult CreateActivityScopeRoot(ActivityAsset activity, string source, string reason)
        {
            if (activity == null)
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Activity, source, reason);
            }

            var owner = CreateActivityOwner(activity);
            var enterResult = _runtimeContentRuntime.CreateScopeRoot(owner, source, reason);
            _runtimeContentRuntime.TryCreateScopeContext(owner, source, reason, out var context);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Activity,
                owner,
                enterResult,
                null,
                context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private ContentAnchorBindingLifecycleResult CleanupPreviousActivityContentAnchorBindings(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            var previousOwner = previousActivity != null ? CreateActivityOwner(previousActivity) : default(RuntimeContentOwner);
            var nextOwner = nextActivity != null ? CreateActivityOwner(nextActivity) : default(RuntimeContentOwner);
            return ContentAnchorBindingCleanup.CleanupPreviousRuntimeOwner(
                _contentAnchorBindingRuntime,
                previousOwner,
                nextOwner,
                source,
                reason);
        }

        private RuntimeRootRegistryOperationResult RemovePreviousActivityScopeRoot(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            if (previousActivity == null || ReferenceEquals(previousActivity, nextActivity))
            {
                return null;
            }

            var owner = CreateActivityOwner(previousActivity);
            if (nextActivity != null && owner == CreateActivityOwner(nextActivity))
            {
                return null;
            }

            return _runtimeContentRuntime.RemoveScopeRoot(owner, source, reason);
        }

        private RuntimeScopeLifecycleResult MergeActivityScopeResults(
            RuntimeScopeLifecycleResult enterResult,
            RuntimeScopeLifecycleResult exitResult,
            ActivityAsset nextActivity,
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            var owner = nextActivity != null
                ? CreateActivityOwner(nextActivity)
                : previousActivity != null ? CreateActivityOwner(previousActivity) : default(RuntimeContentOwner);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Activity,
                owner,
                enterResult.EnterRootResult,
                exitResult.ExitRootResult,
                enterResult.Context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private static RuntimeContentOwner CreateActivityOwner(ActivityAsset activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (!activity.HasValidActivityId)
            {
                throw new ArgumentException("Activity runtime owner requires a valid ActivityId.", nameof(activity));
            }

            return RuntimeContentOwner.Activity(activity.ActivityId.StableText, activity.ActivityName);
        }

        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback("Unknown");
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback("None");
        }
    }
}
