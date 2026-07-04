using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Scene-authored request boundary for resetting one ResetSubject.
    /// This trigger does not resolve targets through ObjectEntry snapshots.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Reset/Object Reset Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12G Object Reset trigger over ResetSubject + ResetExecutor.")]
    public sealed class ObjectResetTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ObjectResetTrigger);
        private const string DefaultReason = "Object Reset";

        private readonly EventBus<ObjectResetTriggerEvent> _requestEvents = new EventBus<ObjectResetTriggerEvent>();
        private FrameworkLogger _logger;
        private bool _requestInFlight;
        private FlowRequestEventPhase _lastEventPhase = FlowRequestEventPhase.Completed;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;
        private ResetExecutionResult _lastResult;
        private bool _hasLastResult;

        [Header("Reset Subject Target")]
        [SerializeField] private ResetSubjectReference targetSubject = new ResetSubjectReference();

        [Header("Reset Request Policy")]
        [SerializeField] private string reason;
        [SerializeField] private bool allowNoParticipants = true;
        [SerializeField] private bool stopOnFailure = true;

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public ResetExecutionResult LastResult => _lastResult;

        public ResetExecutionResult LastExecutionResult => _lastResult;

        public bool HasLastResult => _hasLastResult;

        public ResetExecutionStatus LastResultStatus => _hasLastResult ? _lastResult.Status : ResetExecutionStatus.Unknown;

        public ResetExecutionStatus LastExecutionStatus => LastResultStatus;

        public int LastParticipantCount => _hasLastResult ? _lastResult.ParticipantCount : 0;

        public int LastSucceededParticipantCount => _hasLastResult ? _lastResult.ParticipantSucceeded : 0;

        public int LastSkippedParticipantCount => _hasLastResult ? _lastResult.ParticipantSkipped : 0;

        public int LastFailedParticipantCount => _hasLastResult ? _lastResult.ParticipantFailed : 0;

        public int LastBlockingIssueCount => _hasLastResult ? _lastResult.BlockingIssueCount : 0;

        public int LastNonBlockingIssueCount => _hasLastResult ? _lastResult.NonBlockingIssueCount : 0;

        public bool LastResultSucceededNoParticipants => HasSingleSkippedNoParticipants(_lastResult);

        public bool LastResultCompletedWithWarnings => _hasLastResult && _lastResult.Succeeded && _lastResult.NonBlockingIssueCount > 0;

        public string LastResultSummary => BuildLastResultSummary();

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public ResetSubjectReference TargetSubject => targetSubject;

        public UnityResetSubjectAdapter TargetSubjectAdapter => targetSubject?.SubjectAdapter;

        public string AuthoringResetSubjectId => targetSubject?.SubjectIdText ?? string.Empty;

        public string ResolvedTargetSubjectId => targetSubject?.ResolvedSubjectIdText ?? string.Empty;

        public string AuthoringReason => reason;

        public bool HasCustomReason => !string.IsNullOrWhiteSpace(reason);

        public bool AllowNoParticipants => allowNoParticipants;

        public bool StopOnFailure => stopOnFailure;

        public IEventBinding SubscribeRequestEvents(Action<ObjectResetTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<ObjectResetTrigger>();
        }

        [ContextMenu("Request Object Reset")]
        public async void RequestObjectReset()
        {
            EnsureLogger();

            string resolvedReason = ResolveReason();

            if (_requestInFlight)
            {
                string message = "Object Reset ignored. This Object Reset Trigger already has a request in flight.";
                var result = ResetExecutionResult.RejectedInvalidRequest(
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, message),
                    DefaultSource,
                    resolvedReason);
                _logger.Warning(message);
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, result, true);
                return;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                string message = "Object Reset failed. Application Runtime is unavailable.";
                var result = ResetExecutionResult.RejectedInvalidRequest(
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, message),
                    DefaultSource,
                    resolvedReason);
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true);
                return;
            }

            ResetSubjectId subjectId = default;
            ResetIssue targetIssue = default;
            if (targetSubject == null || !targetSubject.TryResolve(out subjectId, out targetIssue))
            {
                ResetExecutionResult rejected = ResetExecutionResult.RejectedInvalidRequest(
                    targetIssue.Kind != ResetIssueKind.Unknown ? targetIssue : ResetIssue.Error(ResetIssueKind.InvalidSubject, "Object Reset target subject is invalid."),
                    DefaultSource,
                    resolvedReason);
                LogResetExecutionResult(rejected, subjectId);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, rejected.Message, rejected, true);
                return;
            }

            var request = ResetExecutionRequest.ForSingleSubject(
                subjectId,
                allowNoParticipants,
                DefaultSource,
                resolvedReason,
                stopOnFailure);

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            ResetExecutionResult executionResult;
            try
            {
                var executor = new ResetExecutor(runtimeHost.ResetRegistry);
                executionResult = await executor.ExecuteAsync(request);
            }
            finally
            {
                _requestInFlight = false;
            }

            LogResetExecutionResult(executionResult, subjectId);
            PublishCompleted(MapOutcome(executionResult), resolvedReason, executionResult.Message, executionResult, true);
        }

        [ContextMenu("Clear Last Object Reset Result")]
        public void ClearLastResult()
        {
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, default, false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            UnityResetSubjectAdapter qaTargetSubjectAdapter,
            string qaResetSubjectId,
            string qaReason,
            bool qaAllowNoParticipants,
            bool qaStopOnFailure)
        {
            targetSubject ??= new ResetSubjectReference();
            targetSubject.ConfigureForQa(qaTargetSubjectAdapter, qaResetSubjectId);
            reason = qaReason;
            allowNoParticipants = qaAllowNoParticipants;
            stopOnFailure = qaStopOnFailure;
        }
#endif

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<ObjectResetTrigger>();
            }
        }

        private string ResolveReason()
        {
            return reason.NormalizeTextOrFallback(DefaultReason);
        }

        private void PublishSubmitted(string resolvedReason)
        {
            string message = $"Object Reset submitted. source='{DefaultSource}' reason='{resolvedReason}' subjectId='{ResolvedTargetSubjectId}'.";
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, default, false);

            _requestEvents.Publish(new ObjectResetTriggerEvent(
                this,
                FlowRequestEventPhase.Submitted,
                FlowRequestOutcome.Submitted,
                DefaultSource,
                resolvedReason,
                message,
                default,
                false));
        }

        private void PublishCompleted(
            FlowRequestOutcome outcome,
            string resolvedReason,
            string message,
            ResetExecutionResult result,
            bool hasResult)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult);

            _requestEvents.Publish(new ObjectResetTriggerEvent(
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
            ResetExecutionResult result,
            bool hasResult)
        {
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
            _lastResult = result;
            _hasLastResult = hasResult;
        }

        private void LogResetExecutionResult(ResetExecutionResult result, ResetSubjectId subjectId)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("source", DefaultSource),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("subjectId", subjectId.IsValid ? subjectId.StableText : ResolvedTargetSubjectId),
                LogFields.Field("subjects", result.SubjectCount),
                LogFields.Field("subjectSucceeded", result.SubjectSucceeded),
                LogFields.Field("subjectFailed", result.SubjectFailed),
                LogFields.Field("participants", result.ParticipantCount),
                LogFields.Field("participantSucceeded", result.ParticipantSucceeded),
                LogFields.Field("participantSkipped", result.ParticipantSkipped),
                LogFields.Field("participantFailed", result.ParticipantFailed),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount));

            if (result.Succeeded)
            {
                _logger.Info("Object Reset Request completed.", fields);
                _logger.Debug("Object Reset Request diagnostics. " + result);
                return;
            }

            _logger.Error("Object Reset Request failed. " + result, fields);
        }

        private string BuildLastResultSummary()
        {
            if (!_hasLastResult)
            {
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Object Reset result yet." : _lastMessage;
            }

            return $"status='{LastExecutionStatus}' subjectId='{ResolvedTargetSubjectId}' subjects='{_lastResult.SubjectCount}' subjectSucceeded='{_lastResult.SubjectSucceeded}' subjectFailed='{_lastResult.SubjectFailed}' participants='{_lastResult.ParticipantCount}' participantSucceeded='{_lastResult.ParticipantSucceeded}' participantSkipped='{_lastResult.ParticipantSkipped}' participantFailed='{_lastResult.ParticipantFailed}' blockingIssues='{_lastResult.BlockingIssueCount}' nonBlockingIssues='{_lastResult.NonBlockingIssueCount}'";
        }

        private static bool HasSingleSkippedNoParticipants(ResetExecutionResult result)
        {
            return result.Subjects.Count == 1
                && result.Subjects[0].Status == ResetSubjectResultStatus.SkippedNoParticipants;
        }

        private static FlowRequestOutcome MapOutcome(ResetExecutionResult result)
        {
            return result.Succeeded ? FlowRequestOutcome.Succeeded : FlowRequestOutcome.Failed;
        }
    }
}
