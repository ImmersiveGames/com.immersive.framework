using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Loading;
using Immersive.Framework.Common;
using Immersive.Framework.GameFlow;
using Immersive.Framework.CycleReset;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private readonly IActivityRuntimePort _activityRuntime;
        private readonly IRouteCycleResetRuntimePort _routeCycleResetRuntime;
        private readonly IActivityCycleResetRuntimePort _activityCycleResetRuntime;
        private readonly IActivityRestartRuntimePort _activityRestartRuntime;
        private readonly EventBus<ActivityEnteredEvent> _activityEnteredEvents = new EventBus<ActivityEnteredEvent>();
        private readonly EventBus<ActivityExitedEvent> _activityExitedEvents = new EventBus<ActivityExitedEvent>();
        private RouteAsset _currentRoute;
        private string _currentRouteInstanceId = string.Empty;
        private int _routeInstanceSequence;
        private ActivityRuntimeState _currentActivityState;

        internal ActivityFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime,
            SceneLifecycleRuntime sceneLifecycleRuntime,
            IActivityRuntimePort activityRuntime,
            IRouteCycleResetRuntimePort routeCycleResetRuntime,
            IActivityCycleResetRuntimePort activityCycleResetRuntime,
            IActivityRestartRuntimePort activityRestartRuntime)
            : this(
                runtimeContentRuntime,
                contentAnchorBindingRuntime,
                sceneLifecycleRuntime,
                activityRuntime,
                routeCycleResetRuntime,
                activityCycleResetRuntime,
                activityRestartRuntime,
                EmptyActivityContentExecutionParticipantSource.Instance)
        {
        }

        internal ActivityFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime,
            SceneLifecycleRuntime sceneLifecycleRuntime,
            IActivityRuntimePort activityRuntime,
            IRouteCycleResetRuntimePort routeCycleResetRuntime,
            IActivityCycleResetRuntimePort activityCycleResetRuntime,
            IActivityRestartRuntimePort activityRestartRuntime,
            IActivityContentExecutionParticipantSource activityContentExecutionParticipantSource)
        {
            _runtimeContentRuntime = runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime));
            _contentAnchorBindingRuntime = contentAnchorBindingRuntime ?? throw new ArgumentNullException(nameof(contentAnchorBindingRuntime));
            _activityRuntime = activityRuntime ?? throw new ArgumentNullException(nameof(activityRuntime));
            _routeCycleResetRuntime = routeCycleResetRuntime ?? throw new ArgumentNullException(nameof(routeCycleResetRuntime));
            _activityCycleResetRuntime = activityCycleResetRuntime ?? throw new ArgumentNullException(nameof(activityCycleResetRuntime));
            _activityRestartRuntime = activityRestartRuntime ?? throw new ArgumentNullException(nameof(activityRestartRuntime));
            _activitySceneCompositionRuntime = new ActivitySceneCompositionRuntime(sceneLifecycleRuntime ?? throw new ArgumentNullException(nameof(sceneLifecycleRuntime)));
            _activityOperationPlanner = new ActivityOperationPlanner(_activitySceneCompositionRuntime);
            _activityContentExecutionParticipantSource = activityContentExecutionParticipantSource ?? EmptyActivityContentExecutionParticipantSource.Instance;
            _currentActivityState = ActivityRuntimeState.Empty();
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
                ActivityOperationResult operationResult =
                    ActivityOperationResult.NotRequested(
                        resolvedSource,
                        resolvedReason);
                return ExecuteActivityClearTransitionAsync(
                    previousActivity,
                    resolvedSource,
                    resolvedReason,
                    progressReporter,
                    operationResult,
                    skippedNoStartupActivity: true);
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

            ActivityAsset previousActivity = _currentActivityState.Activity;
            if (previousActivity == null)
            {
                return ActivityFlowStartResult.Failed(
                    "Activity Flow cannot clear Activity because no Activity is active.");
            }

            return await ExecuteActivityClearTransitionAsync(
                previousActivity,
                resolvedSource,
                resolvedReason,
                progressReporter,
                ActivityOperationResult.NotRequested(
                    resolvedSource,
                    resolvedReason),
                skippedNoStartupActivity: false);
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
            return await ExecuteActivityTransitionCoreAsync(
                nextActivity,
                previousActivity,
                source,
                reason,
                activityOperationResult,
                progressReporter,
                beforeActivation);
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

        private ActivityRequestTriggerBindingResult TryBindActivityRequestTriggers(
            ActivitySceneCompositionResult compositionResult)
        {
            return ActivityRequestTriggerBinding.TryBind(
                ResolveMaterializedActivitySceneRoots(compositionResult),
                _activityRuntime);
        }

        private RouteCycleResetTriggerBindingResult TryBindRouteCycleResetTriggers(
            ActivitySceneCompositionResult compositionResult)
        {
            return RouteCycleResetTriggerBinding.TryBind(
                ResolveMaterializedActivitySceneRoots(compositionResult),
                _routeCycleResetRuntime);
        }

        private ActivityCycleResetTriggerBindingResult TryBindActivityCycleResetTriggers(
            ActivitySceneCompositionResult compositionResult)
        {
            return ActivityCycleResetTriggerBinding.TryBind(
                ResolveMaterializedActivitySceneRoots(compositionResult),
                _activityCycleResetRuntime);
        }

        private ActivityRestartTriggerBindingResult TryBindActivityRestartTriggers(
            ActivitySceneCompositionResult compositionResult)
        {
            return ActivityRestartTriggerBinding.TryBind(
                ResolveMaterializedActivitySceneRoots(compositionResult),
                _activityRestartRuntime);
        }

        private static IReadOnlyList<GameObject> ResolveMaterializedActivitySceneRoots(
            ActivitySceneCompositionResult compositionResult)
        {
            var roots = new List<GameObject>();
            var seenSceneHandles = new HashSet<ulong>();
            for (int index = 0; index < compositionResult.Entries.Count; index++)
            {
                ActivitySceneCompositionResultEntry entry =
                    compositionResult.Entries[index];
                if (!entry.Loaded && !entry.AlreadyLoaded)
                {
                    continue;
                }

                Scene scene = !string.IsNullOrWhiteSpace(entry.ScenePath)
                    ? SceneManager.GetSceneByPath(entry.ScenePath)
                    : SceneManager.GetSceneByName(entry.SceneName);
                if (!scene.IsValid() || !scene.isLoaded ||
                    !seenSceneHandles.Add(scene.handle.GetRawData()))
                {
                    continue;
                }

                GameObject[] sceneRoots = scene.GetRootGameObjects();
                if (sceneRoots == null)
                {
                    continue;
                }

                for (int rootIndex = 0; rootIndex < sceneRoots.Length; rootIndex++)
                {
                    if (sceneRoots[rootIndex] != null)
                    {
                        roots.Add(sceneRoots[rootIndex]);
                    }
                }
            }

            return roots;
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

        private ActivityContentApplyResult ApplyActivityContentThroughLifecycleEvents(
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);
            var discoveryScope =
                _activitySceneCompositionRuntime.CreateActivityContentDiscoveryScope(
                    previousActivity,
                    nextActivity);
            _activityContentRuntime.SetRouteScope(_currentRoute);
            _activityContentRuntime.SetDiscoveryScope(discoveryScope);
            _activityContentRuntime.ClearLastApplyResult();

            ActivityContentRuntime.ActivityContentTransitionContext transition =
                _activityContentRuntime.PrepareActivityContentTransition(
                    previousActivity,
                    nextActivity,
                    resolvedSource,
                    resolvedReason);
            _activityContentRuntime.ExitPreviousActivityContent(transition);
            _activityContentRuntime.EnterTargetActivityContent(transition);
            ActivityContentApplyResult result =
                _activityContentRuntime.CompleteActivityContentTransition(
                    transition);

            // Events are completed facts. They no longer drive content mutation.
            PublishActivityTransition(
                previousActivity,
                nextActivity,
                resolvedSource,
                resolvedReason);
            return result;
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
