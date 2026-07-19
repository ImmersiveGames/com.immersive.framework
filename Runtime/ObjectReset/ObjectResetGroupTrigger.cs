using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Reset;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Scene-authored request boundary for resetting a selected set of ResetSubjects.
    /// This trigger does not resolve targets through ObjectEntry snapshots.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Reset/Object Reset Group Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "H2.2.8 explicit runtime binding for Object Reset Group.")]
    public sealed class ObjectResetGroupTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ObjectResetGroupTrigger);
        private const string DefaultGroupId = "object-reset-group";
        private const string DefaultReason = "Object Reset Group";

        private readonly EventBus<ObjectResetGroupTriggerEvent> _requestEvents = new EventBus<ObjectResetGroupTriggerEvent>();
        private FrameworkLogger _logger;
        private IResetSelectionExecutionRuntimePort _resetSelectionExecutionRuntime;
        private string _resetSelectionExecutionRuntimeBindingDiagnostic = "Reset selection execution runtime port is not bound.";
        private bool _requestInFlight;
        private FlowRequestEventPhase _lastEventPhase = FlowRequestEventPhase.Completed;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;
        private ResetExecutionResult _lastResult;
        private ResetSelectionResolution _lastSelectionResolution;
        private bool _hasLastResult;

        [Header("Object Reset Group")]
        [SerializeField] private string groupId = DefaultGroupId;
        [SerializeField] private string reason;

        [Header("Reset Selection")]
        [SerializeField] private ResetSelectionConfig selection = new ResetSelectionConfig();

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public ResetExecutionResult LastResult => _lastResult;

        public ResetExecutionResult LastExecutionResult => _lastResult;

        public ResetSelectionResolution LastSelectionResolution => _lastSelectionResolution;

        public bool HasLastResult => _hasLastResult;

        public ResetExecutionStatus LastResultStatus => _hasLastResult ? _lastResult.Status : ResetExecutionStatus.Unknown;

        public ResetExecutionStatus LastExecutionStatus => LastResultStatus;

        public int LastTargetCount => _hasLastResult ? _lastResult.SubjectCount : 0;

        public int LastSucceededTargetCount => _hasLastResult ? _lastResult.SubjectSucceeded : 0;

        public int LastWarningTargetCount => _hasLastResult ? _lastResult.Subjects.Count(subject => subject.NonBlockingIssueCount > 0 && subject.Succeeded) : 0;

        public int LastSkippedTargetCount => _hasLastResult ? _lastResult.Subjects.Count(subject => subject.Status == ResetSubjectResultStatus.SkippedNoParticipants) : 0;

        public int LastFailedTargetCount => _hasLastResult ? _lastResult.SubjectFailed : 0;

        public int LastParticipantCount => _hasLastResult ? _lastResult.ParticipantCount : 0;

        public int LastSucceededParticipantCount => _hasLastResult ? _lastResult.ParticipantSucceeded : 0;

        public int LastSkippedParticipantCount => _hasLastResult ? _lastResult.ParticipantSkipped : 0;

        public int LastFailedParticipantCount => _hasLastResult ? _lastResult.ParticipantFailed : 0;

        public int LastBlockingIssueCount => _hasLastResult ? _lastResult.BlockingIssueCount : 0;

        public int LastNonBlockingIssueCount => _hasLastResult ? _lastResult.NonBlockingIssueCount : 0;

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public string LastResultSummary => BuildLastResultSummary();

        public string ResolvedGroupId => ResolveGroupId();

        public string ResolvedReason => ResolveReason();

        public ResetSelectionConfig Selection => selection;

        public ResetSelectionMode ResolvedSelectionMode => selection != null ? selection.Mode : ResetSelectionMode.ExplicitSubjects;

        public bool ResolvedAllowNoSubjects => selection != null && selection.AllowNoSubjects;

        public bool ResolvedAllowNoParticipants => selection == null || selection.AllowNoParticipants;

        public bool ResolvedStopOnFailure => selection == null || selection.StopOnFailure;

        public bool HasResetSelectionExecutionRuntimeBinding => _resetSelectionExecutionRuntime != null;

        public string ResetSelectionExecutionRuntimeBindingStatus => HasResetSelectionExecutionRuntimeBinding ? "Bound" : "Missing";

        public string ResetSelectionExecutionRuntimeBindingDiagnostic => _resetSelectionExecutionRuntimeBindingDiagnostic;

        public IEventBinding SubscribeRequestEvents(Action<ObjectResetGroupTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<ObjectResetGroupTrigger>();
        }

        internal bool TryBindResetSelectionExecutionRuntime(
            IResetSelectionExecutionRuntimePort resetSelectionExecutionRuntime,
            out string issue)
        {
            if (resetSelectionExecutionRuntime == null)
            {
                issue = "Reset selection execution runtime port binding requires a non-null port.";
                _resetSelectionExecutionRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_resetSelectionExecutionRuntime == null)
            {
                _resetSelectionExecutionRuntime = resetSelectionExecutionRuntime;
                issue = string.Empty;
                _resetSelectionExecutionRuntimeBindingDiagnostic = $"Bound '{resetSelectionExecutionRuntime.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(_resetSelectionExecutionRuntime, resetSelectionExecutionRuntime))
            {
                issue = string.Empty;
                _resetSelectionExecutionRuntimeBindingDiagnostic = $"Bound '{resetSelectionExecutionRuntime.GetType().FullName}' (idempotent).";
                return true;
            }

            issue = "Reset selection execution runtime port binding rejected a different port for the current lifetime.";
            _resetSelectionExecutionRuntimeBindingDiagnostic = issue;
            return false;
        }

        [ContextMenu("Request Object Reset Group")]
        public async void RequestObjectResetGroup()
        {
            await RequestObjectResetGroupAsync();
        }

        public async Awaitable<ResetExecutionResult> RequestObjectResetGroupAsync()
        {
            EnsureLogger();

            string resolvedReason = ResolveReason();
            ResetSelectionConfig resolvedSelection = ResolveSelection();

            if (_requestInFlight)
            {
                string message = "Object Reset Group ignored. This Object Reset Group Trigger already has a request in flight.";
                var result = ResetExecutionResult.RejectedInvalidRequest(
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, message),
                    DefaultSource,
                    resolvedReason);
                _logger.Warning(message, BuildGroupFields(ResolveGroupId(), resolvedReason));
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, result, true, default);
                return result;
            }

            IResetSelectionExecutionRuntimePort resetSelectionExecutionRuntime = _resetSelectionExecutionRuntime;
            if (resetSelectionExecutionRuntime == null)
            {
                string message = "Object Reset Group failed. Reset selection execution runtime port is not bound.";
                _resetSelectionExecutionRuntimeBindingDiagnostic = "Reset selection execution runtime port is not bound.";
                var result = ResetExecutionResult.RejectedInvalidRequest(
                    ResetIssue.Error(ResetIssueKind.InvalidRequest, message),
                    DefaultSource,
                    resolvedReason);
                LogGroupResult(result, default);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true, default);
                return result;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            ResetSelectionExecutionRuntimeResult runtimeResult;
            try
            {
                runtimeResult = await resetSelectionExecutionRuntime.ExecuteResetSelectionAsync(
                    resolvedSelection,
                    DefaultSource,
                    resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }

            ResetSelectionResolution selectionResolution = runtimeResult.SelectionResolution;
            ResetExecutionResult executionResult = runtimeResult.ExecutionResult;
            LogGroupResult(executionResult, selectionResolution);
            PublishCompleted(MapOutcome(executionResult), resolvedReason, executionResult.Message, executionResult, true, selectionResolution);
            return executionResult;
        }

        [ContextMenu("Clear Last Object Reset Group Result")]
        public void ClearLastResult()
        {
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, default, false, default);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            string qaGroupId,
            string qaReason,
            ResetSelectionMode qaSelectionMode,
            IReadOnlyList<ResetSubjectReference> qaExplicitSubjects,
            bool qaAllowNoSubjects,
            bool qaAllowNoParticipants,
            bool qaStopOnFailure,
            bool qaYieldBetweenSubjects)
        {
            groupId = qaGroupId;
            reason = qaReason;
            selection ??= new ResetSelectionConfig();
            selection.ConfigureForQa(
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
                _logger = FrameworkLogger.Create<ObjectResetGroupTrigger>();
            }
        }

        private string ResolveGroupId()
        {
            return groupId.NormalizeTextOrFallback(DefaultGroupId);
        }

        private string ResolveReason()
        {
            return reason.NormalizeTextOrFallback(DefaultReason);
        }

        private ResetSelectionConfig ResolveSelection()
        {
            selection ??= new ResetSelectionConfig();
            return selection;
        }

        private void PublishSubmitted(string resolvedReason)
        {
            string message = $"Object Reset Group submitted. source='{DefaultSource}' reason='{resolvedReason}' groupId='{ResolveGroupId()}' selectionMode='{ResolvedSelectionMode}'.";
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, default, false, default);

            _requestEvents.Publish(new ObjectResetGroupTriggerEvent(
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
            bool hasResult,
            ResetSelectionResolution selectionResolution)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult, selectionResolution);

            _requestEvents.Publish(new ObjectResetGroupTriggerEvent(
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
            bool hasResult,
            ResetSelectionResolution selectionResolution)
        {
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
            _lastResult = result;
            _hasLastResult = hasResult;
            _lastSelectionResolution = selectionResolution;
        }

        private void LogGroupResult(
            ResetExecutionResult result,
            ResetSelectionResolution selectionResolution)
        {
            LogField[] summaryFields = BuildGroupSummaryFields(result);
            LogField[] diagnosticFields = BuildGroupDiagnosticFields(result, selectionResolution);
            if (result.Succeeded)
            {
                _logger.Info("Object Reset Group Request completed.", summaryFields);
                _logger.Debug("Object Reset Group Request diagnostics. " + result, diagnosticFields);
                return;
            }

            _logger.Error("Object Reset Group Request failed.", summaryFields);
            _logger.Debug("Object Reset Group Request diagnostics. " + result, diagnosticFields);
        }

        private LogField[] BuildGroupFields(string resolvedGroupId, string resolvedReason)
        {
            return LogFields.Of(
                LogFields.Field("source", DefaultSource),
                LogFields.Field("reason", resolvedReason),
                LogFields.Field("groupId", resolvedGroupId));
        }

        private LogField[] BuildGroupSummaryFields(ResetExecutionResult result)
        {
            return LogFields.Of(
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("groupId", ResolveGroupId()),
                LogFields.Field("blockingIssues", result.BlockingIssueCount));
        }

        private LogField[] BuildGroupDiagnosticFields(
            ResetExecutionResult result,
            ResetSelectionResolution selectionResolution)
        {
            return LogFields.Of(
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("groupId", ResolveGroupId()),
                LogFields.Field("selectionMode", selectionResolution.Status != ResetSelectionResolutionStatus.Unknown ? selectionResolution.Mode.ToString() : ResolvedSelectionMode.ToString()),
                LogFields.Field("subjects", result.SubjectCount),
                LogFields.Field("subjectSucceeded", result.SubjectSucceeded),
                LogFields.Field("subjectFailed", result.SubjectFailed),
                LogFields.Field("participants", result.ParticipantCount),
                LogFields.Field("participantSucceeded", result.ParticipantSucceeded),
                LogFields.Field("participantSkipped", result.ParticipantSkipped),
                LogFields.Field("participantFailed", result.ParticipantFailed),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount));
        }

        private string BuildLastResultSummary()
        {
            if (!_hasLastResult)
            {
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Object Reset Group result yet." : _lastMessage;
            }

            return $"status='{LastExecutionStatus}' selectionMode='{ResolvedSelectionMode}' subjects='{_lastResult.SubjectCount}' subjectSucceeded='{_lastResult.SubjectSucceeded}' subjectFailed='{_lastResult.SubjectFailed}' participants='{_lastResult.ParticipantCount}' participantSucceeded='{_lastResult.ParticipantSucceeded}' participantSkipped='{_lastResult.ParticipantSkipped}' participantFailed='{_lastResult.ParticipantFailed}' blockingIssues='{_lastResult.BlockingIssueCount}' nonBlockingIssues='{_lastResult.NonBlockingIssueCount}'";
        }

        private static FlowRequestOutcome MapOutcome(ResetExecutionResult result)
        {
            return result.Succeeded ? FlowRequestOutcome.Succeeded : FlowRequestOutcome.Failed;
        }
    }
}
