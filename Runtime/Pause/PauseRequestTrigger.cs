using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common.FlowTriggers;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using UnityEngine;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Public scene-authored request boundary for logical Pause requests.
    /// Designed for UnityEvents/UI Buttons/QA panels to invoke Pause, Resume and Toggle without owning Pause state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Pause Request Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F27A authored Pause request trigger for Unity-facing Pause surface validation.")]
    public sealed class PauseRequestTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(PauseRequestTrigger);

        private FrameworkLogger _logger;
        private readonly FrameworkFlowTriggerState _triggerState = new FrameworkFlowTriggerState();
        private PauseRequestStatus _lastStatus = PauseRequestStatus.Unknown;
        private PauseState _lastPreviousState = PauseState.Unknown;
        private PauseState _lastCurrentState = PauseState.Unknown;

        [Header("Request")]
        [SerializeField] private string reason = "qa.pause.toggle";

        public FlowRequestOutcome LastOutcome => ToFlowRequestOutcome(_triggerState.LastOutcome);

        public PauseRequestStatus LastStatus => _lastStatus;

        public PauseState LastPreviousState => _lastPreviousState;

        public PauseState LastCurrentState => _lastCurrentState;

        public string LastReason => _triggerState.LastReason;

        public string LastMessage => _triggerState.LastMessage;

        public bool LastRequestSucceeded => _triggerState.LastSucceeded;

        public bool LastRequestIgnored => _triggerState.LastIgnored;

        public bool LastRequestFailed => _triggerState.LastFailed;

        public bool IsPaused => TryGetPauseSnapshot(out var snapshot) && snapshot.IsPaused;

        public bool TryGetPauseSnapshot(out PauseSnapshot snapshot)
        {
            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                snapshot = default;
                return false;
            }

            return runtimeHost.TryGetPauseSnapshot(out snapshot);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<PauseRequestTrigger>();
        }

        [ContextMenu("Immersive Framework/Pause")]
        public void RequestPause()
        {
            Submit(PauseRequestKind.Pause, "qa.pause.pause");
        }

        [ContextMenu("Immersive Framework/Resume")]
        public void RequestResume()
        {
            Submit(PauseRequestKind.Resume, "qa.pause.resume");
        }

        [ContextMenu("Immersive Framework/Toggle")]
        public void TogglePause()
        {
            Submit(PauseRequestKind.Toggle, "qa.pause.toggle");
        }

        private void Submit(PauseRequestKind kind, string fallbackReason)
        {
            EnsureLogger();
            string resolvedReason = ResolveReason(fallbackReason);

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                string message = "Pause Request failed. Application Runtime is unavailable.";
                _logger.Error(message);
                SetLast(FlowRequestOutcome.Failed, PauseRequestStatus.Failed, PauseState.Unknown, PauseState.Unknown, resolvedReason, message, 1, 1);
                return;
            }

            PauseResult result;
            try
            {
                result = runtimeHost.RequestPause(kind, DefaultSource, resolvedReason);
            }
            catch (System.Exception exception)
            {
                string message = $"Pause Request failed. {exception.Message}";
                _logger.Error(message, exception);
                SetLast(FlowRequestOutcome.Failed, PauseRequestStatus.Failed, PauseState.Unknown, PauseState.Unknown, resolvedReason, message, 1, 1);
                return;
            }

            SetLast(
                MapOutcome(result),
                result.Status,
                result.PreviousState,
                result.CurrentState,
                resolvedReason,
                result.Message,
                result.IssueCount,
                result.BlockingIssueCount);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<PauseRequestTrigger>();
            }
        }

        private string ResolveReason(string fallbackReason)
        {
            return reason.NormalizeTextOrFallback(fallbackReason);
        }

        private void SetLast(
            FlowRequestOutcome outcome,
            PauseRequestStatus status,
            PauseState previousState,
            PauseState currentState,
            string resolvedReason,
            string message,
            int issueCount,
            int blockingIssueCount)
        {
            _lastStatus = status;
            _lastPreviousState = previousState;
            _lastCurrentState = currentState;
            switch (outcome)
            {
                case FlowRequestOutcome.Succeeded:
                    _triggerState.CompleteSucceeded(DefaultSource, resolvedReason, message, issueCount, blockingIssueCount);
                    break;
                case FlowRequestOutcome.Ignored:
                    _triggerState.CompleteIgnored(DefaultSource, resolvedReason, message, issueCount, blockingIssueCount);
                    break;
                case FlowRequestOutcome.Failed:
                    _triggerState.CompleteFailed(DefaultSource, resolvedReason, message, issueCount, blockingIssueCount);
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
                        issueCount,
                        blockingIssueCount);
                    break;
            }
        }

        private static FlowRequestOutcome MapOutcome(PauseResult result)
        {
            if (result.Applied)
            {
                return FlowRequestOutcome.Succeeded;
            }

            if (result.IgnoredNoChange)
            {
                return FlowRequestOutcome.Ignored;
            }

            return FlowRequestOutcome.Failed;
        }

        private static FlowRequestOutcome ToFlowRequestOutcome(string outcome)
        {
            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeSucceeded, System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Succeeded;
            }

            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeIgnored, System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Ignored;
            }

            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeFailed, System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Failed;
            }

            if (string.Equals(outcome, FrameworkFlowTriggerState.OutcomeSubmitted, System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Submitted;
            }

            return FlowRequestOutcome.None;
        }
    }
}
