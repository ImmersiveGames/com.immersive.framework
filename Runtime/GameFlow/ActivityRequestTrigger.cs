using System;
using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common.FlowTriggers;
using Immersive.Framework.Diagnostics;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// API status: Experimental. Public scene-authored request boundary kept for baseline smoke and development use.
    /// Simple scene-authored activity request boundary.
    /// Designed for UnityEvents/UI Buttons to invoke requests, while publishing typed Foundation events for results.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Request Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityRequestTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ActivityRequestTrigger);
        private const string DefaultClearReason = "Clear Activity";

        private readonly EventBus<ActivityRequestTriggerEvent> _requestEvents = new EventBus<ActivityRequestTriggerEvent>();
        private readonly FrameworkFlowTriggerState _triggerState = new FrameworkFlowTriggerState();
        private FrameworkLogger _logger;
        private bool _requestInFlight;
        private bool _lastRequestClearedActivity;
        private IActivityRuntimePort _activityRuntime;
        private string _activityRuntimeBindingDiagnostic =
            "Activity runtime port is not bound.";

        [Header("Activity")]
        [SerializeField] private ActivityAsset targetActivity;

        [Header("Request")]
        [SerializeField] private string reason;

        public ActivityAsset TargetActivity
        {
            get => targetActivity;
            set => targetActivity = value;
        }

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => ToFlowRequestEventPhase(_triggerState.LastPhase);

        public FlowRequestOutcome LastOutcome => ToFlowRequestOutcome(_triggerState.LastOutcome);

        public string LastReason => _triggerState.LastReason;

        public string LastMessage => _triggerState.LastMessage;

        public bool LastRequestClearedActivity => _lastRequestClearedActivity;

        public bool LastRequestSucceeded => _triggerState.LastSucceeded;

        public bool LastRequestIgnored => _triggerState.LastIgnored;

        public bool LastRequestFailed => _triggerState.LastFailed;

        public bool HasActivityRuntimeBinding => _activityRuntime != null;

        public string ActivityRuntimeBindingStatus =>
            HasActivityRuntimeBinding ? "Bound" : "Missing";

        public string ActivityRuntimeBindingDiagnostic =>
            _activityRuntimeBindingDiagnostic;

        public IEventBinding SubscribeRequestEvents(Action<ActivityRequestTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<ActivityRequestTrigger>();
        }

        internal bool TryBindActivityRuntime(
            IActivityRuntimePort activityRuntime,
            out string issue)
        {
            if (activityRuntime == null)
            {
                issue = "Activity runtime port binding requires a non-null port.";
                _activityRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_activityRuntime == null)
            {
                _activityRuntime = activityRuntime;
                issue = string.Empty;
                _activityRuntimeBindingDiagnostic =
                    $"Bound '{activityRuntime.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(_activityRuntime, activityRuntime))
            {
                issue = string.Empty;
                _activityRuntimeBindingDiagnostic =
                    $"Bound '{activityRuntime.GetType().FullName}' (idempotent).";
                return true;
            }

            issue =
                "Activity runtime port binding rejected a different port for the current lifetime.";
            _activityRuntimeBindingDiagnostic = issue;
            return false;
        }

        public async void RequestActivity()
        {
            EnsureLogger();

            string resolvedReason = ResolveRequestReason();

            if (_requestInFlight)
            {
                string message = "Activity Request ignored. This Activity Request Trigger already has a request in flight.";
                _logger.Warning(message);
                PublishCompleted(false, FlowRequestOutcome.Ignored, resolvedReason, message);
                return;
            }

            if (targetActivity == null)
            {
                string message = "Activity Request failed. Target Activity is missing.";
                _logger.Error(message);
                PublishCompleted(false, FlowRequestOutcome.Failed, resolvedReason, message);
                return;
            }

            IActivityRuntimePort activityRuntime = _activityRuntime;
            if (activityRuntime == null)
            {
                const string message =
                    "Activity Request failed. Activity runtime port is not bound.";
                _activityRuntimeBindingDiagnostic =
                    "Activity runtime port is not bound.";
                _logger.Error(message);
                PublishCompleted(false, FlowRequestOutcome.Failed, resolvedReason, message);
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(false, resolvedReason);

            FrameworkActivityRequestResult result;
            try
            {
                result = await activityRuntime.RequestActivityAsync(
                    targetActivity,
                    DefaultSource,
                    resolvedReason);
            }
            catch (Exception exception)
            {
                string message =
                    $"Activity Request failed with an exception. source='{DefaultSource}' reason='{resolvedReason}' exception='{exception.GetType().Name}'.";
                _logger.Error(message, exception);
                PublishCompleted(
                    false,
                    FlowRequestOutcome.Failed,
                    resolvedReason,
                    message);
                return;
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(false, MapOutcome(result.Kind), resolvedReason, result.Message);
        }

        public async void ClearActivity()
        {
            EnsureLogger();

            string resolvedReason = ResolveClearReason();

            if (_requestInFlight)
            {
                string message = "Activity Request ignored. This Activity Request Trigger already has a request in flight.";
                _logger.Warning(message);
                PublishCompleted(true, FlowRequestOutcome.Ignored, resolvedReason, message);
                return;
            }

            IActivityRuntimePort activityRuntime = _activityRuntime;
            if (activityRuntime == null)
            {
                const string message =
                    "Activity Request failed. Activity runtime port is not bound.";
                _activityRuntimeBindingDiagnostic =
                    "Activity runtime port is not bound.";
                _logger.Error(message);
                PublishCompleted(true, FlowRequestOutcome.Failed, resolvedReason, message);
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(true, resolvedReason);

            FrameworkActivityRequestResult result;
            try
            {
                result = await activityRuntime.ClearActivityAsync(
                    DefaultSource,
                    resolvedReason);
            }
            catch (Exception exception)
            {
                string message =
                    $"Activity Request failed with an exception. source='{DefaultSource}' reason='{resolvedReason}' exception='{exception.GetType().Name}'.";
                _logger.Error(message, exception);
                PublishCompleted(
                    true,
                    FlowRequestOutcome.Failed,
                    resolvedReason,
                    message);
                return;
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(true, MapOutcome(result.Kind), resolvedReason, result.Message);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<ActivityRequestTrigger>();
            }
        }

        private string ResolveRequestReason()
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason.Trim();
            }

            return targetActivity != null ? targetActivity.ActivityName : "Activity Request";
        }

        private string ResolveClearReason()
        {
            return reason.NormalizeTextOrFallback(DefaultClearReason);
        }

        private void PublishSubmitted(bool clearsActivity, string resolvedReason)
        {
            string message = $"Activity Request submitted. source='{DefaultSource}' reason='{resolvedReason}'.";
            SetRequestState(clearsActivity, FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message);

            _requestEvents.Publish(new ActivityRequestTriggerEvent(
                this,
                targetActivity,
                clearsActivity,
                FlowRequestEventPhase.Submitted,
                FlowRequestOutcome.Submitted,
                DefaultSource,
                resolvedReason,
                message));
        }

        private void PublishCompleted(bool clearsActivity, FlowRequestOutcome outcome, string resolvedReason, string message)
        {
            SetRequestState(clearsActivity, FlowRequestEventPhase.Completed, outcome, resolvedReason, message);

            _requestEvents.Publish(new ActivityRequestTriggerEvent(
                this,
                targetActivity,
                clearsActivity,
                FlowRequestEventPhase.Completed,
                outcome,
                DefaultSource,
                resolvedReason,
                message));
        }

        private void SetRequestState(
            bool clearsActivity,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string resolvedReason,
            string message)
        {
            _lastRequestClearedActivity = clearsActivity;
            if (phase == FlowRequestEventPhase.Submitted)
            {
                _triggerState.Begin(DefaultSource, resolvedReason, message);
                return;
            }

            switch (outcome)
            {
                case FlowRequestOutcome.Succeeded:
                    _triggerState.CompleteSucceeded(DefaultSource, resolvedReason, message, 0, 0);
                    break;
                case FlowRequestOutcome.Ignored:
                    _triggerState.CompleteIgnored(DefaultSource, resolvedReason, message, 0, 0);
                    break;
                case FlowRequestOutcome.Failed:
                    _triggerState.CompleteFailed(DefaultSource, resolvedReason, message, 0, 0);
                    break;
                default:
                    _triggerState.Complete(
                        outcome.ToString(),
                        false,
                        false,
                        false,
                        DefaultSource,
                        resolvedReason,
                        message,
                        0,
                        0);
                    break;
            }
        }

        private static FlowRequestOutcome MapOutcome(FrameworkActivityRequestKind resultKind)
        {
            switch (resultKind)
            {
                case FrameworkActivityRequestKind.Succeeded:
                    return FlowRequestOutcome.Succeeded;
                case FrameworkActivityRequestKind.IgnoredAlreadyActive:
                case FrameworkActivityRequestKind.IgnoredAlreadyInFlight:
                case FrameworkActivityRequestKind.IgnoredNoActiveActivity:
                case FrameworkActivityRequestKind.RejectedByTransitionGate:
                    return FlowRequestOutcome.Ignored;
                default:
                    return FlowRequestOutcome.Failed;
            }
        }

        private static FlowRequestEventPhase ToFlowRequestEventPhase(string phase)
        {
            return string.Equals(phase, FrameworkFlowTriggerState.PhaseSubmitted, StringComparison.Ordinal)
                ? FlowRequestEventPhase.Submitted
                : FlowRequestEventPhase.Completed;
        }

        private static FlowRequestOutcome ToFlowRequestOutcome(string outcome)
        {
            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeSucceeded, StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Succeeded;
            }

            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeIgnored, StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Ignored;
            }

            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeFailed, StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Failed;
            }

            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeSubmitted, StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Submitted;
            }

            return FlowRequestOutcome.None;
        }
    }
}
