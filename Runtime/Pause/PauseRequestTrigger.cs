using Immersive.Framework.ApiStatus;
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
        private int _requestSequence;
        private IPauseProductRequestPort _pauseProductRequest;
        private string _pauseRuntimeBindingDiagnostic =
            "Pause product request port is not bound.";

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

        public bool HasPauseProductRequestBinding => _pauseProductRequest != null;

        public string ProductRequestBindingStatus =>
            HasPauseProductRequestBinding ? "Bound" : "Missing";

        public string ProductRequestBindingDiagnostic =>
            _pauseRuntimeBindingDiagnostic.NormalizeText();

        // Compatibility aliases now describe the product request binding.
        public bool HasPauseRuntimeBinding => HasPauseProductRequestBinding;

        public string LastRequestStatus => _lastStatus.ToString();
        public string LastRequestReason => LastReason;
        public string LastRequestDiagnostic => LastMessage;

        public string PauseRuntimeBindingStatus =>
            ProductRequestBindingStatus;

        public string PauseRuntimeBindingDiagnostic =>
            ProductRequestBindingDiagnostic;

        public bool IsPaused => TryGetPauseSnapshot(out var snapshot) && snapshot.IsPaused;

        public bool TryGetPauseSnapshot(out PauseSnapshot snapshot)
        {
            IPauseProductRequestPort pauseProductRequest = _pauseProductRequest;
            if (pauseProductRequest == null)
            {
                _pauseRuntimeBindingDiagnostic =
                    "Pause product request port is not bound.";
                snapshot = default;
                return false;
            }

            return pauseProductRequest.TryGetPauseSnapshot(out snapshot);
        }

        internal bool TryBindPauseProductRequest(
            IPauseProductRequestPort pauseProductRequest,
            out string issue)
        {
            if (pauseProductRequest == null)
            {
                issue = "Pause product request binding requires a non-null port.";
                _pauseRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_pauseProductRequest == null)
            {
                _pauseProductRequest = pauseProductRequest;
                issue = string.Empty;
                _pauseRuntimeBindingDiagnostic =
                    $"Bound '{pauseProductRequest.GetType().FullName}'.";
                return true;
            }

            if (object.ReferenceEquals(_pauseProductRequest, pauseProductRequest))
            {
                issue = string.Empty;
                _pauseRuntimeBindingDiagnostic =
                    $"Bound '{pauseProductRequest.GetType().FullName}' (idempotent).";
                return true;
            }

            issue =
                "Pause product request binding rejected a different port for the current lifetime.";
            _pauseRuntimeBindingDiagnostic = issue;
            return false;
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

            IPauseProductRequestPort pauseProductRequest = _pauseProductRequest;
            if (pauseProductRequest == null)
            {
                const string message =
                    "Pause Request BindingUnavailable. Pause product request port is not bound.";
                _pauseRuntimeBindingDiagnostic = "Pause product request port is not bound.";
                _logger.Error(message);
                SetLast(FlowRequestOutcome.Failed, PauseRequestStatus.Failed, PauseState.Unknown, PauseState.Unknown, resolvedReason, message, 1, 1);
                return;
            }

            PauseProductRequestResult productResult;
            try
            {
                productResult = pauseProductRequest.RequestPause(CreatePauseRequest(kind, resolvedReason));
            }
            catch (System.Exception exception)
            {
                string message = $"Pause Request failed. {exception.Message}";
                _logger.Error(message, exception);
                SetLast(FlowRequestOutcome.Failed, PauseRequestStatus.Failed, PauseState.Unknown, PauseState.Unknown, resolvedReason, message, 1, 1);
                return;
            }

            PauseResult result = productResult.PauseResult;
            SetLast(productResult.Succeeded ? FlowRequestOutcome.Succeeded : productResult.Ignored ? FlowRequestOutcome.Ignored : FlowRequestOutcome.Failed,
                result.IsValid ? result.Status : PauseRequestStatus.Failed,
                result.IsValid ? result.PreviousState : PauseState.Unknown,
                result.IsValid ? result.CurrentState : PauseState.Unknown,
                resolvedReason,
                productResult.Diagnostic,
                productResult.Succeeded || productResult.Ignored ? 0 : 1,
                productResult.Succeeded || productResult.Ignored ? 0 : 1);
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

        private PauseRequest CreatePauseRequest(
            PauseRequestKind kind,
            string resolvedReason)
        {
            _requestSequence++;
            string requestId =
                $"pause.request.trigger.{_requestSequence}.{kind.ToString().ToLowerInvariant()}";
            return new PauseRequest(
                PauseRequestId.From(requestId),
                kind,
                DefaultSource,
                resolvedReason);
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
