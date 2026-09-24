using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Scene-authored request boundary for resetting the active Route cycle.
    /// It does not resolve lifecycle state or execute reset; it forwards to its explicitly bound runtime port.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Cycle Reset/Route Cycle Reset Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11F public authored trigger for Route Cycle Reset requests.")]
    public sealed class RouteCycleResetTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(RouteCycleResetTrigger);
        private const string DefaultReason = "Route Cycle Reset";

        private readonly EventBus<CycleResetTriggerEvent> _requestEvents = new EventBus<CycleResetTriggerEvent>();
        private FrameworkLogger _logger;
        private bool _requestInFlight;
        private IRouteCycleResetRuntimePort _routeCycleResetRuntime;
        private string _routeCycleResetRuntimeBindingDiagnostic =
            "Route Cycle Reset runtime port is not bound.";
        private FlowRequestEventPhase _lastEventPhase = FlowRequestEventPhase.Completed;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;
        private CycleResetResult _lastResult;
        private bool _hasLastResult;

        [Header("Cycle Reset")]
        [SerializeField] private string reason;

        public bool IsRequestInFlight => _requestInFlight;
        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;
        public FlowRequestOutcome LastOutcome => _lastOutcome;
        public string LastReason => _lastReason;
        public string LastMessage => _lastMessage;
        public CycleResetResult LastResult => _lastResult;
        public bool HasLastResult => _hasLastResult;
        public CycleResetStatus LastResultStatus => _hasLastResult ? _lastResult.Status : CycleResetStatus.Unknown;
        public int LastParticipantCount => _hasLastResult ? _lastResult.ParticipantCount : 0;
        public int LastSucceededParticipantCount => _hasLastResult ? _lastResult.SucceededCount : 0;
        public int LastSkippedParticipantCount => _hasLastResult ? _lastResult.SkippedCount : 0;
        public int LastFailedParticipantCount => _hasLastResult ? _lastResult.FailedCount : 0;
        public int LastBlockingIssueCount => _hasLastResult ? _lastResult.BlockingIssueCount : 0;
        public int LastNonBlockingIssueCount => _hasLastResult ? _lastResult.NonBlockingIssueCount : 0;
        public bool LastResultSucceededNoParticipants => _hasLastResult && _lastResult.Status == CycleResetStatus.SucceededNoParticipants;
        public bool LastResultCompletedWithWarnings => _hasLastResult && _lastResult.CompletedWithWarnings;
        public string LastResultSummary => BuildLastResultSummary();
        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;
        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;
        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;
        public string AuthoringReason => reason;
        public bool HasCustomReason => !string.IsNullOrWhiteSpace(reason);
        public bool HasRouteCycleResetRuntimeBinding => _routeCycleResetRuntime != null;
        public string RouteCycleResetRuntimeBindingStatus =>
            HasRouteCycleResetRuntimeBinding ? "Bound" : "Missing";
        public string RouteCycleResetRuntimeBindingDiagnostic =>
            _routeCycleResetRuntimeBindingDiagnostic;

        public IEventBinding SubscribeRequestEvents(Action<CycleResetTriggerEvent> handler) =>
            _requestEvents.Subscribe(handler);

        private void Awake() => _logger = FrameworkLogger.Create<RouteCycleResetTrigger>();

        internal bool TryBindRouteCycleResetRuntime(
            IRouteCycleResetRuntimePort routeCycleResetRuntime,
            out string issue)
        {
            if (routeCycleResetRuntime == null)
            {
                issue = "Route Cycle Reset runtime port binding requires a non-null port.";
                _routeCycleResetRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_routeCycleResetRuntime == null)
            {
                _routeCycleResetRuntime = routeCycleResetRuntime;
                issue = string.Empty;
                _routeCycleResetRuntimeBindingDiagnostic =
                    $"Bound '{routeCycleResetRuntime.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(_routeCycleResetRuntime, routeCycleResetRuntime))
            {
                issue = string.Empty;
                _routeCycleResetRuntimeBindingDiagnostic =
                    $"Bound '{routeCycleResetRuntime.GetType().FullName}' (idempotent).";
                return true;
            }

            issue = "Route Cycle Reset runtime port binding rejected a different port for the current lifetime.";
            _routeCycleResetRuntimeBindingDiagnostic = issue;
            return false;
        }

        [ContextMenu("Request Route Cycle Reset")]
        public async void RequestRouteCycleReset()
        {
            EnsureLogger();
            string resolvedReason = ResolveReason();

            if (_requestInFlight)
            {
                const string message = "Route Cycle Reset ignored. This Route Cycle Reset Trigger already has a request in flight.";
                _logger.Warning(message);
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, default, false);
                return;
            }

            IRouteCycleResetRuntimePort routeCycleResetRuntime = _routeCycleResetRuntime;
            if (routeCycleResetRuntime == null)
            {
                const string message = "Route Cycle Reset failed. Route Cycle Reset runtime port is not bound.";
                _routeCycleResetRuntimeBindingDiagnostic =
                    "Route Cycle Reset runtime port is not bound.";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);
            CycleResetResult result;
            try
            {
                result = await routeCycleResetRuntime.RequestRouteCycleResetAsync(
                    DefaultSource,
                    resolvedReason);
            }
            catch (Exception exception)
            {
                string message =
                    $"Route Cycle Reset failed with an exception. source='{DefaultSource}' reason='{resolvedReason}' exception='{exception.GetType().Name}'.";
                _logger.Error(message, exception);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(MapOutcome(result), resolvedReason, result.Message, result, true);
        }

        [ContextMenu("Clear Last Cycle Reset Result")]
        public void ClearLastResult() =>
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, default, false);

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<RouteCycleResetTrigger>();
            }
        }

        private string ResolveReason() => reason.NormalizeTextOrFallback(DefaultReason);

        private void PublishSubmitted(string resolvedReason)
        {
            string message = $"Route Cycle Reset submitted. source='{DefaultSource}' reason='{resolvedReason}'.";
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, default, false);
            _requestEvents.Publish(new CycleResetTriggerEvent(this, CycleResetScope.Route, FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, DefaultSource, resolvedReason, message, default, false));
        }

        private void PublishCompleted(FlowRequestOutcome outcome, string resolvedReason, string message, CycleResetResult result, bool hasResult)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult);
            _requestEvents.Publish(new CycleResetTriggerEvent(this, CycleResetScope.Route, FlowRequestEventPhase.Completed, outcome, DefaultSource, resolvedReason, message, result, hasResult));
        }

        private void SetRequestState(FlowRequestEventPhase phase, FlowRequestOutcome outcome, string resolvedReason, string message, CycleResetResult result, bool hasResult)
        {
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
            _lastResult = result;
            _hasLastResult = hasResult;
        }

        private string BuildLastResultSummary()
        {
            if (!_hasLastResult)
            {
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Cycle Reset result yet." : _lastMessage;
            }

            return $"status='{_lastResult.Status}' participants='{_lastResult.ParticipantCount}' participantSucceeded='{_lastResult.SucceededCount}' participantSkipped='{_lastResult.SkippedCount}' participantFailed='{_lastResult.FailedCount}' blockingIssues='{_lastResult.BlockingIssueCount}' nonBlockingIssues='{_lastResult.NonBlockingIssueCount}'";
        }

        private static FlowRequestOutcome MapOutcome(CycleResetResult result) =>
            result.Succeeded || result.CompletedWithWarnings
                ? FlowRequestOutcome.Succeeded
                : FlowRequestOutcome.Failed;
    }
}
