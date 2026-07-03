using System;
using System.Collections.Generic;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using Immersive.Logging.Records;
using UnityEngine;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Scene-authored request boundary for resetting multiple logical ObjectEntry targets in sequence.
    /// It does not restart Activities, reload scenes, discover participants or perform any reset side effect itself.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Reset/Object Reset Group Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F39A public authored trigger for Object Reset Group requests.")]
    public sealed class ObjectResetGroupTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ObjectResetGroupTrigger);
        private const string DefaultGroupId = "object-reset-group";
        private const string DefaultReason = "Object Reset Group";

        private readonly EventBus<ObjectResetGroupTriggerEvent> _requestEvents = new EventBus<ObjectResetGroupTriggerEvent>();
        private FrameworkLogger _logger;
        private bool _requestInFlight;
        private FlowRequestEventPhase _lastEventPhase = FlowRequestEventPhase.Completed;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;
        private ObjectResetGroupResult _lastResult;
        private bool _hasLastResult;

        [Header("Object Reset Group")]
        [SerializeField] private ObjectResetGroupAsset groupAsset;
        [SerializeField] private string groupId = DefaultGroupId;
        [SerializeField] private string reason;
        [SerializeField] private bool allowNoParticipants = true;
        [SerializeField] private bool stopOnFailure = true;

        [Header("Inline Targets")]
        [SerializeField] private List<ObjectResetGroupEntry> entries = new List<ObjectResetGroupEntry>();

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public ObjectResetGroupResult LastResult => _lastResult;

        public bool HasLastResult => _hasLastResult;

        public ObjectResetGroupResultStatus LastResultStatus => _hasLastResult ? _lastResult.Status : ObjectResetGroupResultStatus.Unknown;

        public int LastTargetCount => _hasLastResult ? _lastResult.TargetCount : 0;

        public int LastSucceededTargetCount => _hasLastResult ? _lastResult.TargetSucceededCount : 0;

        public int LastWarningTargetCount => _hasLastResult ? _lastResult.TargetWarningCount : 0;

        public int LastSkippedTargetCount => _hasLastResult ? _lastResult.TargetSkippedCount : 0;

        public int LastFailedTargetCount => _hasLastResult ? _lastResult.TargetFailedCount : 0;

        public int LastBlockingIssueCount => _hasLastResult ? _lastResult.BlockingIssueCount : 0;

        public int LastNonBlockingIssueCount => _hasLastResult ? _lastResult.NonBlockingIssueCount : 0;

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public string LastResultSummary => BuildLastResultSummary();

        public ObjectResetGroupAsset GroupAsset => groupAsset;

        public string ResolvedGroupId => ResolveGroupId();

        public string ResolvedReason => ResolveReason();

        public bool ResolvedAllowNoParticipants => ResolveAllowNoParticipants();

        public bool ResolvedStopOnFailure => ResolveStopOnFailure();

        public IReadOnlyList<ObjectResetGroupEntry> ResolvedEntries => ResolveEntries();

        public IEventBinding SubscribeRequestEvents(Action<ObjectResetGroupTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<ObjectResetGroupTrigger>();
        }

        [ContextMenu("Request Object Reset Group")]
        public async void RequestObjectResetGroup()
        {
            await RequestObjectResetGroupAsync();
        }

        public async Awaitable<ObjectResetGroupResult> RequestObjectResetGroupAsync()
        {
            EnsureLogger();

            string resolvedGroupId = ResolveGroupId();
            string resolvedReason = ResolveReason();
            bool resolvedAllowNoParticipants = ResolveAllowNoParticipants();
            bool resolvedStopOnFailure = ResolveStopOnFailure();

            if (_requestInFlight)
            {
                string message = "Object Reset Group ignored. This Object Reset Group Trigger already has a request in flight.";
                var result = ObjectResetGroupResult.Rejected(
                    resolvedGroupId,
                    DefaultSource,
                    resolvedReason,
                    ObjectResetGroupResultStatus.RejectedAlreadyInFlight,
                    message);
                _logger.Warning(message, BuildGroupFields(resolvedGroupId, resolvedReason));
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, result, true);
                return result;
            }

            IReadOnlyList<ObjectResetGroupEntry> resolvedEntries = ResolveEntries();
            if (resolvedEntries.Count == 0)
            {
                string message = "Object Reset Group failed. No targets are configured.";
                var result = ObjectResetGroupResult.Rejected(
                    resolvedGroupId,
                    DefaultSource,
                    resolvedReason,
                    ObjectResetGroupResultStatus.RejectedInvalidRequest,
                    message);
                LogGroupResult(result);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true);
                return result;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                string message = "Object Reset Group failed. Application Runtime is unavailable.";
                var result = ObjectResetGroupResult.Rejected(
                    resolvedGroupId,
                    DefaultSource,
                    resolvedReason,
                    ObjectResetGroupResultStatus.RejectedRuntimeUnavailable,
                    message);
                LogGroupResult(result);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, result, true);
                return result;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            ObjectResetGroupResult groupResult;
            try
            {
                groupResult = await ObjectResetGroupExecutor.ExecuteAsync(
                    runtimeHost,
                    ObjectResetSelectionMode.ExplicitTargets,
                    resolvedEntries,
                    resolvedGroupId,
                    DefaultSource,
                    resolvedReason,
                    resolvedAllowNoParticipants,
                    resolvedStopOnFailure);
            }
            finally
            {
                _requestInFlight = false;
            }

            LogGroupResult(groupResult);
            PublishCompleted(MapOutcome(groupResult), resolvedReason, groupResult.Message, groupResult, true);
            return groupResult;
        }

        [ContextMenu("Clear Last Object Reset Group Result")]
        public void ClearLastResult()
        {
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, null, false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            ObjectResetGroupAsset qaGroupAsset,
            string qaGroupId,
            string qaReason,
            bool qaAllowNoParticipants,
            bool qaStopOnFailure,
            List<ObjectResetGroupEntry> qaEntries)
        {
            groupAsset = qaGroupAsset;
            groupId = qaGroupId;
            reason = qaReason;
            allowNoParticipants = qaAllowNoParticipants;
            stopOnFailure = qaStopOnFailure;
            entries = qaEntries ?? new List<ObjectResetGroupEntry>();
        }
#endif

        private bool TryCreateRequest(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetGroupEntry entry,
            int index,
            bool resolvedAllowNoParticipants,
            string resolvedReason,
            out ObjectResetRequest request,
            out string failedTargetId,
            out string failureMessage)
        {
            request = default;
            failedTargetId = entry == null ? string.Empty : entry.ResolveObjectEntryIdText();
            failureMessage = string.Empty;

            if (entry == null)
            {
                failureMessage = "Object Reset Group target failed. Entry is null.";
                return false;
            }

            string idText = entry.ResolveObjectEntryIdText();
            failedTargetId = idText;
            if (string.IsNullOrWhiteSpace(idText))
            {
                failureMessage = $"Object Reset Group target failed. Object Entry Id is missing. index='{index}'.";
                return false;
            }

            ObjectEntryId id;
            try
            {
                id = ObjectEntryId.From(idText);
            }
            catch (ArgumentException exception)
            {
                failureMessage = $"Object Reset Group target failed. Object Entry Id is invalid. index='{index}' objectEntry='{idText}'. {exception.Message}";
                return false;
            }

            if (!snapshot.TryGet(id, out var descriptor))
            {
                failureMessage = $"Object Reset Group target failed. Object Entry target was not found in the current snapshot. index='{index}' objectEntry='{id.StableText}'.";
                return false;
            }

            try
            {
                request = new ObjectResetRequest(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    new ObjectResetPolicy(requireCurrentSnapshot: true, allowNoParticipants: resolvedAllowNoParticipants),
                    DefaultSource,
                    entry.ResolveReason(resolvedReason));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                failureMessage = $"Object Reset Group target failed. Request could not be created. index='{index}' objectEntry='{id.StableText}'. {exception.Message}";
                return false;
            }

            return true;
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<ObjectResetGroupTrigger>();
            }
        }

        private string ResolveGroupId()
        {
            if (groupAsset != null)
            {
                return groupAsset.GroupId;
            }

            return groupId.NormalizeTextOrFallback(DefaultGroupId);
        }

        private string ResolveReason()
        {
            string localReason = reason.NormalizeText();
            if (!string.IsNullOrWhiteSpace(localReason))
            {
                return localReason;
            }

            return groupAsset != null ? groupAsset.Reason : DefaultReason;
        }

        private bool ResolveAllowNoParticipants()
        {
            return groupAsset != null ? groupAsset.AllowNoParticipants : allowNoParticipants;
        }

        private bool ResolveStopOnFailure()
        {
            return groupAsset != null ? groupAsset.StopOnFailure : stopOnFailure;
        }

        private IReadOnlyList<ObjectResetGroupEntry> ResolveEntries()
        {
            if (groupAsset != null)
            {
                return groupAsset.Entries;
            }

            return entries != null ? (IReadOnlyList<ObjectResetGroupEntry>)entries : Array.Empty<ObjectResetGroupEntry>();
        }

        private void PublishSubmitted(string resolvedReason)
        {
            string message = $"Object Reset Group submitted. source='{DefaultSource}' reason='{resolvedReason}' groupId='{ResolveGroupId()}'.";
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, null, false);

            _requestEvents.Publish(new ObjectResetGroupTriggerEvent(
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
            ObjectResetGroupResult result,
            bool hasResult)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult);

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
            ObjectResetGroupResult result,
            bool hasResult)
        {
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
            _lastResult = result;
            _hasLastResult = hasResult;
        }

        private void LogGroupResult(ObjectResetGroupResult result)
        {
            if (result == null)
            {
                _logger.Error("Object Reset Group Request failed. Result is null.");
                return;
            }

            if (result.Succeeded || result.CompletedWithWarnings)
            {
                _logger.Info("Object Reset Group Request completed.", BuildGroupFields(result));
                _logger.Debug("Object Reset Group Request diagnostics. " + result.ToDiagnosticString());
                return;
            }

            _logger.Error("Object Reset Group Request failed. " + result.ToDiagnosticString(), BuildGroupFields(result));
        }

        private LogField[] BuildGroupFields(string resolvedGroupId, string resolvedReason)
        {
            return LogFields.Of(
                LogFields.Field("source", DefaultSource),
                LogFields.Field("reason", resolvedReason),
                LogFields.Field("groupId", resolvedGroupId));
        }

        private LogField[] BuildGroupFields(ObjectResetGroupResult result)
        {
            return LogFields.Of(
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("groupId", result.GroupId),
                LogFields.Field("targets", result.TargetCount),
                LogFields.Field("targetSucceeded", result.TargetSucceededCount),
                LogFields.Field("targetWarnings", result.TargetWarningCount),
                LogFields.Field("targetSkipped", result.TargetSkippedCount),
                LogFields.Field("targetFailed", result.TargetFailedCount),
                LogFields.Field("resetRequests", result.ResetRequestCount),
                LogFields.Field("participants", result.ParticipantCount),
                LogFields.Field("participantSucceeded", result.ParticipantSucceededCount),
                LogFields.Field("participantSkipped", result.ParticipantSkippedCount),
                LogFields.Field("participantFailed", result.ParticipantFailedCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount),
                LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount),
                LogFields.Field("stoppedOnFailure", result.StoppedOnFailure));
        }

        private string BuildLastResultSummary()
        {
            if (!_hasLastResult || _lastResult == null)
            {
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Object Reset Group result yet." : _lastMessage;
            }

            return $"status='{_lastResult.Status}' targets='{_lastResult.TargetCount}' targetSucceeded='{_lastResult.TargetSucceededCount}' targetWarnings='{_lastResult.TargetWarningCount}' targetSkipped='{_lastResult.TargetSkippedCount}' targetFailed='{_lastResult.TargetFailedCount}' blockingIssues='{_lastResult.BlockingIssueCount}' nonBlockingIssues='{_lastResult.NonBlockingIssueCount}'";
        }

        private static FlowRequestOutcome MapOutcome(ObjectResetGroupResult result)
        {
            if (result != null && (result.Succeeded || result.CompletedWithWarnings))
            {
                return FlowRequestOutcome.Succeeded;
            }

            return FlowRequestOutcome.Failed;
        }
    }
}
