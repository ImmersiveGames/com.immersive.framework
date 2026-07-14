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
            ActivitySceneLedgerSnapshot activitySceneLedgerSnapshot = default(ActivitySceneLedgerSnapshot))
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
            ActivityContentAnchorBindingCleanupResult = activityContentAnchorBindingCleanupResult;
            ActivityContentAnchorDiscoveryResult = activityContentAnchorDiscoveryResult;
            ActivityContentExecutionResult = activityContentExecutionResult;
            ActivitySceneCompositionResult = activitySceneCompositionResult;
            ActivitySceneReleaseResult = activitySceneReleaseResult;
            ActivityOperationResult = activityOperationResult;
            ActivitySceneLedgerSnapshot = activitySceneLedgerSnapshot;
        }

        public bool Started { get; }
        public bool Skipped { get; }
        public bool KeptActive { get; }
        public bool Cleared { get; }
        public string Message { get; }
        public ActivityRuntimeState ActivityState { get; }
        public ActivityAsset Activity => ActivityState.Activity;
        public ActivityAsset PreviousActivity { get; }
        public ActivityContentApplyResult ActivityContentResult { get; }
        public ActivityContentSet ActivityContentSet => ActivityContentResult.ActivityContentSet;
        public ActivityContentLifecycleResult ActivityContentLifecycleResult => ActivityContentResult.LifecycleResult;
        public ActivityReadinessState ActivityReadinessState { get; }
        public RuntimeScopeLifecycleResult RuntimeActivityScopeResult { get; }
        public ContentAnchorBindingLifecycleResult ActivityContentAnchorBindingCleanupResult { get; }
        public ActivityContentAnchorDiscoveryResult ActivityContentAnchorDiscoveryResult { get; }
        public ActivityContentExecutionLifecycleResult ActivityContentExecutionResult { get; }
        public ActivitySceneCompositionResult ActivitySceneCompositionResult { get; }
        public ActivitySceneReleaseResult ActivitySceneReleaseResult { get; }
        public ActivityOperationResult ActivityOperationResult { get; }
        public ActivitySceneLedgerSnapshot ActivitySceneLedgerSnapshot { get; }
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
