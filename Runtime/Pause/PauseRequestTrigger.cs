using Immersive.Framework.Common;
using Immersive.Framework.Common.FlowTriggers;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Logging.Records;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Public scene-authored request boundary for logical Pause requests.
    /// Designed for UnityEvents/UI Buttons/QA panels to invoke Pause, Resume and Toggle without owning Pause state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Pause/Pause Request Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public sealed class PauseRequestTrigger :
        MonoBehaviour
    {
        private const string DefaultSource =
            nameof(PauseRequestTrigger);

        private FrameworkLogger _logger;
        private readonly FrameworkFlowTriggerState _triggerState =
            new FrameworkFlowTriggerState();
        private PauseRequestStatus _lastStatus =
            PauseRequestStatus.Unknown;
        private PauseState _lastPreviousState =
            PauseState.Unknown;
        private PauseState _lastCurrentState =
            PauseState.Unknown;
        private string _lastProductStatus =
            PauseProductRequestStatus.Unknown.ToString();
        private string _lastExecutionMode =
            "None";
        private int _requestSequence;
        private IPauseProductRequestPort _pauseProductRequest;
        private string _pauseRuntimeBindingDiagnostic =
            "Pause product request port is not bound.";

        [Header("Request")]
        [SerializeField]
        private string reason =
            "pause.toggle";

        public FlowRequestOutcome LastOutcome =>
            ToFlowRequestOutcome(
                _triggerState.LastOutcome);

        public PauseRequestStatus LastStatus =>
            _lastStatus;

        public PauseState LastPreviousState =>
            _lastPreviousState;

        public PauseState LastCurrentState =>
            _lastCurrentState;

        public string LastProductStatus =>
            _lastProductStatus.NormalizeText();

        public string LastExecutionMode =>
            _lastExecutionMode.NormalizeText();

        public string LastReason =>
            _triggerState.LastReason;

        public string LastMessage =>
            _triggerState.LastMessage;

        public bool LastRequestSucceeded =>
            _triggerState.LastSucceeded;

        public bool LastRequestIgnored =>
            _triggerState.LastIgnored;

        public bool LastRequestFailed =>
            _triggerState.LastFailed;

        public bool HasPauseProductRequestBinding =>
            _pauseProductRequest != null;

        public string ProductRequestBindingStatus =>
            HasPauseProductRequestBinding
                ? "Bound"
                : "Missing";

        public string ProductRequestBindingDiagnostic =>
            _pauseRuntimeBindingDiagnostic.NormalizeText();

        public bool IsPaused =>
            TryGetPauseSnapshot(
                out PauseSnapshot snapshot) &&
            snapshot.IsPaused;

        public bool TryGetPauseSnapshot(
            out PauseSnapshot snapshot)
        {
            IPauseProductRequestPort pauseProductRequest =
                _pauseProductRequest;
            if (pauseProductRequest == null)
            {
                _pauseRuntimeBindingDiagnostic =
                    "Pause product request port is not bound.";
                snapshot = default;
                return false;
            }

            return pauseProductRequest.TryGetPauseSnapshot(
                out snapshot);
        }

        internal bool TryBindPauseProductRequest(
            IPauseProductRequestPort pauseProductRequest,
            out string issue)
        {
            if (pauseProductRequest == null)
            {
                issue =
                    "Pause product request binding requires a non-null port.";
                _pauseRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_pauseProductRequest == null)
            {
                _pauseProductRequest =
                    pauseProductRequest;
                issue = string.Empty;
                _pauseRuntimeBindingDiagnostic =
                    $"Bound '{pauseProductRequest.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(
                    _pauseProductRequest,
                    pauseProductRequest))
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

        internal bool TryReleasePauseProductRequest(
            IPauseProductRequestPort expectedPauseProductRequest,
            out string issue)
        {
            if (expectedPauseProductRequest == null)
            {
                issue =
                    "Pause product request release requires the exact non-null bound port.";
                _pauseRuntimeBindingDiagnostic = issue;
                return false;
            }

            if (_pauseProductRequest == null)
            {
                issue = string.Empty;
                _pauseRuntimeBindingDiagnostic =
                    "Pause product request port is already released.";
                return true;
            }

            if (!ReferenceEquals(
                    _pauseProductRequest,
                    expectedPauseProductRequest))
            {
                issue =
                    "Pause product request release rejected a foreign or stale port.";
                _pauseRuntimeBindingDiagnostic = issue;
                return false;
            }

            _pauseProductRequest = null;
            issue = string.Empty;
            _pauseRuntimeBindingDiagnostic =
                "Pause product request port was released by Scene Lifecycle.";
            return true;
        }

        private void Awake()
        {
            _logger =
                FrameworkLogger.Create<
                    PauseRequestTrigger>();
        }

        [ContextMenu(
            "Immersive Framework/Pause")]
        public void RequestPause()
        {
            Submit(
                PauseRequestKind.Pause,
                "pause.pause");
        }

        [ContextMenu(
            "Immersive Framework/Resume")]
        public void RequestResume()
        {
            Submit(
                PauseRequestKind.Resume,
                "pause.resume");
        }

        [ContextMenu(
            "Immersive Framework/Toggle")]
        public void TogglePause()
        {
            Submit(
                PauseRequestKind.Toggle,
                "pause.toggle");
        }

        private void Submit(
            PauseRequestKind kind,
            string fallbackReason)
        {
            EnsureLogger();
            string resolvedReason =
                ResolveReason(fallbackReason);

            IPauseProductRequestPort pauseProductRequest =
                _pauseProductRequest;
            if (pauseProductRequest == null)
            {
                const string message =
                    "Pause Request BindingUnavailable. Pause product request port is not bound.";
                _pauseRuntimeBindingDiagnostic =
                    "Pause product request port is not bound.";
                _lastProductStatus =
                    PauseProductRequestStatus.BindingUnavailable.ToString();
                _lastExecutionMode =
                    "None";
                SetLast(
                    FlowRequestOutcome.Failed,
                    PauseRequestStatus.Failed,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    resolvedReason,
                    message,
                    1,
                    1);
                _logger.Error(
                    message,
                    BuildFields(
                        kind,
                        resolvedReason,
                        PauseProductRequestStatus.BindingUnavailable,
                        "None",
                        default,
                        message));
                return;
            }

            PauseProductRequestResult productResult;
            try
            {
                productResult =
                    pauseProductRequest.RequestPause(
                        CreatePauseRequest(
                            kind,
                            resolvedReason));
            }
            catch (System.Exception exception)
            {
                string message =
                    $"Pause Request failed. {exception.Message}";
                _lastProductStatus =
                    PauseProductRequestStatus.Failed.ToString();
                _lastExecutionMode =
                    "None";
                SetLast(
                    FlowRequestOutcome.Failed,
                    PauseRequestStatus.Failed,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    resolvedReason,
                    message,
                    1,
                    1);
                _logger.Error(
                    message,
                    exception,
                    BuildFields(
                        kind,
                        resolvedReason,
                        PauseProductRequestStatus.Failed,
                        "None",
                        default,
                        message));
                return;
            }

            PauseResult result =
                productResult.PauseResult;
            _lastProductStatus =
                productResult.Status.ToString();
            _lastExecutionMode =
                productResult.ExecutionMode;

            FlowRequestOutcome outcome =
                productResult.Succeeded
                    ? FlowRequestOutcome.Succeeded
                    : productResult.Ignored
                        ? FlowRequestOutcome.Ignored
                        : FlowRequestOutcome.Failed;
            SetLast(
                outcome,
                result.IsValid
                    ? result.Status
                    : PauseRequestStatus.Failed,
                result.IsValid
                    ? result.PreviousState
                    : PauseState.Unknown,
                result.IsValid
                    ? result.CurrentState
                    : PauseState.Unknown,
                resolvedReason,
                productResult.Diagnostic,
                productResult.Succeeded ||
                productResult.Ignored
                    ? 0
                    : 1,
                productResult.Succeeded ||
                productResult.Ignored
                    ? 0
                    : 1);

            LogProductResult(
                kind,
                resolvedReason,
                productResult);
        }

        private void LogProductResult(
            PauseRequestKind kind,
            string resolvedReason,
            PauseProductRequestResult productResult)
        {
            LogField[] fields =
                BuildFields(
                    kind,
                    resolvedReason,
                    productResult.Status,
                    productResult.ExecutionMode,
                    productResult.PauseResult,
                    productResult.Diagnostic);

            if (productResult.Succeeded)
            {
                _logger.Info(
                    "Pause Request completed.",
                    fields);
                return;
            }

            if (productResult.Ignored)
            {
                _logger.Info(
                    "Pause Request ignored.",
                    fields);
                return;
            }

            if (productResult.Status is
                    PauseProductRequestStatus.BindingUnavailable or
                    PauseProductRequestStatus.Rejected)
            {
                _logger.Warning(
                    "Pause Request rejected.",
                    fields);
                return;
            }

            _logger.Error(
                "Pause Request failed.",
                fields);
        }

        private static LogField[] BuildFields(
            PauseRequestKind kind,
            string resolvedReason,
            PauseProductRequestStatus productStatus,
            string executionMode,
            PauseResult pauseResult,
            string diagnostic)
        {
            return LogFields.Of(
                LogFields.Field(
                    "requestKind",
                    kind.ToString()),
                LogFields.Field(
                    "source",
                    DefaultSource),
                LogFields.Field(
                    "reason",
                    resolvedReason),
                LogFields.Field(
                    "productStatus",
                    productStatus.ToString()),
                LogFields.Field(
                    "executionMode",
                    executionMode.NormalizeTextOrFallback(
                        "None")),
                LogFields.Field(
                    "pauseResultValid",
                    pauseResult.IsValid),
                LogFields.Field(
                    "pauseStatus",
                    pauseResult.Status.ToString()),
                LogFields.Field(
                    "previousState",
                    pauseResult.PreviousState.ToString()),
                LogFields.Field(
                    "currentState",
                    pauseResult.CurrentState.ToString()),
                LogFields.Field(
                    "applied",
                    pauseResult.Applied),
                LogFields.Field(
                    "diagnostic",
                    diagnostic.NormalizeTextOrFallback(
                        "none")));
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger =
                    FrameworkLogger.Create<
                        PauseRequestTrigger>();
            }
        }

        private string ResolveReason(
            string fallbackReason)
        {
            return reason.NormalizeTextOrFallback(
                fallbackReason);
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
                    _triggerState.CompleteSucceeded(
                        DefaultSource,
                        resolvedReason,
                        message,
                        issueCount,
                        blockingIssueCount);
                    break;

                case FlowRequestOutcome.Ignored:
                    _triggerState.CompleteIgnored(
                        DefaultSource,
                        resolvedReason,
                        message,
                        issueCount,
                        blockingIssueCount);
                    break;

                case FlowRequestOutcome.Failed:
                    _triggerState.CompleteFailed(
                        DefaultSource,
                        resolvedReason,
                        message,
                        issueCount,
                        blockingIssueCount);
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

        private static FlowRequestOutcome ToFlowRequestOutcome(
            string outcome)
        {
            if (string.Equals(
                    outcome,
                    FrameworkFlowTriggerState.OutcomeSucceeded,
                    System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Succeeded;
            }

            if (string.Equals(
                    outcome,
                    FrameworkFlowTriggerState.OutcomeIgnored,
                    System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Ignored;
            }

            if (string.Equals(
                    outcome,
                    FrameworkFlowTriggerState.OutcomeFailed,
                    System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Failed;
            }

            if (string.Equals(
                    outcome,
                    FrameworkFlowTriggerState.OutcomeSubmitted,
                    System.StringComparison.Ordinal))
            {
                return FlowRequestOutcome.Submitted;
            }

            return FlowRequestOutcome.None;
        }
    }
}
