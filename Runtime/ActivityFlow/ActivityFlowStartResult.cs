using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal immutable result for starting or clearing an Activity.
    /// This is diagnostics data, not an activity service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct ActivityFlowStartResult
    {
        private readonly ResultData _data;

        public ActivityFlowStartResult(
            bool started,
            bool skipped,
            bool keptActive,
            bool cleared,
            string message,
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult,
            ActivityReadinessState activityReadinessState,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult),
            ActivityContentAnchorDiscoveryResult activityContentAnchorDiscoveryResult = default(ActivityContentAnchorDiscoveryResult),
            ActivityContentExecutionLifecycleResult activityContentExecutionResult = default(ActivityContentExecutionLifecycleResult),
            ActivitySceneCompositionResult activitySceneCompositionResult = default(ActivitySceneCompositionResult),
            ActivitySceneReleaseResult activitySceneReleaseResult = default(ActivitySceneReleaseResult),
            ActivityOperationResult activityOperationResult = default(ActivityOperationResult),
            ActivitySceneLedgerSnapshot activitySceneLedgerSnapshot = default(ActivitySceneLedgerSnapshot),
            ActivityTransitionSnapshot activityTransitionSnapshot = default(ActivityTransitionSnapshot))
        {
            _data = new ResultData(
                started,
                skipped,
                keptActive,
                cleared,
                message,
                activityState,
                previousActivity,
                activityContentResult,
                activityReadinessState,
                runtimeActivityScopeResult,
                activityContentAnchorBindingCleanupResult,
                activityContentAnchorDiscoveryResult,
                activityContentExecutionResult,
                activitySceneCompositionResult,
                activitySceneReleaseResult,
                activityOperationResult,
                activitySceneLedgerSnapshot,
                activityTransitionSnapshot);
        }

        public bool Started => _data != null && _data.Started;
        public bool Skipped => _data != null && _data.Skipped;
        public bool KeptActive => _data != null && _data.KeptActive;
        public bool Cleared => _data != null && _data.Cleared;
        public string Message => _data != null ? _data.Message : null;
        public ActivityRuntimeState ActivityState =>
            _data != null ? _data.ActivityState : default(ActivityRuntimeState);
        public ActivityAsset Activity => ActivityState.Activity;
        public ActivityAsset PreviousActivity =>
            _data != null ? _data.PreviousActivity : null;
        public ActivityContentApplyResult ActivityContentResult =>
            _data != null
                ? _data.ActivityContentResult
                : default(ActivityContentApplyResult);
        public ActivityContentSet ActivityContentSet => ActivityContentResult.ActivityContentSet;
        public ActivityContentLifecycleResult ActivityContentLifecycleResult => ActivityContentResult.LifecycleResult;
        public ActivityReadinessState ActivityReadinessState =>
            _data != null
                ? _data.ActivityReadinessState
                : default(ActivityReadinessState);
        public RuntimeScopeLifecycleResult RuntimeActivityScopeResult =>
            _data != null
                ? _data.RuntimeActivityScopeResult
                : default(RuntimeScopeLifecycleResult);
        public ContentAnchorBindingLifecycleResult
            ActivityContentAnchorBindingCleanupResult =>
                _data != null
                    ? _data.ActivityContentAnchorBindingCleanupResult
                    : default(ContentAnchorBindingLifecycleResult);
        public ActivityContentAnchorDiscoveryResult
            ActivityContentAnchorDiscoveryResult =>
                _data != null
                    ? _data.ActivityContentAnchorDiscoveryResult
                    : default(ActivityContentAnchorDiscoveryResult);
        public ActivityContentExecutionLifecycleResult
            ActivityContentExecutionResult =>
                _data != null
                    ? _data.ActivityContentExecutionResult
                    : default(ActivityContentExecutionLifecycleResult);
        public ActivitySceneCompositionResult ActivitySceneCompositionResult =>
            _data != null
                ? _data.ActivitySceneCompositionResult
                : default(ActivitySceneCompositionResult);
        public ActivitySceneReleaseResult ActivitySceneReleaseResult =>
            _data != null
                ? _data.ActivitySceneReleaseResult
                : default(ActivitySceneReleaseResult);
        public ActivityOperationResult ActivityOperationResult =>
            _data != null
                ? _data.ActivityOperationResult
                : default(ActivityOperationResult);
        public ActivitySceneLedgerSnapshot ActivitySceneLedgerSnapshot =>
            _data != null
                ? _data.ActivitySceneLedgerSnapshot
                : default(ActivitySceneLedgerSnapshot);
        internal ActivityTransitionSnapshot ActivityTransitionSnapshot =>
            _data != null
                ? _data.ActivityTransitionSnapshot
                : default(ActivityTransitionSnapshot);
        public bool ActivityAuthorityCommitReached => ActivityTransitionSnapshot.CommitReached;
        public bool ActivityTransitionFailedBeforeCommit => ActivityTransitionSnapshot.FailedBeforeCommit;
        public bool ActivityTransitionCommittedNotReady => ActivityTransitionSnapshot.CommittedNotReady;
        public bool ActivityTransitionCommittedFinalizationFailed => ActivityTransitionSnapshot.CommittedFinalizationFailed;
        public ContentAnchorSet ActivityContentAnchorSet => ActivityContentAnchorDiscoveryResult.AnchorSet;
        public bool HasActivityContentAnchors => ActivityContentAnchorDiscoveryResult.HasAnchors;
        public bool HasRuntimeActivityScope => RuntimeActivityScopeResult.Executed;
        public bool HasActivityContent => ActivityContentSet.HasContent;
        public bool HasActivityContentLifecycle => ActivityContentLifecycleResult.Executed;
        public bool HasActivityContentExecution => ActivityContentExecutionResult.Executed;
        public bool HasActivitySceneComposition => ActivitySceneCompositionResult.Executed;
        public bool HasActivitySceneRelease => ActivitySceneReleaseResult.Executed;
        public bool HasActivityReadiness => ActivityReadinessState.IsReady || ActivityReadinessState.IsNone || ActivityReadinessState.IsNotReady;
        public bool IsActivityReady => ActivityReadinessState.IsReady;
        public bool ReplacedPreviousActivity => Started && PreviousActivity != null && !ReferenceEquals(PreviousActivity, Activity);
        public bool Completed => Started || Skipped || KeptActive || Cleared;
        public bool HasActivityState => ActivityState.IsActive || ActivityState.IsNone || ActivityState.IsTransitioning;
        public string ActivityIdentity => ActivityState.DiagnosticIdentity;

        private sealed class ResultData
        {
            internal ResultData(
                bool started,
                bool skipped,
                bool keptActive,
                bool cleared,
                string message,
                ActivityRuntimeState activityState,
                ActivityAsset previousActivity,
                ActivityContentApplyResult activityContentResult,
                ActivityReadinessState activityReadinessState,
                RuntimeScopeLifecycleResult runtimeActivityScopeResult,
                ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult,
                ActivityContentAnchorDiscoveryResult activityContentAnchorDiscoveryResult,
                ActivityContentExecutionLifecycleResult activityContentExecutionResult,
                ActivitySceneCompositionResult activitySceneCompositionResult,
                ActivitySceneReleaseResult activitySceneReleaseResult,
                ActivityOperationResult activityOperationResult,
                ActivitySceneLedgerSnapshot activitySceneLedgerSnapshot,
                ActivityTransitionSnapshot activityTransitionSnapshot)
            {
                Started = started;
                Skipped = skipped;
                KeptActive = keptActive;
                Cleared = cleared;
                Message = message ?? string.Empty;
                ActivityState = activityState;
                PreviousActivity = previousActivity;
                ActivityContentResult = activityContentResult;
                ActivityReadinessState = activityReadinessState;
                RuntimeActivityScopeResult = runtimeActivityScopeResult;
                ActivityContentAnchorBindingCleanupResult =
                    activityContentAnchorBindingCleanupResult;
                ActivityContentAnchorDiscoveryResult =
                    activityContentAnchorDiscoveryResult;
                ActivityContentExecutionResult = activityContentExecutionResult;
                ActivitySceneCompositionResult = activitySceneCompositionResult;
                ActivitySceneReleaseResult = activitySceneReleaseResult;
                ActivityOperationResult = activityOperationResult;
                ActivitySceneLedgerSnapshot = activitySceneLedgerSnapshot;
                ActivityTransitionSnapshot = activityTransitionSnapshot;
            }

            internal bool Started { get; }
            internal bool Skipped { get; }
            internal bool KeptActive { get; }
            internal bool Cleared { get; }
            internal string Message { get; }
            internal ActivityRuntimeState ActivityState { get; }
            internal ActivityAsset PreviousActivity { get; }
            internal ActivityContentApplyResult ActivityContentResult { get; }
            internal ActivityReadinessState ActivityReadinessState { get; }
            internal RuntimeScopeLifecycleResult RuntimeActivityScopeResult { get; }
            internal ContentAnchorBindingLifecycleResult
                ActivityContentAnchorBindingCleanupResult { get; }
            internal ActivityContentAnchorDiscoveryResult
                ActivityContentAnchorDiscoveryResult { get; }
            internal ActivityContentExecutionLifecycleResult
                ActivityContentExecutionResult { get; }
            internal ActivitySceneCompositionResult
                ActivitySceneCompositionResult { get; }
            internal ActivitySceneReleaseResult ActivitySceneReleaseResult { get; }
            internal ActivityOperationResult ActivityOperationResult { get; }
            internal ActivitySceneLedgerSnapshot ActivitySceneLedgerSnapshot { get; }
            internal ActivityTransitionSnapshot ActivityTransitionSnapshot { get; }
        }

        internal ActivityFlowStartResult WithActivityTransition(
            ActivityTransitionSnapshot activityTransitionSnapshot)
        {
            if (!activityTransitionSnapshot.IsValid)
            {
                return this;
            }

            ActivityReadinessState readinessState =
                activityTransitionSnapshot.CommitReached
                    ? activityTransitionSnapshot.ReadinessState
                    : ActivityReadinessState;
            ActivityRuntimeState activityState = ActivityState;
            ActivityAsset previousActivity = PreviousActivity;
            if (activityTransitionSnapshot.FailedBeforeCommit)
            {
                previousActivity = activityTransitionSnapshot.PreviousActivity;
                activityState = previousActivity != null
                    ? ActivityRuntimeState.ActiveWith(
                        previousActivity,
                        activityTransitionSnapshot.TargetActivity,
                        activityTransitionSnapshot.Source,
                        activityTransitionSnapshot.Reason)
                    : ActivityRuntimeState.None(
                        activityTransitionSnapshot.TargetActivity,
                        activityTransitionSnapshot.Source,
                        activityTransitionSnapshot.Reason);
            }

            string transitionMessage =
                $"activityTransition=({activityTransitionSnapshot.ToDiagnosticString()}).";
            string message = string.IsNullOrWhiteSpace(Message)
                ? transitionMessage
                : $"{Message} {transitionMessage}";
            return new ActivityFlowStartResult(
                Started,
                Skipped,
                KeptActive,
                Cleared,
                message,
                activityState,
                previousActivity,
                ActivityContentResult,
                readinessState,
                RuntimeActivityScopeResult,
                ActivityContentAnchorBindingCleanupResult,
                ActivityContentAnchorDiscoveryResult,
                ActivityContentExecutionResult,
                ActivitySceneCompositionResult,
                ActivitySceneReleaseResult,
                ActivityOperationResult,
                ActivitySceneLedgerSnapshot,
                activityTransitionSnapshot);
        }

        public static ActivityFlowStartResult Failed(
            string message,
            ActivityOperationResult activityOperationResult = default(ActivityOperationResult))
        {
            return new ActivityFlowStartResult(
                false,
                false,
                false,
                false,
                message,
                default,
                null,
                default,
                default,
                activityOperationResult: activityOperationResult);
        }

        public static ActivityFlowStartResult SkippedNoStartupActivity(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult),
            ActivityContentAnchorDiscoveryResult activityContentAnchorDiscoveryResult = default(ActivityContentAnchorDiscoveryResult),
            ActivityContentExecutionLifecycleResult activityContentExecutionResult = default(ActivityContentExecutionLifecycleResult),
            ActivitySceneCompositionResult activitySceneCompositionResult = default(ActivitySceneCompositionResult),
            ActivitySceneReleaseResult activitySceneReleaseResult = default(ActivitySceneReleaseResult),
            ActivityOperationResult activityOperationResult = default(ActivityOperationResult),
            ActivitySceneLedgerSnapshot activitySceneLedgerSnapshot = default(ActivitySceneLedgerSnapshot))
        {
            var readinessState = BuildReadinessState(
                activityState,
                activityContentResult,
                activityContentExecutionResult.Executed,
                activityContentExecutionResult.BlocksReadiness,
                activityContentExecutionResult.BlockingIssueCount);
            string runtimeScopeMessage = RuntimeScopeMessage(runtimeActivityScopeResult);
            string bindingCleanupMessage = BindingCleanupMessage(activityContentAnchorBindingCleanupResult);
            string executionMessage = ExecutionMessage(activityContentExecutionResult);
            string sceneCompositionMessage = SceneCompositionMessage(activitySceneCompositionResult);
            string sceneReleaseMessage = SceneReleaseMessage(activitySceneReleaseResult);
            if (previousActivity == null)
            {
                return new ActivityFlowStartResult(
                    false,
                    true,
                    false,
                    false,
                    AppendContentMessage(
                        CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState) +
                        bindingCleanupMessage + executionMessage + sceneCompositionMessage +
                        sceneReleaseMessage + runtimeScopeMessage,
                        activityContentResult),
                    activityState,
                    null,
                    activityContentResult,
                    readinessState,
                    runtimeActivityScopeResult,
                    activityContentAnchorBindingCleanupResult,
                    activityContentAnchorDiscoveryResult,
                    activityContentExecutionResult,
                    activitySceneCompositionResult,
                    activitySceneReleaseResult,
                    activityOperationResult,
                    activitySceneLedgerSnapshot);
            }

            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage(
                    $"Activity Flow cleared Activity '{previousActivity.ActivityName}' because Route has no Startup Activity. " +
                    $"{CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}" +
                    $"{bindingCleanupMessage}{executionMessage}{sceneCompositionMessage}{sceneReleaseMessage}{runtimeScopeMessage}",
                    activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState,
                runtimeActivityScopeResult,
                activityContentAnchorBindingCleanupResult,
                activityContentAnchorDiscoveryResult,
                activityContentExecutionResult,
                activitySceneCompositionResult,
                activitySceneReleaseResult,
                activityOperationResult,
                activitySceneLedgerSnapshot);
        }

        public static ActivityFlowStartResult ClearedByRequest(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult),
            ActivityContentAnchorDiscoveryResult activityContentAnchorDiscoveryResult = default(ActivityContentAnchorDiscoveryResult),
            ActivityContentExecutionLifecycleResult activityContentExecutionResult = default(ActivityContentExecutionLifecycleResult),
            ActivitySceneCompositionResult activitySceneCompositionResult = default(ActivitySceneCompositionResult),
            ActivitySceneReleaseResult activitySceneReleaseResult = default(ActivitySceneReleaseResult),
            ActivityOperationResult activityOperationResult = default(ActivityOperationResult),
            ActivitySceneLedgerSnapshot activitySceneLedgerSnapshot = default(ActivitySceneLedgerSnapshot))
        {
            if (previousActivity == null)
            {
                return Failed("Activity Flow cannot clear Activity because no Activity is active.");
            }

            var readinessState = BuildReadinessState(
                activityState,
                activityContentResult,
                activityContentExecutionResult.Executed,
                activityContentExecutionResult.BlocksReadiness,
                activityContentExecutionResult.BlockingIssueCount);
            string runtimeScopeMessage = RuntimeScopeMessage(runtimeActivityScopeResult);
            string bindingCleanupMessage = BindingCleanupMessage(activityContentAnchorBindingCleanupResult);
            string executionMessage = ExecutionMessage(activityContentExecutionResult);
            string sceneCompositionMessage = SceneCompositionMessage(activitySceneCompositionResult);
            string sceneReleaseMessage = SceneReleaseMessage(activitySceneReleaseResult);
            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage(
                    $"Activity Flow cleared Activity '{previousActivity.ActivityName}' by request. " +
                    $"{CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}" +
                    $"{bindingCleanupMessage}{executionMessage}{sceneCompositionMessage}{sceneReleaseMessage}{runtimeScopeMessage}",
                    activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState,
                runtimeActivityScopeResult,
                activityContentAnchorBindingCleanupResult,
                activityContentAnchorDiscoveryResult,
                activityContentExecutionResult,
                activitySceneCompositionResult,
                activitySceneReleaseResult,
                activityOperationResult,
                activitySceneLedgerSnapshot);
        }

        public static ActivityFlowStartResult KeptCurrentActivity(ActivityRuntimeState activityState)
        {
            var activityContentResult = ActivityContentApplyResult.Empty(activityState.Activity);
            var readinessState = BuildReadinessState(
                activityState,
                activityContentResult,
                false,
                false,
                0);
            return new ActivityFlowStartResult(
                false,
                false,
                true,
                false,
                $"Activity Flow kept Activity '{activityState.ActivityName}' active. " +
                $"{CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}",
                activityState,
                activityState.Activity,
                activityContentResult,
                readinessState);
        }

        public static ActivityFlowStartResult StartedWith(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult),
            ActivityContentAnchorDiscoveryResult activityContentAnchorDiscoveryResult = default(ActivityContentAnchorDiscoveryResult),
            ActivityContentExecutionLifecycleResult activityContentExecutionResult = default(ActivityContentExecutionLifecycleResult),
            ActivitySceneCompositionResult activitySceneCompositionResult = default(ActivitySceneCompositionResult),
            ActivitySceneReleaseResult activitySceneReleaseResult = default(ActivitySceneReleaseResult),
            ActivityOperationResult activityOperationResult = default(ActivityOperationResult),
            ActivitySceneLedgerSnapshot activitySceneLedgerSnapshot = default(ActivitySceneLedgerSnapshot))
        {
            var activity = activityState.Activity;
            var readinessState = BuildReadinessState(
                activityState,
                activityContentResult,
                activityContentExecutionResult.Executed,
                activityContentExecutionResult.BlocksReadiness,
                activityContentExecutionResult.BlockingIssueCount);
            string stateMessage = CombineStateAndReadinessMessage(
                ActivityStateMessage(activityState),
                readinessState);
            string runtimeScopeMessage = RuntimeScopeMessage(runtimeActivityScopeResult);
            string bindingCleanupMessage = BindingCleanupMessage(activityContentAnchorBindingCleanupResult);
            string executionMessage = ExecutionMessage(activityContentExecutionResult);
            string sceneCompositionMessage = SceneCompositionMessage(activitySceneCompositionResult);
            string sceneReleaseMessage = SceneReleaseMessage(activitySceneReleaseResult);
            string message = previousActivity != null && !ReferenceEquals(previousActivity, activity)
                ? $"Activity Flow switched from Activity '{previousActivity.ActivityName}' to Activity '{activity.ActivityName}'. " +
                  $"{stateMessage}{bindingCleanupMessage}{executionMessage}{sceneCompositionMessage}{sceneReleaseMessage}{runtimeScopeMessage}"
                : $"Activity Flow started Activity '{activity.ActivityName}'. " +
                  $"{stateMessage}{bindingCleanupMessage}{executionMessage}{sceneCompositionMessage}{sceneReleaseMessage}{runtimeScopeMessage}";

            return new ActivityFlowStartResult(
                true,
                false,
                false,
                false,
                AppendContentMessage(message, activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState,
                runtimeActivityScopeResult,
                activityContentAnchorBindingCleanupResult,
                activityContentAnchorDiscoveryResult,
                activityContentExecutionResult,
                activitySceneCompositionResult,
                activitySceneReleaseResult,
                activityOperationResult,
                activitySceneLedgerSnapshot);
        }

        private static ActivityReadinessState BuildReadinessState(
            ActivityRuntimeState activityState,
            ActivityContentApplyResult activityContentResult,
            bool activityContentExecutionExecuted,
            bool activityContentExecutionBlocksReadiness,
            int activityContentExecutionBlockingIssueCount)
        {
            return ActivityReadinessState.FromActivityResult(
                activityState,
                activityContentResult,
                activityContentExecutionExecuted,
                activityContentExecutionBlocksReadiness,
                activityContentExecutionBlockingIssueCount,
                activityState.Source,
                activityState.Reason);
        }

        private static string ActivityStateMessage(ActivityRuntimeState activityState)
        {
            if (activityState.IsActive)
            {
                return $"activityState='{activityState.DiagnosticStatus}' activityIdentity='{activityState.DiagnosticIdentity}'.";
            }

            return $"activityState='{activityState.DiagnosticStatus}'.";
        }

        private static string ActivityReadinessMessage(ActivityReadinessState activityReadinessState)
        {
            if (activityReadinessState.IsReady)
            {
                return $"activityReadiness='{activityReadinessState.DiagnosticStatus}' activityReadinessReason='{activityReadinessState.DiagnosticReason}' activityReadinessIssues='0'.";
            }

            if (activityReadinessState.IsNotReady)
            {
                return $"activityReadiness='{activityReadinessState.DiagnosticStatus}' activityReadinessReason='{activityReadinessState.DiagnosticReason}' activityReadinessIssues='{activityReadinessState.BlockingIssueCount}'.";
            }

            return $"activityReadiness='{activityReadinessState.DiagnosticStatus}' activityReadinessReason='{activityReadinessState.DiagnosticReason}' activityReadinessIssues='0'.";
        }

        private static string CombineStateAndReadinessMessage(
            string stateMessage,
            ActivityReadinessState readinessState)
        {
            if (string.IsNullOrWhiteSpace(stateMessage))
            {
                return ActivityReadinessMessage(readinessState);
            }

            return $"{stateMessage} {ActivityReadinessMessage(readinessState)}";
        }

        private static string RuntimeScopeMessage(
            RuntimeScopeLifecycleResult runtimeActivityScopeResult)
        {
            if (!runtimeActivityScopeResult.Executed)
            {
                return string.Empty;
            }

            return $" runtimeActivityScope='{runtimeActivityScopeResult.DiagnosticStatus}' runtimeActivityRootEnter='{runtimeActivityScopeResult.EnterStatus}' runtimeActivityRootExit='{runtimeActivityScopeResult.ExitStatus}' runtimeActivityContext='{runtimeActivityScopeResult.ContextStatus}' runtimeRootCount='{runtimeActivityScopeResult.RootCount}'.";
        }

        private static string ExecutionMessage(ActivityContentExecutionLifecycleResult result)
        {
            if (!result.Executed)
            {
                return string.Empty;
            }

            return $" activityContentExecution='{result.DiagnosticStatus}' activityContentExecutionParticipantSource='{result.ParticipantSourceStatus}' activityContentExecutionParticipantSourceIssues='{result.ParticipantSourceIssueCount}' activityContentExecutionParticipants='{result.ParticipantCount}' activityContentExecutionEnter='{result.EnterResult.Status}' activityContentExecutionEnterRequests='{result.EnterRequestCount}' activityContentExecutionExit='{result.ExitResult.Status}' activityContentExecutionExitRequests='{result.ExitRequestCount}' activityContentExecutionBlockingIssues='{result.BlockingIssueCount}' activityContentExecutionBlocksReadiness='{result.BlocksReadiness}' activityContentParticipantExecution='{result.DiagnosticStatus}' activityContentParticipantSource='{result.ParticipantSourceStatus}' activityContentParticipantSourceIssues='{result.ParticipantSourceIssueCount}' activityContentParticipantCount='{result.ParticipantCount}' activityContentParticipantEnter='{result.EnterResult.Status}' activityContentParticipantEnterRequests='{result.EnterRequestCount}' activityContentParticipantExit='{result.ExitResult.Status}' activityContentParticipantExitRequests='{result.ExitRequestCount}' activityContentParticipantBlockingIssues='{result.BlockingIssueCount}' activityContentParticipantBlocksReadiness='{result.BlocksReadiness}'.";
        }

        private static string SceneCompositionMessage(ActivitySceneCompositionResult result)
        {
            if (!result.Executed)
            {
                return string.Empty;
            }

            return $" activitySceneComposition='{result.DiagnosticStatus}' activitySceneCompositionProfile='{result.ProfileId}' activitySceneCompositionScenes='{result.SceneCount}' activitySceneCompositionRequired='{result.RequiredSceneCount}' activitySceneCompositionOptional='{result.OptionalSceneCount}' activitySceneCompositionExecutionReady='{result.ExecutionReadySceneCount}' activitySceneCompositionLoaded='{result.LoadedSceneCount}' activitySceneCompositionFailed='{result.FailedSceneCount}' activitySceneCompositionSkipped='{result.SkippedSceneCount}' activitySceneCompositionSideEffects='{result.SideEffectsExecuted}' activitySceneCompositionBlockingIssues='{result.BlockingIssueCount}'.";
        }

        private static string SceneReleaseMessage(ActivitySceneReleaseResult result)
        {
            if (!result.Executed)
            {
                return string.Empty;
            }

            return $" activitySceneRelease='{result.DiagnosticStatus}' activitySceneReleaseScenes='{result.SceneCount}' activitySceneReleaseReleased='{result.ReleasedSceneCount}' activitySceneReleaseFailed='{result.FailedSceneCount}' activitySceneReleaseSkipped='{result.SkippedSceneCount}' activitySceneReleaseSideEffects='{result.SideEffectsExecuted}' activitySceneReleaseBlockingIssues='{result.BlockingIssueCount}'.";
        }

        private static string BindingCleanupMessage(ContentAnchorBindingLifecycleResult result)
        {
            if (!result.Executed)
            {
                return string.Empty;
            }

            return $" activityContentAnchorBindingCleanup='{result.DiagnosticStatus}' activityContentAnchorBindingCleanupRemoved='{result.RemovedCount}' activityContentAnchorBindingCleanupBefore='{result.BindingCountBefore}' activityContentAnchorBindingCleanupAfter='{result.BindingCountAfter}'.";
        }

        private static string AppendContentMessage(
            string message,
            ActivityContentApplyResult activityContentResult)
        {
            if (!activityContentResult.HasBindings)
            {
                return message ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return activityContentResult.Message;
            }

            return $"{message} {activityContentResult.Message}";
        }
    }
}
