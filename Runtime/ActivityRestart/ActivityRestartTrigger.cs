using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Reset;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Experimental. Scene-authored Activity Restart composed from ResetSelectionConfig + ResetExecutor + Activity Restart flow.
    /// This is not Cycle Reset and does not introduce Player/Actor lifecycle ownership.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Restart/Activity Restart Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12E Activity Restart trigger over ResetSelectionConfig + ResetExecutor.")]
    public sealed class ActivityRestartTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ActivityRestartTrigger);
        private const string DefaultReason = "Activity Restart";

        private readonly EventBus<ActivityRestartTriggerEvent> _requestEvents = new EventBus<ActivityRestartTriggerEvent>();
        private FrameworkLogger _logger;
        private bool _requestInFlight;
        private FlowRequestEventPhase _lastEventPhase = FlowRequestEventPhase.Completed;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;
        private ActivityRestartResult _lastResult;
        private ResetExecutionResult _lastResetExecutionResult;
        private ResetSelectionResolution _lastSelectionResolution;
        private bool _hasLastResult;

        [Header("Activity Restart")]
        [SerializeField] private ActivityAsset targetActivity;
        [SerializeField] private bool useCurrentActivityWhenTargetMissing = true;
        [SerializeField] private bool requireTargetActivityIsCurrent = true;
        [SerializeField] private string reason;

        [Header("Reset Selection")]
        [SerializeField] private ResetSelectionConfig resetSelection = new ResetSelectionConfig();

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public ActivityRestartResult LastResult => _lastResult;

        public ResetExecutionResult LastResetExecutionResult => _lastResetExecutionResult;

        public ResetSelectionResolution LastSelectionResolution => _lastSelectionResolution;

        public bool HasLastResult => _hasLastResult;

        public ActivityRestartResultStatus LastResultStatus => _hasLastResult ? _lastResult.Status : ActivityRestartResultStatus.Unknown;

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public string LastResultSummary => BuildLastResultSummary();

        public ActivityAsset TargetActivity
        {
            get => targetActivity;
            set => targetActivity = value;
        }

        public ResetSelectionConfig ResetSelection => resetSelection;

        public ResetSelectionMode ResetSelectionMode => resetSelection != null ? resetSelection.Mode : ResetSelectionMode.ExplicitSubjects;

        public IEventBinding SubscribeRequestEvents(Action<ActivityRestartTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<ActivityRestartTrigger>();
        }

        [ContextMenu("Request Activity Restart")]
        public async void RequestActivityRestart()
        {
            await RequestActivityRestartAsync();
        }

        public async Awaitable<ActivityRestartResult> RequestActivityRestartAsync()
        {
            EnsureLogger();

            string resolvedReason = ResolveReason();

            if (_requestInFlight)
            {
                string message = "Activity Restart ignored. This Activity Restart Trigger already has a request in flight.";
                var result = ActivityRestartResult.Rejected(
                    ActivityRestartResultStatus.RejectedAlreadyInFlight,
                    targetActivity,
                    ResolveActivityName(targetActivity),
                    DefaultSource,
                    resolvedReason,
                    message);
                _logger.Warning(message, BuildResultSummaryFields(result));
                _logger.Debug("Activity Restart Request diagnostics. " + result.ToDiagnosticString(), BuildResultDiagnosticFields(result));
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, result, true, default, default);
                return result;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                string message = "Activity Restart failed. Application Runtime is unavailable.";
                var result = ActivityRestartResult.Rejected(
                    ActivityRestartResultStatus.RejectedRuntimeUnavailable,
                    targetActivity,
                    ResolveActivityName(targetActivity),
                    DefaultSource,
                    resolvedReason,
                    message);
                LogRestartResult(result);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true, default, default);
                return result;
            }

            ActivityAsset currentActivity = runtimeHost.State.CurrentActivity;
            ActivityAsset resolvedActivity = ResolveActivity(currentActivity);
            string resolvedActivityName = ResolveActivityName(resolvedActivity);

            if (resolvedActivity == null)
            {
                string message = "Activity Restart failed. No target Activity is configured and no active Activity is available.";
                var result = ActivityRestartResult.Rejected(
                    ActivityRestartResultStatus.RejectedNoActiveActivity,
                    null,
                    string.Empty,
                    DefaultSource,
                    resolvedReason,
                    message);
                LogRestartResult(result);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true, default, default);
                return result;
            }

            if (requireTargetActivityIsCurrent && !ReferenceEquals(currentActivity, resolvedActivity))
            {
                string message = $"Activity Restart failed. Target Activity must be the current active Activity. current='{ResolveActivityName(currentActivity)}' target='{resolvedActivityName}'.";
                var result = ActivityRestartResult.Rejected(
                    ActivityRestartResultStatus.RejectedTargetMismatch,
                    resolvedActivity,
                    resolvedActivityName,
                    DefaultSource,
                    resolvedReason,
                    message);
                LogRestartResult(result);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true, default, default);
                return result;
            }

            ResetSelectionConfig resolvedSelection = ResolveSelection();
            ResetSelectionResolution selectionResolution = resolvedSelection.Resolve(
                runtimeHost,
                DefaultSource,
                BuildStageReason(resolvedReason, "reset"));
            if (selectionResolution.Failed)
            {
                ResetIssue issue = selectionResolution.Issues.Count > 0
                    ? selectionResolution.Issues[0]
                    : ResetIssue.Error(ResetIssueKind.InvalidRequest, "Activity Restart reset selection failed.");
                ResetExecutionResult resetSelectionFailure = ResetExecutionResult.RejectedInvalidRequest(
                    issue,
                    DefaultSource,
                    BuildStageReason(resolvedReason, "reset"));
                string message = "Activity Restart failed. Reset selection failed.";
                var result = new ActivityRestartResult(
                    ActivityRestartResultStatus.ResetExecutionFailed,
                    resolvedActivity,
                    resolvedActivityName,
                    DefaultSource,
                    resolvedReason,
                    resetSelectionFailure,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    message);
                LogRestartResult(result);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true, selectionResolution, resetSelectionFailure);
                return result;
            }

            ResetExecutionRequest resetRequest = resolvedSelection.CreateExecutionRequest(selectionResolution);

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            ActivityRestartResult restartResult;
            ResetExecutionResult resetExecutionResult = default;
            ResetSelectionResolution completedSelectionResolution = selectionResolution;
            try
            {
                var executor = new ResetExecutor(runtimeHost.ResetRegistry);
                FrameworkActivityRestartFlowResult restartFlowResult = await runtimeHost.RestartActivityAsync(
                    resolvedActivity,
                    DefaultSource,
                    BuildStageReason(resolvedReason, "flow"),
                    async () =>
                    {
                        try
                        {
                            resetExecutionResult = await executor.ExecuteAsync(resetRequest);
                            _logger.Debug(
                                "Activity Restart reset stage completed inside restart transition.",
                                LogFields.Of(
                                    LogFields.Field("stage", "pre-clear-reset"),
                                    LogFields.Field("reason", BuildStageReason(resolvedReason, "reset")),
                                    LogFields.Field("resetStatus", resetExecutionResult.Status.ToString()),
                                    LogFields.Field("resetSubjects", resetExecutionResult.SubjectCount.ToString()),
                                    LogFields.Field("resetParticipants", resetExecutionResult.ParticipantCount.ToString()),
                                    LogFields.Field("resetBlockingIssues", resetExecutionResult.BlockingIssueCount.ToString())));
                            return !resetExecutionResult.Failed;
                        }
                        catch (Exception ex)
                        {
                            resetExecutionResult = ResetExecutionResult.RejectedInvalidRequest(
                                ResetIssue.Error(ResetIssueKind.Exception, $"Activity Restart reset execution threw an exception. {ex.Message}"),
                                DefaultSource,
                                BuildStageReason(resolvedReason, "reset"));
                            return false;
                        }
                    });

                if (resetExecutionResult.Failed)
                {
                    string message = "Activity Restart failed. Reset execution failed.";
                    restartResult = new ActivityRestartResult(
                        ActivityRestartResultStatus.ResetExecutionFailed,
                        resolvedActivity,
                        resolvedActivityName,
                        DefaultSource,
                        resolvedReason,
                        resetExecutionResult,
                        string.Empty,
                        restartFlowResult.ClearResult.Message,
                        string.Empty,
                        restartFlowResult.ReenterResult.Message,
                        message);
                    LogRestartResult(restartResult);
                    PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, restartResult, true, completedSelectionResolution, resetExecutionResult);
                    return restartResult;
                }

                FrameworkActivityRequestResult clearResult = restartFlowResult.ClearResult;
                FrameworkActivityRequestResult reenterResult = restartFlowResult.ReenterResult;

                if (!clearResult.Succeeded)
                {
                    string message = "Activity Restart failed. Activity Clear failed.";
                    restartResult = new ActivityRestartResult(
                        ActivityRestartResultStatus.ActivityClearFailed,
                        resolvedActivity,
                        resolvedActivityName,
                        DefaultSource,
                        resolvedReason,
                        resetExecutionResult,
                        clearResult.Kind.ToString(),
                        clearResult.Message,
                        reenterResult.Kind.ToString(),
                        reenterResult.Message,
                        message);
                    LogRestartResult(restartResult);
                    PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, restartResult, true, completedSelectionResolution, resetExecutionResult);
                    return restartResult;
                }

                if (!reenterResult.Succeeded)
                {
                    string message = "Activity Restart failed. Activity re-enter failed.";
                    restartResult = new ActivityRestartResult(
                        ActivityRestartResultStatus.ActivityReenterFailed,
                        resolvedActivity,
                        resolvedActivityName,
                        DefaultSource,
                        resolvedReason,
                        resetExecutionResult,
                        clearResult.Kind.ToString(),
                        clearResult.Message,
                        reenterResult.Kind.ToString(),
                        reenterResult.Message,
                        message);
                    LogRestartResult(restartResult);
                    PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, restartResult, true, completedSelectionResolution, resetExecutionResult);
                    return restartResult;
                }

                ActivityRestartResultStatus status = resetExecutionResult.NonBlockingIssueCount > 0
                    ? ActivityRestartResultStatus.CompletedWithWarnings
                    : ActivityRestartResultStatus.Succeeded;
                string successMessage = status == ActivityRestartResultStatus.CompletedWithWarnings
                    ? "Activity Restart completed with reset warnings."
                    : "Activity Restart completed successfully.";

                restartResult = new ActivityRestartResult(
                    status,
                    resolvedActivity,
                    resolvedActivityName,
                    DefaultSource,
                    resolvedReason,
                    resetExecutionResult,
                    clearResult.Kind.ToString(),
                    clearResult.Message,
                    reenterResult.Kind.ToString(),
                    reenterResult.Message,
                    successMessage);
            }
            finally
            {
                _requestInFlight = false;
            }

            LogRestartResult(restartResult);
            PublishCompleted(MapOutcome(restartResult), resolvedReason, restartResult.Message, restartResult, true, completedSelectionResolution, resetExecutionResult);
            return restartResult;
        }

        [ContextMenu("Clear Last Activity Restart Result")]
        public void ClearLastResult()
        {
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, null, false, default, default);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            ActivityAsset qaTargetActivity,
            bool qaUseCurrentActivityWhenTargetMissing,
            bool qaRequireTargetActivityIsCurrent,
            string qaReason,
            ResetSelectionMode qaSelectionMode,
            System.Collections.Generic.IReadOnlyList<ResetSubjectReference> qaExplicitSubjects,
            bool qaAllowNoSubjects,
            bool qaAllowNoParticipants,
            bool qaStopOnFailure,
            bool qaYieldBetweenSubjects)
        {
            targetActivity = qaTargetActivity;
            useCurrentActivityWhenTargetMissing = qaUseCurrentActivityWhenTargetMissing;
            requireTargetActivityIsCurrent = qaRequireTargetActivityIsCurrent;
            reason = qaReason;
            resetSelection ??= new ResetSelectionConfig();
            resetSelection.ConfigureForQa(
                qaSelectionMode,
                qaExplicitSubjects,
                qaAllowNoSubjects,
                qaAllowNoParticipants,
                qaStopOnFailure,
                qaYieldBetweenSubjects);
        }
#endif

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<ActivityRestartTrigger>();
            }
        }

        private string ResolveReason()
        {
            return reason.NormalizeTextOrFallback(DefaultReason);
        }

        private ResetSelectionConfig ResolveSelection()
        {
            resetSelection ??= new ResetSelectionConfig();
            return resetSelection;
        }

        private ActivityAsset ResolveActivity(ActivityAsset currentActivity)
        {
            if (targetActivity != null)
            {
                return targetActivity;
            }

            return useCurrentActivityWhenTargetMissing ? currentActivity : null;
        }

        private static string ResolveActivityName(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName.NormalizeTextOrFallback(activity.name) : string.Empty;
        }

        private static string BuildStageReason(string resolvedReason, string stage)
        {
            return $"{resolvedReason.NormalizeTextOrFallback(DefaultReason)}:{stage}";
        }

        private void PublishSubmitted(string resolvedReason)
        {
            string message = $"Activity Restart submitted. source='{DefaultSource}' reason='{resolvedReason}'.";
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, null, false, default, default);

            _requestEvents.Publish(new ActivityRestartTriggerEvent(
                this,
                FlowRequestEventPhase.Submitted,
                FlowRequestOutcome.Submitted,
                DefaultSource,
                resolvedReason,
                message,
                null,
                false));
        }

        private void PublishCompleted(
            FlowRequestOutcome outcome,
            string resolvedReason,
            string message,
            ActivityRestartResult result,
            bool hasResult,
            ResetSelectionResolution selectionResolution,
            ResetExecutionResult resetExecutionResult)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult, selectionResolution, resetExecutionResult);

            _requestEvents.Publish(new ActivityRestartTriggerEvent(
                this,
                FlowRequestEventPhase.Completed,
                outcome,
                DefaultSource,
                resolvedReason,
                message,
                result,
                hasResult));
        }

        private void SetRequestState(
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string resolvedReason,
            string message,
            ActivityRestartResult result,
            bool hasResult,
            ResetSelectionResolution selectionResolution,
            ResetExecutionResult resetExecutionResult)
        {
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
            _lastResult = result;
            _hasLastResult = hasResult;
            _lastSelectionResolution = selectionResolution;
            _lastResetExecutionResult = resetExecutionResult;
        }

        private void LogRestartResult(ActivityRestartResult result)
        {
            if (result == null)
            {
                _logger.Error("Activity Restart failed. Result is null.");
                return;
            }

            if (result.Succeeded || result.CompletedWithWarnings)
            {
                _logger.Info("Activity Restart Request completed.", BuildResultSummaryFields(result));
                _logger.Debug("Activity Restart Request diagnostics. " + result.ToDiagnosticString(), BuildResultDiagnosticFields(result));
                return;
            }

            _logger.Error("Activity Restart Request failed.", BuildResultSummaryFields(result));
            _logger.Debug("Activity Restart Request diagnostics. " + result.ToDiagnosticString(), BuildResultDiagnosticFields(result));
        }

        private LogField[] BuildResultSummaryFields(ActivityRestartResult result)
        {
            return LogFields.Of(
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("activity", result.ActivityName),
                LogFields.Field("resetStatus", result.ResetStatus),
                LogFields.Field("clearStatus", result.ClearStatus),
                LogFields.Field("reenterStatus", result.ReenterStatus));
        }

        private LogField[] BuildResultDiagnosticFields(ActivityRestartResult result)
        {
            return LogFields.Of(
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("activity", result.ActivityName),
                LogFields.Field("resetStatus", result.ResetStatus),
                LogFields.Field("resetSubjects", result.ResetSubjectCount.ToString()),
                LogFields.Field("resetSubjectSucceeded", result.ResetSubjectSucceededCount.ToString()),
                LogFields.Field("resetSubjectWarnings", result.ResetSubjectWarningCount.ToString()),
                LogFields.Field("resetSubjectFailed", result.ResetSubjectFailedCount.ToString()),
                LogFields.Field("resetParticipants", result.ResetParticipantCount.ToString()),
                LogFields.Field("resetParticipantSucceeded", result.ResetParticipantSucceededCount.ToString()),
                LogFields.Field("resetParticipantSkipped", result.ResetParticipantSkippedCount.ToString()),
                LogFields.Field("resetParticipantFailed", result.ResetParticipantFailedCount.ToString()),
                LogFields.Field("resetBlockingIssues", result.ResetBlockingIssueCount.ToString()),
                LogFields.Field("resetNonBlockingIssues", result.ResetNonBlockingIssueCount.ToString()),
                LogFields.Field("clearStatus", result.ClearStatus),
                LogFields.Field("reenterStatus", result.ReenterStatus));
        }

        private string BuildLastResultSummary()
        {
            if (!_hasLastResult || _lastResult == null)
            {
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Activity Restart result yet." : _lastMessage;
            }

            return $"status='{_lastResult.Status}' activity='{_lastResult.ActivityName}' resetStatus='{_lastResult.ResetStatus}' resetSubjects='{_lastResult.ResetSubjectCount}' resetSubjectSucceeded='{_lastResult.ResetSubjectSucceededCount}' resetSubjectFailed='{_lastResult.ResetSubjectFailedCount}' resetParticipants='{_lastResult.ResetParticipantCount}' resetBlockingIssues='{_lastResult.ResetBlockingIssueCount}' clearStatus='{_lastResult.ClearStatus}' reenterStatus='{_lastResult.ReenterStatus}'";
        }

        private static FlowRequestOutcome MapOutcome(ActivityRestartResult result)
        {
            if (result != null && (result.Succeeded || result.CompletedWithWarnings))
            {
                return FlowRequestOutcome.Succeeded;
            }

            return FlowRequestOutcome.Failed;
        }
    }
}
