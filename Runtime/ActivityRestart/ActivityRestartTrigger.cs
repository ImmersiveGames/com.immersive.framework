using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Reset;
using UnityEngine;

namespace Immersive.Framework.ActivityRestart
{
    /// <summary>
    /// API status: Experimental. Scene-authored Activity Restart request bound to an explicit runtime port.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Restart/Activity Restart Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "H2.2.6 explicit runtime binding for Activity Restart.")]
    public sealed class ActivityRestartTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ActivityRestartTrigger);
        private const string DefaultReason = "Activity Restart";

        private readonly EventBus<ActivityRestartTriggerEvent> _requestEvents = new EventBus<ActivityRestartTriggerEvent>();
        private bool _requestInFlight;
        private IActivityRestartRuntimePort _activityRestartRuntime;
        private string _activityRestartRuntimeBindingDiagnostic = "Activity Restart runtime port is not bound.";
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
        public ActivityAsset TargetActivity { get => targetActivity; set => targetActivity = value; }
        public ResetSelectionConfig ResetSelection => resetSelection;
        public ResetSelectionMode ResetSelectionMode => resetSelection != null ? resetSelection.Mode : ResetSelectionMode.ExplicitSubjects;
        public bool HasActivityRestartRuntimeBinding => _activityRestartRuntime != null;
        public string ActivityRestartRuntimeBindingStatus => HasActivityRestartRuntimeBinding ? "Bound" : "Missing";
        public string ActivityRestartRuntimeBindingDiagnostic => _activityRestartRuntimeBindingDiagnostic;

        public IEventBinding SubscribeRequestEvents(Action<ActivityRestartTriggerEvent> handler) => _requestEvents.Subscribe(handler);

        internal bool TryBindActivityRestartRuntime(IActivityRestartRuntimePort activityRestartRuntime, out string issue)
        {
            if (activityRestartRuntime == null)
            {
                issue = "Activity Restart runtime port binding requires a non-null port.";
                _activityRestartRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_activityRestartRuntime == null)
            {
                _activityRestartRuntime = activityRestartRuntime;
                issue = string.Empty;
                _activityRestartRuntimeBindingDiagnostic = $"Bound '{activityRestartRuntime.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(_activityRestartRuntime, activityRestartRuntime))
            {
                issue = string.Empty;
                _activityRestartRuntimeBindingDiagnostic = $"Bound '{activityRestartRuntime.GetType().FullName}' (idempotent).";
                return true;
            }

            issue = "Activity Restart runtime port binding rejected a different port for the current lifetime.";
            _activityRestartRuntimeBindingDiagnostic = issue;
            return false;
        }

        [ContextMenu("Request Activity Restart")]
        public async void RequestActivityRestart()
        {
            await RequestActivityRestartAsync();
        }

        public async Awaitable<ActivityRestartResult> RequestActivityRestartAsync()
        {
            string resolvedReason = ResolveReason();
            if (_requestInFlight)
            {
                const string inFlightMessage = "Activity Restart ignored. This Activity Restart Trigger already has a request in flight.";
                ActivityRestartResult ignored = ActivityRestartResult.Rejected(
                    ActivityRestartResultStatus.RejectedAlreadyInFlight,
                    targetActivity,
                    ResolveActivityName(targetActivity),
                    DefaultSource,
                    resolvedReason,
                    inFlightMessage);
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, inFlightMessage, ignored, true, default, default);
                return ignored;
            }

            IActivityRestartRuntimePort activityRestartRuntime = _activityRestartRuntime;
            if (activityRestartRuntime == null)
            {
                const string unavailableMessage = "Activity Restart failed. Activity Restart runtime port is not bound.";
                _activityRestartRuntimeBindingDiagnostic = "Activity Restart runtime port is not bound.";
                ActivityRestartResult unavailable = ActivityRestartResult.Rejected(
                    ActivityRestartResultStatus.RejectedRuntimeUnavailable,
                    targetActivity,
                    ResolveActivityName(targetActivity),
                    DefaultSource,
                    resolvedReason,
                    unavailableMessage);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, unavailableMessage, unavailable, true, default, default);
                return unavailable;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);
            ActivityRestartRuntimeResult runtimeResult;
            try
            {
                runtimeResult = await activityRestartRuntime.RequestActivityRestartAsync(
                    targetActivity,
                    useCurrentActivityWhenTargetMissing,
                    requireTargetActivityIsCurrent,
                    resetSelection,
                    DefaultSource,
                    resolvedReason);
            }
            catch (Exception exception)
            {
                string exceptionMessage = $"Activity Restart failed with an exception. source='{DefaultSource}' reason='{resolvedReason}' exception='{exception.GetType().Name}'.";
                runtimeResult = ActivityRestartRuntimeResult.From(
                    ActivityRestartResult.Rejected(
                        ActivityRestartResultStatus.ResetExecutionFailed,
                        targetActivity,
                        ResolveActivityName(targetActivity),
                        DefaultSource,
                        resolvedReason,
                        exceptionMessage));
            }
            finally
            {
                _requestInFlight = false;
            }

            ActivityRestartResult result = runtimeResult.Result;
            ResetSelectionResolution selectionResolution = runtimeResult.SelectionResolution;
            ResetExecutionResult resetExecutionResult = runtimeResult.ResetExecutionResult;
            string completedMessage = result != null ? result.Message : "Activity Restart failed. Runtime port returned no result.";
            PublishCompleted(MapOutcome(result), resolvedReason, completedMessage, result, result != null, selectionResolution, resetExecutionResult);
            return result;
        }

        [ContextMenu("Clear Last Activity Restart Result")]
        public void ClearLastResult() =>
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, null, false, default, default);

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

        private string ResolveReason() => reason.NormalizeTextOrFallback(DefaultReason);

        private static string ResolveActivityName(ActivityAsset activity) =>
            activity != null ? activity.ActivityName.NormalizeTextOrFallback(activity.name) : string.Empty;

        private void PublishSubmitted(string resolvedReason)
        {
            string message = $"Activity Restart submitted. source='{DefaultSource}' reason='{resolvedReason}'.";
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, null, false, default, default);
            _requestEvents.Publish(new ActivityRestartTriggerEvent(this, FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, DefaultSource, resolvedReason, message, null, false));
        }

        private void PublishCompleted(FlowRequestOutcome outcome, string resolvedReason, string message, ActivityRestartResult result, bool hasResult, ResetSelectionResolution selectionResolution, ResetExecutionResult resetExecutionResult)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult, selectionResolution, resetExecutionResult);
            _requestEvents.Publish(new ActivityRestartTriggerEvent(this, FlowRequestEventPhase.Completed, outcome, DefaultSource, resolvedReason, message, result, hasResult));
        }

        private void SetRequestState(FlowRequestEventPhase phase, FlowRequestOutcome outcome, string resolvedReason, string message, ActivityRestartResult result, bool hasResult, ResetSelectionResolution selectionResolution, ResetExecutionResult resetExecutionResult)
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

        private string BuildLastResultSummary()
        {
            if (!_hasLastResult || _lastResult == null)
            {
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Activity Restart result yet." : _lastMessage;
            }

            return $"status='{_lastResult.Status}' activity='{_lastResult.ActivityName}' selectionStatus='{_lastSelectionResolution.Status}' resetStatus='{_lastResult.ResetStatus}' resetSubjects='{_lastResult.ResetSubjectCount}' resetSubjectSucceeded='{_lastResult.ResetSubjectSucceededCount}' resetSubjectFailed='{_lastResult.ResetSubjectFailedCount}' resetParticipants='{_lastResult.ResetParticipantCount}' resetBlockingIssues='{_lastResult.ResetBlockingIssueCount}' clearStatus='{_lastResult.ClearStatus}' reenterStatus='{_lastResult.ReenterStatus}'";
        }

        private static FlowRequestOutcome MapOutcome(ActivityRestartResult result) =>
            result != null && (result.Succeeded || result.CompletedWithWarnings)
                ? FlowRequestOutcome.Succeeded
                : FlowRequestOutcome.Failed;
    }
}
