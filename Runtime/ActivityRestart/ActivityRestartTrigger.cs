using System;
using System.Collections.Generic;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectReset;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Experimental. Scene-authored Activity Restart composed from reset selection + Activity Clear + Activity Request.
    /// This is not Cycle Reset and does not introduce Player/Actor lifecycle ownership.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Restart/Activity Restart Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F40B public authored trigger for Activity Restart via reset selection policy.")]
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
        private bool _hasLastResult;

        [Header("Activity Restart")]
        [SerializeField] private ActivityAsset targetActivity;
        [SerializeField] private bool useCurrentActivityWhenTargetMissing = true;
        [SerializeField] private bool requireTargetActivityIsCurrent = true;
        [SerializeField] private string reason;

        [Header("Reset Selection")]
        [SerializeField] private ObjectResetSelectionMode resetSelectionMode = ObjectResetSelectionMode.ExplicitTargets;
        [SerializeField] private ObjectResetGroupAsset resetGroupAsset;
        [SerializeField] private string resetGroupId = "activity-restart-reset";
        [SerializeField] private bool allowNoParticipants = true;
        [SerializeField] private bool stopOnFailure = true;
        [SerializeField] private List<ObjectResetGroupEntry> resetEntries = new List<ObjectResetGroupEntry>();

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public ActivityRestartResult LastResult => _lastResult;

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

        public ObjectResetSelectionMode ResetSelectionMode
        {
            get => resetSelectionMode;
            set => resetSelectionMode = value;
        }

        public ObjectResetGroupAsset ResetGroupAsset
        {
            get => resetGroupAsset;
            set => resetGroupAsset = value;
        }

        public string ResetGroupId => ResolveResetGroupId();

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
                _logger.Warning(message, BuildResultFields(result));
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, result, true);
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
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true);
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
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true);
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
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true);
                return result;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            ActivityRestartResult restartResult;
            try
            {
                ObjectResetGroupResult resetResult = await ObjectResetGroupExecutor.ExecuteAsync(
                    runtimeHost,
                    resetSelectionMode,
                    ResolveResetEntries(),
                    ResolveResetGroupId(),
                    DefaultSource,
                    BuildStageReason(resolvedReason, "reset"),
                    ResolveAllowNoParticipants(),
                    ResolveStopOnFailure());
                if (resetResult == null || resetResult.Failed)
                {
                    string message = resetResult == null
                        ? "Activity Restart failed. Object Reset Group returned no result."
                        : "Activity Restart failed. Object Reset Group failed.";
                    restartResult = new ActivityRestartResult(
                        ActivityRestartResultStatus.ResetGroupFailed,
                        resolvedActivity,
                        resolvedActivityName,
                        DefaultSource,
                        resolvedReason,
                        resetResult,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        message);
                    LogRestartResult(restartResult);
                    PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, restartResult, true);
                    return restartResult;
                }

                FrameworkActivityRestartFlowResult restartFlowResult = await runtimeHost.RestartActivityAsync(
                    resolvedActivity,
                    DefaultSource,
                    BuildStageReason(resolvedReason, "flow"));
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
                        resetResult,
                        clearResult.Kind.ToString(),
                        clearResult.Message,
                        reenterResult.Kind.ToString(),
                        reenterResult.Message,
                        message);
                    LogRestartResult(restartResult);
                    PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, restartResult, true);
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
                        resetResult,
                        clearResult.Kind.ToString(),
                        clearResult.Message,
                        reenterResult.Kind.ToString(),
                        reenterResult.Message,
                        message);
                    LogRestartResult(restartResult);
                    PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, restartResult, true);
                    return restartResult;
                }

                ActivityRestartResultStatus status = resetResult.CompletedWithWarnings
                    ? ActivityRestartResultStatus.CompletedWithWarnings
                    : ActivityRestartResultStatus.Succeeded;
                string successMessage = status == ActivityRestartResultStatus.CompletedWithWarnings
                    ? "Activity Restart completed with reset group warnings."
                    : "Activity Restart completed successfully.";

                restartResult = new ActivityRestartResult(
                    status,
                    resolvedActivity,
                    resolvedActivityName,
                    DefaultSource,
                    resolvedReason,
                    resetResult,
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
            PublishCompleted(MapOutcome(restartResult), resolvedReason, restartResult.Message, restartResult, true);
            return restartResult;
        }

        [ContextMenu("Clear Last Activity Restart Result")]
        public void ClearLastResult()
        {
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, null, false);
        }

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

        private string ResolveResetGroupId()
        {
            if (resetSelectionMode == ObjectResetSelectionMode.ExplicitTargets && resetGroupAsset != null)
            {
                return resetGroupAsset.GroupId;
            }

            return resetGroupId.NormalizeTextOrFallback("activity-restart-reset");
        }

        private IReadOnlyList<ObjectResetGroupEntry> ResolveResetEntries()
        {
            if (resetSelectionMode == ObjectResetSelectionMode.ExplicitTargets && resetGroupAsset != null)
            {
                return resetGroupAsset.Entries;
            }

            return resetEntries != null ? (IReadOnlyList<ObjectResetGroupEntry>)resetEntries : Array.Empty<ObjectResetGroupEntry>();
        }

        private bool ResolveAllowNoParticipants()
        {
            if (resetSelectionMode == ObjectResetSelectionMode.ExplicitTargets && resetGroupAsset != null)
            {
                return resetGroupAsset.AllowNoParticipants;
            }

            return allowNoParticipants;
        }

        private bool ResolveStopOnFailure()
        {
            if (resetSelectionMode == ObjectResetSelectionMode.ExplicitTargets && resetGroupAsset != null)
            {
                return resetGroupAsset.StopOnFailure;
            }

            return stopOnFailure;
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
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, null, false);

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
            bool hasResult)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult);

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
            bool hasResult)
        {
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
            _lastResult = result;
            _hasLastResult = hasResult;
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
                _logger.Info("Activity Restart Request completed.", BuildResultFields(result));
                _logger.Debug("Activity Restart Request diagnostics. " + result.ToDiagnosticString());
                return;
            }

            _logger.Error("Activity Restart Request failed. " + result.ToDiagnosticString(), BuildResultFields(result));
        }

        private LogField[] BuildResultFields(ActivityRestartResult result)
        {
            return LogFields.Of(
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("activity", result.ActivityName),
                LogFields.Field("resetStatus", result.HasResetGroupResult ? result.ResetGroupResult.Status.ToString() : "None"),
                LogFields.Field("resetTargets", result.ResetTargetCount),
                LogFields.Field("resetTargetSucceeded", result.ResetTargetSucceededCount),
                LogFields.Field("resetTargetWarnings", result.ResetTargetWarningCount),
                LogFields.Field("resetTargetFailed", result.ResetTargetFailedCount),
                LogFields.Field("resetBlockingIssues", result.ResetBlockingIssueCount),
                LogFields.Field("resetNonBlockingIssues", result.ResetNonBlockingIssueCount),
                LogFields.Field("clearStatus", result.ClearStatus),
                LogFields.Field("reenterStatus", result.ReenterStatus));
        }

        private string BuildLastResultSummary()
        {
            if (!_hasLastResult || _lastResult == null)
            {
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Activity Restart result yet." : _lastMessage;
            }

            return $"status='{_lastResult.Status}' activity='{_lastResult.ActivityName}' resetStatus='{(_lastResult.HasResetGroupResult ? _lastResult.ResetGroupResult.Status.ToString() : "None")}' clearStatus='{_lastResult.ClearStatus}' reenterStatus='{_lastResult.ReenterStatus}' resetTargets='{_lastResult.ResetTargetCount}' resetTargetSucceeded='{_lastResult.ResetTargetSucceededCount}' resetTargetFailed='{_lastResult.ResetTargetFailedCount}' resetBlockingIssues='{_lastResult.ResetBlockingIssueCount}'";
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
