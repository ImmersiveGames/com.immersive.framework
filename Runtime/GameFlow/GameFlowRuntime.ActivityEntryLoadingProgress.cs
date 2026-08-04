using System;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Loading;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.Transition;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    internal sealed partial class GameFlowRuntime
    {
        private ActivityEntryLoadingProgressForwarder
            _activeActivityEntryLoadingProgressForwarder;

        internal async Task<FrameworkGameFlowStartResult>
            StartWithActivityEntryLoadingProgressAsync(
                GameApplicationAsset gameApplication,
                Func<Awaitable> beforeRouteLifecycle,
                Func<Awaitable> afterRouteLifecycle,
                IFrameworkLoadingProgressReporter progressReporter,
                Action<ActivityEntryLoadingProgressDiagnostics>
                    progressDiagnosticsSink = null)
        {
            ActivityAsset startupActivity =
                gameApplication != null && gameApplication.StartupRoute != null
                    ? gameApplication.StartupRoute.StartupActivity
                    : null;
            if (!ShouldProjectActivityEntryReadinessProgress(
                    startupActivity,
                    progressReporter))
            {
                return await StartAsync(gameApplication);
            }

            if (gameApplication == null)
            {
                return FrameworkGameFlowStartResult.Failed(
                    "Game Application is missing.");
            }

            RouteAsset startupRoute = gameApplication.StartupRoute;
            if (startupRoute == null)
            {
                return FrameworkGameFlowStartResult.Failed(
                    "Startup Route is missing.");
            }

            if (!startupRoute.HasPrimaryScene)
            {
                return FrameworkGameFlowStartResult.Failed(
                    "Startup Route Primary Scene is missing.");
            }

            TransitionGateMode transitionGateMode =
                ResolveRouteTransitionGateMode(startupRoute);
            if (!TryValidateActivityEntryReadinessConfiguration(
                    startupActivity,
                    transitionGateMode,
                    hasVisualCover: true,
                    out string startupReadinessIssue))
            {
                return FrameworkGameFlowStartResult.Failed(
                    "Startup Route Activity readiness configuration is invalid. " +
                    startupReadinessIssue);
            }

            ActivityEntryLoadingProgressEnvelope progressEnvelope =
                CreateActivityEntryLoadingProgressEnvelope(
                    startupActivity,
                    _routeLifecycleRuntime
                        .PreviewRouteLoadingProgressStepCount(
                            startupRoute,
                            "GameApplication",
                            "startup"),
                    progressReporter,
                    "RouteTransition",
                    "Game Application startup loading progress.");
            var progressForwarder =
                new ActivityEntryLoadingProgressForwarder(
                    this,
                    startupActivity,
                    progressEnvelope);
            ActivateActivityEntryLoadingProgressForwarder(
                progressForwarder);

            _routeRequestInFlight = true;
            TransitionOperationId operationId = default;
            bool loadingHidden = false;
            bool revealCompleted = false;
            try
            {
                operationId = CreateTransitionOperationId(
                    TransitionScope.Startup);
                var transitionGateSnapshot = ApplyTransitionGate(
                    operationId,
                    TransitionKind.RouteStartup,
                    transitionGateMode,
                    "GameApplication",
                    "startup");
                await ExecuteTransitionAsync(
                    TransitionRequest.Before(
                        operationId,
                        TransitionScope.Startup,
                        "GameApplication",
                        "startup",
                        null,
                        startupRoute,
                        null,
                        startupActivity));

                if (beforeRouteLifecycle != null)
                {
                    await beforeRouteLifecycle();
                }

                RouteLifecycleStartResult routeLifecycleResult =
                    await StartRouteCoreAsync(
                        startupRoute,
                        "GameApplication",
                        "startup",
                        progressEnvelope.TechnicalReporter);
                if (!routeLifecycleResult.Started)
                {
                    progressEnvelope.MarkTerminalFailure();
                    if (afterRouteLifecycle != null)
                    {
                        await afterRouteLifecycle();
                        loadingHidden = true;
                    }

                    await ExecuteTransitionAsync(
                        TransitionRequest.After(
                            operationId,
                            TransitionScope.Startup,
                            "GameApplication",
                            "startup",
                            null,
                            startupRoute,
                            null,
                            routeLifecycleResult.ActivityFlowResult.Activity));
                    ReleaseTransitionGate(
                        transitionGateMode,
                        transitionGateSnapshot);
                    return FrameworkGameFlowStartResult.Failed(
                        routeLifecycleResult.Message);
                }

                if (!TryPrepareActivityEntryReadinessExecution(
                        startupActivity,
                        routeLifecycleResult.ActivityFlowResult,
                        transitionGateMode,
                        requiresVisualCover: true,
                        out ActivityEntryReadinessExecutionResult
                            readinessExecution))
                {
                    progressEnvelope.MarkTerminalFailure();
                    ReleaseTransitionGate(
                        transitionGateMode,
                        transitionGateSnapshot);
                    return FrameworkGameFlowStartResult.Failed(
                        readinessExecution.Reason);
                }

                readinessExecution =
                    await WaitForPreparedActivityEntryReadinessAsync(
                        readinessExecution,
                        operationId,
                        startupRoute);

                if (readinessExecution.IsReady)
                {
                    await progressForwarder
                        .ReportCurrentReadyAndFlushAsync();
                    EnsureTerminalLoadingCompletion(
                        progressEnvelope,
                        startupActivity,
                        "Game Application startup");

                    if (afterRouteLifecycle != null)
                    {
                        await afterRouteLifecycle();
                        loadingHidden = true;
                    }

                    await ExecuteTransitionAsync(
                        TransitionRequest.After(
                            operationId,
                            TransitionScope.Startup,
                            "GameApplication",
                            "startup",
                            null,
                            startupRoute,
                            null,
                            readinessExecution.ActivityFlowResult.Activity));
                    revealCompleted = true;
                }
                else
                {
                    await progressForwarder.MarkTerminalFailureAsync();
                }

                if (_routeLifecycleRuntime.TryGetCurrentRouteResult(
                        out RouteLifecycleStartResult currentRouteResult) &&
                    currentRouteResult.Started &&
                    ReferenceEquals(
                        currentRouteResult.Route,
                        startupRoute))
                {
                    routeLifecycleResult = currentRouteResult;
                }

                ReleaseTransitionGate(
                    transitionGateMode,
                    transitionGateSnapshot);
                readinessExecution = readinessExecution.WithPresentation(
                    readinessExecution.IsReady,
                    loadingReleased: readinessExecution.IsReady &&
                        afterRouteLifecycle != null,
                    transitionGateReleased: true,
                    recoveryGateApplied: readinessExecution.IsFailure);

                if (readinessExecution.IsFailure)
                {
                    ApplyActivityEntryReadinessRecoveryGate(
                        readinessExecution,
                        "GameApplication",
                        "startup");
                    return FrameworkGameFlowStartResult
                        .FailedCommittedDestination(
                            "Game Flow Startup committed the Startup Route " +
                            "but Startup Activity entry readiness did not " +
                            "complete. " +
                            readinessExecution.ToDiagnosticString(),
                            startupRoute,
                            routeLifecycleResult,
                            readinessExecution.Status);
                }

                ReleaseActivityEntryReadinessRecoveryGate();
                SetCurrentFlowContext(routeLifecycleResult);
                return FrameworkGameFlowStartResult.StartedWith(
                    startupRoute,
                    routeLifecycleResult);
            }
            catch
            {
                await progressForwarder.MarkTerminalFailureAsync();
                throw;
            }
            finally
            {
                progressDiagnosticsSink?.Invoke(
                    progressEnvelope.CreateDiagnostics(
                        loadingHidden,
                        revealCompleted));
                DeactivateActivityEntryLoadingProgressForwarder(
                    progressForwarder);
                progressForwarder.Dispose();
                ReleaseTransitionGateIfStillActive();
                _routeRequestInFlight = false;
                CompleteActivityEntryReadinessActiveOperation(
                    operationId);
            }
        }

        internal async Task<FrameworkRouteRequestResult>
            RequestRouteWithActivityEntryLoadingProgressAsync(
                RouteAsset targetRoute,
                string source,
                string reason,
                Func<Awaitable> beforeRouteLifecycle,
                Func<Awaitable> afterRouteLifecycle,
                IFrameworkLoadingProgressReporter progressReporter,
                Action<ActivityEntryLoadingProgressDiagnostics>
                    progressDiagnosticsSink = null)
        {
            ActivityAsset startupActivity =
                targetRoute != null
                    ? targetRoute.StartupActivity
                    : null;
            if (!ShouldProjectActivityEntryReadinessProgress(
                    startupActivity,
                    progressReporter))
            {
                return await RequestRouteAsync(
                    targetRoute,
                    source,
                    reason,
                    beforeRouteLifecycle,
                    afterRouteLifecycle,
                    progressReporter);
            }

            ActivityEntryLoadingProgressEnvelope progressEnvelope =
                CreateActivityEntryLoadingProgressEnvelope(
                    startupActivity,
                    _routeLifecycleRuntime
                        .PreviewRouteLoadingProgressStepCount(
                            targetRoute,
                            source,
                            reason),
                    progressReporter,
                    "RouteTransition",
                    "Route transition loading progress.");
            var progressForwarder =
                new ActivityEntryLoadingProgressForwarder(
                    this,
                    startupActivity,
                    progressEnvelope);
            ActivateActivityEntryLoadingProgressForwarder(
                progressForwarder);
            bool loadingHidden = false;
            bool revealCompleted = false;

            async Awaitable CompleteOrFailAndHideAsync()
            {
                if (progressForwarder.HasCapturedOccurrence)
                {
                    await progressForwarder
                        .ReportCurrentReadyAndFlushAsync();
                    EnsureTerminalLoadingCompletion(
                        progressEnvelope,
                        startupActivity,
                        "Route request");
                }
                else
                {
                    await progressForwarder.MarkTerminalFailureAsync();
                }

                if (afterRouteLifecycle != null)
                {
                    await afterRouteLifecycle();
                    loadingHidden = true;
                }
            }

            try
            {
                FrameworkRouteRequestResult result =
                    await RequestRouteAsync(
                        targetRoute,
                        source,
                        reason,
                        beforeRouteLifecycle,
                        CompleteOrFailAndHideAsync,
                        progressEnvelope.TechnicalReporter);
                if (!result.Succeeded)
                {
                    await progressForwarder.MarkTerminalFailureAsync();
                }
                else
                {
                    revealCompleted = true;
                }

                return result;
            }
            catch
            {
                await progressForwarder.MarkTerminalFailureAsync();
                throw;
            }
            finally
            {
                progressDiagnosticsSink?.Invoke(
                    progressEnvelope.CreateDiagnostics(
                        loadingHidden,
                        revealCompleted));
                DeactivateActivityEntryLoadingProgressForwarder(
                    progressForwarder);
                progressForwarder.Dispose();
            }
        }

        internal async Task<FrameworkActivityRequestResult>
            RequestActivityWithActivityEntryLoadingProgressAsync(
                ActivityAsset targetActivity,
                string source,
                string reason,
                Func<Awaitable> beforeActivityLifecycle,
                Func<Awaitable> afterActivityLifecycle,
                IFrameworkLoadingProgressReporter progressReporter,
                Action<ActivityEntryLoadingProgressDiagnostics>
                    progressDiagnosticsSink = null)
        {
            if (!ShouldProjectActivityEntryReadinessProgress(
                    targetActivity,
                    progressReporter))
            {
                return await RequestActivityAsync(
                    targetActivity,
                    source,
                    reason,
                    beforeActivityLifecycle,
                    afterActivityLifecycle,
                    progressReporter);
            }

            ActivityEntryLoadingProgressEnvelope progressEnvelope =
                CreateActivityEntryLoadingProgressEnvelope(
                    targetActivity,
                    _routeLifecycleRuntime
                        .PreviewActivityLoadingProgressStepCount(
                            targetActivity,
                            source,
                            reason),
                    progressReporter,
                    "ActivityTransition",
                    "Activity transition loading progress.");
            var progressForwarder =
                new ActivityEntryLoadingProgressForwarder(
                    this,
                    targetActivity,
                    progressEnvelope);
            ActivateActivityEntryLoadingProgressForwarder(
                progressForwarder);
            bool loadingHidden = false;
            bool revealCompleted = false;

            async Awaitable CompleteOrFailAndHideAsync()
            {
                if (progressForwarder.HasCapturedOccurrence)
                {
                    await progressForwarder
                        .ReportCurrentReadyAndFlushAsync();
                    EnsureTerminalLoadingCompletion(
                        progressEnvelope,
                        targetActivity,
                        "Activity request");
                }
                else
                {
                    await progressForwarder.MarkTerminalFailureAsync();
                }

                if (afterActivityLifecycle != null)
                {
                    await afterActivityLifecycle();
                    loadingHidden = true;
                }
            }

            try
            {
                FrameworkActivityRequestResult result =
                    await RequestActivityAsync(
                        targetActivity,
                        source,
                        reason,
                        beforeActivityLifecycle,
                        CompleteOrFailAndHideAsync,
                        progressEnvelope.TechnicalReporter);
                if (!result.Succeeded)
                {
                    await progressForwarder.MarkTerminalFailureAsync();
                }
                else
                {
                    revealCompleted = true;
                }

                return result;
            }
            catch
            {
                await progressForwarder.MarkTerminalFailureAsync();
                throw;
            }
            finally
            {
                progressDiagnosticsSink?.Invoke(
                    progressEnvelope.CreateDiagnostics(
                        loadingHidden,
                        revealCompleted));
                DeactivateActivityEntryLoadingProgressForwarder(
                    progressForwarder);
                progressForwarder.Dispose();
            }
        }

        private void ActivateActivityEntryLoadingProgressForwarder(
            ActivityEntryLoadingProgressForwarder forwarder)
        {
            if (forwarder == null)
            {
                throw new ArgumentNullException(nameof(forwarder));
            }

            if (_activeActivityEntryLoadingProgressForwarder != null)
            {
                throw new InvalidOperationException(
                    "An Activity entry Loading progress forwarder is already " +
                    "active for another lifecycle operation.");
            }

            _activeActivityEntryLoadingProgressForwarder = forwarder;
        }

        private void DeactivateActivityEntryLoadingProgressForwarder(
            ActivityEntryLoadingProgressForwarder forwarder)
        {
            if (ReferenceEquals(
                    _activeActivityEntryLoadingProgressForwarder,
                    forwarder))
            {
                _activeActivityEntryLoadingProgressForwarder = null;
            }
        }

        private async Task ReportInitialActivityEntryLoadingProgressAsync(
            ActivityEntryReadinessExecutionResult prepared)
        {
            ActivityEntryLoadingProgressForwarder forwarder =
                _activeActivityEntryLoadingProgressForwarder;
            if (forwarder == null ||
                !prepared.RequiresWait ||
                prepared.Status !=
                ActivityEntryReadinessExecutionStatus.Unknown ||
                !prepared.Occurrence.IsValid)
            {
                return;
            }

            await forwarder.ReportInitialAndFlushAsync(
                prepared.Occurrence);
        }

        private static bool ShouldProjectActivityEntryReadinessProgress(
            ActivityAsset activity,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return activity != null &&
                   activity.EntryReadinessPolicy ==
                   ActivityEntryReadinessPolicy.WaitCovered &&
                   progressReporter != null &&
                   !ReferenceEquals(
                       progressReporter,
                       NoOpFrameworkLoadingProgressReporter.Instance);
        }

        private static ActivityEntryLoadingProgressEnvelope
            CreateActivityEntryLoadingProgressEnvelope(
                ActivityAsset activity,
                int technicalStepCount,
                IFrameworkLoadingProgressReporter progressReporter,
                string technicalPhase,
                string technicalMessage)
        {
            if (activity == null ||
                activity.EntryReadinessPolicy !=
                ActivityEntryReadinessPolicy.WaitCovered)
            {
                throw new InvalidOperationException(
                    "Participant-aware Loading progress requires a " +
                    "WaitCovered Activity.");
            }

            ActivityEntryLoadingProgressPlan plan =
                ActivityEntryLoadingProgressPlan.Create(
                    technicalStepCount,
                    reserveReadinessPhase: true);
            return new ActivityEntryLoadingProgressEnvelope(
                progressReporter,
                plan,
                technicalPhase,
                technicalMessage);
        }

        private static void EnsureTerminalLoadingCompletion(
            ActivityEntryLoadingProgressEnvelope progressEnvelope,
            ActivityAsset activity,
            string operationName)
        {
            if (progressEnvelope != null &&
                progressEnvelope.TerminalCompletionIssued)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{operationName} cannot release Loading because " +
                $"Activity '{(activity != null ? activity.ActivityName : string.Empty)}' " +
                "did not publish terminal participant-aware readiness progress.");
        }

        private bool TryCreateCurrentReadinessProgressSnapshot(
            ActivityAsset targetActivity,
            out ActivityReadinessProgressSnapshot snapshot)
        {
            snapshot = default;
            if (targetActivity == null ||
                !_routeLifecycleRuntime.TryGetCurrentRouteResult(
                    out RouteLifecycleStartResult currentRouteResult))
            {
                return false;
            }

            ActivityReadinessOccurrence occurrence =
                _routeLifecycleRuntime.CurrentOccurrence;
            ActivityReadinessState readinessState =
                currentRouteResult.ActivityFlowResult
                    .ActivityReadinessState;
            if (!occurrence.Matches(
                    targetActivity,
                    occurrence.TransitionSequence) ||
                !readinessState.HasActivity ||
                !ReferenceEquals(
                    readinessState.Activity,
                    targetActivity))
            {
                return false;
            }

            snapshot = ActivityReadinessProgressSnapshot.Create(
                occurrence,
                readinessState);
            return true;
        }

        private sealed class ActivityEntryLoadingProgressForwarder :
            IDisposable
        {
            private readonly GameFlowRuntime _owner;
            private readonly ActivityAsset _targetActivity;
            private readonly ActivityEntryLoadingProgressEnvelope _envelope;
            private readonly IEventBinding _binding;
            private ActivityReadinessOccurrence _capturedOccurrence;
            private bool _disposed;

            internal ActivityEntryLoadingProgressForwarder(
                GameFlowRuntime owner,
                ActivityAsset targetActivity,
                ActivityEntryLoadingProgressEnvelope envelope)
            {
                _owner = owner ??
                    throw new ArgumentNullException(nameof(owner));
                _targetActivity = targetActivity ??
                    throw new ArgumentNullException(nameof(targetActivity));
                _envelope = envelope ??
                    throw new ArgumentNullException(nameof(envelope));
                _binding = owner._routeLifecycleRuntime
                    .SubscribeActivityReadinessUpdates(
                        HandleReadinessUpdate);
            }

            internal bool HasCapturedOccurrence =>
                _capturedOccurrence.IsValid;

            internal async Task ReportInitialAndFlushAsync(
                ActivityReadinessOccurrence expectedOccurrence)
            {
                if (!expectedOccurrence.IsValid ||
                    !_owner.TryCreateCurrentReadinessProgressSnapshot(
                        _targetActivity,
                        out ActivityReadinessProgressSnapshot snapshot) ||
                    !snapshot.Occurrence.Matches(
                        expectedOccurrence.Activity,
                        expectedOccurrence.TransitionSequence))
                {
                    throw new InvalidOperationException(
                        "The authoritative initial Activity readiness snapshot " +
                        "does not match the captured Loading occurrence.");
                }

                EnsureTechnicalRangeCompleted();
                if (snapshot.IsReady)
                {
                    _capturedOccurrence = expectedOccurrence;
                    return;
                }

                Task initialReport =
                    _envelope.QueueReadinessAsync(snapshot);
                _capturedOccurrence = expectedOccurrence;
                await initialReport;
                await _envelope.FlushQueuedReportsAsync();
            }

            internal async Task ReportCurrentReadyAndFlushAsync()
            {
                if (!_capturedOccurrence.IsValid ||
                    !_owner.TryCreateCurrentReadinessProgressSnapshot(
                        _targetActivity,
                        out ActivityReadinessProgressSnapshot snapshot) ||
                    !snapshot.Occurrence.Matches(
                        _capturedOccurrence.Activity,
                        _capturedOccurrence.TransitionSequence) ||
                    !snapshot.IsReady)
                {
                    throw new InvalidOperationException(
                        "The authoritative Activity readiness snapshot is not " +
                        "Ready at the Loading release boundary.");
                }

                await _envelope.QueueReadinessAsync(snapshot);
                await _envelope.FlushQueuedReportsAsync();
            }

            internal async Task MarkTerminalFailureAsync()
            {
                await _envelope.FlushQueuedReportsAsync();
                _envelope.MarkTerminalFailure();
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _binding?.Dispose();
            }

            private void HandleReadinessUpdate(
                ActivityReadinessUpdate update)
            {
                if (_disposed || update == null || !update.IsValid ||
                    !_capturedOccurrence.IsValid ||
                    !ReferenceEquals(
                        update.Activity,
                        _targetActivity) ||
                    !update.Occurrence.Matches(
                        _capturedOccurrence.Activity,
                        _capturedOccurrence.TransitionSequence) ||
                    !CanAcceptReadinessUpdate())
                {
                    return;
                }

                ActivityReadinessProgressSnapshot snapshot =
                    ActivityReadinessProgressSnapshot.Create(
                        update.Occurrence,
                        update.ReadinessState);
                if (snapshot.IsReady)
                {
                    return;
                }

                _ = _envelope.QueueReadinessAsync(snapshot);
            }

            private bool CanAcceptReadinessUpdate()
            {
                return _envelope.Plan.TechnicalStepCount == 0 ||
                       (_envelope.HasDeterminateProgress &&
                        _envelope.LastAcceptedProgress01 >=
                        _envelope.Plan.TechnicalRange.End01);
            }

            private void EnsureTechnicalRangeCompleted()
            {
                if (CanAcceptReadinessUpdate())
                {
                    return;
                }

                throw new InvalidOperationException(
                    "Activity readiness progress cannot begin before the " +
                    "technical Loading range reaches its reserved boundary.");
            }
        }
    }
}
