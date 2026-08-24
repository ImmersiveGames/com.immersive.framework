using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Loading;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Pause;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal owner for starting and switching Routes.
    /// It owns the active Route identity and delegates scene loading to Scene Lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed partial class RouteLifecycleRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime;
        private readonly ActivityFlowRuntime _activityFlowRuntime;
        private readonly RouteContentRuntime _routeContentRuntime = new RouteContentRuntime();
        private readonly RouteSceneCompositionRuntime _routeSceneCompositionRuntime;
        private readonly ContentReleaseRuntime _contentReleaseRuntime;
        private readonly RuntimeContentRuntime _runtimeContentRuntime;
        private readonly IRouteRuntimePort _routeRuntime;
        private readonly IActivityRuntimePort _activityRuntime;
        private readonly IRouteCycleResetRuntimePort _routeCycleResetRuntime;
        private readonly IActivityCycleResetRuntimePort _activityCycleResetRuntime;
        private readonly IActivityRestartRuntimePort _activityRestartRuntime;
        private readonly CycleResetRuntime _cycleResetRuntime = new CycleResetRuntime();
        private readonly EventBus<RouteEnteredEvent> _routeEnteredEvents = new EventBus<RouteEnteredEvent>();
        private readonly EventBus<RouteExitedEvent> _routeExitedEvents = new EventBus<RouteExitedEvent>();
        private readonly EventBus<ActivityReadinessUpdate> _activityReadinessUpdates = new EventBus<ActivityReadinessUpdate>();
        private RouteRuntimeState _currentRouteState;
        private RouteContentDiscoveryScope _currentRouteContentDiscoveryScope;
        private RoutePlayerSpatialEntryContext _currentPlayerSpatialEntryContext;
        private RouteLifecycleStartResult _currentRouteResult;
        private bool _hasCurrentRouteContext;
        private int _routeOccurrenceSequence;
        private IRoutePlayerSpatialEntryLifecycleParticipant
            _playerSpatialEntryParticipant;
        private ICycleResetParticipantSource _cycleResetParticipantSource = EmptyCycleResetParticipantSource.Instance;

        internal RouteLifecycleRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            IRouteRuntimePort routeRuntime,
            IActivityRuntimePort activityRuntime,
            IRouteCycleResetRuntimePort routeCycleResetRuntime,
            IActivityCycleResetRuntimePort activityCycleResetRuntime,
            IActivityRestartRuntimePort activityRestartRuntime,
            SceneLifecycleRuntime sceneLifecycleRuntime = null)
        {
            _sceneLifecycleRuntime = sceneLifecycleRuntime ?? new SceneLifecycleRuntime();
            _runtimeContentRuntime = runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime));
            _routeRuntime = routeRuntime ?? throw new ArgumentNullException(nameof(routeRuntime));
            _activityRuntime = activityRuntime ?? throw new ArgumentNullException(nameof(activityRuntime));
            _routeCycleResetRuntime = routeCycleResetRuntime ?? throw new ArgumentNullException(nameof(routeCycleResetRuntime));
            _activityCycleResetRuntime = activityCycleResetRuntime ?? throw new ArgumentNullException(nameof(activityCycleResetRuntime));
            _activityRestartRuntime = activityRestartRuntime ?? throw new ArgumentNullException(nameof(activityRestartRuntime));
            _activityFlowRuntime = new ActivityFlowRuntime(
                _runtimeContentRuntime,
                _sceneLifecycleRuntime,
                _activityRuntime,
                _routeCycleResetRuntime,
                _activityCycleResetRuntime,
                _activityRestartRuntime);
            _routeSceneCompositionRuntime = new RouteSceneCompositionRuntime(_sceneLifecycleRuntime);
            _contentReleaseRuntime = new ContentReleaseRuntime(_sceneLifecycleRuntime);
            _activityFlowRuntime.SubscribeActivityReadinessUpdates(HandleActivityReadinessUpdate);
        }

        internal RouteRuntimeState CurrentRouteState => _currentRouteState;

        internal RouteAsset CurrentRoute => _currentRouteState.Route;

        internal RouteContentSet CurrentRouteContentSet => _currentRouteState.RouteContentSet;

        internal ActivityAsset CurrentActivity => _activityFlowRuntime.CurrentActivity;

        internal ActivityReadinessOccurrence CurrentOccurrence => _activityFlowRuntime.CurrentOccurrence;

        internal ActivityFlowRuntime CurrentActivityFlowRuntime =>
            _hasCurrentRouteContext ? _activityFlowRuntime : null;

        internal int ActivityFlowRuntimeInstanceIdentity =>
            RuntimeHelpers.GetHashCode(_activityFlowRuntime);

        internal int CurrentReadinessRevision =>
            _activityFlowRuntime.CurrentReadinessRevision;

        internal bool TryGetCurrentRouteResult(out RouteLifecycleStartResult result)
        {
            result = _currentRouteResult;
            return _hasCurrentRouteContext;
        }

        internal bool TryCreateCurrentRouteContentDiscoveryScope(
            RouteAsset route,
            out RouteContentDiscoveryScope scope)
        {
            scope = default;
            if (!_hasCurrentRouteContext ||
                route == null ||
                !ReferenceEquals(route, CurrentRoute))
            {
                return false;
            }

            scope = _currentRouteContentDiscoveryScope;
            return true;
        }

        internal bool HasActiveRoute => CurrentRoute != null;

        internal bool HasActiveActivity => _activityFlowRuntime.HasActiveActivity;

        internal bool IsRouteActive(RouteAsset route)
        {
            // IF-ADR-014 / IF-ID-03: authored-definition equality is the exact asset reference.
            return route != null && CurrentRoute != null && ReferenceEquals(CurrentRoute, route);
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

        internal bool SetPlayerSpatialEntryParticipant(
            IRoutePlayerSpatialEntryLifecycleParticipant participant,
            out string issue)
        {
            issue = string.Empty;
            _playerSpatialEntryParticipant = participant;
            if (participant == null || !_currentPlayerSpatialEntryContext.IsValid)
            {
                return true;
            }

            if (!participant.TryEnterRouteSpatialEntry(
                    _currentPlayerSpatialEntryContext,
                    out issue))
            {
                participant.ExitRouteSpatialEntry(_currentPlayerSpatialEntryContext);
                _playerSpatialEntryParticipant = null;
                return false;
            }

            return true;
        }

        internal IEventBinding SubscribeActivityReadinessUpdates(Action<ActivityReadinessUpdate> handler)
        {
            return _activityReadinessUpdates.Subscribe(handler);
        }

        private void HandleActivityReadinessUpdate(ActivityReadinessUpdate update)
        {
            if (!_hasCurrentRouteContext || !update.IsValid ||
                !ReferenceEquals(update.Activity, CurrentActivity) ||
                !CurrentOccurrence.Matches(update.Activity, update.Occurrence.TransitionSequence) ||
                !_activityFlowRuntime.TryGetCurrentActivityResult(out ActivityFlowStartResult current))
            {
                return;
            }

            UpdateCurrentActivityProjection(current);
            _activityReadinessUpdates.Publish(update);
        }

        internal void SetPauseActivityBindingLifecycle(
            PauseActivityBindingRuntimeHostModule lifecycle)
        {
            _activityFlowRuntime.SetPauseActivityBindingLifecycle(lifecycle);
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
            IFrameworkLoadingProgressReporter progressReporter,
            Func<ActivityActivationGateResult> beforeStartupActivityActivation = null)
        {
            if (route == null)
            {
                return RouteLifecycleStartResult.Failed("Route is missing.");
            }

            if (!route.HasValidRouteId)
            {
                return RouteLifecycleStartResult.Failed("Route ID is missing or invalid.");
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
            var routeContentExitResult = _routeContentRuntime.ExitRouteContent(_currentRouteContentDiscoveryScope, route, source, reason);
            ExitCurrentPlayerSpatialEntry();
            _currentRouteContentDiscoveryScope = default;
            _activityFlowRuntime.SetRouteContentDiscoveryScope(default);

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

            RouteRequestTriggerBindingResult routeTriggerBinding =
                RouteRequestTriggerBinding.TryBind(
                    ResolveMaterializedRouteSceneRoots(routeSceneCompositionResult),
                    _routeRuntime);
            if (!routeTriggerBinding.Succeeded)
            {
                return RouteLifecycleStartResult.Failed(
                    routeTriggerBinding.Message);
            }

            ActivityRequestTriggerBindingResult activityTriggerBinding =
                ActivityRequestTriggerBinding.TryBind(
                    ResolveMaterializedRouteSceneRoots(routeSceneCompositionResult),
                    _activityRuntime);
            if (!activityTriggerBinding.Succeeded)
            {
                return RouteLifecycleStartResult.Failed(
                    activityTriggerBinding.Message);
            }

            RouteCycleResetTriggerBindingResult routeCycleResetTriggerBinding =
                RouteCycleResetTriggerBinding.TryBind(
                    ResolveMaterializedRouteSceneRoots(routeSceneCompositionResult),
                    _routeCycleResetRuntime);
            if (!routeCycleResetTriggerBinding.Succeeded)
            {
                return RouteLifecycleStartResult.Failed(
                    routeCycleResetTriggerBinding.Message);
            }

            ActivityCycleResetTriggerBindingResult activityCycleResetTriggerBinding =
                ActivityCycleResetTriggerBinding.TryBind(
                    ResolveMaterializedRouteSceneRoots(routeSceneCompositionResult),
                    _activityCycleResetRuntime);
            if (!activityCycleResetTriggerBinding.Succeeded)
            {
                return RouteLifecycleStartResult.Failed(
                    activityCycleResetTriggerBinding.Message);
            }

            ActivityRestartTriggerBindingResult activityRestartTriggerBinding =
                ActivityRestartTriggerBinding.TryBind(
                    ResolveMaterializedRouteSceneRoots(routeSceneCompositionResult),
                    _activityRestartRuntime);
            if (!activityRestartTriggerBinding.Succeeded)
            {
                return RouteLifecycleStartResult.Failed(
                    activityRestartTriggerBinding.Message);
            }

            var runtimeRouteEnterResult = CreateRouteScopeRoot(route, source, reason);
            var sceneLifecycleResult = routeSceneCompositionResult.PrimarySceneLoadResult;
            var routeContentSet = RouteContentSet.FromSceneCompositionResult(
                route,
                routeContentPlan,
                routeSceneCompositionResult,
                source,
                reason);
            var routeContentDiscoveryScope = RouteContentDiscoveryScope.FromCompositionResult(routeSceneCompositionResult);
            _activityFlowRuntime.SetRouteContentDiscoveryScope(routeContentDiscoveryScope);
            var routeContentEnterResult = _routeContentRuntime.EnterRouteContent(routeContentDiscoveryScope, previousRoute, source, reason);

            var playerSpatialEntryContext = new RoutePlayerSpatialEntryContext(
                route,
                _routeOccurrenceSequence + 1,
                routeContentDiscoveryScope);
            if (!TryEnterPlayerSpatialEntry(playerSpatialEntryContext, out string playerSpatialEntryIssue))
            {
                return RouteLifecycleStartResult.Failed(playerSpatialEntryIssue);
            }

            var startupActivityProgressReporter = FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                progressReporter,
                routeProgressStepIndex,
                startupActivityProgressCount,
                routeProgressStepCount,
                "RouteTransition",
                "Route transition loading progress.");
            var startupActivityFlowResult =
                beforeStartupActivityActivation != null
                    ? await _activityFlowRuntime
                        .StartStartupActivityWithActivationGateAsync(
                            route,
                            source,
                            reason,
                            startupActivityProgressReporter,
                            beforeStartupActivityActivation)
                    : await _activityFlowRuntime
                        .StartStartupActivityAsync(
                            route,
                            source,
                            reason,
                            startupActivityProgressReporter);
            await FrameworkLoadingProgressReporterUtility.ReportCompletedIfAnyAsync(
                progressReporter,
                "RouteTransition",
                "Route transition loading progress completed.");
            if (!startupActivityFlowResult.Completed)
            {
                ExitCurrentPlayerSpatialEntry();
                return RouteLifecycleStartResult.Failed(startupActivityFlowResult.Message);
            }
            ActivityFlowStartResult routeStartupActivityFlowResult =
                startupActivityFlowResult;
            if (beforeStartupActivityActivation != null)
            {
                if (previousActivity != null)
                {
                    RouteStartupActivityScopeFinalizationResult
                        previousActivityScopeFinalization =
                            _activityFlowRuntime
                                .FinalizeRouteStartupPreviousActivityScope(
                                    previousActivity,
                                    route.StartupActivity,
                                    source,
                                    reason);
                    if (!previousActivityScopeFinalization.Succeeded)
                    {
                        routeStartupActivityFlowResult =
                            startupActivityFlowResult.WithActivityTransition(
                                startupActivityFlowResult.ActivityTransitionSnapshot
                                    .WithPostCommitFinalizationFailure(
                                        previousActivityScopeFinalization
                                            .ToDiagnosticString()));
                        Debug.LogWarning(
                            "Route Startup Player handoff committed destination authority, " +
                            "but previous Activity scope finalization reported an issue. " +
                            previousActivityScopeFinalization.ToDiagnosticString());
                    }
                }
            }

            // When the destination Route has no Startup Activity, preserve the real
            // Activity teardown result instead of replacing it with a second empty
            // no-Activity result. This keeps exit lifecycle, scope cleanup and scene
            // release evidence visible in the consolidated Route diagnostics.
            var activityFlowResult = !route.HasStartupActivity && activityRouteExitResult.Completed
                ? activityRouteExitResult
                : routeStartupActivityFlowResult;

            RuntimeScopeLifecycleResult routeRuntimeScopeResult = runtimeRouteEnterResult;
            // Definition equality: exact reference. Operational owners include definition tokens (IF-ID-05).
            if (previousRoute != null && !ReferenceEquals(previousRoute, route))
            {
                RuntimeRootRegistryOperationResult previousRouteScopeRemoval =
                    RemovePreviousRouteScopeRoot(previousRoute, route, source, reason);
                var previousRouteScopeResult = new RuntimeScopeLifecycleResult(
                    RuntimeContentScope.Route,
                    CreateRouteOwner(previousRoute),
                    null,
                    previousRouteScopeRemoval,
                    default,
                    _runtimeContentRuntime.RootCount,
                    source,
                    reason);
                routeRuntimeScopeResult = MergeRouteScopeResults(
                    runtimeRouteEnterResult,
                    previousRouteScopeResult,
                    route,
                    previousRoute,
                    source,
                    reason);
            }

            var result = RouteLifecycleStartResult.StartedWith(
                route,
                previousRouteState,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                routeContentEnterResult,
                routeContentExitResult,
                releaseResult,
                activityFlowResult,
                _activityFlowRuntime,
                source,
                reason,
                routeRuntimeScopeResult,
                activitySceneRouteReleaseResult);
            _currentRouteState = result.RouteState;
            _currentRouteContentDiscoveryScope = routeContentDiscoveryScope;
            _currentPlayerSpatialEntryContext = playerSpatialEntryContext;
            _routeOccurrenceSequence = playerSpatialEntryContext.OccurrenceSequence;
            _currentRouteResult = result;
            _hasCurrentRouteContext = true;
            PublishRouteTransition(previousRoute, route, source, reason);
            return result;
        }

        private bool TryEnterPlayerSpatialEntry(
            RoutePlayerSpatialEntryContext context,
            out string issue)
        {
            issue = string.Empty;
            if (!context.IsValid)
            {
                issue = "Route Player spatial entry requires a valid materialized Route occurrence context.";
                return false;
            }

            _currentPlayerSpatialEntryContext = context;
            if (_playerSpatialEntryParticipant == null)
            {
                return true;
            }

            if (_playerSpatialEntryParticipant.TryEnterRouteSpatialEntry(context, out issue))
            {
                return true;
            }

            _playerSpatialEntryParticipant.ExitRouteSpatialEntry(context);
            _currentPlayerSpatialEntryContext = default;
            return false;
        }

        private void ExitCurrentPlayerSpatialEntry()
        {
            if (_currentPlayerSpatialEntryContext.IsValid)
            {
                _playerSpatialEntryParticipant?.ExitRouteSpatialEntry(
                    _currentPlayerSpatialEntryContext);
            }

            _currentPlayerSpatialEntryContext = default;
        }

        private static IReadOnlyList<GameObject> ResolveMaterializedRouteSceneRoots(
            RouteSceneCompositionResult compositionResult)
        {
            var roots = new List<GameObject>();
            var seenSceneHandles = new HashSet<ulong>();
            for (int index = 0; index < compositionResult.Entries.Count; index++)
            {
                RouteSceneCompositionResultEntry entry =
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

            if (nextRoute != null && (previousRoute == null || !ReferenceEquals(previousRoute, nextRoute)))
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

        internal async Task<ActivityFlowStartResult> StartActivityAsync(
            ActivityAsset activity,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (CurrentRoute == null)
            {
                return ActivityFlowStartResult.Failed("No active Route is available.");
            }

            ActivityFlowStartResult result = await _activityFlowRuntime.StartActivityAsync(activity, CurrentRoute, source, reason, progressReporter);
            UpdateCurrentActivityProjection(result);
            return result;
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            return ClearActivityAsync(source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<ActivityFlowStartResult> ClearActivityAsync(
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            if (CurrentRoute == null)
            {
                return ActivityFlowStartResult.Failed("No active Route is available.");
            }

            ActivityFlowStartResult result = await _activityFlowRuntime.ClearActivityAsync(CurrentRoute, source, reason, progressReporter);
            UpdateCurrentActivityProjection(result);
            return result;
        }

        private void UpdateCurrentActivityProjection(ActivityFlowStartResult activityFlowResult)
        {
            if (!_hasCurrentRouteContext || !activityFlowResult.Completed)
            {
                return;
            }

            _currentRouteState = _currentRouteState.WithActivityFlowResult(activityFlowResult);
            _currentRouteResult = _currentRouteResult.WithActivityFlowResult(activityFlowResult);
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

        private RuntimeRootRegistryOperationResult RemovePreviousRouteScopeRoot(RouteAsset previousRoute, RouteAsset nextRoute, string source, string reason)
        {
            // Authored-definition distinctness is reference-based (IF-ID-03).
            // Operational owners include definition tokens so stable-ID collisions never share release authority (IF-ID-05).
            if (previousRoute == null || ReferenceEquals(previousRoute, nextRoute))
            {
                throw new InvalidOperationException("Route scope root removal is only valid for a distinct previous Route.");
            }

            var owner = CreateRouteOwner(previousRoute);
            if (nextRoute != null && owner == CreateRouteOwner(nextRoute))
            {
                throw new InvalidOperationException(
                    "Route scope root removal is only valid when previous and next Routes resolve to different operational owners.");
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

            if (!route.HasValidRouteId)
            {
                throw new ArgumentException("Route runtime owner requires a valid RouteId.", nameof(route));
            }

            return RuntimeContentOwner.Route(
                route.RouteId.StableText,
                route.RouteName,
                RuntimeDefinitionToken.FromUnityObject(route));
        }
    }
}
