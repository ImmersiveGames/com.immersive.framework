using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.SessionLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Loading;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.RuntimeObjects;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;
using Immersive.Framework.Gate;
using Immersive.Framework.GlobalUi;
using Immersive.Framework.Pause;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using Immersive.Framework.Common;
using Immersive.Framework.Common.LifecycleOperations;

namespace Immersive.Framework.ApplicationLifecycle
{
    /// <summary>
    /// Minimal persistent application-scope runtime owner for the framework.
    /// It owns the Game Flow instance for this boot, but does not expose a global service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class FrameworkRuntimeHost : MonoBehaviour
    {
        private const string RuntimeHostName = "Immersive Framework Runtime";

        private static FrameworkRuntimeHost _current;

        private GameApplicationAsset _gameApplication;
        private GameFlowRuntime _gameFlowRuntime;
        private PauseRuntime _pauseRuntime;
        private PauseTimeScaleRuntime _pauseTimeScaleRuntime;
        private PauseSurfaceRuntime _pauseSurfaceRuntime;
        private int _pauseRequestSequence;
        private RuntimeContentRuntime _runtimeContentRuntime;
        private RuntimeContentAnchorBinding _contentAnchorBindingRuntime;
        private RuntimeScopeLifecycleResult _runtimeSessionScopeResult;
        private ObjectEntryRuntimeContextSnapshot _objectEntryRuntimeContextSnapshot;
        private ObjectResetRuntime _objectResetRuntime;
        private readonly List<IObjectResetParticipantSource> _objectResetParticipantSources = new();
        private RuntimeObjectParticipationRegistry _runtimeObjectParticipationRegistry;
        private LoadingSurfaceRuntime _loadingSurfaceRuntime;
        private GlobalUiSceneRuntime _globalUiSceneRuntime;
        private bool _objectResetRequestInFlight;
        private int _objectEntryRuntimeContextRevision;
        private int _objectEntryRuntimeContextInvalidationCount;
        private string _lastObjectEntryRuntimeContextInvalidationReason = string.Empty;
        private FrameworkRuntimeState _state;
        private FrameworkLogger _logger;

        public FrameworkRuntimeState State => _state;

        public SessionRuntimeState SessionState => _state.SessionState;

        internal PauseState PauseState => _pauseRuntime?.State ?? PauseState.Unknown;

        internal GateSnapshot PauseGateSnapshot => _pauseRuntime?.GateSnapshot ?? GateSnapshot.Empty();

        internal GateSnapshot TransitionGateSnapshot => _gameFlowRuntime?.CurrentTransitionGateSnapshot ?? GateSnapshot.Empty();

        internal GateSnapshot CurrentGateSnapshot => CreateCombinedGateSnapshot(PauseGateSnapshot, TransitionGateSnapshot);

        internal bool TryGetPauseSnapshot(out PauseSnapshot snapshot)
        {
            if (_pauseRuntime == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = _pauseRuntime.Snapshot;
            return true;
        }

        internal RuntimeContentRuntime RuntimeContentRuntime => _runtimeContentRuntime;

        private static GateSnapshot CreateCombinedGateSnapshot(GateSnapshot first, GateSnapshot second)
        {
            if (!first.HasBlockers && !second.HasBlockers)
            {
                return GateSnapshot.Empty();
            }

            var blockers = new List<GateBlocker>(first.BlockerCount + second.BlockerCount);
            AddBlockers(blockers, first);
            AddBlockers(blockers, second);
            return new GateSnapshot(blockers);
        }

        private static void AddBlockers(List<GateBlocker> target, GateSnapshot snapshot)
        {
            if (target == null || !snapshot.HasBlockers)
            {
                return;
            }

            var blockers = snapshot.Blockers;
            for (int i = 0; i < blockers.Count; i++)
            {
                target.Add(blockers[i]);
            }
        }

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _gameFlowRuntime?.SetActivityContentExecutionParticipantSource(participantSource);
        }

        internal void SetCycleResetParticipantSource(ICycleResetParticipantSource participantSource)
        {
            _gameFlowRuntime?.SetCycleResetParticipantSource(participantSource);
        }

        internal void SetObjectResetParticipantSource(IObjectResetParticipantSource participantSource)
        {
            if (participantSource == null || _objectResetParticipantSources.Contains(participantSource))
            {
                return;
            }

            _objectResetParticipantSources.Add(participantSource);
        }

        internal bool ClearObjectResetParticipantSource(IObjectResetParticipantSource participantSource)
        {
            if (participantSource == null)
            {
                return false;
            }

            return _objectResetParticipantSources.Remove(participantSource);
        }

        internal bool IsObjectResetParticipantSourceRegistered(IObjectResetParticipantSource participantSource)
        {
            return participantSource != null && _objectResetParticipantSources.Contains(participantSource);
        }

        internal bool RegisterRuntimeObjectParticipation(
            ObjectEntryDescriptor descriptor,
            IReadOnlyList<IObjectResetParticipant> resetParticipants,
            UnityEngine.Object owner,
            string source,
            string reason,
            out RuntimeObjectParticipationHandle handle,
            out string issue)
        {
            EnsureRuntimeObjectParticipationRegistry();
            bool registered = _runtimeObjectParticipationRegistry.TryRegister(
                descriptor,
                resetParticipants,
                owner,
                source,
                reason,
                out handle,
                out issue);
            if (!registered)
            {
                return false;
            }

            InvalidateObjectEntryRuntimeContextSnapshot($"runtime-object-register:{NormalizeLifecycleSource(source)}");
            RefreshObjectEntryRuntimeContextSnapshotIfReady($"FrameworkRuntimeHost:runtime-object-register:{NormalizeLifecycleSource(source)}");
            return true;
        }

        internal bool UnregisterRuntimeObjectParticipation(
            RuntimeObjectParticipationHandle handle,
            UnityEngine.Object owner,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (_runtimeObjectParticipationRegistry == null)
            {
                issue = "Runtime object participation registry is unavailable.";
                return false;
            }

            bool unregistered = _runtimeObjectParticipationRegistry.TryUnregister(
                handle,
                owner,
                out _,
                out issue);
            if (!unregistered)
            {
                return false;
            }

            InvalidateObjectEntryRuntimeContextSnapshot($"runtime-object-unregister:{NormalizeLifecycleSource(source)}");
            RefreshObjectEntryRuntimeContextSnapshotIfReady($"FrameworkRuntimeHost:runtime-object-unregister:{NormalizeLifecycleSource(source)}");
            return true;
        }

        internal bool TryResolveCurrentObjectEntryOwnerIdentity(
            ObjectEntryScope scope,
            out FrameworkIdentityKey ownerIdentity)
        {
            var context = CreateCurrentObjectEntryScopedCollectionContext();
            if (!context.TryValidate(out _))
            {
                ownerIdentity = default;
                return false;
            }

            return context.TryResolveOwnerIdentity(scope, out ownerIdentity);
        }

        internal int ContentAnchorBindingCount => _contentAnchorBindingRuntime?.BindingCount ?? 0;

        internal int ObjectEntryRuntimeContextRevision => _objectEntryRuntimeContextRevision;

        internal int ObjectEntryRuntimeContextInvalidationCount => _objectEntryRuntimeContextInvalidationCount;

        internal string LastObjectEntryRuntimeContextInvalidationReason => _lastObjectEntryRuntimeContextInvalidationReason;

        internal bool TryGetObjectEntryRuntimeContextSnapshot(out ObjectEntryRuntimeContextSnapshot snapshot)
        {
            snapshot = _objectEntryRuntimeContextSnapshot;
            return snapshot != null;
        }

        internal ObjectEntryRuntimeContextSnapshot RefreshObjectEntryRuntimeContextSnapshot(string source)
        {
            var declarationSource = new ObjectEntryDeclarationSource(includeInactiveDeclarations: true);
            var context = CreateCurrentObjectEntryScopedCollectionContext();
            var sceneResult = declarationSource.CollectScoped(context);
            var result = CombineSceneAndRuntimeObjectEntries(sceneResult, context);
            _objectEntryRuntimeContextSnapshot = result.ToRuntimeContextSnapshot(source);
            _objectEntryRuntimeContextRevision++;
            return _objectEntryRuntimeContextSnapshot;
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _current = null;
        }

        internal static FrameworkRuntimeHost Create(GameApplicationAsset gameApplication)
        {
            if (_current != null)
            {
                Destroy(_current.gameObject);
                _current = null;
            }

            var runtimeObject = new GameObject(RuntimeHostName);
            DontDestroyOnLoad(runtimeObject);

            var host = runtimeObject.AddComponent<FrameworkRuntimeHost>();
            host.Initialize(gameApplication);
            _current = host;
            return host;
        }

        internal static bool TryGetCurrent(out FrameworkRuntimeHost runtimeHost)
        {
            runtimeHost = _current;
            return runtimeHost != null;
        }

        internal async Task<FrameworkGameFlowStartResult> StartAsync()
        {
            InvalidateObjectEntryRuntimeContextSnapshot("framework-start");

            var startupPrimaryScenePreparation = await PrepareStartupPrimarySceneBeforeGlobalUiAsync();
            if (!startupPrimaryScenePreparation.Loaded)
            {
                var failed = FrameworkGameFlowStartResult.Failed(startupPrimaryScenePreparation.Message);
                _state = FrameworkRuntimeState.FromGameFlowResult(_gameApplication, failed);
                return failed;
            }

            _globalUiSceneRuntime = await GlobalUiSceneRuntime.LoadAndPersistAsync(_gameApplication, transform, _logger);
            if (_globalUiSceneRuntime.HasBlockingConfigurationIssue)
            {
                var failed = FrameworkGameFlowStartResult.Failed(_globalUiSceneRuntime.BlockingConfigurationMessage);
                _state = FrameworkRuntimeState.FromGameFlowResult(_gameApplication, failed);
                return failed;
            }

            _loadingSurfaceRuntime = CreateLoadingSurfaceRuntime(_globalUiSceneRuntime);
            _pauseSurfaceRuntime = CreatePauseSurfaceRuntime(_globalUiSceneRuntime);
            ApplyPauseSurfaceSnapshot("FrameworkRuntimeHost", "framework-start");
            var transitionOrchestrator = CreateTransitionOrchestrator(_globalUiSceneRuntime);
            _gameFlowRuntime = new GameFlowRuntime(_runtimeContentRuntime, _contentAnchorBindingRuntime, transitionOrchestrator);

            var result = await _gameFlowRuntime.StartAsync(_gameApplication);
            _state = FrameworkRuntimeState.FromGameFlowResult(_gameApplication, result);
            if (result.Started)
            {
                RefreshObjectEntryRuntimeContextSnapshot("FrameworkRuntimeHost:framework-start");
            }

            return result;
        }

        private async Task<SceneLifecycleLoadResult> PrepareStartupPrimarySceneBeforeGlobalUiAsync()
        {
            var sceneLifecycleRuntime = new SceneLifecycleRuntime();
            var result = await sceneLifecycleRuntime.LoadPrimarySceneAsync(_gameApplication.StartupRoute);
            if (!result.Loaded)
            {
                _logger.Error(
                    "Startup Route Primary Scene preparation failed before UIGlobal load.",
                    BuildStartupPrimaryScenePreparationFields(result));
                return result;
            }

            _logger.Info(
                "Startup Route Primary Scene prepared before UIGlobal load.",
                BuildStartupPrimaryScenePreparationFields(result));

            return result;
        }

        private static LogField[] BuildStartupPrimaryScenePreparationFields(SceneLifecycleLoadResult result)
        {
            return LogFields.Of(
                LogFields.Field("scene", result.SceneName),
                LogFields.Field("scenePath", result.ScenePath),
                LogFields.Field("alreadyLoaded", result.AlreadyLoaded),
                LogFields.Field("loadMode", result.LoadMode),
                LogFields.Field("loaded", result.Loaded),
                LogFields.Field("message", result.Message));
        }

        internal async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            InvalidateObjectEntryRuntimeContextSnapshot($"route-request:{NormalizeLifecycleSource(source)}");
            if (targetRoute != null && ReferenceEquals(_state.CurrentRoute, targetRoute))
            {
                var result = await _gameFlowRuntime.RequestRouteAsync(targetRoute, source, reason);
                if (result.Succeeded)
                {
                    _state = FrameworkRuntimeState.FromRouteRequestResult(_state, result, true);
                    RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:route-request:{NormalizeLifecycleSource(source)}");
                }
                else if (result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive)
                {
                    RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:route-request-kept:{NormalizeLifecycleSource(source)}");
                }

                LogRouteRequestResult(result, FrameworkLoadingDiagnostics.SkippedAlreadyLoaded());
                return result;
            }

            bool showLoadingSurface = ShouldShowLoadingSurface(targetRoute);
            bool loadingProgressSupported = showLoadingSurface && _loadingSurfaceRuntime.ProgressSupported;
            var loadingShowRequest = CreateLoadingSurfaceRequest(
                targetRoute,
                source,
                reason,
                true,
                LoadingProgress.Zero,
                loadingProgressSupported);
            var loadingProgressReporter = CreateLoadingProgressReporter(loadingShowRequest, showLoadingSurface);
            LoadingSurfaceResult loadingBeforeResult = default;
            LoadingSurfaceResult loadingAfterResult = default;

            async Awaitable ShowLoadingAfterTransitionGate()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                loadingBeforeResult = await _loadingSurfaceRuntime.ShowAsync(loadingShowRequest);
            }

            async Awaitable HideLoadingBeforeTransitionRelease()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                var loadingHideRequest = CreateLoadingSurfaceRequest(
                    targetRoute,
                    source,
                    reason,
                    false,
                    ToSurfaceProgress(loadingProgressReporter.LastProgress),
                    loadingProgressReporter.HasReportedProgress && loadingProgressReporter.LastProgress is { Supported: true, IsDeterminate: true });
                loadingAfterResult = await _loadingSurfaceRuntime.HideAsync(loadingHideRequest);
            }

            var routeResult = await _gameFlowRuntime.RequestRouteAsync(
                targetRoute,
                source,
                reason,
                ShowLoadingAfterTransitionGate,
                HideLoadingBeforeTransitionRelease,
                loadingProgressReporter);
            if (routeResult.Succeeded)
            {
                _state = FrameworkRuntimeState.FromRouteRequestResult(_state, routeResult, true);
                RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:route-request:{NormalizeLifecycleSource(source)}");
            }
            else if (routeResult.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive)
            {
                RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:route-request-kept:{NormalizeLifecycleSource(source)}");
            }

            var loadingDiagnostics = showLoadingSurface
                ? FrameworkLoadingDiagnostics.FromUnitySurface(
                    loadingBeforeResult,
                    loadingAfterResult,
                    _loadingSurfaceRuntime.AdapterCount,
                    _loadingSurfaceRuntime.ProgressSupported,
                    loadingProgressReporter.LastProgress)
                : FrameworkLoadingDiagnostics.SucceededWithNoOp();

            if (!showLoadingSurface && routeResult.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive)
            {
                loadingDiagnostics = FrameworkLoadingDiagnostics.SkippedAlreadyLoaded();
            }

            LogRouteRequestResult(routeResult, loadingDiagnostics);
            return routeResult;
        }

        internal async Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            InvalidateObjectEntryRuntimeContextSnapshot($"activity-request:{NormalizeLifecycleSource(source)}");
            var previousActivity = _state.CurrentActivity;
            bool showLoadingSurface = ShouldShowActivityLoadingSurface(targetActivity, previousActivity, source, reason);
            bool loadingProgressSupported = showLoadingSurface && _loadingSurfaceRuntime.ProgressSupported;
            var loadingShowRequest = CreateActivityLoadingSurfaceRequest(
                targetActivity,
                source,
                reason,
                true,
                LoadingProgress.Zero,
                loadingProgressSupported);
            var loadingProgressReporter = CreateLoadingProgressReporter(loadingShowRequest, showLoadingSurface);
            LoadingSurfaceResult loadingBeforeResult = default;
            LoadingSurfaceResult loadingAfterResult = default;

            async Awaitable ShowLoadingAfterTransitionGate()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                loadingBeforeResult = await _loadingSurfaceRuntime.ShowAsync(loadingShowRequest);
            }

            async Awaitable HideLoadingBeforeTransitionRelease()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                var loadingHideRequest = CreateActivityLoadingSurfaceRequest(
                    targetActivity,
                    source,
                    reason,
                    false,
                    ToSurfaceProgress(loadingProgressReporter.LastProgress),
                    loadingProgressReporter.HasReportedProgress && loadingProgressReporter.LastProgress is { Supported: true, IsDeterminate: true });
                loadingAfterResult = await _loadingSurfaceRuntime.HideAsync(loadingHideRequest);
            }

            var result = await _gameFlowRuntime.RequestActivityAsync(
                targetActivity,
                source,
                reason,
                ShowLoadingAfterTransitionGate,
                HideLoadingBeforeTransitionRelease,
                loadingProgressReporter);
            if (result.Succeeded)
            {
                _state = FrameworkRuntimeState.FromActivityRequestResult(_state, result);
                RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:activity-request:{NormalizeLifecycleSource(source)}");
            }
            else if (result.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive)
            {
                RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:activity-request-kept:{NormalizeLifecycleSource(source)}");
            }

            var loadingDiagnostics = showLoadingSurface
                ? FrameworkLoadingDiagnostics.FromUnitySurface(
                    loadingBeforeResult,
                    loadingAfterResult,
                    _loadingSurfaceRuntime.AdapterCount,
                    _loadingSurfaceRuntime.ProgressSupported,
                    loadingProgressReporter.LastProgress)
                : CreateSkippedActivityLoadingDiagnostics(result);

            LogActivityRequestResult(result, loadingDiagnostics);
            return result;
        }

        internal async Task<FrameworkActivityRequestResult> ClearActivityAsync(string source, string reason)
        {
            InvalidateObjectEntryRuntimeContextSnapshot($"activity-clear:{NormalizeLifecycleSource(source)}");
            var previousActivity = _state.CurrentActivity;
            bool showLoadingSurface = ShouldShowActivityClearLoadingSurface(previousActivity, source, reason);
            bool loadingProgressSupported = showLoadingSurface && _loadingSurfaceRuntime.ProgressSupported;
            var loadingShowRequest = CreateActivityLoadingSurfaceRequest(
                previousActivity,
                source,
                reason,
                true,
                LoadingProgress.Zero,
                loadingProgressSupported);
            var loadingProgressReporter = CreateLoadingProgressReporter(loadingShowRequest, showLoadingSurface);
            LoadingSurfaceResult loadingBeforeResult = default;
            LoadingSurfaceResult loadingAfterResult = default;

            async Awaitable ShowLoadingAfterTransitionGate()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                loadingBeforeResult = await _loadingSurfaceRuntime.ShowAsync(loadingShowRequest);
            }

            async Awaitable HideLoadingBeforeTransitionRelease()
            {
                if (!showLoadingSurface)
                {
                    return;
                }

                var loadingHideRequest = CreateActivityLoadingSurfaceRequest(
                    previousActivity,
                    source,
                    reason,
                    false,
                    ToSurfaceProgress(loadingProgressReporter.LastProgress),
                    loadingProgressReporter.HasReportedProgress && loadingProgressReporter.LastProgress is { Supported: true, IsDeterminate: true });
                loadingAfterResult = await _loadingSurfaceRuntime.HideAsync(loadingHideRequest);
            }

            var result = await _gameFlowRuntime.ClearActivityAsync(
                source,
                reason,
                ShowLoadingAfterTransitionGate,
                HideLoadingBeforeTransitionRelease,
                loadingProgressReporter);
            if (result.Succeeded)
            {
                _state = FrameworkRuntimeState.FromActivityRequestResult(_state, result);
                RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:activity-clear:{NormalizeLifecycleSource(source)}");
            }
            else if (result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity)
            {
                RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:activity-clear-kept:{NormalizeLifecycleSource(source)}");
            }

            var loadingDiagnostics = showLoadingSurface
                ? FrameworkLoadingDiagnostics.FromUnitySurface(
                    loadingBeforeResult,
                    loadingAfterResult,
                    _loadingSurfaceRuntime.AdapterCount,
                    _loadingSurfaceRuntime.ProgressSupported,
                    loadingProgressReporter.LastProgress)
                : CreateSkippedActivityLoadingDiagnostics(result);

            LogActivityRequestResult(result, loadingDiagnostics);
            return result;
        }


        internal async Awaitable<FrameworkActivityRestartFlowResult> RestartActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            InvalidateObjectEntryRuntimeContextSnapshot($"activity-restart:{NormalizeLifecycleSource(source)}");
            var result = await _gameFlowRuntime.RestartActivityAsync(targetActivity, source, reason);

            if (result.ClearSucceeded)
            {
                _state = FrameworkRuntimeState.FromActivityRequestResult(_state, result.ClearResult);
            }

            if (result.ReenterSucceeded)
            {
                _state = FrameworkRuntimeState.FromActivityRequestResult(_state, result.ReenterResult);
            }

            RefreshObjectEntryRuntimeContextSnapshot($"FrameworkRuntimeHost:activity-restart:{NormalizeLifecycleSource(source)}");
            return result;
        }


        private static FrameworkLoadingDiagnostics CreateSkippedActivityLoadingDiagnostics(FrameworkActivityRequestResult result)
        {
            var activityFlowResult = result.ActivityFlowResult;
            bool hasActivitySceneSideEffects = activityFlowResult.ActivitySceneCompositionResult.SideEffectsExecuted
                || activityFlowResult.ActivitySceneReleaseResult.SideEffectsExecuted;

            return hasActivitySceneSideEffects
                ? FrameworkLoadingDiagnostics.SkippedByActivityPolicy()
                : FrameworkLoadingDiagnostics.SkippedNoSceneLoad();
        }

        internal async Task<CycleResetResult> RequestRouteCycleResetAsync(string source, string reason)
        {
            var result = await _gameFlowRuntime.RequestRouteCycleResetAsync(source, reason);
            LogCycleResetResult(result);
            return result;
        }

        internal async Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason)
        {
            var result = await _gameFlowRuntime.RequestActivityCycleResetAsync(source, reason);
            LogCycleResetResult(result);
            return result;
        }

        internal PauseResult RequestPause(PauseRequestKind kind, string source, string reason)
        {
            return RequestPause(CreatePauseRequest(kind, source, reason));
        }

        internal PauseResult RequestPause(PauseRequest request)
        {
            if (_pauseRuntime == null)
            {
                throw new InvalidOperationException("Pause runtime is not initialized.");
            }

            var result = _pauseRuntime.Request(request);
            var timeScaleResult = _pauseTimeScaleRuntime != null
                ? _pauseTimeScaleRuntime.Apply(result)
                : default;
            var pauseSurfaceResult = ApplyPauseSurfaceSnapshot(request.Source, request.Reason);
            LogPauseRequestResult(result, pauseSurfaceResult, timeScaleResult);
            return result;
        }

        internal GateEvaluationResult EvaluatePauseGateAdmission(
            GateScope scope,
            GateDomain domain,
            string subject,
            string source,
            string reason)
        {
            if (_pauseRuntime == null)
            {
                throw new InvalidOperationException("Pause runtime is not initialized.");
            }

            return _pauseRuntime.EvaluateGate(scope, domain, subject, source, reason);
        }

        internal GateEvaluationResult EvaluateTransitionGateAdmission(
            GateScope scope,
            GateDomain domain,
            string subject,
            string source,
            string reason)
        {
            if (_gameFlowRuntime == null)
            {
                throw new InvalidOperationException("Game Flow runtime is not initialized.");
            }

            return _gameFlowRuntime.EvaluateTransitionGateAdmission(scope, domain, subject, source, reason);
        }

        internal async Task<ObjectResetResult> RequestObjectResetAsync(ObjectResetRequest request)
        {
            if (_objectResetRuntime == null)
            {
                _objectResetRuntime = new ObjectResetRuntime();
            }

            var gateEvaluation = EvaluateObjectResetRequestAdmission(request);
            if (!gateEvaluation.IsAllowed)
            {
                string blockedMessage = GateRequestAdmission.FormatBlockedMessage(
                    "Object Reset Request",
                    gateEvaluation);
                var inFlightResult = ObjectResetResult.Rejected(
                    request,
                    ObjectResetResultStatus.RejectedInvalidRequest,
                    ObjectResetIssue.Error(
                        ObjectResetIssueKind.RequestAlreadyInFlight,
                        blockedMessage),
                    blockedMessage);
                LogObjectResetResult(inFlightResult);
                return inFlightResult;
            }

            _objectResetRequestInFlight = true;
            try
            {
                await Awaitable.NextFrameAsync();
                var snapshot = _objectEntryRuntimeContextSnapshot;
                var result = _objectResetRuntime.Execute(snapshot, request, CreateObjectResetParticipantSource());
                LogObjectResetResult(result);
                return result;
            }
            finally
            {
                _objectResetRequestInFlight = false;
            }
        }

        private IObjectResetParticipantSource CreateObjectResetParticipantSource()
        {
            var sources = new List<IObjectResetParticipantSource>(_objectResetParticipantSources.Count + 1);
            for (int i = 0; i < _objectResetParticipantSources.Count; i++)
            {
                var source = _objectResetParticipantSources[i];
                if (source != null)
                {
                    sources.Add(source);
                }
            }

            if (_runtimeObjectParticipationRegistry != null)
            {
                sources.Add(_runtimeObjectParticipationRegistry);
            }

            return sources.Count == 0
                ? null
                : new CompositeObjectResetParticipantSource(sources);
        }

        private void EnsureRuntimeObjectParticipationRegistry()
        {
            _runtimeObjectParticipationRegistry ??= new RuntimeObjectParticipationRegistry();
        }

        private ObjectEntryScopedCollectionContext CreateCurrentObjectEntryScopedCollectionContext()
        {
            return new ObjectEntryScopedCollectionContext(
                _runtimeSessionScopeResult.HasOwner
                    ? _runtimeSessionScopeResult.Owner.OwnerIdentity
                    : default,
                _state.CurrentRoute,
                _state.RouteState.RouteIdentity,
                _state.RouteSceneCompositionResult,
                _state.CurrentActivity,
                _state.ActivityState.ActivityIdentity);
        }

        private ObjectEntryDeclarationSourceResult CombineSceneAndRuntimeObjectEntries(
            ObjectEntryDeclarationSourceResult sceneResult,
            ObjectEntryScopedCollectionContext context)
        {
            if (_runtimeObjectParticipationRegistry == null || !_runtimeObjectParticipationRegistry.HasRegistrations)
            {
                return sceneResult;
            }

            var runtimeResult = _runtimeObjectParticipationRegistry.CollectScoped(context);
            var descriptors = new List<ObjectEntryDescriptor>(sceneResult.ObjectEntries.Count + runtimeResult.Descriptors.Count);
            descriptors.AddRange(sceneResult.ObjectEntries.Entries);
            descriptors.AddRange(runtimeResult.Descriptors);

            var issues = new List<ObjectEntryIssue>(sceneResult.Issues.Count + runtimeResult.Issues.Count);
            issues.AddRange(sceneResult.Issues);
            issues.AddRange(runtimeResult.Issues);

            ObjectEntrySet set;
            bool aggregateRejected = false;
            try
            {
                set = new ObjectEntrySet(descriptors);
            }
            catch (ArgumentException exception)
            {
                aggregateRejected = true;
                set = ObjectEntrySet.Empty();
                issues.Add(ObjectEntryIssue.Error(
                    ObjectEntryIssueKind.DuplicateIdentity,
                    $"Runtime Object Entry context rejected duplicate identity while merging scene and runtime entries. {exception.Message}"));
            }

            bool hasBlockingIssue = aggregateRejected;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].IsBlocking)
                {
                    hasBlockingIssue = true;
                    break;
                }
            }

            var status = hasBlockingIssue
                ? ObjectEntryResultStatus.Rejected
                : issues.Count == 0
                    ? ObjectEntryResultStatus.Accepted
                    : ObjectEntryResultStatus.AcceptedWithWarnings;

            int declarationCount = sceneResult.DeclarationCount + runtimeResult.CandidateDescriptorCount;
            int filteredCount = sceneResult.FilteredDeclarationCount + runtimeResult.FilteredDescriptorCount;
            int acceptedCount = status == ObjectEntryResultStatus.Rejected ? 0 : set.Count;
            int rejectedCount = Math.Max(0, declarationCount - acceptedCount - filteredCount);

            return new ObjectEntryDeclarationSourceResult(
                set,
                status,
                declarationCount,
                sceneResult.CandidateDescriptorCount + runtimeResult.CandidateDescriptorCount,
                acceptedCount,
                rejectedCount,
                filteredCount,
                issues);
        }

        private void RefreshObjectEntryRuntimeContextSnapshotIfReady(string source)
        {
            var context = CreateCurrentObjectEntryScopedCollectionContext();
            if (!context.TryValidate(out _))
            {
                return;
            }

            RefreshObjectEntryRuntimeContextSnapshot(source);
        }

        private GateEvaluationResult EvaluateObjectResetRequestAdmission(ObjectResetRequest request)
        {
            if (_gameFlowRuntime != null)
            {
                return _gameFlowRuntime.EvaluateExternalLifecycleRequestAdmission(
                    "ObjectResetRequest",
                    request.Source,
                    request.Reason,
                    _objectResetRequestInFlight);
            }

            return GateRequestAdmission.EvaluateLifecycleRequest(
                "ObjectResetRequest",
                request.Source,
                request.Reason,
                routeRequestInFlight: false,
                activityRequestInFlight: false,
                cycleResetRequestInFlight: false,
                objectResetRequestInFlight: _objectResetRequestInFlight);
        }

        private void Initialize(GameApplicationAsset application)
        {
            _gameApplication = application;
            _runtimeContentRuntime = new RuntimeContentRuntime();
            _runtimeObjectParticipationRegistry = new RuntimeObjectParticipationRegistry();
            _contentAnchorBindingRuntime = new RuntimeContentAnchorBinding();
            _pauseRuntime = new PauseRuntime();
            _pauseTimeScaleRuntime = new PauseTimeScaleRuntime();
            _logger = FrameworkLogger.Create<FrameworkRuntimeHost>();
            _runtimeSessionScopeResult = CreateSessionScopeRoot(application, "FrameworkRuntimeHost", "session-start");
            _state = FrameworkRuntimeState.Empty(application);
        }

        private void InvalidateObjectEntryRuntimeContextSnapshot(string reason)
        {
            _objectEntryRuntimeContextSnapshot = null;
            _objectEntryRuntimeContextInvalidationCount++;
            _lastObjectEntryRuntimeContextInvalidationReason = reason.NormalizeTextOrFallback("lifecycle-boundary");
        }

        private PauseRequest CreatePauseRequest(PauseRequestKind kind, string source, string reason)
        {
            if (!Enum.IsDefined(typeof(PauseRequestKind), kind) || kind == PauseRequestKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Pause request kind must be explicit.");
            }

            _pauseRequestSequence++;
            string requestId = $"framework.pause.{_pauseRequestSequence}.{kind.ToString().ToLowerInvariant()}";
            return new PauseRequest(
                PauseRequestId.From(requestId),
                kind,
                NormalizeLifecycleSource(source),
                string.IsNullOrWhiteSpace(reason) ? "pause.request" : reason.Trim());
        }

        private static string NormalizeLifecycleSource(string source)
        {
            return source.NormalizeTextOrFallback("Unknown");
        }

        private bool ShouldShowLoadingSurface(RouteAsset targetRoute)
        {
            return _loadingSurfaceRuntime is { HasVisibleSurface: true }
                && targetRoute != null;
        }

        private static LoadingSurfaceRequest CreateLoadingSurfaceRequest(
            RouteAsset targetRoute,
            string source,
            string reason,
            bool show,
            LoadingProgress progress,
            bool progressSupported)
        {
            string routeLabel = targetRoute != null && !string.IsNullOrWhiteSpace(targetRoute.RouteName)
                ? targetRoute.RouteName
                : "Loading";
            string sceneLabel = targetRoute != null && !string.IsNullOrWhiteSpace(targetRoute.PrimarySceneName)
                ? targetRoute.PrimarySceneName
                : targetRoute != null && !string.IsNullOrWhiteSpace(targetRoute.PrimaryScenePath)
                    ? targetRoute.PrimaryScenePath
                    : string.Empty;

            string detail = string.IsNullOrWhiteSpace(sceneLabel)
                ? routeLabel
                : $"{routeLabel} / {sceneLabel}";

            return show
                ? LoadingSurfaceRequest.Show(routeLabel, detail, source, reason, progress, progressSupported)
                : LoadingSurfaceRequest.Hide(routeLabel, detail, source, reason, progress, progressSupported);
        }

        private bool ShouldShowActivityLoadingSurface(
            ActivityAsset targetActivity,
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            if (_loadingSurfaceRuntime == null || !_loadingSurfaceRuntime.HasVisibleSurface)
            {
                return false;
            }

            if (_gameFlowRuntime == null || targetActivity == null)
            {
                return false;
            }

            if (ReferenceEquals(targetActivity, previousActivity))
            {
                return false;
            }

            var operationPreview = _gameFlowRuntime.PreviewActivityOperation(
                previousActivity == null ? ActivityOperationKind.Start : ActivityOperationKind.Switch,
                previousActivity,
                targetActivity,
                targetActivity.VisualTransitionMode,
                source,
                reason);

            return operationPreview is { IsValid: true, RequiresLoadingSurface: true };
        }

        private bool ShouldShowActivityClearLoadingSurface(ActivityAsset previousActivity, string source, string reason)
        {
            if (_loadingSurfaceRuntime == null || !_loadingSurfaceRuntime.HasVisibleSurface)
            {
                return false;
            }

            if (_gameFlowRuntime == null || previousActivity == null)
            {
                return false;
            }

            var operationPreview = _gameFlowRuntime.PreviewActivityOperation(
                ActivityOperationKind.Clear,
                previousActivity,
                null,
                previousActivity.VisualTransitionMode,
                source,
                reason);

            return operationPreview is { IsValid: true, RequiresLoadingSurface: true };
        }

        private static LoadingSurfaceRequest CreateActivityLoadingSurfaceRequest(
            ActivityAsset targetActivity,
            string source,
            string reason,
            bool show,
            LoadingProgress progress,
            bool progressSupported)
        {
            string activityLabel = targetActivity != null && !string.IsNullOrWhiteSpace(targetActivity.ActivityName)
                ? targetActivity.ActivityName
                : "Activity";
            var profile = targetActivity != null ? targetActivity.ActivityContentProfile : null;
            string detail = profile != null && !string.IsNullOrWhiteSpace(profile.ProfileId)
                ? $"{activityLabel} / {profile.ProfileId}"
                : activityLabel;

            return show
                ? LoadingSurfaceRequest.Show(activityLabel, detail, source, reason, progress, progressSupported)
                : LoadingSurfaceRequest.Hide(activityLabel, detail, source, reason, progress, progressSupported);
        }


        private IFrameworkLoadingProgressReporter CreateLoadingProgressReporter(
            LoadingSurfaceRequest baseRequest,
            bool showLoadingSurface)
        {
            if (!showLoadingSurface || _loadingSurfaceRuntime == null || !_loadingSurfaceRuntime.ProgressSupported)
            {
                return NoOpFrameworkLoadingProgressReporter.Instance;
            }

            return new LoadingSurfaceProgressReporter(_loadingSurfaceRuntime, baseRequest);
        }

        private static LoadingProgress ToSurfaceProgress(FrameworkLoadingProgress progress)
        {
            return progress is { Supported: true, IsDeterminate: true }
                ? LoadingProgress.FromNormalized(progress.Value01)
                : LoadingProgress.Zero;
        }

        private LoadingSurfaceRuntime CreateLoadingSurfaceRuntime(GlobalUiSceneRuntime globalUiSceneRuntime)
        {
            return LoadingSurfaceRuntime.Create(
                _logger,
                globalUiSceneRuntime != null ? globalUiSceneRuntime.LoadingAdapters : Array.Empty<ILoadingSurfaceAdapter>(),
                globalUiSceneRuntime != null ? globalUiSceneRuntime.Label : string.Empty);
        }

        private PauseSurfaceRuntime CreatePauseSurfaceRuntime(GlobalUiSceneRuntime globalUiSceneRuntime)
        {
            return PauseSurfaceRuntime.Create(
                _logger,
                globalUiSceneRuntime != null ? globalUiSceneRuntime.PauseAdapters : Array.Empty<IPauseSurfaceAdapter>(),
                globalUiSceneRuntime != null ? globalUiSceneRuntime.Label : string.Empty);
        }

        private PauseSurfaceApplicationResult ApplyPauseSurfaceSnapshot(string source, string reason)
        {
            if (_pauseRuntime == null)
            {
                throw new InvalidOperationException("Pause runtime is not initialized.");
            }

            if (_pauseSurfaceRuntime == null)
            {
                return PauseSurfaceApplicationResult.NoSurface(
                    _pauseRuntime.Snapshot,
                    "Pause Surface",
                    source,
                    reason);
            }

            return _pauseSurfaceRuntime.ApplySnapshot(
                _pauseRuntime.Snapshot,
                NormalizeLifecycleSource(source),
                string.IsNullOrWhiteSpace(reason) ? "pause.surface.apply" : reason.Trim());
        }

        private ITransitionOrchestrator CreateTransitionOrchestrator(GlobalUiSceneRuntime globalUiSceneRuntime)
        {
            if (globalUiSceneRuntime == null)
            {
                _logger.Warning("Transition surface is not configured because the UIGlobal scene runtime is missing.");
                return NoOpTransitionOrchestrator.Instance;
            }

            IReadOnlyList<ITransitionEffectAdapter> sceneAdapters = globalUiSceneRuntime != null
                ? globalUiSceneRuntime.TransitionAdapters
                : Array.Empty<ITransitionEffectAdapter>();
            bool hasSceneAdapters = sceneAdapters is { Count: > 0 };
            string sceneLabel = globalUiSceneRuntime != null && !string.IsNullOrWhiteSpace(globalUiSceneRuntime.Label)
                ? globalUiSceneRuntime.Label
                : "UIGlobal Transition Surface";

            if (!hasSceneAdapters)
            {
                _logger.Info("Transition surface is not configured. Transition will remain explicit NoOp.");
                return NoOpTransitionOrchestrator.Instance;
            }

            _logger.Info($"Transition surface resolved from UIGlobal scene '{sceneLabel}' with adapterCount='{sceneAdapters.Count}'.");
            return new TransitionEffectOrchestrator(sceneAdapters, sceneLabel);
        }


        internal ContentAnchorBindingResult BindContentAnchor(
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest request,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            if (_runtimeContentRuntime == null)
            {
                throw new InvalidOperationException("Runtime content runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.Bind(
                anchorSet,
                _runtimeContentRuntime,
                request,
                source,
                reason);
        }

        internal bool UnbindContentAnchor(ContentAnchorBindingRequest request)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.Unbind(request);
        }

        internal bool UnbindContentAnchor(ContentAnchorContentHandle handle)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.Unbind(handle);
        }

        internal ContentAnchorBindingLifecycleResult UnbindContentAnchorRuntimeContent(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeContent(identity, source, reason);
        }

        internal ContentAnchorBindingLifecycleResult UnbindContentAnchorRuntimeOwner(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeOwner(owner, source, reason);
        }

        internal ContentAnchorBindingLifecycleResult UnbindContentAnchorRuntimeScope(
            RuntimeContentScope scope,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeScope(scope, source, reason);
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindings()
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindings();
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindingsForRuntimeContent(RuntimeContentIdentity identity)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindingsForRuntimeContent(identity);
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindingsForRuntimeOwner(RuntimeContentOwner owner)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindingsForRuntimeOwner(owner);
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindingsForRuntimeScope(RuntimeContentScope scope)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindingsForRuntimeScope(scope);
        }


        private RuntimeScopeLifecycleResult CreateSessionScopeRoot(GameApplicationAsset application, string source, string reason)
        {
            var owner = RuntimeContentOwner.Session(application.ApplicationName, application.ApplicationName);
            var enterResult = _runtimeContentRuntime.CreateScopeRoot(owner, source, reason);
            _runtimeContentRuntime.TryCreateScopeContext(owner, source, reason, out var context);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Session,
                owner,
                enterResult,
                null,
                context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private void LogRouteRequestResult(FrameworkRouteRequestResult result, FrameworkLoadingDiagnostics loadingDiagnostics)
        {
            if (result.Succeeded)
            {
                _logger.Info("Route Request completed.", BuildRouteRequestFields(result, loadingDiagnostics));
                _logger.Debug("Route Request diagnostics. " + result.Message);
                LogActivityContentObservability(result.RouteLifecycleResult.ActivityFlowResult.ActivityContentResult);
                return;
            }

            if (result.Kind is FrameworkRouteRequestKind.IgnoredAlreadyActive or FrameworkRouteRequestKind.IgnoredAlreadyInFlight)
            {
                _logger.Warning(result.Message, BuildRouteRequestFields(result, loadingDiagnostics));
                return;
            }

            _logger.Error(result.Message, BuildRouteRequestFields(result, loadingDiagnostics));
        }

        private void LogActivityRequestResult(FrameworkActivityRequestResult result, FrameworkLoadingDiagnostics loadingDiagnostics)
        {
            if (result.Succeeded)
            {
                _logger.Info("Activity Request completed.", BuildActivityRequestFields(result, loadingDiagnostics));
                _logger.Debug("Activity Request diagnostics. " + result.Message);
                LogActivityContentObservability(result.ActivityFlowResult.ActivityContentResult);
                return;
            }

            if (result.Kind is FrameworkActivityRequestKind.IgnoredAlreadyActive or FrameworkActivityRequestKind.IgnoredAlreadyInFlight or FrameworkActivityRequestKind.IgnoredNoActiveActivity)
            {
                _logger.Warning(result.Message, BuildActivityRequestFields(result, loadingDiagnostics));
                return;
            }

            _logger.Error(result.Message, BuildActivityRequestFields(result, loadingDiagnostics));
        }

        private void LogPauseRequestResult(
            PauseResult result,
            PauseSurfaceApplicationResult pauseSurfaceResult,
            PauseTimeScaleApplicationResult timeScaleResult)
        {
            if (result.Applied || result.IgnoredNoChange)
            {
                _logger.Info("Pause Request completed.", BuildPauseRequestFields(result, pauseSurfaceResult, timeScaleResult));
                _logger.Debug("Pause Request diagnostics. " + result.ToDiagnosticString());
                return;
            }

            if (result.Rejected)
            {
                _logger.Warning("Pause Request rejected.", BuildPauseRequestFields(result, pauseSurfaceResult, timeScaleResult));
                _logger.Debug("Pause Request diagnostics. " + result.ToDiagnosticString());
                return;
            }

            _logger.Error("Pause Request failed. " + result.ToDiagnosticString(), BuildPauseRequestFields(result, pauseSurfaceResult, timeScaleResult));
        }

        private void LogCycleResetResult(CycleResetResult result)
        {
            if (result.Succeeded || result.CompletedWithWarnings)
            {
                _logger.Info("Cycle Reset Request completed.", BuildCycleResetFields(result));
                _logger.Debug("Cycle Reset Request diagnostics. " + result.ToDiagnosticString());
                return;
            }

            if (result.Status == CycleResetStatus.SucceededNoParticipants)
            {
                _logger.Info("Cycle Reset Request completed with no participants.", BuildCycleResetFields(result));
                return;
            }

            _logger.Error("Cycle Reset Request failed. " + result.ToDiagnosticString());
        }

        private void LogObjectResetResult(ObjectResetResult result)
        {
            if (result.Succeeded || result.CompletedWithWarnings)
            {
                _logger.Info("Object Reset Request completed.", BuildObjectResetFields(result));
                _logger.Debug("Object Reset Request diagnostics. " + result.ToDiagnosticString());
                return;
            }

            _logger.Error("Object Reset Request failed. " + result.ToDiagnosticString());
        }

        private LogField[] BuildObjectResetFields(ObjectResetResult result)
        {
            return LogFields.Of(
                LogFields.Field("source", result.Request.Source),
                LogFields.Field("reason", result.Request.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("objectEntry", result.Request.Target.ObjectEntryId.IsValid ? result.Request.Target.ObjectEntryId.StableText : "<invalid>"),
                LogFields.Field("scope", result.Request.Target.Scope.ToString()),
                LogFields.Field("owner", result.Request.Target.OwnerIdentity.IsValid ? result.Request.Target.OwnerIdentity.StableText : "<invalid>"),
                LogFields.Field("participants", result.ParticipantCount),
                LogFields.Field("participantSucceeded", result.ParticipantSucceededCount),
                LogFields.Field("participantSkipped", result.ParticipantSkippedCount),
                LogFields.Field("participantFailed", result.ParticipantFailedCount),
                LogFields.Field("participantBlockingFailures", result.ParticipantBlockingFailureCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount));
        }

        private LogField[] BuildPauseRequestFields(
            PauseResult result,
            PauseSurfaceApplicationResult pauseSurfaceResult,
            PauseTimeScaleApplicationResult timeScaleResult)
        {
            var gateSnapshot = PauseGateSnapshot;
            return LogFields.Of(
                LogFields.Field("request", result.RequestId.StableText),
                LogFields.Field("kind", result.Kind.ToString()),
                LogFields.Field("source", result.Request.Source),
                LogFields.Field("reason", result.Request.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("previousState", result.PreviousState.ToString()),
                LogFields.Field("currentState", result.CurrentState.ToString()),
                LogFields.Field("completed", result.Completed),
                LogFields.Field("applied", result.Applied),
                LogFields.Field("ignoredNoChange", result.IgnoredNoChange),
                LogFields.Field("rejected", result.Rejected),
                LogFields.Field("failed", result.Failed),
                LogFields.Field("stateChanged", result.StateChanged),
                LogFields.Field("gateBlockers", gateSnapshot.BlockerCount),
                LogFields.Field("blocksInputAcceptance", gateSnapshot.IsBlocked(GateScope.Input, GateDomain.InputAcceptance)),
                LogFields.Field("blocksInteractionAcceptance", gateSnapshot.IsBlocked(GateScope.Interaction, GateDomain.InteractionAcceptance)),
                LogFields.Field("blocksPauseRequest", gateSnapshot.IsBlocked(GateScope.Pause, GateDomain.PauseRequest)),
                LogFields.Field("issues", result.IssueCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("timeScale", timeScaleResult.IsValid ? timeScaleResult.StatusText : "Unknown"),
                LogFields.Field("timeScaleApplied", timeScaleResult.IsValid && timeScaleResult.Applied),
                LogFields.Field("timeScaleRestored", timeScaleResult.IsValid && timeScaleResult.Restored),
                LogFields.Field("timeScaleChanged", timeScaleResult.IsValid && timeScaleResult.Changed),
                LogFields.Field("previousTimeScale", timeScaleResult.IsValid ? timeScaleResult.PreviousTimeScale.ToString("0.###") : "<unknown>"),
                LogFields.Field("targetTimeScale", timeScaleResult.IsValid ? timeScaleResult.TargetTimeScale.ToString("0.###") : "<unknown>"),
                LogFields.Field("currentTimeScale", timeScaleResult.IsValid ? timeScaleResult.CurrentTimeScale.ToString("0.###") : "<unknown>"),
                LogFields.Field("capturedRunningTimeScale", timeScaleResult.IsValid ? timeScaleResult.CapturedRunningTimeScale.ToString("0.###") : "<unknown>"),
                LogFields.Field("pauseSurface", pauseSurfaceResult.StatusText),
                LogFields.Field("pauseSurfaceVisual", pauseSurfaceResult.VisualText),
                LogFields.Field("pauseSurfaceAdapterCount", pauseSurfaceResult.AdapterCount),
                LogFields.Field("pauseSurfaceSupportedAdapters", pauseSurfaceResult.SupportedAdapterCount),
                LogFields.Field("pauseSurfaceAppliedAdapters", pauseSurfaceResult.AppliedAdapterCount),
                LogFields.Field("pauseSurfaceFailedAdapters", pauseSurfaceResult.FailedAdapterCount),
                LogFields.Field("pauseSurfaceIssues", pauseSurfaceResult.IssueCount),
                LogFields.Field("pauseSurfaceState", pauseSurfaceResult.Snapshot.State.ToString()),
                LogFields.Field("pauseSurfacePaused", pauseSurfaceResult.Snapshot.IsPaused));
        }

        private LogField[] BuildCycleResetFields(CycleResetResult result)
        {
            return LogFields.Of(
                LogFields.Field("scope", result.Request.Scope.ToString()),
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("route", GetRouteName(result.Request.ActiveRoute)),
                LogFields.Field("activity", GetActivityName(result.Request.ActiveActivity)),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("participants", result.ParticipantCount),
                LogFields.Field("participantSucceeded", result.SucceededCount),
                LogFields.Field("participantSkipped", result.SkippedCount),
                LogFields.Field("participantFailed", result.FailedCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount));
        }

        private LogField[] BuildRouteRequestFields(
            FrameworkRouteRequestResult result,
            FrameworkLoadingDiagnostics loadingDiagnostics)
        {
            RouteLifecycleStartResult routeLifecycle = result.RouteLifecycleResult;
            ActivityFlowStartResult activityFlow = routeLifecycle.ActivityFlowResult;
            ActivityContentApplyResult activityContent = activityFlow.ActivityContentResult;
            FrameworkLifecycleOperationEvidence lifecycleOperation = BuildRouteLifecycleOperationEvidence(result, loadingDiagnostics);
            FrameworkLifecycleContentEvidence lifecycleContent = BuildRouteLifecycleContentEvidence(routeLifecycle, result.Source, result.Reason);
            FrameworkLifecycleReadinessEvidence lifecycleReadiness = BuildActivityLifecycleReadinessEvidence(activityFlow, result.Source, result.Reason);
            GameFlowRequestEnvelope envelope = GameFlowRequestEnvelopeBuilder.BuildRoute(
                result,
                GetRouteName(routeLifecycle.PreviousRoute),
                GetRouteName(result.TargetRoute),
                result.TransitionDiagnostics.TransitionText,
                loadingDiagnostics.LoadingText,
                lifecycleOperation,
                lifecycleContent,
                lifecycleReadiness,
                loadingDiagnostics.AdapterEvidenceCount,
                loadingDiagnostics.AppliedAdapterEvidenceCount,
                loadingDiagnostics.SkippedAdapterEvidenceCount,
                loadingDiagnostics.FailedAdapterEvidenceCount,
                loadingDiagnostics.AdapterEvidenceBlockingIssueCount);

            return LogFields.Of(
                LogFields.Field("kind", result.Kind),
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("gameFlowEnvelopeKind", envelope.OperationKindText),
                LogFields.Field("gameFlowEnvelopeAdmission", envelope.AdmissionText),
                LogFields.Field("gameFlowEnvelopeSource", envelope.Source),
                LogFields.Field("gameFlowEnvelopeReason", envelope.Reason),
                LogFields.Field("gameFlowEnvelopeTargetRoute", envelope.TargetRoute),
                LogFields.Field("gameFlowEnvelopePreviousRoute", envelope.PreviousRoute),
                LogFields.Field("gameFlowEnvelopeTargetActivity", envelope.TargetActivity),
                LogFields.Field("gameFlowEnvelopePreviousActivity", envelope.PreviousActivity),
                LogFields.Field("gameFlowEnvelopeTransitionStatus", envelope.TransitionStatus),
                LogFields.Field("gameFlowEnvelopeLoadingStatus", envelope.LoadingStatus),
                LogFields.Field("gameFlowEnvelopeValidationMode", envelope.ValidationMode),
                LogFields.Field("gameFlowEnvelopeDomainStatus", envelope.DomainStatus),
                LogFields.Field("gameFlowEnvelopeLifecycleOperationKind", envelope.LifecycleOperationKind),
                LogFields.Field("gameFlowEnvelopeLifecycleStages", envelope.LifecycleStageCount),
                LogFields.Field("gameFlowEnvelopeLifecycleBlockingIssues", envelope.LifecycleBlockingIssueCount),
                LogFields.Field("gameFlowEnvelopeLifecycleFailedStages", envelope.LifecycleFailedStageCount),
                LogFields.Field("gameFlowEnvelopeLifecycleSkippedStages", envelope.LifecycleSkippedStageCount),
                LogFields.Field("gameFlowEnvelopeContentStatus", envelope.LifecycleContentStatus),
                LogFields.Field("gameFlowEnvelopeContentBlockingIssues", envelope.LifecycleContentBlockingIssueCount),
                LogFields.Field("gameFlowEnvelopeContentHandles", envelope.LifecycleContentHandleCount),
                LogFields.Field("gameFlowEnvelopeReadiness", envelope.LifecycleReadiness),
                LogFields.Field("gameFlowEnvelopeReadinessReason", envelope.LifecycleReadinessReason),
                LogFields.Field("gameFlowEnvelopeReadinessIssues", envelope.LifecycleReadinessIssueCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceCount", envelope.LoadingAdapterEvidenceCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceApplied", envelope.LoadingAdapterEvidenceAppliedCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceSkipped", envelope.LoadingAdapterEvidenceSkippedCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceFailed", envelope.LoadingAdapterEvidenceFailedCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterBlockingIssues", envelope.LoadingAdapterBlockingIssueCount),
                LogFields.Field("gameFlowEnvelopeDiagnostics", envelope.DiagnosticText),
                LogFields.Field("lifecycleOperationKind", lifecycleOperation.OperationKindText),
                LogFields.Field("lifecycleOperationStages", lifecycleOperation.StageCount),
                LogFields.Field("lifecycleOperationBlockingIssues", lifecycleOperation.BlockingIssueCount),
                LogFields.Field("lifecycleOperationIssues", lifecycleOperation.IssueCount),
                LogFields.Field("lifecycleOperationSideEffects", lifecycleOperation.SideEffectCount),
                LogFields.Field("lifecycleOperationFailedStages", lifecycleOperation.FailedStageCount),
                LogFields.Field("lifecycleOperationSkippedStages", lifecycleOperation.SkippedStageCount),
                LogFields.Field("lifecycleOperationStageNames", lifecycleOperation.StageNamesText),
                LogFields.Field("lifecycleOperationStageStatuses", lifecycleOperation.StageStatusesText),
                LogFields.Field("lifecycleOperationDiagnostics", lifecycleOperation.DiagnosticText),
                LogFields.Field("lifecycleContentStatus", lifecycleContent.Status),
                LogFields.Field("lifecycleContentEnter", lifecycleContent.EnterStatus),
                LogFields.Field("lifecycleContentExit", lifecycleContent.ExitStatus),
                LogFields.Field("lifecycleContentEnterRequests", lifecycleContent.EnterRequestCount),
                LogFields.Field("lifecycleContentExitRequests", lifecycleContent.ExitRequestCount),
                LogFields.Field("lifecycleContentParticipants", lifecycleContent.ParticipantCount),
                LogFields.Field("lifecycleContentParticipantSource", lifecycleContent.ParticipantSourceStatus),
                LogFields.Field("lifecycleContentBlockingIssues", lifecycleContent.BlockingIssueCount),
                LogFields.Field("lifecycleContentBlocksReadiness", lifecycleContent.BlocksReadiness),
                LogFields.Field("lifecycleContentHandles", lifecycleContent.ContentHandleCount),
                LogFields.Field("lifecycleContentDiagnostics", lifecycleContent.DiagnosticText),
                LogFields.Field("lifecycleReadiness", lifecycleReadiness.Status),
                LogFields.Field("lifecycleReadinessReason", lifecycleReadiness.Reason),
                LogFields.Field("lifecycleReadinessIssues", lifecycleReadiness.IssueCount),
                LogFields.Field("lifecycleReadinessBlockedByContent", lifecycleReadiness.BlockedByContent),
                LogFields.Field("transition", result.TransitionDiagnostics.TransitionText),
                LogFields.Field("transitionScope", result.TransitionDiagnostics.ScopeText),
                LogFields.Field("transitionBefore", result.TransitionDiagnostics.BeforeText),
                LogFields.Field("transitionAfter", result.TransitionDiagnostics.AfterText),
                LogFields.Field("transitionBlockingIssues", result.TransitionDiagnostics.BlockingIssueCount),
                LogFields.Field("transitionVisual", result.TransitionDiagnostics.VisualText),
                LogFields.Field("transitionEffect", result.TransitionDiagnostics.EffectText),
                LogFields.Field("transitionEffectBefore", result.TransitionDiagnostics.EffectBeforeText),
                LogFields.Field("transitionEffectAfter", result.TransitionDiagnostics.EffectAfterText),
                LogFields.Field("transitionEffectBlockingIssues", result.TransitionDiagnostics.EffectBlockingIssueCount),
                LogFields.Field("transitionEffectAdapterCount", result.TransitionDiagnostics.EffectAdapterCount),
                LogFields.Field("transitionEffectAdapterEvidenceCount", result.TransitionDiagnostics.EffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceApplied", result.TransitionDiagnostics.AppliedEffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceSkipped", result.TransitionDiagnostics.SkippedEffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceFailed", result.TransitionDiagnostics.FailedEffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceBlockingIssues", result.TransitionDiagnostics.EffectAdapterEvidenceBlockingIssueCount),
                LogFields.Field("transitionEffectAdapterEvidenceNames", result.TransitionDiagnostics.EffectAdapterEvidenceNamesText),
                LogFields.Field("transitionEffectAdapterEvidenceStatuses", result.TransitionDiagnostics.EffectAdapterEvidenceStatusesText),
                LogFields.Field("transitionGate", result.TransitionGateDiagnostics.GateText),
                LogFields.Field("transitionGateMode", result.TransitionGateDiagnostics.ModeText),
                LogFields.Field("transitionGateApplied", result.TransitionGateDiagnostics.Applied),
                LogFields.Field("transitionGateReleased", result.TransitionGateDiagnostics.Released),
                LogFields.Field("transitionGateBlockers", result.TransitionGateDiagnostics.BlockersText),
                LogFields.Field("blocksInputAcceptance", result.TransitionGateDiagnostics.BlocksInputAcceptance),
                LogFields.Field("blocksInteractionAcceptance", result.TransitionGateDiagnostics.BlocksInteractionAcceptance),
                LogFields.Field("blocksGameplayAction", result.TransitionGateDiagnostics.BlocksGameplayAction),
                LogFields.Field("transitionGateBlockingIssues", result.TransitionGateDiagnostics.BlockingIssuesText),
                LogFields.Field("previousRoute", GetRouteName(routeLifecycle.PreviousRoute)),
                LogFields.Field("targetRoute", GetRouteName(result.TargetRoute)),
                LogFields.Field("scene", routeLifecycle.SceneLifecycleResult.SceneName),
                LogFields.Field("alreadyLoaded", routeLifecycle.SceneLifecycleResult.AlreadyLoaded),
                LogFields.Field("loadMode", routeLifecycle.SceneLifecycleResult.LoadMode),
                LogFields.Field("routeSceneComposition", routeLifecycle.RouteSceneCompositionResult.Status),
                LogFields.Field("routeSceneLoaded", routeLifecycle.RouteSceneCompositionResult.LoadedCount),
                LogFields.Field("routeSceneFailed", routeLifecycle.RouteSceneCompositionResult.FailedCount),
                LogFields.Field("routeSceneBlockingIssues", routeLifecycle.RouteSceneCompositionResult.BlockingIssueCount),
                LogFields.Field("routeRelease", routeLifecycle.ContentReleaseResult.Status),
                LogFields.Field("routeReleaseReleased", routeLifecycle.ContentReleaseResult.ReleasedCount),
                LogFields.Field("routeReleaseSkipped", routeLifecycle.ContentReleaseResult.SkippedCount),
                LogFields.Field("routeReleaseFailed", routeLifecycle.ContentReleaseResult.FailedCount),
                LogFields.Field("routeReleaseBlockingIssues", routeLifecycle.ContentReleaseResult.BlockingIssueCount),
                LogFields.Field("routeActivitySceneRelease", routeLifecycle.ActivitySceneRouteReleaseResult.DiagnosticStatus),
                LogFields.Field("routeActivitySceneReleaseScenes", routeLifecycle.ActivitySceneRouteReleaseResult.SceneCount),
                LogFields.Field("routeActivitySceneReleaseReleased", routeLifecycle.ActivitySceneRouteReleaseResult.ReleasedSceneCount),
                LogFields.Field("routeActivitySceneReleaseSkipped", routeLifecycle.ActivitySceneRouteReleaseResult.SkippedSceneCount),
                LogFields.Field("routeActivitySceneReleaseFailed", routeLifecycle.ActivitySceneRouteReleaseResult.FailedSceneCount),
                LogFields.Field("routeActivitySceneReleaseSideEffects", routeLifecycle.ActivitySceneRouteReleaseResult.SideEffectsExecuted),
                LogFields.Field("routeActivitySceneReleaseBlockingIssues", routeLifecycle.ActivitySceneRouteReleaseResult.BlockingIssueCount),
                LogFields.Field("routeExit", routeLifecycle.RouteExitResult.DiagnosticStatus),
                LogFields.Field("runtimeRouteScope", routeLifecycle.RuntimeRouteScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeRouteRootEnter", routeLifecycle.RuntimeRouteScopeResult.EnterStatus),
                LogFields.Field("runtimeRouteRootExit", routeLifecycle.RuntimeRouteScopeResult.ExitStatus),
                LogFields.Field("runtimeRouteContext", routeLifecycle.RuntimeRouteScopeResult.ContextStatus),
                LogFields.Field("runtimeRootCount", routeLifecycle.RuntimeRouteScopeResult.RootCount),
                LogFields.Field("routeContentHandles", routeLifecycle.RouteContentSet.Count),
                LogFields.Field("contentAnchors", routeLifecycle.ContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("contentAnchorCandidates", routeLifecycle.ContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("contentAnchorIssues", routeLifecycle.ContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("contentAnchorInvalid", routeLifecycle.ContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("contentAnchorRouteMismatch", routeLifecycle.ContentAnchorDiscoveryResult.SkippedRouteMismatchCount),
                LogFields.Field("contentAnchorBindings", ContentAnchorBindingCount),
                LogFields.Field("routeContentAnchorBindingCleanup", routeLifecycle.RouteContentAnchorBindingCleanupResult.DiagnosticStatus),
                LogFields.Field("routeContentAnchorBindingCleanupRemoved", routeLifecycle.RouteContentAnchorBindingCleanupResult.RemovedCount),
                LogFields.Field("activityContentAnchorBindingCleanup", activityFlow.ActivityContentAnchorBindingCleanupResult.DiagnosticStatus),
                LogFields.Field("activityContentAnchorBindingCleanupRemoved", activityFlow.ActivityContentAnchorBindingCleanupResult.RemovedCount),
                LogFields.Field("activityContentExecution", activityFlow.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentExecutionParticipantSource", activityFlow.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentExecutionParticipantSourceIssues", activityFlow.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentExecutionParticipants", activityFlow.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentExecutionEnter", activityFlow.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentExecutionEnterRequests", activityFlow.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentExecutionExit", activityFlow.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentExecutionExitRequests", activityFlow.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentExecutionBlockingIssues", activityFlow.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentExecutionBlocksReadiness", activityFlow.ActivityContentExecutionResult.BlocksReadiness),
                LogFields.Field("activityContentParticipantExecution", activityFlow.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentParticipantSource", activityFlow.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentParticipantSourceIssues", activityFlow.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentParticipantCount", activityFlow.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentParticipantEnter", activityFlow.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentParticipantEnterRequests", activityFlow.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentParticipantExit", activityFlow.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentParticipantExitRequests", activityFlow.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentParticipantBlockingIssues", activityFlow.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentParticipantBlocksReadiness", activityFlow.ActivityContentExecutionResult.BlocksReadiness),
                LogFields.Field("activityContentAnchors", activityFlow.ActivityContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("activityContentAnchorCandidates", activityFlow.ActivityContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("activityContentDiscoverySceneRoots", activityFlow.ActivityContentAnchorDiscoveryResult.DiscoverySceneRootCount),
                LogFields.Field("activityContentAnchorIssues", activityFlow.ActivityContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("activityContentAnchorInvalid", activityFlow.ActivityContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("activityContentAnchorActivityMismatch", activityFlow.ActivityContentAnchorDiscoveryResult.SkippedActivityMismatchCount),
                LogFields.Field("routeContentEnterReceivers", routeLifecycle.RouteContentEnterResult.ReceiverCount),
                LogFields.Field("routeContentExitReceivers", routeLifecycle.RouteContentExitResult.ReceiverCount),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlow.ActivityState.ActivityName)),
                LogFields.Field("activityState", activityFlow.ActivityState.DiagnosticStatus),
                LogFields.Field("activityReadiness", activityFlow.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("runtimeActivityScope", activityFlow.RuntimeActivityScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeActivityRootEnter", activityFlow.RuntimeActivityScopeResult.EnterStatus),
                LogFields.Field("runtimeActivityRootExit", activityFlow.RuntimeActivityScopeResult.ExitStatus),
                LogFields.Field("runtimeActivityContext", activityFlow.RuntimeActivityScopeResult.ContextStatus),
                LogFields.Field("activityContentHandles", activityContent.ActivityContentCount),
                LogFields.Field("routeStartupActivityOperation", activityFlow.ActivityOperationResult.DiagnosticStatus),
                LogFields.Field("routeStartupActivityOperationKind", activityFlow.ActivityOperationResult.OperationKind),
                LogFields.Field("routeStartupActivityOperationVisualMode", activityFlow.ActivityOperationResult.VisualMode),
                LogFields.Field("routeStartupActivityOperationScenes", activityFlow.ActivityOperationResult.SceneCount),
                LogFields.Field("routeStartupActivityOperationLoad", activityFlow.ActivityOperationResult.ScenesToLoadCount),
                LogFields.Field("routeStartupActivityOperationRelease", activityFlow.ActivityOperationResult.ScenesToReleaseCount),
                LogFields.Field("routeStartupActivityOperationSideEffects", activityFlow.ActivityOperationResult.SceneSideEffectCount),
                LogFields.Field("routeStartupActivityOperationRequiresLoadingSurface", activityFlow.ActivityOperationResult.RequiresLoadingSurface),
                LogFields.Field("routeStartupActivityOperationBlockingIssues", activityFlow.ActivityOperationResult.BlockingIssueCount),
                LogFields.Field("activitySceneComposition", activityFlow.ActivitySceneCompositionResult.DiagnosticStatus),
                LogFields.Field("activitySceneCompositionProfile", activityFlow.ActivitySceneCompositionResult.ProfileId),
                LogFields.Field("activitySceneCompositionScenes", activityFlow.ActivitySceneCompositionResult.SceneCount),
                LogFields.Field("activitySceneCompositionRequired", activityFlow.ActivitySceneCompositionResult.RequiredSceneCount),
                LogFields.Field("activitySceneCompositionOptional", activityFlow.ActivitySceneCompositionResult.OptionalSceneCount),
                LogFields.Field("activitySceneCompositionExecutionReady", activityFlow.ActivitySceneCompositionResult.ExecutionReadySceneCount),
                LogFields.Field("activitySceneCompositionLoaded", activityFlow.ActivitySceneCompositionResult.LoadedSceneCount),
                LogFields.Field("activitySceneCompositionAlreadyLoaded", activityFlow.ActivitySceneCompositionResult.AlreadyLoadedSceneCount),
                LogFields.Field("activitySceneCompositionFailed", activityFlow.ActivitySceneCompositionResult.FailedSceneCount),
                LogFields.Field("activitySceneCompositionSkipped", activityFlow.ActivitySceneCompositionResult.SkippedSceneCount),
                LogFields.Field("activitySceneCompositionSideEffects", activityFlow.ActivitySceneCompositionResult.SideEffectsExecuted),
                LogFields.Field("activitySceneCompositionBlockingIssues", activityFlow.ActivitySceneCompositionResult.BlockingIssueCount),
                LogFields.Field("activitySceneRelease", activityFlow.ActivitySceneReleaseResult.DiagnosticStatus),
                LogFields.Field("activitySceneReleaseScenes", activityFlow.ActivitySceneReleaseResult.SceneCount),
                LogFields.Field("activitySceneReleaseReleased", activityFlow.ActivitySceneReleaseResult.ReleasedSceneCount),
                LogFields.Field("activitySceneReleaseFailed", activityFlow.ActivitySceneReleaseResult.FailedSceneCount),
                LogFields.Field("activitySceneReleaseSkipped", activityFlow.ActivitySceneReleaseResult.SkippedSceneCount),
                LogFields.Field("activitySceneReleaseSideEffects", activityFlow.ActivitySceneReleaseResult.SideEffectsExecuted),
                LogFields.Field("activitySceneReleaseBlockingIssues", activityFlow.ActivitySceneReleaseResult.BlockingIssueCount),
                LogFields.Field("activitySceneLedger", activityFlow.ActivitySceneLedgerSnapshot.DiagnosticStatus),
                LogFields.Field("activitySceneLedgerEntries", activityFlow.ActivitySceneLedgerSnapshot.EntryCount),
                LogFields.Field("activitySceneLedgerLoaded", activityFlow.ActivitySceneLedgerSnapshot.LoadedCount),
                LogFields.Field("activitySceneLedgerReleased", activityFlow.ActivitySceneLedgerSnapshot.ReleasedCount),
                LogFields.Field("activitySceneLedgerStale", activityFlow.ActivitySceneLedgerSnapshot.StaleCount),
                LogFields.Field("loading", loadingDiagnostics.LoadingText),
                LogFields.Field("loadingVisual", loadingDiagnostics.VisualText),
                LogFields.Field("loadingBefore", loadingDiagnostics.BeforeText),
                LogFields.Field("loadingAfter", loadingDiagnostics.AfterText),
                LogFields.Field("loadingBlockingIssues", loadingDiagnostics.BlockingIssueCount),
                LogFields.Field("loadingAdapterCount", loadingDiagnostics.AdapterCount),
                LogFields.Field("loadingAdapterEvidenceCount", loadingDiagnostics.AdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceApplied", loadingDiagnostics.AppliedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceSkipped", loadingDiagnostics.SkippedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceFailed", loadingDiagnostics.FailedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceIssues", loadingDiagnostics.AdapterEvidenceIssueCount),
                LogFields.Field("loadingAdapterEvidenceBlockingIssues", loadingDiagnostics.AdapterEvidenceBlockingIssueCount),
                LogFields.Field("loadingAdapterEvidenceNames", loadingDiagnostics.AdapterEvidenceNamesText),
                LogFields.Field("loadingAdapterEvidenceStatuses", loadingDiagnostics.AdapterEvidenceStatusesText),
                LogFields.Field("loadingProgressSupported", loadingDiagnostics.ProgressSupported),
                LogFields.Field("loadingProgressMode", loadingDiagnostics.ProgressModeText),
                LogFields.Field("loadingProgressValue", loadingDiagnostics.ProgressValueText),
                LogFields.Field("loadingProgressPercent", loadingDiagnostics.ProgressPercentText),
                LogFields.Field("loadingProgressPhase", loadingDiagnostics.ProgressPhaseText),
                LogFields.Field("loadingProgressMessage", loadingDiagnostics.ProgressMessageText),
                LogFields.Field("loadingProgress", loadingDiagnostics.ProgressText));
        }

        private LogField[] BuildActivityRequestFields(
            FrameworkActivityRequestResult result,
            FrameworkLoadingDiagnostics loadingDiagnostics)
        {
            ActivityFlowStartResult activityFlow = result.ActivityFlowResult;
            ActivityContentApplyResult activityContent = activityFlow.ActivityContentResult;
            ActivityContentLifecycleResult lifecycle = activityContent.LifecycleResult;
            FrameworkLifecycleOperationEvidence lifecycleOperation = BuildActivityLifecycleOperationEvidence(result, loadingDiagnostics);
            FrameworkLifecycleContentEvidence lifecycleContent = BuildActivityLifecycleContentEvidence(activityFlow, result.Source, result.Reason);
            FrameworkLifecycleReadinessEvidence lifecycleReadiness = BuildActivityLifecycleReadinessEvidence(activityFlow, result.Source, result.Reason);
            GameFlowRequestEnvelope envelope = GameFlowRequestEnvelopeBuilder.BuildActivity(
                result,
                GetActivityName(activityFlow.PreviousActivity),
                GetActivityName(result.TargetActivity),
                result.ActivityTransitionMode.ToString(),
                result.ActivityLoadingMode,
                lifecycleOperation,
                lifecycleContent,
                lifecycleReadiness,
                loadingDiagnostics.AdapterEvidenceCount,
                loadingDiagnostics.AppliedAdapterEvidenceCount,
                loadingDiagnostics.SkippedAdapterEvidenceCount,
                loadingDiagnostics.FailedAdapterEvidenceCount,
                loadingDiagnostics.AdapterEvidenceBlockingIssueCount);

            return LogFields.Of(
                LogFields.Field("kind", result.Kind),
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("gameFlowEnvelopeKind", envelope.OperationKindText),
                LogFields.Field("gameFlowEnvelopeAdmission", envelope.AdmissionText),
                LogFields.Field("gameFlowEnvelopeSource", envelope.Source),
                LogFields.Field("gameFlowEnvelopeReason", envelope.Reason),
                LogFields.Field("gameFlowEnvelopeTargetRoute", envelope.TargetRoute),
                LogFields.Field("gameFlowEnvelopePreviousRoute", envelope.PreviousRoute),
                LogFields.Field("gameFlowEnvelopeTargetActivity", envelope.TargetActivity),
                LogFields.Field("gameFlowEnvelopePreviousActivity", envelope.PreviousActivity),
                LogFields.Field("gameFlowEnvelopeTransitionStatus", envelope.TransitionStatus),
                LogFields.Field("gameFlowEnvelopeLoadingStatus", envelope.LoadingStatus),
                LogFields.Field("gameFlowEnvelopeValidationMode", envelope.ValidationMode),
                LogFields.Field("gameFlowEnvelopeDomainStatus", envelope.DomainStatus),
                LogFields.Field("gameFlowEnvelopeLifecycleOperationKind", envelope.LifecycleOperationKind),
                LogFields.Field("gameFlowEnvelopeLifecycleStages", envelope.LifecycleStageCount),
                LogFields.Field("gameFlowEnvelopeLifecycleBlockingIssues", envelope.LifecycleBlockingIssueCount),
                LogFields.Field("gameFlowEnvelopeLifecycleFailedStages", envelope.LifecycleFailedStageCount),
                LogFields.Field("gameFlowEnvelopeLifecycleSkippedStages", envelope.LifecycleSkippedStageCount),
                LogFields.Field("gameFlowEnvelopeContentStatus", envelope.LifecycleContentStatus),
                LogFields.Field("gameFlowEnvelopeContentBlockingIssues", envelope.LifecycleContentBlockingIssueCount),
                LogFields.Field("gameFlowEnvelopeContentHandles", envelope.LifecycleContentHandleCount),
                LogFields.Field("gameFlowEnvelopeReadiness", envelope.LifecycleReadiness),
                LogFields.Field("gameFlowEnvelopeReadinessReason", envelope.LifecycleReadinessReason),
                LogFields.Field("gameFlowEnvelopeReadinessIssues", envelope.LifecycleReadinessIssueCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceCount", envelope.LoadingAdapterEvidenceCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceApplied", envelope.LoadingAdapterEvidenceAppliedCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceSkipped", envelope.LoadingAdapterEvidenceSkippedCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterEvidenceFailed", envelope.LoadingAdapterEvidenceFailedCount),
                LogFields.Field("gameFlowEnvelopeLoadingAdapterBlockingIssues", envelope.LoadingAdapterBlockingIssueCount),
                LogFields.Field("gameFlowEnvelopeDiagnostics", envelope.DiagnosticText),
                LogFields.Field("lifecycleOperationKind", lifecycleOperation.OperationKindText),
                LogFields.Field("lifecycleOperationStages", lifecycleOperation.StageCount),
                LogFields.Field("lifecycleOperationBlockingIssues", lifecycleOperation.BlockingIssueCount),
                LogFields.Field("lifecycleOperationIssues", lifecycleOperation.IssueCount),
                LogFields.Field("lifecycleOperationSideEffects", lifecycleOperation.SideEffectCount),
                LogFields.Field("lifecycleOperationFailedStages", lifecycleOperation.FailedStageCount),
                LogFields.Field("lifecycleOperationSkippedStages", lifecycleOperation.SkippedStageCount),
                LogFields.Field("lifecycleOperationStageNames", lifecycleOperation.StageNamesText),
                LogFields.Field("lifecycleOperationStageStatuses", lifecycleOperation.StageStatusesText),
                LogFields.Field("lifecycleOperationDiagnostics", lifecycleOperation.DiagnosticText),
                LogFields.Field("lifecycleContentStatus", lifecycleContent.Status),
                LogFields.Field("lifecycleContentEnter", lifecycleContent.EnterStatus),
                LogFields.Field("lifecycleContentExit", lifecycleContent.ExitStatus),
                LogFields.Field("lifecycleContentEnterRequests", lifecycleContent.EnterRequestCount),
                LogFields.Field("lifecycleContentExitRequests", lifecycleContent.ExitRequestCount),
                LogFields.Field("lifecycleContentParticipants", lifecycleContent.ParticipantCount),
                LogFields.Field("lifecycleContentParticipantSource", lifecycleContent.ParticipantSourceStatus),
                LogFields.Field("lifecycleContentBlockingIssues", lifecycleContent.BlockingIssueCount),
                LogFields.Field("lifecycleContentBlocksReadiness", lifecycleContent.BlocksReadiness),
                LogFields.Field("lifecycleContentHandles", lifecycleContent.ContentHandleCount),
                LogFields.Field("lifecycleContentDiagnostics", lifecycleContent.DiagnosticText),
                LogFields.Field("lifecycleReadiness", lifecycleReadiness.Status),
                LogFields.Field("lifecycleReadinessReason", lifecycleReadiness.Reason),
                LogFields.Field("lifecycleReadinessIssues", lifecycleReadiness.IssueCount),
                LogFields.Field("lifecycleReadinessBlockedByContent", lifecycleReadiness.BlockedByContent),
                LogFields.Field("transition", result.TransitionDiagnostics.TransitionText),
                LogFields.Field("transitionScope", result.TransitionDiagnostics.ScopeText),
                LogFields.Field("transitionBefore", result.TransitionDiagnostics.BeforeText),
                LogFields.Field("transitionAfter", result.TransitionDiagnostics.AfterText),
                LogFields.Field("transitionBlockingIssues", result.TransitionDiagnostics.BlockingIssueCount),
                LogFields.Field("transitionVisual", result.TransitionDiagnostics.VisualText),
                LogFields.Field("transitionEffect", result.TransitionDiagnostics.EffectText),
                LogFields.Field("transitionEffectBefore", result.TransitionDiagnostics.EffectBeforeText),
                LogFields.Field("transitionEffectAfter", result.TransitionDiagnostics.EffectAfterText),
                LogFields.Field("transitionEffectBlockingIssues", result.TransitionDiagnostics.EffectBlockingIssueCount),
                LogFields.Field("transitionEffectAdapterCount", result.TransitionDiagnostics.EffectAdapterCount),
                LogFields.Field("transitionEffectAdapterEvidenceCount", result.TransitionDiagnostics.EffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceApplied", result.TransitionDiagnostics.AppliedEffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceSkipped", result.TransitionDiagnostics.SkippedEffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceFailed", result.TransitionDiagnostics.FailedEffectAdapterEvidenceCount),
                LogFields.Field("transitionEffectAdapterEvidenceBlockingIssues", result.TransitionDiagnostics.EffectAdapterEvidenceBlockingIssueCount),
                LogFields.Field("transitionEffectAdapterEvidenceNames", result.TransitionDiagnostics.EffectAdapterEvidenceNamesText),
                LogFields.Field("transitionEffectAdapterEvidenceStatuses", result.TransitionDiagnostics.EffectAdapterEvidenceStatusesText),
                LogFields.Field("transitionGate", result.TransitionGateDiagnostics.GateText),
                LogFields.Field("transitionGateMode", result.TransitionGateDiagnostics.ModeText),
                LogFields.Field("transitionGateApplied", result.TransitionGateDiagnostics.Applied),
                LogFields.Field("transitionGateReleased", result.TransitionGateDiagnostics.Released),
                LogFields.Field("transitionGateBlockers", result.TransitionGateDiagnostics.BlockersText),
                LogFields.Field("blocksInputAcceptance", result.TransitionGateDiagnostics.BlocksInputAcceptance),
                LogFields.Field("blocksInteractionAcceptance", result.TransitionGateDiagnostics.BlocksInteractionAcceptance),
                LogFields.Field("blocksGameplayAction", result.TransitionGateDiagnostics.BlocksGameplayAction),
                LogFields.Field("transitionGateBlockingIssues", result.TransitionGateDiagnostics.BlockingIssuesText),
                LogFields.Field("activityTransitionMode", result.ActivityTransitionMode.ToString()),
                LogFields.Field("activityLoadingMode", result.ActivityLoadingMode),
                LogFields.Field("targetActivity", GetActivityName(result.TargetActivity)),
                LogFields.Field("previousActivity", GetActivityName(activityFlow.PreviousActivity)),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlow.ActivityState.ActivityName)),
                LogFields.Field("activityState", activityFlow.ActivityState.DiagnosticStatus),
                LogFields.Field("activityReadiness", activityFlow.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("activityReadinessReason", activityFlow.ActivityReadinessState.DiagnosticReason),
                LogFields.Field("runtimeActivityScope", activityFlow.RuntimeActivityScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeActivityRootEnter", activityFlow.RuntimeActivityScopeResult.EnterStatus),
                LogFields.Field("runtimeActivityRootExit", activityFlow.RuntimeActivityScopeResult.ExitStatus),
                LogFields.Field("runtimeActivityContext", activityFlow.RuntimeActivityScopeResult.ContextStatus),
                LogFields.Field("runtimeRootCount", activityFlow.RuntimeActivityScopeResult.RootCount),
                LogFields.Field("activityReadinessIssues", activityFlow.ActivityReadinessState.BlockingIssueCount),
                LogFields.Field("activityContentBindings", activityContent.BindingCount),
                LogFields.Field("contentAnchorBindings", ContentAnchorBindingCount),
                LogFields.Field("activityContentAnchors", activityFlow.ActivityContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("activityContentAnchorCandidates", activityFlow.ActivityContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("activityContentDiscoverySceneRoots", activityFlow.ActivityContentAnchorDiscoveryResult.DiscoverySceneRootCount),
                LogFields.Field("activityContentAnchorIssues", activityFlow.ActivityContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("activityContentAnchorInvalid", activityFlow.ActivityContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("activityContentAnchorActivityMismatch", activityFlow.ActivityContentAnchorDiscoveryResult.SkippedActivityMismatchCount),
                LogFields.Field("activityContentAnchorBindingCleanup", activityFlow.ActivityContentAnchorBindingCleanupResult.DiagnosticStatus),
                LogFields.Field("activityContentAnchorBindingCleanupRemoved", activityFlow.ActivityContentAnchorBindingCleanupResult.RemovedCount),
                LogFields.Field("activityContentExecution", activityFlow.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentExecutionParticipantSource", activityFlow.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentExecutionParticipantSourceIssues", activityFlow.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentExecutionParticipants", activityFlow.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentExecutionEnter", activityFlow.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentExecutionEnterRequests", activityFlow.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentExecutionExit", activityFlow.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentExecutionExitRequests", activityFlow.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentExecutionBlockingIssues", activityFlow.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentExecutionBlocksReadiness", activityFlow.ActivityContentExecutionResult.BlocksReadiness),
                LogFields.Field("activityContentParticipantExecution", activityFlow.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentParticipantSource", activityFlow.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentParticipantSourceIssues", activityFlow.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentParticipantCount", activityFlow.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentParticipantEnter", activityFlow.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentParticipantEnterRequests", activityFlow.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentParticipantExit", activityFlow.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentParticipantExitRequests", activityFlow.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentParticipantBlockingIssues", activityFlow.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentParticipantBlocksReadiness", activityFlow.ActivityContentExecutionResult.BlocksReadiness),
                LogFields.Field("activityContentHandles", activityContent.ActivityContentCount),
                LogFields.Field("activitySceneComposition", activityFlow.ActivitySceneCompositionResult.DiagnosticStatus),
                LogFields.Field("activitySceneCompositionProfile", activityFlow.ActivitySceneCompositionResult.ProfileId),
                LogFields.Field("activitySceneCompositionScenes", activityFlow.ActivitySceneCompositionResult.SceneCount),
                LogFields.Field("activitySceneCompositionRequired", activityFlow.ActivitySceneCompositionResult.RequiredSceneCount),
                LogFields.Field("activitySceneCompositionOptional", activityFlow.ActivitySceneCompositionResult.OptionalSceneCount),
                LogFields.Field("activitySceneCompositionExecutionReady", activityFlow.ActivitySceneCompositionResult.ExecutionReadySceneCount),
                LogFields.Field("activitySceneCompositionLoaded", activityFlow.ActivitySceneCompositionResult.LoadedSceneCount),
                LogFields.Field("activitySceneCompositionAlreadyLoaded", activityFlow.ActivitySceneCompositionResult.AlreadyLoadedSceneCount),
                LogFields.Field("activitySceneCompositionFailed", activityFlow.ActivitySceneCompositionResult.FailedSceneCount),
                LogFields.Field("activitySceneCompositionSkipped", activityFlow.ActivitySceneCompositionResult.SkippedSceneCount),
                LogFields.Field("activitySceneCompositionSideEffects", activityFlow.ActivitySceneCompositionResult.SideEffectsExecuted),
                LogFields.Field("activitySceneCompositionBlockingIssues", activityFlow.ActivitySceneCompositionResult.BlockingIssueCount),
                LogFields.Field("activitySceneRelease", activityFlow.ActivitySceneReleaseResult.DiagnosticStatus),
                LogFields.Field("activitySceneReleaseScenes", activityFlow.ActivitySceneReleaseResult.SceneCount),
                LogFields.Field("activitySceneReleaseReleased", activityFlow.ActivitySceneReleaseResult.ReleasedSceneCount),
                LogFields.Field("activitySceneReleaseFailed", activityFlow.ActivitySceneReleaseResult.FailedSceneCount),
                LogFields.Field("activitySceneReleaseSkipped", activityFlow.ActivitySceneReleaseResult.SkippedSceneCount),
                LogFields.Field("activitySceneReleaseSideEffects", activityFlow.ActivitySceneReleaseResult.SideEffectsExecuted),
                LogFields.Field("activitySceneReleaseBlockingIssues", activityFlow.ActivitySceneReleaseResult.BlockingIssueCount),
                LogFields.Field("activitySceneLedger", activityFlow.ActivitySceneLedgerSnapshot.DiagnosticStatus),
                LogFields.Field("activitySceneLedgerEntries", activityFlow.ActivitySceneLedgerSnapshot.EntryCount),
                LogFields.Field("activitySceneLedgerLoaded", activityFlow.ActivitySceneLedgerSnapshot.LoadedCount),
                LogFields.Field("activitySceneLedgerReleased", activityFlow.ActivitySceneLedgerSnapshot.ReleasedCount),
                LogFields.Field("activitySceneLedgerStale", activityFlow.ActivitySceneLedgerSnapshot.StaleCount),
                LogFields.Field("activityContentLifecycle", lifecycle.DiagnosticStatus),
                LogFields.Field("activityContentEnterFailed", lifecycle.EnterFailedReceiverCount),
                LogFields.Field("activityContentExitFailed", lifecycle.ExitFailedReceiverCount),
                LogFields.Field("loading", loadingDiagnostics.LoadingText),
                LogFields.Field("loadingVisual", loadingDiagnostics.VisualText),
                LogFields.Field("loadingBefore", loadingDiagnostics.BeforeText),
                LogFields.Field("loadingAfter", loadingDiagnostics.AfterText),
                LogFields.Field("loadingBlockingIssues", loadingDiagnostics.BlockingIssueCount),
                LogFields.Field("loadingAdapterCount", loadingDiagnostics.AdapterCount),
                LogFields.Field("loadingAdapterEvidenceCount", loadingDiagnostics.AdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceApplied", loadingDiagnostics.AppliedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceSkipped", loadingDiagnostics.SkippedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceFailed", loadingDiagnostics.FailedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceIssues", loadingDiagnostics.AdapterEvidenceIssueCount),
                LogFields.Field("loadingAdapterEvidenceBlockingIssues", loadingDiagnostics.AdapterEvidenceBlockingIssueCount),
                LogFields.Field("loadingAdapterEvidenceNames", loadingDiagnostics.AdapterEvidenceNamesText),
                LogFields.Field("loadingAdapterEvidenceStatuses", loadingDiagnostics.AdapterEvidenceStatusesText),
                LogFields.Field("loadingProgressSupported", loadingDiagnostics.ProgressSupported),
                LogFields.Field("loadingProgressMode", loadingDiagnostics.ProgressModeText),
                LogFields.Field("loadingProgressValue", loadingDiagnostics.ProgressValueText),
                LogFields.Field("loadingProgressPercent", loadingDiagnostics.ProgressPercentText),
                LogFields.Field("loadingProgressPhase", loadingDiagnostics.ProgressPhaseText),
                LogFields.Field("loadingProgressMessage", loadingDiagnostics.ProgressMessageText),
                LogFields.Field("loadingProgress", loadingDiagnostics.ProgressText));
        }

        private static FrameworkLifecycleOperationEvidence BuildRouteLifecycleOperationEvidence(
            FrameworkRouteRequestResult result,
            FrameworkLoadingDiagnostics loadingDiagnostics)
        {
            RouteLifecycleStartResult routeLifecycle = result.RouteLifecycleResult;
            ActivityFlowStartResult activityFlow = routeLifecycle.ActivityFlowResult;
            var builder = new FrameworkLifecycleOperationEvidenceBuilder(
                FrameworkLifecycleOperationKind.Route,
                result.Source,
                result.Reason);

            AddTransitionStages(builder, result.TransitionDiagnostics, result.Source, result.Reason);
            AddLoadingStages(builder, loadingDiagnostics, result.Source, result.Reason);

            builder.AddStage(
                FrameworkLifecycleOperationStage.RouteExit,
                routeLifecycle.RouteExitResult.DiagnosticStatus,
                routeLifecycle.RouteExitResult.Source,
                routeLifecycle.RouteExitResult.Reason,
                0,
                0,
                routeLifecycle.RouteExitResult.Completed,
                false,
                !routeLifecycle.RouteExitResult.Completed,
                routeLifecycle.RouteExitResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.RuntimeScopeExit,
                routeLifecycle.RuntimeRouteScopeResult.ExitStatus,
                routeLifecycle.RuntimeRouteScopeResult.Source,
                routeLifecycle.RuntimeRouteScopeResult.Reason,
                0,
                0,
                routeLifecycle.RuntimeRouteScopeResult.HasExitRootResult,
                routeLifecycle.RuntimeRouteScopeResult.Rejected && routeLifecycle.RuntimeRouteScopeResult.HasExitRootResult,
                !routeLifecycle.RuntimeRouteScopeResult.HasExitRootResult,
                routeLifecycle.RuntimeRouteScopeResult.ToDiagnosticString());

            builder.AddStage(
                FrameworkLifecycleOperationStage.RouteRelease,
                routeLifecycle.ContentReleaseResult.Status.ToString(),
                result.Source,
                result.Reason,
                routeLifecycle.ContentReleaseResult.IssueCount,
                routeLifecycle.ContentReleaseResult.BlockingIssueCount,
                routeLifecycle.ContentReleaseResult.ReleasedCount > 0,
                routeLifecycle.ContentReleaseResult.Failed,
                routeLifecycle.ContentReleaseResult.NotExecuted || routeLifecycle.ContentReleaseResult.SkippedCount > 0 && routeLifecycle.ContentReleaseResult.ReleasedCount == 0,
                routeLifecycle.ContentReleaseResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.ActivitySceneRelease,
                routeLifecycle.ActivitySceneRouteReleaseResult.DiagnosticStatus,
                result.Source,
                result.Reason,
                routeLifecycle.ActivitySceneRouteReleaseResult.BlockingIssueCount,
                routeLifecycle.ActivitySceneRouteReleaseResult.BlockingIssueCount,
                routeLifecycle.ActivitySceneRouteReleaseResult.SideEffectsExecuted,
                routeLifecycle.ActivitySceneRouteReleaseResult.FailedSceneCount > 0,
                !routeLifecycle.ActivitySceneRouteReleaseResult.Executed || routeLifecycle.ActivitySceneRouteReleaseResult.SkippedSceneCount > 0 && routeLifecycle.ActivitySceneRouteReleaseResult.ReleasedSceneCount == 0,
                routeLifecycle.ActivitySceneRouteReleaseResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.ContentExit,
                FrameworkLifecycleContentEvidenceProjection.BuildStageStatus(
                    routeLifecycle.RouteContentExitResult.DiagnosticStatus,
                    routeLifecycle.RouteContentExitResult.BindingCount,
                    routeLifecycle.RouteContentExitResult.FailedReceiverCount),
                result.Source,
                result.Reason,
                routeLifecycle.RouteContentExitResult.FailedReceiverCount,
                routeLifecycle.RouteContentExitResult.FailedReceiverCount,
                routeLifecycle.RouteContentExitResult.Executed,
                routeLifecycle.RouteContentExitResult.HasFailures,
                !routeLifecycle.RouteContentExitResult.Executed,
                "Route content exit lifecycle callbacks.");

            builder.AddStage(
                FrameworkLifecycleOperationStage.SceneComposition,
                routeLifecycle.RouteSceneCompositionResult.Status.ToString(),
                result.Source,
                result.Reason,
                routeLifecycle.RouteSceneCompositionResult.IssueCount,
                routeLifecycle.RouteSceneCompositionResult.BlockingIssueCount,
                routeLifecycle.RouteSceneCompositionResult.LoadedCount > 0,
                routeLifecycle.RouteSceneCompositionResult.Failed,
                routeLifecycle.RouteSceneCompositionResult.NotExecuted || routeLifecycle.RouteSceneCompositionResult.SkippedCount > 0 && routeLifecycle.RouteSceneCompositionResult.LoadedCount == 0,
                routeLifecycle.RouteSceneCompositionResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.ContentAnchorBindingCleanup,
                routeLifecycle.RouteContentAnchorBindingCleanupResult.DiagnosticStatus,
                result.Source,
                result.Reason,
                0,
                0,
                routeLifecycle.RouteContentAnchorBindingCleanupResult.RemovedAny,
                false,
                !routeLifecycle.RouteContentAnchorBindingCleanupResult.Executed,
                routeLifecycle.RouteContentAnchorBindingCleanupResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.RuntimeScopeEnter,
                routeLifecycle.RuntimeRouteScopeResult.EnterStatus,
                routeLifecycle.RuntimeRouteScopeResult.Source,
                routeLifecycle.RuntimeRouteScopeResult.Reason,
                0,
                0,
                routeLifecycle.RuntimeRouteScopeResult.HasEnterRootResult || routeLifecycle.RuntimeRouteScopeResult.HasContext,
                routeLifecycle.RuntimeRouteScopeResult.Rejected && routeLifecycle.RuntimeRouteScopeResult.HasEnterRootResult,
                !routeLifecycle.RuntimeRouteScopeResult.HasEnterRootResult && !routeLifecycle.RuntimeRouteScopeResult.HasContext,
                routeLifecycle.RuntimeRouteScopeResult.ToDiagnosticString());

            builder.AddStage(
                FrameworkLifecycleOperationStage.ContentEnter,
                FrameworkLifecycleContentEvidenceProjection.BuildStageStatus(
                    routeLifecycle.RouteContentEnterResult.DiagnosticStatus,
                    routeLifecycle.RouteContentEnterResult.BindingCount,
                    routeLifecycle.RouteContentEnterResult.FailedReceiverCount),
                result.Source,
                result.Reason,
                routeLifecycle.RouteContentEnterResult.FailedReceiverCount,
                routeLifecycle.RouteContentEnterResult.FailedReceiverCount,
                routeLifecycle.RouteContentEnterResult.Executed,
                routeLifecycle.RouteContentEnterResult.HasFailures,
                !routeLifecycle.RouteContentEnterResult.Executed,
                "Route content enter lifecycle callbacks.");

            AddActivityFlowStages(builder, activityFlow, result.Source, result.Reason);
            return builder.Build();
        }

        private static FrameworkLifecycleContentEvidence BuildRouteLifecycleContentEvidence(
            RouteLifecycleStartResult routeLifecycle,
            string source,
            string reason)
        {
            int blockingIssues = routeLifecycle.RouteContentEnterResult.FailedReceiverCount
                + routeLifecycle.RouteContentExitResult.FailedReceiverCount;
            string status = blockingIssues > 0
                ? "Failed"
                : routeLifecycle.RouteContentEnterResult.Executed || routeLifecycle.RouteContentExitResult.Executed
                    ? "Executed"
                    : "Skipped";

            return new FrameworkLifecycleContentEvidence(
                status,
                routeLifecycle.RouteContentEnterResult.DiagnosticStatus,
                routeLifecycle.RouteContentExitResult.DiagnosticStatus,
                routeLifecycle.RouteContentEnterResult.BindingCount,
                routeLifecycle.RouteContentExitResult.BindingCount,
                0,
                "None",
                blockingIssues,
                blockingIssues,
                false,
                routeLifecycle.RouteContentSet.Count,
                source,
                reason);
        }

        private static FrameworkLifecycleContentEvidence BuildActivityLifecycleContentEvidence(
            ActivityFlowStartResult activityFlow,
            string source,
            string reason)
        {
            ActivityContentApplyResult activityContent = activityFlow.ActivityContentResult;
            ActivityContentExecutionLifecycleResult execution = activityFlow.ActivityContentExecutionResult;
            int issueCount = execution.BlockingIssueCount + execution.ParticipantSourceIssueCount;

            return new FrameworkLifecycleContentEvidence(
                execution.DiagnosticStatus,
                execution.EnterResult.Status.ToString(),
                execution.ExitResult.Status.ToString(),
                execution.EnterRequestCount,
                execution.ExitRequestCount,
                execution.ParticipantCount,
                execution.ParticipantSourceStatus,
                issueCount,
                execution.BlockingIssueCount,
                execution.BlocksReadiness,
                activityContent.ActivityContentCount,
                source,
                reason);
        }

        private static FrameworkLifecycleReadinessEvidence BuildActivityLifecycleReadinessEvidence(
            ActivityFlowStartResult activityFlow,
            string source,
            string reason)
        {
            bool blockedByContent = activityFlow.ActivityContentExecutionResult.BlocksReadiness
                || activityFlow.ActivityContentResult.HasLifecycleFailures;
            return new FrameworkLifecycleReadinessEvidence(
                activityFlow.ActivityReadinessState.DiagnosticStatus,
                activityFlow.ActivityReadinessState.DiagnosticReason,
                activityFlow.ActivityReadinessState.BlockingIssueCount,
                blockedByContent,
                source,
                reason);
        }

        private static FrameworkLifecycleOperationEvidence BuildActivityLifecycleOperationEvidence(
            FrameworkActivityRequestResult result,
            FrameworkLoadingDiagnostics loadingDiagnostics)
        {
            var builder = new FrameworkLifecycleOperationEvidenceBuilder(
                result.TargetActivity == null ? FrameworkLifecycleOperationKind.ActivityClear : FrameworkLifecycleOperationKind.Activity,
                result.Source,
                result.Reason);

            AddTransitionStages(builder, result.TransitionDiagnostics, result.Source, result.Reason);
            AddLoadingStages(builder, loadingDiagnostics, result.Source, result.Reason);
            AddActivityFlowStages(builder, result.ActivityFlowResult, result.Source, result.Reason);
            return builder.Build();
        }

        private static void AddTransitionStages(
            FrameworkLifecycleOperationEvidenceBuilder builder,
            FrameworkTransitionDiagnostics transitionDiagnostics,
            string source,
            string reason)
        {
            builder.AddStage(
                FrameworkLifecycleOperationStage.TransitionBefore,
                transitionDiagnostics.BeforeText,
                source,
                reason,
                0,
                0,
                false,
                false,
                StatusIndicatesSkipped(transitionDiagnostics.BeforeText),
                "Transition before request projection.");

            builder.AddStage(
                FrameworkLifecycleOperationStage.TransitionAfter,
                transitionDiagnostics.AfterText,
                source,
                reason,
                transitionDiagnostics.BlockingIssueCount,
                transitionDiagnostics.BlockingIssueCount,
                transitionDiagnostics.EffectAdapterCount > 0,
                transitionDiagnostics.BlockingIssueCount > 0,
                StatusIndicatesSkipped(transitionDiagnostics.AfterText),
                "Transition after request projection.");
        }

        private static void AddLoadingStages(
            FrameworkLifecycleOperationEvidenceBuilder builder,
            FrameworkLoadingDiagnostics loadingDiagnostics,
            string source,
            string reason)
        {
            builder.AddStage(
                FrameworkLifecycleOperationStage.LoadingBefore,
                loadingDiagnostics.BeforeText,
                source,
                reason,
                0,
                0,
                false,
                false,
                StatusIndicatesSkipped(loadingDiagnostics.BeforeText),
                loadingDiagnostics.ProgressMessageText);

            int issueCount = Math.Max(loadingDiagnostics.AdapterEvidenceIssueCount, loadingDiagnostics.BlockingIssueCount);
            builder.AddStage(
                FrameworkLifecycleOperationStage.LoadingAfter,
                loadingDiagnostics.AfterText,
                source,
                reason,
                issueCount,
                loadingDiagnostics.BlockingIssueCount,
                loadingDiagnostics.AppliedAdapterEvidenceCount > 0,
                loadingDiagnostics.BlockingIssueCount > 0,
                StatusIndicatesSkipped(loadingDiagnostics.AfterText),
                loadingDiagnostics.ProgressMessageText,
                $"adapterEvidence='{loadingDiagnostics.AdapterEvidenceCount}' adapterStatuses='{loadingDiagnostics.AdapterEvidenceStatusesText}'");
        }

        private static void AddActivityFlowStages(
            FrameworkLifecycleOperationEvidenceBuilder builder,
            ActivityFlowStartResult activityFlow,
            string source,
            string reason)
        {
            ActivityContentApplyResult activityContent = activityFlow.ActivityContentResult;
            ActivityContentLifecycleResult lifecycle = activityContent.LifecycleResult;

            builder.AddStage(
                FrameworkLifecycleOperationStage.RuntimeScopeExit,
                activityFlow.RuntimeActivityScopeResult.ExitStatus,
                activityFlow.RuntimeActivityScopeResult.Source,
                activityFlow.RuntimeActivityScopeResult.Reason,
                0,
                0,
                activityFlow.RuntimeActivityScopeResult.HasExitRootResult,
                activityFlow.RuntimeActivityScopeResult.Rejected && activityFlow.RuntimeActivityScopeResult.HasExitRootResult,
                !activityFlow.RuntimeActivityScopeResult.HasExitRootResult,
                activityFlow.RuntimeActivityScopeResult.ToDiagnosticString());

            builder.AddStage(
                FrameworkLifecycleOperationStage.ActivitySceneRelease,
                activityFlow.ActivitySceneReleaseResult.DiagnosticStatus,
                source,
                reason,
                activityFlow.ActivitySceneReleaseResult.BlockingIssueCount,
                activityFlow.ActivitySceneReleaseResult.BlockingIssueCount,
                activityFlow.ActivitySceneReleaseResult.SideEffectsExecuted,
                activityFlow.ActivitySceneReleaseResult.FailedSceneCount > 0,
                !activityFlow.ActivitySceneReleaseResult.Executed || activityFlow.ActivitySceneReleaseResult.SkippedSceneCount > 0 && activityFlow.ActivitySceneReleaseResult.ReleasedSceneCount == 0,
                activityFlow.ActivitySceneReleaseResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.ContentAnchorBindingCleanup,
                activityFlow.ActivityContentAnchorBindingCleanupResult.DiagnosticStatus,
                source,
                reason,
                0,
                0,
                activityFlow.ActivityContentAnchorBindingCleanupResult.RemovedAny,
                false,
                !activityFlow.ActivityContentAnchorBindingCleanupResult.Executed,
                activityFlow.ActivityContentAnchorBindingCleanupResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.ActivityContentExecution,
                FrameworkLifecycleContentEvidenceProjection.BuildStageStatus(
                    activityFlow.ActivityContentExecutionResult.DiagnosticStatus,
                    activityFlow.ActivityContentExecutionResult.EnterRequestCount + activityFlow.ActivityContentExecutionResult.ExitRequestCount,
                    activityFlow.ActivityContentExecutionResult.BlockingIssueCount),
                source,
                reason,
                activityFlow.ActivityContentExecutionResult.BlockingIssueCount + activityFlow.ActivityContentExecutionResult.ParticipantSourceIssueCount,
                activityFlow.ActivityContentExecutionResult.BlockingIssueCount,
                activityFlow.ActivityContentExecutionResult.EnterResultCount > 0 || activityFlow.ActivityContentExecutionResult.ExitResultCount > 0,
                activityFlow.ActivityContentExecutionResult.BlocksReadiness,
                !activityFlow.ActivityContentExecutionResult.Executed,
                activityFlow.ActivityContentExecutionResult.ToDiagnosticString());

            builder.AddStage(
                FrameworkLifecycleOperationStage.ContentExit,
                FrameworkLifecycleContentEvidenceProjection.BuildStageStatus(
                    lifecycle.DiagnosticStatus,
                    lifecycle.ExitBindingCount,
                    lifecycle.ExitFailedReceiverCount),
                source,
                reason,
                lifecycle.ExitFailedReceiverCount,
                lifecycle.ExitFailedReceiverCount,
                lifecycle.Executed && lifecycle.ExitBindingCount > 0,
                lifecycle.ExitFailedReceiverCount > 0,
                !lifecycle.Executed,
                "Activity content exit lifecycle callbacks.");

            builder.AddStage(
                FrameworkLifecycleOperationStage.ActivitySceneComposition,
                activityFlow.ActivitySceneCompositionResult.DiagnosticStatus,
                source,
                reason,
                activityFlow.ActivitySceneCompositionResult.BlockingIssueCount,
                activityFlow.ActivitySceneCompositionResult.BlockingIssueCount,
                activityFlow.ActivitySceneCompositionResult.SideEffectsExecuted,
                activityFlow.ActivitySceneCompositionResult.FailedSceneCount > 0,
                !activityFlow.ActivitySceneCompositionResult.Executed || activityFlow.ActivitySceneCompositionResult.SkippedSceneCount > 0 && activityFlow.ActivitySceneCompositionResult.LoadedSceneCount == 0,
                activityFlow.ActivitySceneCompositionResult.Message);

            builder.AddStage(
                FrameworkLifecycleOperationStage.RuntimeScopeEnter,
                activityFlow.RuntimeActivityScopeResult.EnterStatus,
                activityFlow.RuntimeActivityScopeResult.Source,
                activityFlow.RuntimeActivityScopeResult.Reason,
                0,
                0,
                activityFlow.RuntimeActivityScopeResult.HasEnterRootResult || activityFlow.RuntimeActivityScopeResult.HasContext,
                activityFlow.RuntimeActivityScopeResult.Rejected && activityFlow.RuntimeActivityScopeResult.HasEnterRootResult,
                !activityFlow.RuntimeActivityScopeResult.HasEnterRootResult && !activityFlow.RuntimeActivityScopeResult.HasContext,
                activityFlow.RuntimeActivityScopeResult.ToDiagnosticString());

            builder.AddStage(
                FrameworkLifecycleOperationStage.ContentEnter,
                FrameworkLifecycleContentEvidenceProjection.BuildStageStatus(
                    lifecycle.DiagnosticStatus,
                    lifecycle.EnterBindingCount,
                    lifecycle.EnterFailedReceiverCount),
                source,
                reason,
                lifecycle.EnterFailedReceiverCount,
                lifecycle.EnterFailedReceiverCount,
                lifecycle.Executed && lifecycle.EnterBindingCount > 0,
                lifecycle.EnterFailedReceiverCount > 0,
                !lifecycle.Executed,
                "Activity content enter lifecycle callbacks.");

            builder.AddStage(
                FrameworkLifecycleOperationStage.Readiness,
                FrameworkLifecycleReadinessEvidenceProjection.BuildStageStatus(
                    BuildActivityLifecycleReadinessEvidence(activityFlow, source, reason)),
                source,
                reason,
                activityFlow.ActivityReadinessState.BlockingIssueCount,
                activityFlow.ActivityReadinessState.BlockingIssueCount,
                false,
                activityFlow.ActivityReadinessState.HasBlockingIssues,
                activityFlow.ActivityReadinessState.IsNone,
                activityFlow.ActivityReadinessState.DiagnosticReason);

            builder.AddStage(
                FrameworkLifecycleOperationStage.ActivitySceneLedger,
                activityFlow.ActivitySceneLedgerSnapshot.DiagnosticStatus,
                source,
                reason,
                activityFlow.ActivitySceneLedgerSnapshot.StaleCount,
                0,
                false,
                false,
                activityFlow.ActivitySceneLedgerSnapshot.EntryCount == 0,
                $"entries='{activityFlow.ActivitySceneLedgerSnapshot.EntryCount}' loaded='{activityFlow.ActivitySceneLedgerSnapshot.LoadedCount}' released='{activityFlow.ActivitySceneLedgerSnapshot.ReleasedCount}' stale='{activityFlow.ActivitySceneLedgerSnapshot.StaleCount}'.");
        }

        private static bool StatusIndicatesSkipped(string statusText)
        {
            if (string.IsNullOrWhiteSpace(statusText))
            {
                return true;
            }

            return statusText.IndexOf("Skipped", StringComparison.OrdinalIgnoreCase) >= 0
                || statusText.IndexOf("NoOp", StringComparison.OrdinalIgnoreCase) >= 0
                || statusText.IndexOf("NotRequested", StringComparison.OrdinalIgnoreCase) >= 0
                || statusText.IndexOf("NotExecuted", StringComparison.OrdinalIgnoreCase) >= 0
                || string.Equals(statusText, "None", StringComparison.OrdinalIgnoreCase);
        }

        private LogField[] BuildLoadingFields(FrameworkLoadingDiagnostics loadingDiagnostics)
        {
            return LogFields.Of(
                LogFields.Field("loading", loadingDiagnostics.LoadingText),
                LogFields.Field("loadingVisual", loadingDiagnostics.VisualText),
                LogFields.Field("loadingBefore", loadingDiagnostics.BeforeText),
                LogFields.Field("loadingAfter", loadingDiagnostics.AfterText),
                LogFields.Field("loadingBlockingIssues", loadingDiagnostics.BlockingIssueCount),
                LogFields.Field("loadingAdapterCount", loadingDiagnostics.AdapterCount),
                LogFields.Field("loadingAdapterEvidenceCount", loadingDiagnostics.AdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceApplied", loadingDiagnostics.AppliedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceSkipped", loadingDiagnostics.SkippedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceFailed", loadingDiagnostics.FailedAdapterEvidenceCount),
                LogFields.Field("loadingAdapterEvidenceIssues", loadingDiagnostics.AdapterEvidenceIssueCount),
                LogFields.Field("loadingAdapterEvidenceBlockingIssues", loadingDiagnostics.AdapterEvidenceBlockingIssueCount),
                LogFields.Field("loadingAdapterEvidenceNames", loadingDiagnostics.AdapterEvidenceNamesText),
                LogFields.Field("loadingAdapterEvidenceStatuses", loadingDiagnostics.AdapterEvidenceStatusesText),
                LogFields.Field("loadingProgressSupported", loadingDiagnostics.ProgressSupported),
                LogFields.Field("loadingProgressMode", loadingDiagnostics.ProgressModeText),
                LogFields.Field("loadingProgressValue", loadingDiagnostics.ProgressValueText),
                LogFields.Field("loadingProgressPercent", loadingDiagnostics.ProgressPercentText),
                LogFields.Field("loadingProgressPhase", loadingDiagnostics.ProgressPhaseText),
                LogFields.Field("loadingProgressMessage", loadingDiagnostics.ProgressMessageText),
                LogFields.Field("loadingProgress", loadingDiagnostics.ProgressText));
        }

        private void LogActivityContentObservability(ActivityContentApplyResult activityContentResult)
        {
            if (activityContentResult.HasDetailMessage)
            {
                _logger.Debug(activityContentResult.DetailMessage);
            }

            if (activityContentResult.HasWarningMessage)
            {
                _logger.Warning(activityContentResult.WarningMessage);
            }
        }

        private static string GetRouteName(RouteAsset route)
        {
            return route != null ? FormatDiagnosticValue(route.RouteName) : "<none>";
        }

        private static string GetActivityName(ActivityAsset activity)
        {
            return activity != null ? FormatDiagnosticValue(activity.ActivityName) : "<none>";
        }

        private static string FormatDiagnosticValue(string value)
        {
            return value.NormalizeTextOrFallback("<none>");
        }

        private void OnDestroy()
        {
            _pauseTimeScaleRuntime?.RestoreIfCaptured("framework-runtime-host-destroy");

            if (_current == this)
            {
                _current = null;
            }
        }
    }
}
