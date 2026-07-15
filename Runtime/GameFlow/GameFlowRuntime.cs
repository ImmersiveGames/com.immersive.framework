using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.ActivityFlow;
using System;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Gate;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using Immersive.Framework.Loading;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Minimal owner for the game-flow route handoff.
    /// It accepts route and activity requests and delegates route startup to Route Lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed partial class GameFlowRuntime
    {
        private readonly RouteLifecycleRuntime _routeLifecycleRuntime;
        private readonly ITransitionOrchestrator _transitionOrchestrator;
        private int _transitionRequestSequence;
        private bool _routeRequestInFlight;
        private bool _activityRequestInFlight;
        private bool _cycleResetRequestInFlight;
        private GateSnapshot _transitionGateSnapshot;
        private TransitionGateMode _transitionGateMode;

        internal bool HasLifecycleRequestInFlight => _routeRequestInFlight
            || _activityRequestInFlight
            || _cycleResetRequestInFlight;

        internal GateSnapshot CurrentTransitionGateSnapshot => _transitionGateSnapshot;

        internal TransitionGateMode CurrentTransitionGateMode => _transitionGateMode;

        internal GameFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime)
            : this(
                runtimeContentRuntime,
                contentAnchorBindingRuntime,
                NoOpTransitionOrchestrator.Instance)
        {
        }

        internal GameFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime,
            ITransitionOrchestrator transitionOrchestrator)
        {
            _transitionOrchestrator = transitionOrchestrator ?? throw new ArgumentNullException(nameof(transitionOrchestrator));
            _routeLifecycleRuntime = new RouteLifecycleRuntime(
                runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime)),
                contentAnchorBindingRuntime ?? throw new ArgumentNullException(nameof(contentAnchorBindingRuntime)));
        }

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _routeLifecycleRuntime.SetActivityContentExecutionParticipantSource(participantSource);
        }

        internal void SetCycleResetParticipantSource(ICycleResetParticipantSource participantSource)
        {
            _routeLifecycleRuntime.SetCycleResetParticipantSource(participantSource);
        }

        internal ActivityOperationResult PreviewActivityOperation(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            string source,
            string reason)
        {
            return _routeLifecycleRuntime.PreviewActivityOperation(
                operationKind,
                previousActivity,
                targetActivity,
                visualMode,
                source.NormalizeTextOrFallback("Unknown"),
                reason.NormalizeTextOrFallback("None"));
        }

        internal async Task<FrameworkGameFlowStartResult> StartAsync(GameApplicationAsset gameApplication)
        {
            if (gameApplication == null)
            {
                return FrameworkGameFlowStartResult.Failed("Game Application is missing.");
            }

            var startupRoute = gameApplication.StartupRoute;
            if (startupRoute == null)
            {
                return FrameworkGameFlowStartResult.Failed("Startup Route is missing.");
            }

            if (!startupRoute.HasPrimaryScene)
            {
                return FrameworkGameFlowStartResult.Failed("Startup Route Primary Scene is missing.");
            }

            var routeLifecycleResult = await StartRouteCoreAsync(startupRoute, "GameApplication", "startup");
            if (!routeLifecycleResult.Started)
            {
                return FrameworkGameFlowStartResult.Failed(routeLifecycleResult.Message);
            }

            return FrameworkGameFlowStartResult.StartedWith(startupRoute, routeLifecycleResult);
        }

        internal async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            return await RequestRouteAsync(
                targetRoute,
                source,
                reason,
                beforeRouteLifecycle: null,
                afterRouteLifecycle: null);
        }

        internal Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason,
            Func<Awaitable> beforeRouteLifecycle,
            Func<Awaitable> afterRouteLifecycle)
        {
            return RequestRouteAsync(
                targetRoute,
                source,
                reason,
                beforeRouteLifecycle,
                afterRouteLifecycle,
                NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason,
            Func<Awaitable> beforeRouteLifecycle,
            Func<Awaitable> afterRouteLifecycle,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback("None");

            if (targetRoute == null)
            {
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    "Route Request failed. Target Route is missing.",
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            if (!targetRoute.HasPrimaryScene)
            {
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    $"Route Request failed. Target Route '{targetRoute.RouteName}' has no Primary Scene.",
                    targetRoute,
                    resolvedSource,
                    resolvedReason);
            }

            var gateEvaluation = EvaluateLifecycleRequestAdmission("RouteRequest", resolvedSource, resolvedReason);
            if (!gateEvaluation.IsAllowed)
            {
                return FrameworkRouteRequestResult.IgnoredBlockedByGate(
                    targetRoute,
                    resolvedSource,
                    resolvedReason,
                    gateEvaluation,
                    CreateBlockedTransitionGateDiagnostics(gateEvaluation));
            }

            if (_routeLifecycleRuntime.IsRouteActive(targetRoute))
            {
                return FrameworkRouteRequestResult.IgnoredAlreadyActive(targetRoute, resolvedSource, resolvedReason);
            }

            _routeRequestInFlight = true;
            TransitionGateDiagnostics transitionGateDiagnostics = default;
            try
            {
                var previousRoute = _routeLifecycleRuntime.CurrentRoute;
                var previousActivity = _routeLifecycleRuntime.CurrentActivity;
                var operationId = CreateTransitionOperationId(TransitionScope.Route);
                var transitionGateMode = ResolveRouteTransitionGateMode(targetRoute);
                var transitionGateSnapshot = ApplyTransitionGate(
                    operationId,
                    TransitionKind.RouteSwitch,
                    transitionGateMode,
                    resolvedSource,
                    resolvedReason);
                var transitionBefore = await ExecuteTransitionAsync(
                    TransitionRequest.Before(
                        operationId,
                        TransitionScope.Route,
                        resolvedSource,
                        resolvedReason,
                        previousRoute,
                        targetRoute,
                        previousActivity,
                        previousActivity));

                if (beforeRouteLifecycle != null)
                {
                    await beforeRouteLifecycle();
                }

                var routeLifecycleResult = await StartRouteCoreAsync(targetRoute, resolvedSource, resolvedReason, progressReporter);

                if (afterRouteLifecycle != null)
                {
                    await afterRouteLifecycle();
                }

                var transitionAfter = await ExecuteTransitionAsync(
                    TransitionRequest.After(
                        operationId,
                        TransitionScope.Route,
                        resolvedSource,
                        resolvedReason,
                        previousRoute,
                        targetRoute,
                        previousActivity,
                        routeLifecycleResult.ActivityFlowResult.Activity));
                var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                    TransitionScope.Route,
                    transitionBefore,
                    transitionAfter);
                transitionGateDiagnostics = ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot);

                if (!routeLifecycleResult.Started)
                {
                    return FrameworkRouteRequestResult.FailedInvalidConfig(
                        routeLifecycleResult.Message,
                        targetRoute,
                        resolvedSource,
                        resolvedReason,
                        transitionGateDiagnostics);
                }

                return FrameworkRouteRequestResult.SucceededWith(
                    targetRoute,
                    resolvedSource,
                    resolvedReason,
                    routeLifecycleResult,
                    transitionDiagnostics,
                    transitionGateDiagnostics);
            }
            finally
            {
                ReleaseTransitionGateIfStillActive();
                _routeRequestInFlight = false;
            }
        }

        internal async Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            return await RequestActivityAsync(
                targetActivity,
                source,
                reason,
                beforeActivityLifecycle: null,
                afterActivityLifecycle: null);
        }

        internal Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason,
            Func<Awaitable> beforeActivityLifecycle,
            Func<Awaitable> afterActivityLifecycle)
        {
            return RequestActivityAsync(
                targetActivity,
                source,
                reason,
                beforeActivityLifecycle,
                afterActivityLifecycle,
                NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason,
            Func<Awaitable> beforeActivityLifecycle,
            Func<Awaitable> afterActivityLifecycle,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback("None");

            if (targetActivity == null)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Request failed. Target Activity is missing.",
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            if (!_routeLifecycleRuntime.HasActiveRoute)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Request failed. No active Route is available.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason);
            }

            var gateEvaluation = EvaluateLifecycleRequestAdmission("ActivityRequest", resolvedSource, resolvedReason);
            if (!gateEvaluation.IsAllowed)
            {
                return FrameworkActivityRequestResult.IgnoredBlockedByGate(
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    gateEvaluation,
                    CreateBlockedTransitionGateDiagnostics(gateEvaluation));
            }

            if (_routeLifecycleRuntime.IsActivityActive(targetActivity))
            {
                return FrameworkActivityRequestResult.IgnoredAlreadyActive(targetActivity, resolvedSource, resolvedReason);
            }

            var currentRoute = _routeLifecycleRuntime.CurrentRoute;
            var previousActivity = _routeLifecycleRuntime.CurrentActivity;
            var activityTransitionMode = ResolveActivityTransitionMode(targetActivity);
            var operationKind = ResolveActivityOperationKind(previousActivity, targetActivity);
            var operationPreview = PreviewActivityOperation(
                operationKind,
                previousActivity,
                targetActivity,
                activityTransitionMode,
                resolvedSource,
                resolvedReason);
            if (operationPreview.IsBlocked)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Request blocked by ActivityOperationPlan. " + operationPreview.ToDiagnosticString(),
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode);
            }

            if (RequiresGameplayReady(targetActivity) &&
                operationPreview.HasSceneSideEffects &&
                !operationPreview.RequiresVisualOcclusion)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "GameplayReady Activity switches with scene side effects require explicit visual occlusion before asynchronous target preparation.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode);
            }

            ActivityPlayerLifecycleAdmissionResult playerAdmissionPreparation =
                PrepareActivityPlayerLifecycleAdmission(
                    previousActivity,
                    targetActivity,
                    resolvedSource,
                    resolvedReason);
            if (playerAdmissionPreparation == null ||
                !playerAdmissionPreparation.ReadyForTransition)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    playerAdmissionPreparation != null
                        ? playerAdmissionPreparation.ToDiagnosticString()
                        : "Activity Player lifecycle admission preparation returned no result.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode);
            }

            ActivityPlayerLifecycleAdmissionResult playerAdmissionAuthorization = null;
            _activityRequestInFlight = true;
            TransitionGateDiagnostics transitionGateDiagnostics = default;
            try
            {
                var operationId = CreateTransitionOperationId(TransitionScope.Activity);
                var transitionGateMode = ResolveActivityTransitionGateMode(targetActivity);
                var transitionGateSnapshot = ApplyTransitionGate(
                    operationId,
                    TransitionKind.ActivitySwitch,
                    transitionGateMode,
                    resolvedSource,
                    resolvedReason);
                playerAdmissionAuthorization =
                    AuthorizeActivityPlayerTransition(
                        playerAdmissionPreparation,
                        resolvedSource,
                        resolvedReason);
                if (playerAdmissionAuthorization == null ||
                    !playerAdmissionAuthorization.ReadyForTransition)
                {
                    return FrameworkActivityRequestResult.FailedInvalidConfig(
                        playerAdmissionAuthorization != null
                            ? playerAdmissionAuthorization.ToDiagnosticString()
                            : "Activity Player transition authorization returned no result.",
                        targetActivity,
                        resolvedSource,
                        resolvedReason,
                        activityTransitionMode);
                }

                var transitionBefore = await ExecuteActivityTransitionAsync(
                    TransitionRequest.Before(
                        operationId,
                        TransitionScope.Activity,
                        resolvedSource,
                        resolvedReason,
                        currentRoute,
                        currentRoute,
                        previousActivity,
                        targetActivity),
                    activityTransitionMode);

                if (beforeActivityLifecycle != null)
                {
                    await beforeActivityLifecycle();
                }

                var activityFlowResult = await _routeLifecycleRuntime
                    .StartActivityWithActivationGateAsync(
                        targetActivity,
                        resolvedSource,
                        resolvedReason,
                        progressReporter,
                        () => CommitActivityPlayerLifecycleAdmission(
                            playerAdmissionAuthorization,
                            resolvedSource,
                            resolvedReason));

                if (afterActivityLifecycle != null)
                {
                    await afterActivityLifecycle();
                }

                var transitionAfter = await ExecuteActivityTransitionAsync(
                    TransitionRequest.After(
                        operationId,
                        TransitionScope.Activity,
                        resolvedSource,
                        resolvedReason,
                        currentRoute,
                        currentRoute,
                        previousActivity,
                        activityFlowResult.Activity),
                    activityTransitionMode);
                var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                    TransitionScope.Activity,
                    transitionBefore,
                    transitionAfter);
                transitionGateDiagnostics = ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot);

                if (!activityFlowResult.Completed)
                {
                    return FrameworkActivityRequestResult.FailedInvalidConfig(
                        activityFlowResult.Message,
                        targetActivity,
                        resolvedSource,
                        resolvedReason,
                        activityTransitionMode,
                        GameFlowRequestOperationKind.Activity,
                        transitionGateDiagnostics);
                }
                if (RequiresGameplayReady(targetActivity) &&
                    !activityFlowResult.IsActivityReady)
                {
                    return FrameworkActivityRequestResult.FailedInvalidConfig(
                        "GameplayReady target Activity did not complete lifecycle adoption. " +
                        activityFlowResult.Message,
                        targetActivity,
                        resolvedSource,
                        resolvedReason,
                        activityTransitionMode,
                        GameFlowRequestOperationKind.Activity,
                        transitionGateDiagnostics);
                }

                return FrameworkActivityRequestResult.SucceededWith(
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    activityFlowResult,
                    transitionDiagnostics,
                    transitionGateDiagnostics: transitionGateDiagnostics,
                    activityTransitionMode: activityTransitionMode);
            }
            finally
            {
                RollbackPendingActivityPlayerLifecycleAdmission(
                    playerAdmissionAuthorization ?? playerAdmissionPreparation,
                    resolvedSource,
                    "activity-request-finalization");
                ReleaseTransitionGateIfStillActive();
                _activityRequestInFlight = false;
            }
        }

        internal async Task<FrameworkActivityRequestResult> ClearActivityAsync(string source, string reason)
        {
            return await ClearActivityAsync(source, reason, beforeActivityLifecycle: null, afterActivityLifecycle: null);
        }

        internal Task<FrameworkActivityRequestResult> ClearActivityAsync(
            string source,
            string reason,
            Func<Awaitable> beforeActivityLifecycle,
            Func<Awaitable> afterActivityLifecycle)
        {
            return ClearActivityAsync(
                source,
                reason,
                beforeActivityLifecycle,
                afterActivityLifecycle,
                NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<FrameworkActivityRequestResult> ClearActivityAsync(
            string source,
            string reason,
            Func<Awaitable> beforeActivityLifecycle,
            Func<Awaitable> afterActivityLifecycle,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback("None");

            if (!_routeLifecycleRuntime.HasActiveRoute)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Request failed. No active Route is available.",
                    null,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.ActivityClear);
            }

            var gateEvaluation = EvaluateLifecycleRequestAdmission("ClearActivityRequest", resolvedSource, resolvedReason);
            if (!gateEvaluation.IsAllowed)
            {
                return FrameworkActivityRequestResult.IgnoredBlockedByGate(
                    null,
                    resolvedSource,
                    resolvedReason,
                    gateEvaluation,
                    CreateBlockedTransitionGateDiagnostics(gateEvaluation),
                    GameFlowRequestOperationKind.ActivityClear);
            }

            if (!_routeLifecycleRuntime.HasActiveActivity)
            {
                return FrameworkActivityRequestResult.IgnoredNoActiveActivity(resolvedSource, resolvedReason);
            }

            var currentRoute = _routeLifecycleRuntime.CurrentRoute;
            var previousActivity = _routeLifecycleRuntime.CurrentActivity;
            var activityTransitionMode = ResolveActivityTransitionMode(previousActivity);
            var operationPreview = PreviewActivityOperation(
                ActivityOperationKind.Clear,
                previousActivity,
                null,
                activityTransitionMode,
                resolvedSource,
                resolvedReason);
            if (operationPreview.IsBlocked)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Clear blocked by ActivityOperationPlan. " + operationPreview.ToDiagnosticString(),
                    null,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode,
                    GameFlowRequestOperationKind.ActivityClear);
            }

            _activityRequestInFlight = true;
            TransitionGateDiagnostics transitionGateDiagnostics = default;
            try
            {
                var operationId = CreateTransitionOperationId(TransitionScope.ActivityClear);
                var transitionGateMode = ResolveActivityTransitionGateMode(previousActivity);
                var transitionGateSnapshot = ApplyTransitionGate(
                    operationId,
                    TransitionKind.ActivityClear,
                    transitionGateMode,
                    resolvedSource,
                    resolvedReason);
                var transitionBefore = await ExecuteActivityTransitionAsync(
                    TransitionRequest.Before(
                        operationId,
                        TransitionScope.ActivityClear,
                        resolvedSource,
                        resolvedReason,
                        currentRoute,
                        currentRoute,
                        previousActivity,
                        null),
                    activityTransitionMode);

                if (beforeActivityLifecycle != null)
                {
                    await beforeActivityLifecycle();
                }

                var activityFlowResult = await _routeLifecycleRuntime.ClearActivityAsync(resolvedSource, resolvedReason, progressReporter);

                if (afterActivityLifecycle != null)
                {
                    await afterActivityLifecycle();
                }

                var transitionAfter = await ExecuteActivityTransitionAsync(
                    TransitionRequest.After(
                        operationId,
                        TransitionScope.ActivityClear,
                        resolvedSource,
                        resolvedReason,
                        currentRoute,
                        currentRoute,
                        previousActivity,
                        activityFlowResult.Activity),
                    activityTransitionMode);
                var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                    TransitionScope.ActivityClear,
                    transitionBefore,
                    transitionAfter);
                transitionGateDiagnostics = ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot);

                if (!activityFlowResult.Completed)
                {
                    return FrameworkActivityRequestResult.FailedInvalidConfig(
                        activityFlowResult.Message,
                        null,
                        resolvedSource,
                        resolvedReason,
                        activityTransitionMode,
                        GameFlowRequestOperationKind.ActivityClear,
                        transitionGateDiagnostics);
                }

                return FrameworkActivityRequestResult.SucceededWith(
                    null,
                    resolvedSource,
                    resolvedReason,
                    activityFlowResult,
                    transitionDiagnostics,
                    transitionGateDiagnostics: transitionGateDiagnostics,
                    activityTransitionMode: activityTransitionMode);
            }
            finally
            {
                ReleaseTransitionGateIfStillActive();
                _activityRequestInFlight = false;
            }
        }

        internal Awaitable<FrameworkActivityRestartFlowResult> RestartActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            return RestartActivityAsync(targetActivity, source, reason, beforeRestartLifecycle: null);
        }

        internal async Awaitable<FrameworkActivityRestartFlowResult> RestartActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason,
            Func<Awaitable<bool>> beforeRestartLifecycle)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback("None");

            if (targetActivity == null)
            {
                var clearMissingTarget = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart failed. Target Activity is missing.",
                    null,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.ActivityClear);
                var reenterMissingTarget = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart failed. Target Activity is missing.",
                    null,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.Activity);
                return FrameworkActivityRestartFlowResult.FailedClear(
                    clearMissingTarget,
                    reenterMissingTarget,
                    "Activity Restart failed. Target Activity is missing.");
            }

            if (!_routeLifecycleRuntime.HasActiveRoute)
            {
                var clearNoRoute = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart failed. No active Route is available.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.ActivityClear);
                var reenterNoRoute = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart failed. No active Route is available.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.Activity);
                return FrameworkActivityRestartFlowResult.FailedClear(
                    clearNoRoute,
                    reenterNoRoute,
                    "Activity Restart failed. No active Route is available.");
            }

            var gateEvaluation = EvaluateLifecycleRequestAdmission("ActivityRestartRequest", resolvedSource, resolvedReason);
            if (!gateEvaluation.IsAllowed)
            {
                var blockedTransitionGateDiagnostics = CreateBlockedTransitionGateDiagnostics(gateEvaluation);
                var clearBlocked = FrameworkActivityRequestResult.IgnoredBlockedByGate(
                    null,
                    resolvedSource,
                    resolvedReason,
                    gateEvaluation,
                    blockedTransitionGateDiagnostics,
                    GameFlowRequestOperationKind.ActivityClear);
                var reenterBlocked = FrameworkActivityRequestResult.IgnoredBlockedByGate(
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    gateEvaluation,
                    blockedTransitionGateDiagnostics,
                    GameFlowRequestOperationKind.Activity);
                return FrameworkActivityRestartFlowResult.FailedClear(
                    clearBlocked,
                    reenterBlocked,
                    "Activity Restart ignored. Request is blocked by Gate admission.");
            }

            if (!_routeLifecycleRuntime.HasActiveActivity)
            {
                var clearNoActivity = FrameworkActivityRequestResult.IgnoredNoActiveActivity(resolvedSource, resolvedReason);
                var reenterNoActivity = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart failed. No active Activity is available to restart.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.Activity);
                return FrameworkActivityRestartFlowResult.FailedClear(
                    clearNoActivity,
                    reenterNoActivity,
                    "Activity Restart failed. No active Activity is available to restart.");
            }

            if (!_routeLifecycleRuntime.IsActivityActive(targetActivity))
            {
                var currentActivity = _routeLifecycleRuntime.CurrentActivity;
                var clearMismatch = FrameworkActivityRequestResult.FailedInvalidConfig(
                    $"Activity Restart failed. Target Activity must be the current active Activity. current='{(currentActivity != null ? currentActivity.ActivityName : string.Empty)}' target='{targetActivity.ActivityName}'.",
                    null,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.ActivityClear);
                var reenterMismatch = FrameworkActivityRequestResult.FailedInvalidConfig(
                    $"Activity Restart failed. Target Activity must be the current active Activity. current='{(currentActivity != null ? currentActivity.ActivityName : string.Empty)}' target='{targetActivity.ActivityName}'.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    ActivityVisualTransitionMode.Seamless,
                    GameFlowRequestOperationKind.Activity);
                return FrameworkActivityRestartFlowResult.FailedClear(
                    clearMismatch,
                    reenterMismatch,
                    "Activity Restart failed. Target Activity must be the current active Activity.");
            }

            var currentRoute = _routeLifecycleRuntime.CurrentRoute;
            var previousActivity = _routeLifecycleRuntime.CurrentActivity;
            var activityTransitionMode = ResolveActivityTransitionMode(targetActivity);
            var clearPreview = PreviewActivityOperation(
                ActivityOperationKind.Clear,
                previousActivity,
                null,
                activityTransitionMode,
                resolvedSource,
                BuildRestartStageReason(resolvedReason, "clear"));
            if (clearPreview.IsBlocked)
            {
                var clearBlocked = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart clear stage blocked by ActivityOperationPlan. " + clearPreview.ToDiagnosticString(),
                    null,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode,
                    GameFlowRequestOperationKind.ActivityClear);
                var reenterBlocked = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart clear stage blocked by ActivityOperationPlan. Activity re-enter was not requested.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode,
                    GameFlowRequestOperationKind.Activity);
                return FrameworkActivityRestartFlowResult.FailedClear(
                    clearBlocked,
                    reenterBlocked,
                    "Activity Restart failed. Clear stage was blocked by ActivityOperationPlan.");
            }

            var reenterPreview = PreviewActivityOperation(
                ActivityOperationKind.Start,
                null,
                targetActivity,
                activityTransitionMode,
                resolvedSource,
                BuildRestartStageReason(resolvedReason, "reenter"));
            if (reenterPreview.IsBlocked)
            {
                var clearBlocked = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart re-enter stage blocked by ActivityOperationPlan. Activity clear was not requested.",
                    null,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode,
                    GameFlowRequestOperationKind.ActivityClear);
                var reenterBlocked = FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Restart re-enter stage blocked by ActivityOperationPlan. " + reenterPreview.ToDiagnosticString(),
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    activityTransitionMode,
                    GameFlowRequestOperationKind.Activity);
                return FrameworkActivityRestartFlowResult.FailedReenter(
                    clearBlocked,
                    reenterBlocked,
                    "Activity Restart failed. Re-enter stage was blocked by ActivityOperationPlan.");
            }

            _activityRequestInFlight = true;
            TransitionGateDiagnostics transitionGateDiagnostics = default;
            try
            {
                var operationId = CreateTransitionOperationId(TransitionScope.Activity);
                var transitionGateMode = ResolveActivityTransitionGateMode(targetActivity);
                var transitionGateSnapshot = ApplyTransitionGate(
                    operationId,
                    TransitionKind.ActivitySwitch,
                    transitionGateMode,
                    resolvedSource,
                    resolvedReason);
                var transitionBefore = await ExecuteActivityTransitionAsync(
                    TransitionRequest.Before(
                        operationId,
                        TransitionScope.Activity,
                        resolvedSource,
                        resolvedReason,
                        currentRoute,
                        currentRoute,
                        previousActivity,
                        targetActivity),
                    activityTransitionMode);

                if (beforeRestartLifecycle != null)
                {
                    bool shouldContinue;
                    try
                    {
                        shouldContinue = await beforeRestartLifecycle();
                    }
                    catch
                    {
                        shouldContinue = false;
                    }

                    if (!shouldContinue)
                    {
                        await ExecuteActivityTransitionAsync(
                            TransitionRequest.After(
                                operationId,
                                TransitionScope.Activity,
                                resolvedSource,
                                resolvedReason,
                                currentRoute,
                                currentRoute,
                                previousActivity,
                                previousActivity),
                            activityTransitionMode);
                        transitionGateDiagnostics = ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot);
                        var clearPreStageFailed = FrameworkActivityRequestResult.FailedInvalidConfig(
                            "Activity Restart failed. Pre-clear restart stage failed; Activity clear was not requested.",
                            null,
                            resolvedSource,
                            BuildRestartStageReason(resolvedReason, "clear"),
                            activityTransitionMode,
                            GameFlowRequestOperationKind.ActivityClear,
                            transitionGateDiagnostics);
                        var reenterPreStageSkipped = FrameworkActivityRequestResult.FailedInvalidConfig(
                            "Activity Restart failed. Pre-clear restart stage failed; Activity re-enter was not requested.",
                            targetActivity,
                            resolvedSource,
                            BuildRestartStageReason(resolvedReason, "reenter"),
                            activityTransitionMode,
                            GameFlowRequestOperationKind.Activity,
                            transitionGateDiagnostics);
                        return FrameworkActivityRestartFlowResult.FailedClear(
                            clearPreStageFailed,
                            reenterPreStageSkipped,
                            "Activity Restart failed. Pre-clear restart stage failed.");
                    }
                }

                var clearFlowResult = await _routeLifecycleRuntime.ClearActivityAsync(
                    resolvedSource,
                    BuildRestartStageReason(resolvedReason, "clear"),
                    NoOpFrameworkLoadingProgressReporter.Instance);

                if (!clearFlowResult.Completed)
                {
                    await ExecuteActivityTransitionAsync(
                        TransitionRequest.After(
                            operationId,
                            TransitionScope.Activity,
                            resolvedSource,
                            resolvedReason,
                            currentRoute,
                            currentRoute,
                            previousActivity,
                            clearFlowResult.Activity),
                        activityTransitionMode);
                    transitionGateDiagnostics = ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot);
                    var clearResult = FrameworkActivityRequestResult.FailedInvalidConfig(
                        clearFlowResult.Message,
                        null,
                        resolvedSource,
                        BuildRestartStageReason(resolvedReason, "clear"),
                        activityTransitionMode,
                        GameFlowRequestOperationKind.ActivityClear,
                        transitionGateDiagnostics);
                    var reenterSkippedResult = FrameworkActivityRequestResult.FailedInvalidConfig(
                        "Activity Restart failed. Activity Clear did not complete; Activity re-enter was not requested.",
                        targetActivity,
                        resolvedSource,
                        BuildRestartStageReason(resolvedReason, "reenter"),
                        activityTransitionMode,
                        GameFlowRequestOperationKind.Activity,
                        transitionGateDiagnostics);
                    return FrameworkActivityRestartFlowResult.FailedClear(
                        clearResult,
                        reenterSkippedResult,
                        "Activity Restart failed. Activity Clear did not complete.");
                }

                var reenterFlowResult = await _routeLifecycleRuntime.StartActivityAsync(
                    targetActivity,
                    resolvedSource,
                    BuildRestartStageReason(resolvedReason, "reenter"),
                    NoOpFrameworkLoadingProgressReporter.Instance);
                var transitionAfter = await ExecuteActivityTransitionAsync(
                    TransitionRequest.After(
                        operationId,
                        TransitionScope.Activity,
                        resolvedSource,
                        resolvedReason,
                        currentRoute,
                        currentRoute,
                        previousActivity,
                        reenterFlowResult.Activity),
                    activityTransitionMode);
                var transitionDiagnostics = FrameworkTransitionDiagnostics.Completed(
                    TransitionScope.Activity,
                    transitionBefore,
                    transitionAfter);
                transitionGateDiagnostics = ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot);

                var clearSucceededResult = FrameworkActivityRequestResult.SucceededWith(
                    null,
                    resolvedSource,
                    BuildRestartStageReason(resolvedReason, "clear"),
                    clearFlowResult,
                    transitionDiagnostics,
                    transitionGateDiagnostics: transitionGateDiagnostics,
                    activityTransitionMode: activityTransitionMode);

                if (!reenterFlowResult.Completed)
                {
                    var reenterFailedResult = FrameworkActivityRequestResult.FailedInvalidConfig(
                        reenterFlowResult.Message,
                        targetActivity,
                        resolvedSource,
                        BuildRestartStageReason(resolvedReason, "reenter"),
                        activityTransitionMode,
                        GameFlowRequestOperationKind.Activity,
                        transitionGateDiagnostics);
                    return FrameworkActivityRestartFlowResult.FailedReenter(
                        clearSucceededResult,
                        reenterFailedResult,
                        "Activity Restart failed. Activity re-enter did not complete.");
                }

                var reenterSucceededResult = FrameworkActivityRequestResult.SucceededWith(
                    targetActivity,
                    resolvedSource,
                    BuildRestartStageReason(resolvedReason, "reenter"),
                    reenterFlowResult,
                    transitionDiagnostics,
                    transitionGateDiagnostics: transitionGateDiagnostics,
                    activityTransitionMode: activityTransitionMode);

                return FrameworkActivityRestartFlowResult.Completed(
                    clearSucceededResult,
                    reenterSucceededResult,
                    "Activity Restart flow completed with a single Activity transition.");
            }
            finally
            {
                ReleaseTransitionGateIfStillActive();
                _activityRequestInFlight = false;
            }
        }


        internal async Task<CycleResetResult> RequestRouteCycleResetAsync(string source, string reason)
        {
            return await RequestCycleResetAsync(CycleResetScope.Route, CycleResetPolicy.RouteDefault(), source, reason);
        }

        internal async Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason)
        {
            return await RequestCycleResetAsync(CycleResetScope.Activity, CycleResetPolicy.ActivityDefault(), source, reason);
        }

        internal async Task<CycleResetResult> RequestCycleResetAsync(
            CycleResetScope scope,
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            var gateEvaluation = EvaluateLifecycleRequestAdmission("CycleResetRequest", source, reason);
            if (!gateEvaluation.IsAllowed)
            {
                string blockedMessage = GateRequestAdmission.FormatBlockedMessage(
                    "Cycle Reset Request",
                    gateEvaluation);

                return CycleResetResult.RejectedInvalidRequest(
                    default,
                    new[]
                    {
                        CycleResetIssue.BlockingIssue(
                            CycleResetIssueKind.RequestAlreadyInFlight,
                            default,
                            scope,
                            blockedMessage)
                    },
                    source,
                    reason,
                    blockedMessage);
            }

            _cycleResetRequestInFlight = true;
            try
            {
                if (scope == CycleResetScope.Route)
                {
                    return await _routeLifecycleRuntime.RequestRouteCycleResetAsync(policy, source, reason);
                }

                if (scope == CycleResetScope.Activity)
                {
                    return await _routeLifecycleRuntime.RequestActivityCycleResetAsync(policy, source, reason);
                }

                return CycleResetResult.RejectedInvalidRequest(
                    default,
                    new[]
                    {
                        CycleResetIssue.BlockingIssue(
                            CycleResetIssueKind.InvalidRequest,
                            default,
                            scope,
                            "Cycle Reset Request failed because scope is invalid.")
                    },
                    source,
                    reason,
                    "Cycle Reset Request failed because scope is invalid.");
            }
            finally
            {
                _cycleResetRequestInFlight = false;
            }
        }

        internal GateEvaluationResult EvaluateExternalLifecycleRequestAdmission(
            string subject,
            string source,
            string reason,
            bool objectResetRequestInFlight)
        {
            if (_transitionGateSnapshot.HasBlockers && TransitionGateBlockerPolicy.BlocksLifecycleRequests(_transitionGateMode))
            {
                var transitionGateEvaluation = EvaluateTransitionGateAdmission(
                    GateScope.GameFlow,
                    GateDomain.LifecycleRequest,
                    subject,
                    source,
                    reason);
                if (!transitionGateEvaluation.IsAllowed)
                {
                    return transitionGateEvaluation;
                }
            }

            return GateRequestAdmission.EvaluateLifecycleRequest(
                subject,
                source,
                reason,
                _routeRequestInFlight,
                _activityRequestInFlight,
                _cycleResetRequestInFlight,
                objectResetRequestInFlight);
        }

        private GateEvaluationResult EvaluateLifecycleRequestAdmission(
            string subject,
            string source,
            string reason)
        {
            return EvaluateExternalLifecycleRequestAdmission(
                subject,
                source,
                reason,
                objectResetRequestInFlight: false);
        }

        internal GateEvaluationResult EvaluateTransitionGateAdmission(
            GateScope scope,
            GateDomain domain,
            string subject,
            string source,
            string reason)
        {
            return _transitionGateSnapshot.Evaluate(
                scope,
                domain,
                default,
                subject.NormalizeTextOrFallback("TransitionGateAdmission"),
                source,
                reason,
                TransitionGateBlockerPolicy.PolicySource);
        }

        private Task<RouteLifecycleStartResult> StartRouteCoreAsync(RouteAsset route, string source, string reason)
        {
            return StartRouteCoreAsync(route, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        private Task<RouteLifecycleStartResult> StartRouteCoreAsync(
            RouteAsset route,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return _routeLifecycleRuntime.StartRouteAsync(route, source, reason, progressReporter);
        }

        private async Awaitable<TransitionResult> ExecuteActivityTransitionAsync(
            TransitionRequest request,
            ActivityVisualTransitionMode mode)
        {
            if (!ShouldExecuteActivityTransition(mode))
            {
                return CreateSkippedActivityTransitionResult(request, mode);
            }

            return await ExecuteTransitionAsync(request);
        }

        private static string BuildRestartStageReason(string resolvedReason, string stage)
        {
            return $"{resolvedReason.NormalizeTextOrFallback("Activity Restart")}:{stage}";
        }


        private static ActivityOperationKind ResolveActivityOperationKind(ActivityAsset previousActivity, ActivityAsset targetActivity)
        {
            if (targetActivity == null)
            {
                return ActivityOperationKind.Clear;
            }

            return previousActivity == null
                ? ActivityOperationKind.Start
                : ActivityOperationKind.Switch;
        }

        private static ActivityVisualTransitionMode ResolveActivityTransitionMode(ActivityAsset activity)
        {
            return activity != null ? activity.VisualTransitionMode : ActivityVisualTransitionMode.Seamless;
        }

        private static TransitionGateMode ResolveRouteTransitionGateMode(RouteAsset route)
        {
            return route != null ? route.TransitionGateMode : TransitionGateMode.InputInteractionAndGameplay;
        }

        private static TransitionGateMode ResolveActivityTransitionGateMode(ActivityAsset activity)
        {
            return activity != null ? activity.TransitionGateMode : TransitionGateMode.LifecycleRequestsOnly;
        }

        private static bool ShouldExecuteActivityTransition(ActivityVisualTransitionMode mode)
        {
            return mode is ActivityVisualTransitionMode.Fade or ActivityVisualTransitionMode.FadeWithLoading;
        }

        private static TransitionResult CreateSkippedActivityTransitionResult(
            TransitionRequest request,
            ActivityVisualTransitionMode mode)
        {
            var step = TransitionStep.Skipped(
                0,
                request.Phase,
                BuildSkippedActivityTransitionStepLabel(request),
                $"Activity Transition skipped by policy. mode='{mode}'.");

            return TransitionResult.SkippedResult(
                request.OperationId,
                request.Kind,
                request.Source,
                request.Reason,
                "SkippedByActivityPolicy",
                new[] { step },
                TransitionEffectKind.Unknown,
                TransitionEffectStatus.Skipped,
                0,
                "None",
                0);
        }

        private static string BuildSkippedActivityTransitionStepLabel(TransitionRequest request)
        {
            string phase = request.Phase == TransitionPhase.OperationOpened ? "before" : "after";
            return $"{request.Scope.ToString().ToLowerInvariant()}-{phase}-policy-skip";
        }

        private Awaitable<TransitionResult> ExecuteTransitionAsync(TransitionRequest request)
        {
            return _transitionOrchestrator.ExecuteAsync(request);
        }

        private GateSnapshot ApplyTransitionGate(
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionGateMode mode,
            string source,
            string reason)
        {
            _transitionGateMode = Enum.IsDefined(typeof(TransitionGateMode), mode)
                ? mode
                : TransitionGateMode.None;
            _transitionGateSnapshot = TransitionGateBlockerPolicy.CreateRunningSnapshot(
                operationId,
                kind,
                _transitionGateMode,
                source,
                reason);
            return _transitionGateSnapshot;
        }

        private TransitionGateDiagnostics ReleaseTransitionGate(TransitionGateMode mode, GateSnapshot appliedSnapshot)
        {
            _transitionGateSnapshot = TransitionGateBlockerPolicy.CreateReleasedSnapshot();
            _transitionGateMode = TransitionGateMode.None;

            return appliedSnapshot.HasBlockers
                ? TransitionGateDiagnostics.AppliedAndReleased(mode, appliedSnapshot)
                : TransitionGateDiagnostics.NotApplied(mode);
        }

        private void ReleaseTransitionGateIfStillActive()
        {
            if (!_transitionGateSnapshot.HasBlockers)
            {
                return;
            }

            _transitionGateSnapshot = TransitionGateBlockerPolicy.CreateReleasedSnapshot();
            _transitionGateMode = TransitionGateMode.None;
        }

        private TransitionGateDiagnostics CreateBlockedTransitionGateDiagnostics(GateEvaluationResult gateEvaluation)
        {
            if (!_transitionGateSnapshot.HasBlockers || !gateEvaluation.IsValid || gateEvaluation.IsAllowed)
            {
                return default;
            }

            return TransitionGateDiagnostics.Rejected(_transitionGateMode, _transitionGateSnapshot, gateEvaluation);
        }

        private TransitionOperationId CreateTransitionOperationId(TransitionScope scope)
        {
            _transitionRequestSequence++;
            return TransitionOperationId.From($"framework.{scope.ToString().ToLowerInvariant()}.{_transitionRequestSequence}");
        }
    }
}
