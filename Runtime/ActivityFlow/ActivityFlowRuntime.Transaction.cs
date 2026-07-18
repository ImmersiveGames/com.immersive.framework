using System;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Loading;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityFlowRuntime
    {
        private sealed class ActivityParticipantTransitionContext
        {
            internal ActivityParticipantTransitionContext(
                ActivityAsset previousActivity,
                ActivityAsset nextActivity,
                ActivityContentExecutionParticipantSourceResult participantSourceResult,
                ActivityContentExecutionParticipantCollection participants,
                string source,
                string reason)
            {
                PreviousActivity = previousActivity;
                NextActivity = nextActivity;
                ParticipantSourceResult = participantSourceResult;
                Participants = participants;
                Source = source;
                Reason = reason;
            }

            internal ActivityAsset PreviousActivity { get; }

            internal ActivityAsset NextActivity { get; }

            internal ActivityContentExecutionParticipantSourceResult
                ParticipantSourceResult { get; }

            internal ActivityContentExecutionParticipantCollection Participants { get; }

            internal string Source { get; }

            internal string Reason { get; }

            internal ActivityContentExecutionPhasePlan ExitPlan { get; set; }

            internal ActivityContentExecutionAggregateResult ExitResult { get; set; }

            internal ActivityContentExecutionPhasePlan EnterPlan { get; set; }

            internal ActivityContentExecutionAggregateResult EnterResult { get; set; }

            internal bool ExitExecuted { get; set; }

            internal bool EnterExecuted { get; set; }
        }

        private int _activityTransitionSequence;
        private ActivityTransitionRuntimeTransaction _activeActivityTransition;
        private ActivityTransitionSnapshot _lastActivityTransitionSnapshot;

        internal ActivityTransitionSnapshot CurrentActivityTransitionSnapshot =>
            _activeActivityTransition != null
                ? _activeActivityTransition.Snapshot
                : _lastActivityTransitionSnapshot;

        private async Task<ActivityFlowStartResult>
            ExecuteActivityTransitionCoreAsync(
                ActivityAsset nextActivity,
                ActivityAsset previousActivity,
                string source,
                string reason,
                ActivityOperationResult activityOperationResult,
                IFrameworkLoadingProgressReporter progressReporter,
                Func<ActivityActivationGateResult> beforeActivation)
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
                    _currentActivityState = ActivityRuntimeState.ActiveWith(
                        nextActivity,
                        previousActivity,
                        resolvedSource,
                        resolvedReason);
                }

                return ActivityFlowStartResult.KeptCurrentActivity(
                    _currentActivityState);
            }

            if (!TryBeginActivityTransition(
                    previousActivity,
                    nextActivity,
                    resolvedSource,
                    resolvedReason,
                    out ActivityTransitionRuntimeTransaction transaction,
                    out string beginIssue))
            {
                return ActivityFlowStartResult.Failed(beginIssue);
            }

            RuntimeScopeLifecycleResult runtimeEnterResult = default;
            ActivitySceneCompositionResult sceneCompositionResult = default;
            ActivityContentApplyResult contentResult = default;
            ActivityContentAnchorDiscoveryResult anchorDiscoveryResult = default;
            ActivityContentExecutionLifecycleResult executionResult = default;
            ActivitySceneReleaseResult sceneReleaseResult = default;
            ActivityContentRuntime.ActivityContentTransitionContext
                contentTransition = null;
            ActivityParticipantTransitionContext participantTransition = null;
            IFrameworkLoadingProgressReporter resolvedProgressReporter =
                progressReporter ?? NoOpFrameworkLoadingProgressReporter.Instance;

            try
            {
                runtimeEnterResult = CreateActivityScopeRoot(
                    nextActivity,
                    resolvedSource,
                    resolvedReason);
                if (runtimeEnterResult.Rejected || !runtimeEnterResult.HasContext)
                {
                    return await FailBeforeCommitAsync(
                        transaction,
                        nextActivity,
                        previousActivity,
                        resolvedSource,
                        resolvedReason,
                        "Target Activity runtime scope could not be prepared. " +
                        runtimeEnterResult.ToDiagnosticString(),
                        activityOperationResult);
                }

                ActivityOperationResult operationForProgress =
                    ResolveActivityOperationForProgress(
                        activityOperationResult,
                        previousActivity,
                        nextActivity,
                        resolvedSource,
                        resolvedReason);
                int loadProgressCount = CountActivityOperationSceneSideEffects(
                    operationForProgress,
                    ActivityOperationSceneAction.Load);
                int releaseProgressCount = CountActivityOperationSceneSideEffects(
                    operationForProgress,
                    ActivityOperationSceneAction.Release);
                int totalProgressCount =
                    loadProgressCount + releaseProgressCount;
                IFrameworkLoadingProgressReporter compositionProgressReporter =
                    FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                        resolvedProgressReporter,
                        0,
                        loadProgressCount,
                        totalProgressCount,
                        "ActivityTransition",
                        "Activity transition loading progress.");

                sceneCompositionResult =
                    await ExecuteActivitySceneCompositionAsync(
                        nextActivity,
                        resolvedSource,
                        resolvedReason,
                        compositionProgressReporter);
                if (sceneCompositionResult.HasBlockingIssues)
                {
                    return await FailBeforeCommitAsync(
                        transaction,
                        nextActivity,
                        previousActivity,
                        resolvedSource,
                        resolvedReason,
                        sceneCompositionResult.ToDiagnosticString(),
                        activityOperationResult);
                }

                ActivityActivationGateResult activationGate =
                    beforeActivation != null
                        ? beforeActivation()
                        : ActivityActivationGateResult.Allowed(
                            resolvedSource,
                            resolvedReason,
                            "Activity activation gate approved target authority commit.");
                if (!activationGate.CanActivate)
                {
                    return await FailBeforeCommitAsync(
                        transaction,
                        nextActivity,
                        previousActivity,
                        resolvedSource,
                        resolvedReason,
                        "Activity activation gate blocked target Activity authority commit. " +
                        activationGate.ToDiagnosticString(),
                        activityOperationResult);
                }

                transaction.MarkReadyToCommit(
                    "Target Activity scenes, runtime scope and activation requirements are prepared.");
                _currentActivityState = ActivityRuntimeState.ActiveWith(
                    nextActivity,
                    previousActivity,
                    resolvedSource,
                    resolvedReason);
                transaction.Commit(
                    "Target Activity became the current Activity authority.");

                ConfigureActivityContentTransitionScope(
                    previousActivity,
                    nextActivity);
                contentTransition =
                    _activityContentRuntime.PrepareActivityContentTransition(
                        previousActivity,
                        nextActivity,
                        resolvedSource,
                        resolvedReason);
                participantTransition = PrepareActivityParticipantTransition(
                    previousActivity,
                    nextActivity,
                    resolvedSource,
                    resolvedReason);

                transaction.BeginPreviousExit(
                    "Previous Activity content and participants started exit.");
                _activityContentRuntime.ExitPreviousActivityContent(
                    contentTransition);
                transaction.MarkPreviousContentExited(
                    "All previous scene-content Exit callbacks completed before participant Exit.");

                ExecuteActivityParticipantExit(participantTransition);
                transaction.MarkPreviousParticipantsExited(
                    "All previous Activity participants completed Exit before target Enter.");
                PublishActivityExitedFact(
                    previousActivity,
                    nextActivity,
                    resolvedSource,
                    resolvedReason);

                transaction.BeginTargetEnter(
                    "Target Activity requirements and participants started enter.");
                anchorDiscoveryResult =
                    _contentAnchorDiscoveryRuntime.DiscoverActivityAnchors(
                        nextActivity,
                        _currentRoute,
                        _activitySceneCompositionRuntime
                            .CreateActivityContentDiscoveryScope(nextActivity),
                        resolvedSource,
                        resolvedReason);

                ExecuteActivityParticipantEnter(participantTransition);
                transaction.MarkTargetParticipantsEntered(
                    "Target Activity participants entered before scene content.");

                _activityContentRuntime.EnterTargetActivityContent(
                    contentTransition);
                transaction.MarkTargetContentEntered(
                    "Target scene-content Enter callbacks completed after participant Enter.");
                PublishActivityEnteredFact(
                    nextActivity,
                    previousActivity,
                    resolvedSource,
                    resolvedReason);

                contentResult =
                    _activityContentRuntime.CompleteActivityContentTransition(
                        contentTransition);
                executionResult = CompleteActivityParticipantTransition(
                    participantTransition);
                bool previousExitSucceeded = PreviousExitSucceeded(
                    previousActivity,
                    contentResult,
                    executionResult);

                transaction.BeginPreviousFinalization(
                    "Previous Activity bindings and runtime scope started finalization.");
                FrameworkScopeTailOperationRequest activityScopeTailRequest =
                    new FrameworkScopeTailOperationRequest(
                        runtimeEnterResult.Owner,
                        previousActivity != null
                            ? CreateActivityOwner(previousActivity)
                            : default,
                        runtimeEnterResult.EnterRootResult,
                        runtimeEnterResult.Context,
                        _runtimeContentRuntime.RootCount,
                        resolvedSource,
                        resolvedReason,
                        () => _runtimeContentRuntime.RootCount);
                var activityScopeTailResult =
                    FrameworkScopeTailOperationExecutor.Execute(
                        activityScopeTailRequest,
                        cleanupRequest =>
                            CleanupPreviousActivityContentAnchorBindings(
                                previousActivity,
                                nextActivity,
                                cleanupRequest.Source,
                                cleanupRequest.Reason),
                        removeRequest => RemovePreviousActivityScopeRoot(
                            previousActivity,
                            nextActivity,
                            removeRequest.Source,
                            removeRequest.Reason));
                bool scopeFinalizationSucceeded =
                    PreviousScopeFinalizationSucceeded(
                        previousActivity,
                        activityScopeTailResult.ScopeResult,
                        activityScopeTailResult.BindingCleanupResult);
                bool previousFinalizationSucceeded =
                    previousExitSucceeded && scopeFinalizationSucceeded;
                transaction.MarkPreviousFinalized(
                    previousFinalizationSucceeded,
                    previousFinalizationSucceeded
                        ? "Previous Activity exit, bindings and runtime scope finalized."
                        : "Previous Activity exit, bindings or runtime scope remain unresolved.");

                IFrameworkLoadingProgressReporter releaseProgressReporter =
                    FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                        resolvedProgressReporter,
                        loadProgressCount,
                        releaseProgressCount,
                        totalProgressCount,
                        "ActivityTransition",
                        "Activity transition loading progress.");
                sceneReleaseResult = await ReleasePreviousActivityScenesAsync(
                    previousActivity,
                    resolvedSource,
                    resolvedReason,
                    releaseProgressReporter);
                bool sceneReleaseSucceeded =
                    !sceneReleaseResult.HasBlockingIssues;
                transaction.MarkPreviousScenesReleased(
                    sceneReleaseSucceeded,
                    sceneReleaseSucceeded
                        ? "Previous Activity scene release completed."
                        : sceneReleaseResult.ToDiagnosticString());

                await FrameworkLoadingProgressReporterUtility
                    .ReportCompletedIfAnyAsync(
                        resolvedProgressReporter,
                        "ActivityTransition",
                        "Activity transition loading progress completed.");

                ActivityReadinessState readiness = BuildTransitionReadiness(
                    _currentActivityState,
                    contentResult,
                    executionResult,
                    targetReadinessBlocked: false,
                    resolvedSource,
                    resolvedReason);
                ActivityTransitionSnapshot snapshot = transaction.Complete(
                    readiness,
                    previousFinalizationSucceeded,
                    sceneReleaseSucceeded,
                    "Activity transition reached a terminal committed result.");
                FinishActivityTransition(transaction);

                return ActivityFlowStartResult.StartedWith(
                        _currentActivityState,
                        previousActivity,
                        contentResult,
                        activityScopeTailResult.ScopeResult,
                        activityScopeTailResult.BindingCleanupResult,
                        anchorDiscoveryResult,
                        executionResult,
                        sceneCompositionResult,
                        sceneReleaseResult,
                        activityOperationResult,
                        CreateActivitySceneLedgerSnapshot())
                    .WithActivityTransition(snapshot);
            }
            catch (Exception exception)
            {
                return await HandleTransitionExceptionAsync(
                    transaction,
                    nextActivity,
                    previousActivity,
                    resolvedSource,
                    resolvedReason,
                    exception,
                    runtimeEnterResult,
                    contentTransition,
                    participantTransition,
                    sceneCompositionResult,
                    sceneReleaseResult,
                    activityOperationResult,
                    resolvedProgressReporter);
            }
        }

        private async Task<ActivityFlowStartResult>
            ExecuteActivityClearTransitionAsync(
                ActivityAsset previousActivity,
                string source,
                string reason,
                IFrameworkLoadingProgressReporter progressReporter,
                ActivityOperationResult activityOperationResult,
                bool skippedNoStartupActivity)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);
            if (!skippedNoStartupActivity && previousActivity == null)
            {
                return ActivityFlowStartResult.Failed(
                    "Activity Flow cannot clear Activity because no Activity is active.");
            }

            if (!TryBeginActivityTransition(
                    previousActivity,
                    null,
                    resolvedSource,
                    resolvedReason,
                    out ActivityTransitionRuntimeTransaction transaction,
                    out string beginIssue))
            {
                return ActivityFlowStartResult.Failed(beginIssue);
            }

            ActivityContentRuntime.ActivityContentTransitionContext
                contentTransition = null;
            ActivityParticipantTransitionContext participantTransition = null;
            ActivityContentApplyResult contentResult = default;
            ActivityContentExecutionLifecycleResult executionResult = default;
            ActivitySceneReleaseResult sceneReleaseResult = default;
            IFrameworkLoadingProgressReporter resolvedProgressReporter =
                progressReporter ?? NoOpFrameworkLoadingProgressReporter.Instance;

            try
            {
                transaction.MarkReadyToCommit(
                    "No target Activity requirements remain before clearing authority.");
                _currentActivityState = ActivityRuntimeState.None(
                    previousActivity,
                    resolvedSource,
                    resolvedReason);
                transaction.Commit(
                    "Activity authority committed to the explicit no-active-Activity state.");

                ConfigureActivityContentTransitionScope(
                    previousActivity,
                    null);
                contentTransition =
                    _activityContentRuntime.PrepareActivityContentTransition(
                        previousActivity,
                        null,
                        resolvedSource,
                        resolvedReason);
                participantTransition = PrepareActivityParticipantTransition(
                    previousActivity,
                    null,
                    resolvedSource,
                    resolvedReason);

                transaction.BeginPreviousExit(
                    "Previous Activity clear exit started.");
                _activityContentRuntime.ExitPreviousActivityContent(
                    contentTransition);
                transaction.MarkPreviousContentExited(
                    "Previous scene content exited before participant release.");
                ExecuteActivityParticipantExit(participantTransition);
                transaction.MarkPreviousParticipantsExited(
                    "Previous Activity participants completed release.");
                PublishActivityExitedFact(
                    previousActivity,
                    null,
                    resolvedSource,
                    resolvedReason);

                transaction.BeginTargetEnter(
                    "No target Activity enter is required; inactive content state is being enforced.");
                ExecuteActivityParticipantEnter(participantTransition);
                transaction.MarkTargetParticipantsEntered(
                    "No target participants were required.");
                _activityContentRuntime.EnterTargetActivityContent(
                    contentTransition);
                transaction.MarkTargetContentEntered(
                    "No-active-Activity scene content state was enforced.");

                contentResult =
                    _activityContentRuntime.CompleteActivityContentTransition(
                        contentTransition);
                executionResult = CompleteActivityParticipantTransition(
                    participantTransition);
                bool previousExitSucceeded = PreviousExitSucceeded(
                    previousActivity,
                    contentResult,
                    executionResult);

                transaction.BeginPreviousFinalization(
                    "Previous Activity clear finalization started.");
                RuntimeScopeLifecycleResult scopeLifecycleResult;
                ContentAnchorBindingLifecycleResult bindingCleanupResult;
                bool scopeFinalizationSucceeded;
                if (previousActivity == null)
                {
                    scopeLifecycleResult = RuntimeScopeLifecycleResult.None(
                        RuntimeContentScope.Activity,
                        resolvedSource,
                        resolvedReason);
                    bindingCleanupResult = default;
                    scopeFinalizationSucceeded = true;
                }
                else
                {
                    FrameworkScopeTailOperationRequest activityScopeTailRequest =
                        new FrameworkScopeTailOperationRequest(
                            default,
                            CreateActivityOwner(previousActivity),
                            null,
                            default,
                            _runtimeContentRuntime.RootCount,
                            resolvedSource,
                            resolvedReason,
                            () => _runtimeContentRuntime.RootCount);
                    var activityScopeTailResult =
                        FrameworkScopeTailOperationExecutor.Execute(
                            activityScopeTailRequest,
                            cleanupRequest =>
                                CleanupPreviousActivityContentAnchorBindings(
                                    previousActivity,
                                    null,
                                    cleanupRequest.Source,
                                    cleanupRequest.Reason),
                            removeRequest => RemovePreviousActivityScopeRoot(
                                previousActivity,
                                null,
                                removeRequest.Source,
                                removeRequest.Reason));
                    scopeLifecycleResult = activityScopeTailResult.ScopeResult;
                    bindingCleanupResult =
                        activityScopeTailResult.BindingCleanupResult;
                    scopeFinalizationSucceeded =
                        PreviousScopeFinalizationSucceeded(
                            previousActivity,
                            scopeLifecycleResult,
                            bindingCleanupResult);
                }
                bool previousFinalizationSucceeded =
                    previousExitSucceeded && scopeFinalizationSucceeded;
                transaction.MarkPreviousFinalized(
                    previousFinalizationSucceeded,
                    previousFinalizationSucceeded
                        ? "Previous Activity clear exit and scope finalized."
                        : "Previous Activity clear exit or scope finalization failed.");

                bool sceneReleaseSucceeded;
                if (previousActivity == null)
                {
                    sceneReleaseResult = default;
                    sceneReleaseSucceeded = true;
                }
                else
                {
                    int releaseCount =
                        PreviewActivitySceneReleaseForActivityChangeCount(
                            previousActivity);
                    IFrameworkLoadingProgressReporter releaseProgressReporter =
                        FrameworkLoadingProgressReporterUtility.CreateWeightedRangeReporter(
                            resolvedProgressReporter,
                            0,
                            releaseCount,
                            releaseCount,
                            "ActivityTransition",
                            "Activity transition loading progress.");
                    sceneReleaseResult = await ReleasePreviousActivityScenesAsync(
                        previousActivity,
                        resolvedSource,
                        resolvedReason,
                        releaseProgressReporter);
                    sceneReleaseSucceeded =
                        !sceneReleaseResult.HasBlockingIssues;
                }
                transaction.MarkPreviousScenesReleased(
                    sceneReleaseSucceeded,
                    sceneReleaseSucceeded
                        ? "Previous Activity clear scene release completed."
                        : sceneReleaseResult.ToDiagnosticString());

                await FrameworkLoadingProgressReporterUtility
                    .ReportCompletedIfAnyAsync(
                        resolvedProgressReporter,
                        "ActivityTransition",
                        "Activity transition loading progress completed.");

                ActivityReadinessState readiness = BuildTransitionReadiness(
                    _currentActivityState,
                    contentResult,
                    executionResult,
                    targetReadinessBlocked: false,
                    resolvedSource,
                    resolvedReason);
                ActivityTransitionSnapshot snapshot = transaction.Complete(
                    readiness,
                    previousFinalizationSucceeded,
                    sceneReleaseSucceeded,
                    skippedNoStartupActivity
                        ? "Route has no Startup Activity; no-active-Activity state committed."
                        : "Activity clear transition completed.");
                FinishActivityTransition(transaction);

                ActivitySceneCompositionResult sceneCompositionResult =
                    CreateActivitySceneCompositionResult(
                        null,
                        resolvedSource,
                        resolvedReason);
                ActivityContentAnchorDiscoveryResult emptyDiscovery =
                    ActivityContentAnchorDiscoveryResult.Empty(
                        null,
                        resolvedSource,
                        resolvedReason,
                        skippedNoStartupActivity
                            ? "No startup Activity is active; Activity Content Anchor discovery was skipped."
                            : "Activity was cleared; Activity Content Anchor discovery was skipped.");

                ActivityFlowStartResult result = skippedNoStartupActivity
                    ? ActivityFlowStartResult.SkippedNoStartupActivity(
                        _currentActivityState,
                        previousActivity,
                        contentResult,
                        scopeLifecycleResult,
                        bindingCleanupResult,
                        emptyDiscovery,
                        executionResult,
                        sceneCompositionResult,
                        sceneReleaseResult,
                        activityOperationResult,
                        CreateActivitySceneLedgerSnapshot())
                    : ActivityFlowStartResult.ClearedByRequest(
                        _currentActivityState,
                        previousActivity,
                        contentResult,
                        scopeLifecycleResult,
                        bindingCleanupResult,
                        emptyDiscovery,
                        executionResult,
                        sceneCompositionResult,
                        sceneReleaseResult,
                        activityOperationResult,
                        CreateActivitySceneLedgerSnapshot());
                return result.WithActivityTransition(snapshot);
            }
            catch (Exception exception)
            {
                ActivityReadinessState readiness = BuildTransitionReadiness(
                    _currentActivityState,
                    ActivityContentApplyResult.Empty(null),
                    CompleteActivityParticipantTransition(participantTransition),
                    targetReadinessBlocked: false,
                    resolvedSource,
                    resolvedReason);
                ActivityTransitionSnapshot snapshot = transaction.CommitReached
                    ? transaction.FailCommittedException(
                        readiness,
                        $"Activity clear transition threw '{exception.GetType().Name}': {exception.Message}")
                    : transaction.FailBeforeCommit(
                        $"Activity clear transition threw before commit '{exception.GetType().Name}': {exception.Message}");
                FinishActivityTransition(transaction);
                await FrameworkLoadingProgressReporterUtility
                    .ReportCompletedIfAnyAsync(
                        resolvedProgressReporter,
                        "ActivityTransition",
                        "Activity transition ended after an exception.");

                if (!transaction.CommitReached)
                {
                    return ActivityFlowStartResult.Failed(
                            "Activity clear transition failed before commit. " +
                            snapshot.ToDiagnosticString())
                        .WithActivityTransition(snapshot);
                }

                ActivityFlowStartResult committedResult =
                    skippedNoStartupActivity
                        ? ActivityFlowStartResult.SkippedNoStartupActivity(
                            _currentActivityState,
                            previousActivity,
                            ActivityContentApplyResult.Empty(null))
                        : ActivityFlowStartResult.ClearedByRequest(
                            _currentActivityState,
                            previousActivity,
                            ActivityContentApplyResult.Empty(null));
                return committedResult.WithActivityTransition(snapshot);
            }
        }

        private async Task<ActivityFlowStartResult> FailBeforeCommitAsync(
            ActivityTransitionRuntimeTransaction transaction,
            ActivityAsset targetActivity,
            ActivityAsset previousActivity,
            string source,
            string reason,
            string issue,
            ActivityOperationResult activityOperationResult)
        {
            string compensationDiagnostic = string.Empty;
            try
            {
                compensationDiagnostic +=
                    await RollbackPreparedTargetScenesAsync(
                        targetActivity,
                        source,
                        "activity-transition-failed-before-commit");
            }
            catch (Exception rollbackException)
            {
                compensationDiagnostic +=
                    $" Target scene compensation threw '{rollbackException.GetType().Name}': " +
                    rollbackException.Message;
            }

            try
            {
                RuntimeRootRegistryOperationResult targetScopeRollback =
                    RemovePreviousActivityScopeRoot(
                        targetActivity,
                        previousActivity,
                        source,
                        "activity-transition-target-scope-rollback");
                if (targetScopeRollback is { Rejected: true })
                {
                    compensationDiagnostic +=
                        " Target scope compensation was rejected. " +
                        targetScopeRollback.ToDiagnosticString();
                }
            }
            catch (Exception rollbackException)
            {
                compensationDiagnostic +=
                    $" Target scope compensation threw '{rollbackException.GetType().Name}': " +
                    rollbackException.Message;
            }

            _currentActivityState = previousActivity != null
                ? ActivityRuntimeState.ActiveWith(
                    previousActivity,
                    targetActivity,
                    source,
                    reason)
                : ActivityRuntimeState.None(
                    targetActivity,
                    source,
                    reason);
            string diagnostic = issue + compensationDiagnostic;
            ActivityTransitionSnapshot snapshot =
                transaction.FailBeforeCommit(diagnostic);
            FinishActivityTransition(transaction);
            return ActivityFlowStartResult.Failed(
                    diagnostic,
                    activityOperationResult)
                .WithActivityTransition(snapshot);
        }

        private async Task<ActivityFlowStartResult>
            HandleTransitionExceptionAsync(
                ActivityTransitionRuntimeTransaction transaction,
                ActivityAsset targetActivity,
                ActivityAsset previousActivity,
                string source,
                string reason,
                Exception exception,
                RuntimeScopeLifecycleResult runtimeEnterResult,
                ActivityContentRuntime.ActivityContentTransitionContext
                    contentTransition,
                ActivityParticipantTransitionContext participantTransition,
                ActivitySceneCompositionResult sceneCompositionResult,
                ActivitySceneReleaseResult sceneReleaseResult,
                ActivityOperationResult activityOperationResult,
                IFrameworkLoadingProgressReporter progressReporter)
        {
            string diagnostic =
                $"Activity transition threw '{exception.GetType().Name}': {exception.Message}";
            if (!transaction.CommitReached)
            {
                return await FailBeforeCommitAsync(
                    transaction,
                    targetActivity,
                    previousActivity,
                    source,
                    reason,
                    diagnostic,
                    activityOperationResult);
            }

            ActivityContentApplyResult contentResult =
                contentTransition != null &&
                contentTransition.ExitExecuted &&
                contentTransition.EnterExecuted
                    ? _activityContentRuntime
                        .CompleteActivityContentTransition(contentTransition)
                    : ActivityContentApplyResult.Empty(targetActivity);
            ActivityContentExecutionLifecycleResult executionResult =
                CompleteActivityParticipantTransition(participantTransition);
            ActivityTransitionSnapshot currentSnapshot = transaction.Snapshot;
            bool targetReadinessBlocked = targetActivity != null &&
                (!currentSnapshot.TargetParticipantsEntered ||
                 !currentSnapshot.TargetContentEntered);
            ActivityReadinessState readiness = BuildTransitionReadiness(
                _currentActivityState,
                contentResult,
                executionResult,
                targetReadinessBlocked,
                source,
                reason);
            ActivityTransitionSnapshot snapshot =
                transaction.FailCommittedException(readiness, diagnostic);
            FinishActivityTransition(transaction);
            await FrameworkLoadingProgressReporterUtility
                .ReportCompletedIfAnyAsync(
                    progressReporter,
                    "ActivityTransition",
                    "Activity transition ended after a committed exception.");

            return ActivityFlowStartResult.StartedWith(
                    _currentActivityState,
                    previousActivity,
                    contentResult,
                    runtimeEnterResult,
                    default(ContentAnchorBindingLifecycleResult),
                    default(ActivityContentAnchorDiscoveryResult),
                    executionResult,
                    sceneCompositionResult,
                    sceneReleaseResult,
                    activityOperationResult,
                    CreateActivitySceneLedgerSnapshot())
                .WithActivityTransition(snapshot);
        }

        private void ConfigureActivityContentTransitionScope(
            ActivityAsset previousActivity,
            ActivityAsset nextActivity)
        {
            ActivityContentDiscoveryScope discoveryScope =
                _activitySceneCompositionRuntime
                    .CreateActivityContentDiscoveryScope(
                        previousActivity,
                        nextActivity);
            _activityContentRuntime.SetRouteScope(_currentRoute);
            _activityContentRuntime.SetDiscoveryScope(discoveryScope);
            _activityContentRuntime.ClearLastApplyResult();
        }

        private ActivityParticipantTransitionContext
            PrepareActivityParticipantTransition(
                ActivityAsset previousActivity,
                ActivityAsset nextActivity,
                string source,
                string reason)
        {
            if (previousActivity == null && nextActivity == null)
            {
                return new ActivityParticipantTransitionContext(
                    null,
                    null,
                    ActivityContentExecutionParticipantSourceResult.None(
                        source,
                        reason,
                        "No Activity transition participants are required."),
                    ActivityContentExecutionParticipantCollection.Empty(),
                    source,
                    reason);
            }

            ActivityContentExecutionParticipantSourceRequest request =
                new ActivityContentExecutionParticipantSourceRequest(
                    _currentRoute,
                    previousActivity,
                    nextActivity,
                    source,
                    reason);
            ActivityContentExecutionParticipantSourceResult sourceResult =
                ResolveActivityContentExecutionParticipants(request);
            return new ActivityParticipantTransitionContext(
                previousActivity,
                nextActivity,
                sourceResult,
                sourceResult.Collection,
                source,
                reason);
        }

        private void ExecuteActivityParticipantExit(
            ActivityParticipantTransitionContext context)
        {
            if (context == null)
            {
                return;
            }

            if (context.ExitExecuted)
            {
                throw new InvalidOperationException(
                    "Activity participant Exit phase already executed.");
            }

            if (context.PreviousActivity != null &&
                !ReferenceEquals(
                    context.PreviousActivity,
                    context.NextActivity))
            {
                if (TryCreateActivityScopeContext(
                        context.PreviousActivity,
                        context.Source,
                        context.Reason,
                        out RuntimeScopeContext exitContext))
                {
                    context.ExitPlan =
                        ActivityContentExecutionRequestFactory.CreateExitPlan(
                            context.PreviousActivity,
                            context.NextActivity,
                            exitContext,
                            context.Participants,
                            context.Source,
                            context.Reason);
                    context.ExitResult =
                        _activityContentExecutionRuntime.ExecutePhasePlan(
                            context.ExitPlan,
                            context.Source,
                            context.Reason);
                }
                else
                {
                    context.ExitResult =
                        ActivityContentExecutionAggregateResult
                            .RejectedInvalidResults(
                                ActivityContentExecutionPhase.Exit,
                                context.PreviousActivity,
                                context.PreviousActivity,
                                context.NextActivity,
                                context.Source,
                                context.Reason,
                                "Previous Activity runtime scope context is unavailable during participant Exit.");
                }
            }

            context.ExitExecuted = true;
        }

        private void ExecuteActivityParticipantEnter(
            ActivityParticipantTransitionContext context)
        {
            if (context == null)
            {
                return;
            }

            if (!context.ExitExecuted)
            {
                throw new InvalidOperationException(
                    "Activity participant Enter phase requires completed Exit phase.");
            }

            if (context.EnterExecuted)
            {
                throw new InvalidOperationException(
                    "Activity participant Enter phase already executed.");
            }

            if (context.NextActivity != null &&
                !ReferenceEquals(
                    context.PreviousActivity,
                    context.NextActivity))
            {
                if (TryCreateActivityScopeContext(
                        context.NextActivity,
                        context.Source,
                        context.Reason,
                        out RuntimeScopeContext enterContext))
                {
                    context.EnterPlan =
                        ActivityContentExecutionRequestFactory.CreateEnterPlan(
                            context.NextActivity,
                            context.PreviousActivity,
                            enterContext,
                            context.Participants,
                            context.Source,
                            context.Reason);
                    context.EnterResult =
                        _activityContentExecutionRuntime.ExecutePhasePlan(
                            context.EnterPlan,
                            context.Source,
                            context.Reason);
                }
                else
                {
                    context.EnterResult =
                        ActivityContentExecutionAggregateResult
                            .RejectedInvalidResults(
                                ActivityContentExecutionPhase.Enter,
                                context.NextActivity,
                                context.PreviousActivity,
                                context.NextActivity,
                                context.Source,
                                context.Reason,
                                "Target Activity runtime scope context is unavailable during participant Enter.");
                }
            }

            context.EnterExecuted = true;
        }

        private static ActivityContentExecutionLifecycleResult
            CompleteActivityParticipantTransition(
                ActivityParticipantTransitionContext context)
        {
            if (context == null)
            {
                return ActivityContentExecutionLifecycleResult.None(
                    "ActivityFlowRuntime",
                    "activity-transition",
                    "Activity participant transition was not prepared.");
            }

            if (context.PreviousActivity == null &&
                context.NextActivity == null)
            {
                return ActivityContentExecutionLifecycleResult.None(
                    context.Source,
                    context.Reason,
                    "No previous or target Activity participants were required.");
            }

            ActivityAsset activity = context.NextActivity != null
                ? context.NextActivity
                : context.PreviousActivity;
            return ActivityContentExecutionLifecycleResult.FromResults(
                activity,
                context.PreviousActivity,
                context.NextActivity,
                context.ParticipantSourceResult,
                context.Participants,
                context.EnterPlan,
                context.EnterResult,
                context.ExitPlan,
                context.ExitResult,
                context.Source,
                context.Reason,
                "Activity participant Exit and Enter phases executed from one frozen participant collection.");
        }

        private bool TryBeginActivityTransition(
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason,
            out ActivityTransitionRuntimeTransaction transaction,
            out string issue)
        {
            if (_activeActivityTransition != null &&
                !_activeActivityTransition.IsTerminal)
            {
                transaction = null;
                issue =
                    "Activity transition request rejected because another transaction is non-terminal. " +
                    _activeActivityTransition.Snapshot.ToDiagnosticString();
                return false;
            }

            _activityTransitionSequence++;
            transaction = new ActivityTransitionRuntimeTransaction(
                _activityTransitionSequence,
                previousActivity,
                targetActivity,
                source,
                reason);
            _activeActivityTransition = transaction;
            _lastActivityTransitionSnapshot = transaction.Snapshot;
            issue = string.Empty;
            return true;
        }

        private void FinishActivityTransition(
            ActivityTransitionRuntimeTransaction transaction)
        {
            if (transaction == null)
            {
                return;
            }

            _lastActivityTransitionSnapshot = transaction.Snapshot;
            if (ReferenceEquals(_activeActivityTransition, transaction))
            {
                _activeActivityTransition = null;
            }
        }

        private static bool PreviousScopeFinalizationSucceeded(
            ActivityAsset previousActivity,
            RuntimeScopeLifecycleResult scopeResult,
            ContentAnchorBindingLifecycleResult bindingCleanupResult)
        {
            if (previousActivity == null)
            {
                return true;
            }

            return bindingCleanupResult.Succeeded &&
                scopeResult.HasExitRootResult &&
                !scopeResult.Rejected;
        }

        private static bool PreviousExitSucceeded(
            ActivityAsset previousActivity,
            ActivityContentApplyResult contentResult,
            ActivityContentExecutionLifecycleResult executionResult)
        {
            if (previousActivity == null)
            {
                return true;
            }

            bool sceneContentExitSucceeded =
                contentResult.LifecycleResult.ExitFailedReceiverCount == 0;
            ActivityContentExecutionAggregateResult participantExit =
                executionResult.ExitResult;
            bool participantExitSucceeded =
                participantExit.Status ==
                    ActivityContentExecutionAggregateStatus.Unknown ||
                (!participantExit.Failed &&
                 !participantExit.BlocksReadiness);
            return sceneContentExitSucceeded && participantExitSucceeded;
        }

        private static ActivityReadinessState BuildTransitionReadiness(
            ActivityRuntimeState activityState,
            ActivityContentApplyResult contentResult,
            ActivityContentExecutionLifecycleResult executionResult,
            bool targetReadinessBlocked,
            string source,
            string reason)
        {
            if (!activityState.IsActive)
            {
                return ActivityReadinessState.None(
                    activityState,
                    contentResult,
                    executionResult.Executed,
                    false,
                    0,
                    source,
                    reason);
            }

            int blockingIssueCount = 0;
            string diagnosticReason = "BaselineReady";
            if (contentResult.MissingActivityCount > 0)
            {
                blockingIssueCount += contentResult.MissingActivityCount;
                diagnosticReason = "MissingActivityReference";
            }

            int targetContentEnterFailures =
                contentResult.LifecycleResult.EnterFailedReceiverCount;
            if (targetContentEnterFailures > 0)
            {
                blockingIssueCount += targetContentEnterFailures;
                diagnosticReason = "ActivityContentEnterFailure";
            }

            ActivityContentExecutionAggregateResult participantEnter =
                executionResult.EnterResult;
            bool participantEnterBlocksReadiness =
                participantEnter.Status !=
                    ActivityContentExecutionAggregateStatus.Unknown &&
                participantEnter.BlocksReadiness;
            int participantEnterBlockingIssues =
                participantEnterBlocksReadiness
                    ? participantEnter.BlockingIssueCount > 0
                        ? participantEnter.BlockingIssueCount
                        : 1
                    : 0;
            if (participantEnterBlockingIssues > 0)
            {
                blockingIssueCount += participantEnterBlockingIssues;
                diagnosticReason =
                    "ActivityContentParticipantEnterBlockingFailure";
            }

            if (targetReadinessBlocked)
            {
                blockingIssueCount++;
                diagnosticReason = "TargetEnterIncomplete";
            }

            return new ActivityReadinessState(
                blockingIssueCount == 0
                    ? ActivityReadinessStatus.Ready
                    : ActivityReadinessStatus.NotReady,
                activityState.Activity,
                contentResult.ActivityContentSet,
                contentResult.LifecycleResult,
                executionResult.Executed,
                participantEnterBlocksReadiness || targetReadinessBlocked,
                participantEnterBlockingIssues +
                    (targetReadinessBlocked ? 1 : 0),
                blockingIssueCount,
                source,
                reason,
                diagnosticReason);
        }

        private void PublishActivityExitedFact(
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason)
        {
            if (previousActivity == null ||
                ReferenceEquals(previousActivity, nextActivity))
            {
                return;
            }

            _activityExitedEvents.Publish(
                new ActivityExitedEvent(
                    previousActivity,
                    nextActivity,
                    source,
                    reason));
        }

        private void PublishActivityEnteredFact(
            ActivityAsset targetActivity,
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            if (targetActivity == null ||
                ReferenceEquals(previousActivity, targetActivity))
            {
                return;
            }

            _activityEnteredEvents.Publish(
                new ActivityEnteredEvent(
                    targetActivity,
                    previousActivity,
                    source,
                    reason));
        }
    }
}
