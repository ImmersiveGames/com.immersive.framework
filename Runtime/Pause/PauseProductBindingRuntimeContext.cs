using System;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.InputMode;
using Immersive.Framework.UnityInput;
using Immersive.Logging.Records;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Pause
{
    internal sealed class PauseProductBindingRuntimeContext :
        IPauseProductBindingPort,
        IPauseProductRequestPort
    {
        private const string PhysicalInputLog =
            "[PAUSE_PRODUCT_INPUT]";

        private readonly IPauseProductApplicationPort _application;
        private readonly FrameworkLogger _logger;

        private long _generation;
        private PauseProductBindingState _state;
        private PauseProductBindingToken _token;
        private PlayerPauseInput _binding;
        private PlayerInput _playerInput;
        private UnityPlayerInputGateAdapter _adapter;
        private InputAction _pauseAction;
        private InputModeRuntimeContext _inputMode;
        private PausePlayerInputPostureReceipt _bindingPosture;
        private UnityPlayerInputActionMapSetWriteReceipt
            _pauseActionMapSetReceipt;
        private bool _requestInFlight;
        private PauseProductRequestResult _lastResult;
        private string _lastDiagnostic =
            "No Pause PlayerInput Binding is active.";

        internal PauseProductBindingRuntimeContext(
            IPauseProductApplicationPort application)
        {
            _application = application ??
                throw new ArgumentNullException(
                    nameof(application));
            _logger =
                FrameworkLogger.Create<
                    PauseProductBindingRuntimeContext>();
            _state = PauseProductBindingState.Unbound;
        }

        internal PauseProductBindingState State =>
            _state;

        internal PauseProductBindingToken ActiveToken =>
            _token;

        internal PauseProductRequestResult LastResult =>
            _lastResult;

        internal string LastDiagnostic =>
            _lastDiagnostic.NormalizeText();

        internal bool HasActivePlayerInputBinding =>
            _state == PauseProductBindingState.Bound &&
            _token.IsValid;

        public bool TryRegister(
            PlayerPauseInput binding,
            out PauseProductBindingToken token,
            out string diagnostic)
        {
            token = default;
            if (binding == null)
            {
                diagnostic =
                    "Pause binding registration requires an explicit binding component.";
                return false;
            }

            if (_state == PauseProductBindingState.Bound)
            {
                diagnostic =
                    ReferenceEquals(_binding, binding)
                        ? "Pause binding is already registered (idempotent)."
                        : "Pause binding registration rejected because another PlayerInput binding is active.";
                token =
                    ReferenceEquals(_binding, binding)
                        ? _token
                        : default;
                return ReferenceEquals(_binding, binding);
            }

            if (_state is
                    PauseProductBindingState.Binding or
                    PauseProductBindingState.Unbinding ||
                _requestInFlight)
            {
                diagnostic =
                    "Pause binding registration rejected because a lifecycle operation is in progress.";
                return false;
            }

            _pauseActionMapSetReceipt = default;
            _state = PauseProductBindingState.Binding;
            if (!binding.TryGetRuntimeConfiguration(
                    out PlayerInput input,
                    out InputAction pauseAction,
                    out UnityPlayerInputGateAdapter adapter,
                    out diagnostic))
            {
                _state = PauseProductBindingState.Failed;
                _lastDiagnostic = diagnostic;
                return false;
            }

            if (!adapter.TryCapturePosture(
                    out _bindingPosture,
                    out diagnostic) ||
                !adapter.TryApplyActionMapSet(
                    binding.GameplayActionMapName,
                    new[]
                    {
                        binding.GlobalActionMapName,
                        binding.GameplayActionMapName
                    },
                    nameof(PauseProductBindingRuntimeContext),
                    "binding-initial-gameplay",
                    out _,
                    out diagnostic))
            {
                _state = PauseProductBindingState.Failed;
                _lastDiagnostic = diagnostic;
                return false;
            }

            if (!pauseAction.enabled)
            {
                adapter.TryRestorePosture(
                    _bindingPosture,
                    out _);
                _state = PauseProductBindingState.Failed;
                diagnostic =
                    "Global/Pause is not enabled after applying the Gameplay posture.";
                _lastDiagnostic = diagnostic;
                return false;
            }

            _generation++;
            int playerEntityHash =
                input.GetEntityId().GetHashCode();
            _token =
                new PauseProductBindingToken(
                    _generation,
                    playerEntityHash);
            _binding = binding;
            _playerInput = input;
            _adapter = adapter;
            _pauseAction = pauseAction;
            _inputMode =
                new InputModeRuntimeContext(
                    $"pause.product.playerinput.{input.GetEntityId()}",
                    InputModeState.InitialGameplay(
                        nameof(PauseProductBindingRuntimeContext),
                        "binding-initial-gameplay"));
            _pauseAction.performed +=
                OnPauseActionPerformed;
            _state = PauseProductBindingState.Bound;
            token = _token;
            diagnostic =
                "Pause PlayerInput Binding registered with Global + Player posture.";
            _lastDiagnostic = diagnostic;
            return true;
        }

        public bool ReleaseBinding(
            PauseProductBindingToken token,
            string reason,
            out string diagnostic)
        {
            if (!token.IsValid ||
                !_token.IsValid ||
                token.Generation != _token.Generation ||
                token.PlayerInstanceId !=
                _token.PlayerInstanceId)
            {
                diagnostic =
                    "Pause binding release rejected missing, foreign or stale token.";
                return false;
            }

            if (_state != PauseProductBindingState.Bound ||
                _requestInFlight)
            {
                diagnostic =
                    "Pause binding release rejected because binding is not releasable.";
                return false;
            }

            _state = PauseProductBindingState.Unbinding;
            string pauseDiagnostic = string.Empty;
            bool pauseRestored =
                !_application.TryGetApplicationPauseSnapshot(
                    out PauseSnapshot snapshot) ||
                snapshot.State == PauseState.Running ||
                _application.TryRestorePauseSnapshot(
                    PauseSnapshot.FromState(
                        PauseState.Running,
                        nameof(PauseProductBindingRuntimeContext),
                        reason,
                        Array.Empty<string>()),
                    reason,
                    out pauseDiagnostic);

            bool postureRestored = true;
            string postureDiagnostic = string.Empty;
            if (_pauseAction != null)
            {
                _pauseAction.performed -=
                    OnPauseActionPerformed;
            }

            if (_adapter != null &&
                _playerInput != null)
            {
                postureRestored =
                    _adapter.TryRestorePosture(
                        _bindingPosture,
                        out postureDiagnostic);
            }

            ClearBinding();
            _state = PauseProductBindingState.Unbound;
            diagnostic =
                pauseRestored && postureRestored
                    ? "Pause binding released and prior PlayerInput posture restored."
                    : $"Pause binding released logically. pause='{pauseDiagnostic}' posture='{postureDiagnostic}'.";
            _lastDiagnostic = diagnostic;
            return pauseRestored && postureRestored;
        }

        public PauseProductRequestResult RequestPause(
            PauseRequest request)
        {
            if (_requestInFlight)
            {
                return Record(
                    PauseProductRequestStatus.Rejected,
                    default,
                    null,
                    "Pause product request rejected because another request is in progress.");
            }

            if (!request.IsValid ||
                !_application.TryGetApplicationPauseSnapshot(
                    out PauseSnapshot previousPause))
            {
                return Record(
                    PauseProductRequestStatus.Rejected,
                    default,
                    null,
                    "Pause product request is invalid or application Pause is unavailable.");
            }

            if (!HasActivePlayerInputBinding)
            {
                if (!IsCleanlyUnbound())
                {
                    return Record(
                        PauseProductRequestStatus.BindingUnavailable,
                        default,
                        null,
                        $"Pause product request rejected because PlayerInput binding evidence is inconsistent. bindingState='{_state}' tokenValid='{_token.IsValid}'.");
                }

                return RequestApplicationOnly(
                    request,
                    previousPause);
            }

            PauseState targetPause =
                PauseRequest.ResolveTargetState(
                    request.Kind,
                    previousPause.State);
            InputModeKind targetMode =
                targetPause == PauseState.Paused
                    ? InputModeKind.PauseOverlay
                    : InputModeKind.Gameplay;
            InputModeRuntimeOperationResult begin =
                _inputMode.TryBegin(
                    InputModeRequest.To(
                        targetMode,
                        request.Source,
                        request.Reason),
                    nameof(PauseProductBindingRuntimeContext),
                    out InputModeRuntimeTransaction transaction);

            if (begin.Ignored)
            {
                return Record(
                    PauseProductRequestStatus.Ignored,
                    default,
                    begin,
                    "Pause product request ignored because InputMode is already current.");
            }

            if (!begin.Prepared)
            {
                return Record(
                    PauseProductRequestStatus.Rejected,
                    default,
                    begin,
                    begin.Message);
            }

            bool paused =
                targetPause == PauseState.Paused;
            if (paused && _pauseActionMapSetReceipt.IsValid)
            {
                _inputMode.Rollback(
                    transaction,
                    nameof(PauseProductBindingRuntimeContext),
                    "pause-baseline-already-retained");
                return Record(
                    PauseProductRequestStatus.Failed,
                    default,
                    begin,
                    "Pause product request rejected because retained Pause-time PlayerInput baseline evidence is inconsistent.");
            }

            if (!paused && !_pauseActionMapSetReceipt.IsValid)
            {
                _inputMode.Rollback(
                    transaction,
                    nameof(PauseProductBindingRuntimeContext),
                    "resume-baseline-missing");
                return Record(
                    PauseProductRequestStatus.Failed,
                    default,
                    begin,
                    "Resume rejected because exact Pause-time PlayerInput baseline evidence is unavailable.");
            }

            _requestInFlight = true;
            try
            {
                if (!_application.TryApplyProductPause(
                        request,
                        out PauseResult pauseResult,
                        out string pauseDiagnostic))
                {
                    _inputMode.Rollback(
                        transaction,
                        nameof(PauseProductBindingRuntimeContext),
                        "application-pause-failed");
                    return Record(
                        PauseProductRequestStatus.Failed,
                        pauseResult,
                        begin,
                        pauseDiagnostic);
                }

                UnityPlayerInputActionMapSetWriteReceipt
                    physicalReceipt = default;
                string physicalDiagnostic;
                if (paused)
                {
                    if (!_adapter.TryApplyActionMapSet(
                            _binding.GlobalActionMapName,
                            new[]
                            {
                                _binding.GlobalActionMapName
                            },
                            nameof(PauseProductBindingRuntimeContext),
                            request.Reason,
                            out physicalReceipt,
                            out physicalDiagnostic))
                    {
                        _inputMode.Rollback(
                            transaction,
                            nameof(PauseProductBindingRuntimeContext),
                            "physical-apply-failed");
                        _application.TryRestorePauseSnapshot(
                            previousPause,
                            "pause-product-physical-rollback",
                            out string rollbackDiagnostic);
                        return Record(
                            PauseProductRequestStatus.Failed,
                            pauseResult,
                            begin,
                            $"Physical Pause posture failed. physical='{physicalDiagnostic}' compensation='{rollbackDiagnostic}'.");
                    }
                }
                else if (!_adapter.TryRestoreActionMapSet(
                             _pauseActionMapSetReceipt,
                             nameof(PauseProductBindingRuntimeContext),
                             request.Reason,
                             out physicalDiagnostic))
                {
                    _adapter.TryApplyActionMapSet(
                        _pauseActionMapSetReceipt.AppliedPrimaryActionMapName,
                        _pauseActionMapSetReceipt.AppliedEnabledActionMapNames,
                        nameof(PauseProductBindingRuntimeContext),
                        "resume-physical-restore-failed",
                        out _,
                        out string physicalRollbackDiagnostic);
                    _inputMode.Rollback(
                        transaction,
                        nameof(PauseProductBindingRuntimeContext),
                        "physical-restore-failed");
                    _application.TryRestorePauseSnapshot(
                        previousPause,
                        "resume-product-physical-rollback",
                        out string rollbackDiagnostic);
                    return Record(
                        PauseProductRequestStatus.Failed,
                        pauseResult,
                        begin,
                        $"Physical Resume posture restore failed. physical='{physicalDiagnostic}' physicalCompensation='{physicalRollbackDiagnostic}' compensation='{rollbackDiagnostic}'.");
                }

                InputModeRuntimeOperationResult commit =
                    _inputMode.Commit(
                        transaction,
                        nameof(PauseProductBindingRuntimeContext),
                        request.Reason);
                if (!commit.Committed)
                {
                    string physicalRollbackDiagnostic;
                    if (paused)
                    {
                        _adapter.TryRestoreActionMapSet(
                            physicalReceipt,
                            nameof(PauseProductBindingRuntimeContext),
                            "inputmode-commit-failed",
                            out physicalRollbackDiagnostic);
                    }
                    else
                    {
                        _adapter.TryApplyActionMapSet(
                            _pauseActionMapSetReceipt.AppliedPrimaryActionMapName,
                            _pauseActionMapSetReceipt.AppliedEnabledActionMapNames,
                            nameof(PauseProductBindingRuntimeContext),
                            "inputmode-commit-failed",
                            out _,
                            out physicalRollbackDiagnostic);
                    }
                    _application.TryRestorePauseSnapshot(
                        previousPause,
                        "inputmode-commit-failed",
                        out string rollbackDiagnostic);
                    return Record(
                        PauseProductRequestStatus.Failed,
                        pauseResult,
                        commit,
                        $"InputMode commit failed. physicalCompensation='{physicalRollbackDiagnostic}' compensation='{rollbackDiagnostic}'.");
                }

                _pauseActionMapSetReceipt =
                    paused
                        ? physicalReceipt
                        : default;

                return Record(
                    PauseProductRequestStatus.Applied,
                    pauseResult,
                    commit,
                    "Pause product request applied through the canonical PlayerInput binding.");
            }
            finally
            {
                _requestInFlight = false;
            }
        }

        public bool TryGetPauseSnapshot(
            out PauseSnapshot snapshot) =>
            _application.TryGetApplicationPauseSnapshot(
                out snapshot);

        private PauseProductRequestResult RequestApplicationOnly(
            PauseRequest request,
            PauseSnapshot previousPause)
        {
            _requestInFlight = true;
            try
            {
                if (!_application.TryApplyProductPause(
                        request,
                        out PauseResult pauseResult,
                        out string diagnostic))
                {
                    return Record(
                        PauseProductRequestStatus.Failed,
                        pauseResult,
                        null,
                        diagnostic);
                }

                if (!pauseResult.IsValid)
                {
                    _application.TryRestorePauseSnapshot(
                        previousPause,
                        "pause-application-only-invalid-result",
                        out string rollbackDiagnostic);
                    return Record(
                        PauseProductRequestStatus.Failed,
                        pauseResult,
                        null,
                        $"Application-only Pause returned an invalid result. compensation='{rollbackDiagnostic}'.");
                }

                if (!pauseResult.Applied)
                {
                    return Record(
                        PauseProductRequestStatus.Ignored,
                        pauseResult,
                        null,
                        "Pause product request completed without a state change and without a PlayerInput transaction.");
                }

                return Record(
                    PauseProductRequestStatus.AppliedWithoutPlayerInput,
                    pauseResult,
                    null,
                    "Pause product request applied to the application without a PlayerInput transaction because no active PlayerInput binding exists.");
            }
            finally
            {
                _requestInFlight = false;
            }
        }

        private void OnPauseActionPerformed(
            InputAction.CallbackContext context)
        {
            PauseProductRequestResult result =
                RequestPause(
                    PauseRequest.Toggle(
                        "pause.product.input",
                        nameof(PauseProductBindingRuntimeContext),
                        "global-pause-action"));
            PauseResult pauseResult =
                result.PauseResult;
            LogField[] fields =
                LogFields.Of(
                    LogFields.Field(
                        "succeeded",
                        result.Succeeded),
                    LogFields.Field(
                        "ignored",
                        result.Ignored),
                    LogFields.Field(
                        "productStatus",
                        result.Status.ToString()),
                    LogFields.Field(
                        "executionMode",
                        result.ExecutionMode),
                    LogFields.Field(
                        "pauseResultValid",
                        pauseResult.IsValid),
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
                        result.Diagnostic));

            if (result.Succeeded ||
                result.Ignored)
            {
                _logger.Debug(
                    PhysicalInputLog,
                    fields);
                return;
            }

            _logger.Error(
                PhysicalInputLog,
                fields);
        }

        private PauseProductRequestResult Record(
            PauseProductRequestStatus status,
            PauseResult pause,
            InputModeRuntimeOperationResult inputMode,
            string diagnostic)
        {
            _lastDiagnostic = diagnostic;
            _lastResult =
                new PauseProductRequestResult(
                    status,
                    pause,
                    inputMode,
                    diagnostic);
            return _lastResult;
        }

        private bool IsCleanlyUnbound()
        {
            return
                _state == PauseProductBindingState.Unbound &&
                !_token.IsValid &&
                _binding == null &&
                _playerInput == null &&
                _adapter == null &&
                _pauseAction == null &&
                _inputMode == null &&
                !_pauseActionMapSetReceipt.IsValid;
        }

        private void ClearBinding()
        {
            _token = default;
            _binding = null;
            _playerInput = null;
            _adapter = null;
            _pauseAction = null;
            _inputMode = null;
            _bindingPosture = default;
            _pauseActionMapSetReceipt = default;
        }
    }
}
