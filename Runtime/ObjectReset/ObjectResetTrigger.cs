using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Scene-authored request boundary for resetting one logical ObjectEntry target.
    /// It does not reset Transform, Rigidbody, Animator, GameObject activation, Player, Actor, prefab, pool, save or scene reload state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Reset/Object Reset Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14F public authored trigger for Object Reset requests.")]
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
        private ObjectResetResult _lastResult;
        private bool _hasLastResult;

        [Header("Object Reset Target")]
        [SerializeField] private ObjectEntryDeclaration targetDeclaration;
        [SerializeField] private string objectEntryId;

        [Header("Object Reset Request")]
        [SerializeField] private string reason;
        [SerializeField] private bool allowNoParticipants = true;

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public ObjectResetResult LastResult => _lastResult;

        public bool HasLastResult => _hasLastResult;

        public ObjectResetResultStatus LastResultStatus => _hasLastResult ? _lastResult.Status : ObjectResetResultStatus.Unknown;

        public int LastParticipantCount => _hasLastResult ? _lastResult.ParticipantCount : 0;

        public int LastSucceededParticipantCount => _hasLastResult ? _lastResult.ParticipantSucceededCount : 0;

        public int LastSkippedParticipantCount => _hasLastResult ? _lastResult.ParticipantSkippedCount : 0;

        public int LastFailedParticipantCount => _hasLastResult ? _lastResult.ParticipantFailedCount : 0;

        public int LastBlockingIssueCount => _hasLastResult ? _lastResult.BlockingIssueCount : 0;

        public int LastNonBlockingIssueCount => _hasLastResult ? _lastResult.NonBlockingIssueCount : 0;

        public bool LastResultSucceededNoParticipants => _hasLastResult && _lastResult.Status == ObjectResetResultStatus.SucceededNoParticipants;

        public bool LastResultCompletedWithWarnings => _hasLastResult && _lastResult.CompletedWithWarnings;

        public string LastResultSummary => BuildLastResultSummary();

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public ObjectEntryDeclaration TargetDeclaration => targetDeclaration;

        public string AuthoringObjectEntryId => objectEntryId;

        public string ResolvedAuthoringObjectEntryId => ResolveObjectEntryIdText();

        public string AuthoringReason => reason;

        public bool HasCustomReason => !string.IsNullOrWhiteSpace(reason);

        public bool AllowNoParticipants => allowNoParticipants;

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
                _logger.Warning(message);
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, default, false);
                return;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                string message = "Object Reset failed. Application Runtime is unavailable.";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            string idText = ResolveObjectEntryIdText();
            if (string.IsNullOrWhiteSpace(idText))
            {
                string message = "Object Reset failed. Object Entry Id is missing.";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            ObjectEntryId id;
            try
            {
                id = ObjectEntryId.From(idText);
            }
            catch (ArgumentException exception)
            {
                string message = $"Object Reset failed. Object Entry Id is invalid. {exception.Message}";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            if (!runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var snapshot) || snapshot == null || !snapshot.IsAvailable)
            {
                string message = "Object Reset failed. Current Object Entry runtime context snapshot is unavailable.";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            if (!snapshot.TryGet(id, out var descriptor))
            {
                string message = $"Object Reset failed. Object Entry target was not found in the current snapshot. objectEntry='{id.StableText}'.";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            ObjectResetRequest request;
            try
            {
                request = new ObjectResetRequest(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    new ObjectResetPolicy(requireCurrentSnapshot: true, allowNoParticipants: allowNoParticipants),
                    DefaultSource,
                    resolvedReason);
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                string message = $"Object Reset failed. Request could not be created. {exception.Message}";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            ObjectResetResult result;
            try
            {
                result = await runtimeHost.RequestObjectResetAsync(request);
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(MapOutcome(result), resolvedReason, result.Message, result, true);
        }

        [ContextMenu("Clear Last Object Reset Result")]
        public void ClearLastResult()
        {
            SetRequestState(FlowRequestEventPhase.Completed, FlowRequestOutcome.None, string.Empty, string.Empty, default, false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void ConfigureForQa(
            ObjectEntryDeclaration qaTargetDeclaration,
            string qaObjectEntryId,
            string qaReason,
            bool qaAllowNoParticipants)
        {
            targetDeclaration = qaTargetDeclaration;
            objectEntryId = qaObjectEntryId;
            reason = qaReason;
            allowNoParticipants = qaAllowNoParticipants;
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

        private string ResolveObjectEntryIdText()
        {
            if (targetDeclaration != null && targetDeclaration.HasObjectEntryId)
            {
                return targetDeclaration.ObjectEntryIdText.Trim();
            }

            return objectEntryId.NormalizeText();
        }

        private void PublishSubmitted(string resolvedReason)
        {
            string message = $"Object Reset submitted. source='{DefaultSource}' reason='{resolvedReason}' objectEntry='{ResolveObjectEntryIdText()}'.";
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
            ObjectResetResult result,
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
            ObjectResetResult result,
            bool hasResult)
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
                return string.IsNullOrWhiteSpace(_lastMessage) ? "No Object Reset result yet." : _lastMessage;
            }

            return $"status='{_lastResult.Status}' participants='{_lastResult.ParticipantCount}' participantSucceeded='{_lastResult.ParticipantSucceededCount}' participantSkipped='{_lastResult.ParticipantSkippedCount}' participantFailed='{_lastResult.ParticipantFailedCount}' blockingIssues='{_lastResult.BlockingIssueCount}' nonBlockingIssues='{_lastResult.NonBlockingIssueCount}'";
        }

        private static FlowRequestOutcome MapOutcome(ObjectResetResult result)
        {
            if (result.Succeeded || result.CompletedWithWarnings)
            {
                return FlowRequestOutcome.Succeeded;
            }

            return FlowRequestOutcome.Failed;
        }
    }
}
