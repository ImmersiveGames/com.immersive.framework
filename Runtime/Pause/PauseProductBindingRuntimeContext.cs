using System;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
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
        private long _transactionSequence;
        private PauseProductTransaction _activeTransaction;
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

            if (_state is PauseProductBindingState.Bound or PauseProductBindingState.ReleasePending)
            {
                diagnostic =
                    ReferenceEquals(_binding, binding)
                        ? _state == PauseProductBindingState.Bound
                            ? "Pause binding is already registered (idempotent)."
                            : "Pause binding registration remains pending physical compensation."
                        : "Pause binding registration rejected because another PlayerInput binding is active.";
                token =
                    ReferenceEquals(_binding, binding)
                        ? _token
                        : default;
                return ReferenceEquals(_binding, binding) &&
                    _state == PauseProductBindingState.Bound;
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
            _bindingPosture = default;
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
                    input,
                    out _bindingPosture,
                    out diagnostic))
            {
                _state = PauseProductBindingState.Failed;
                _lastDiagnostic = diagnostic;
                return false;
            }

            if (!adapter.TryApplyActionMapSet(
                    input,
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
                string setupDiagnostic = diagnostic;
                return RetainBindingForReleaseIfCompensationFails(
                    binding,
                    input,
                    pauseAction,
                    adapter,
                    setupDiagnostic,
                    out token,
                    out diagnostic);
            }

            if (!pauseAction.enabled)
            {
                diagnostic = "Global/Pause is not enabled after applying the Gameplay posture.";
                string setupDiagnostic = diagnostic;
                return RetainBindingForReleaseIfCompensationFails(
                    binding,
                    input,
                    pauseAction,
                    adapter,
                    setupDiagnostic,
                    out token,
                    out diagnostic);
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
            _activeTransaction = default;
            _pauseAction.performed +=
                OnPauseActionPerformed;
            _state = PauseProductBindingState.Bound;
            token = _token;
            diagnostic =
                "Pause PlayerInput Binding registered with Global + Player posture.";
            _lastDiagnostic = diagnostic;
            return true;
        }

        private bool RetainBindingForReleaseIfCompensationFails(
            PlayerPauseInput binding,
            PlayerInput input,
            InputAction pauseAction,
            UnityPlayerInputGateAdapter adapter,
            string operationDiagnostic,
            out PauseProductBindingToken token,
            out string diagnostic)
        {
            token = default;
            PlayerInputPostureRestoreOutcome compensation = adapter.TryRestorePosture(
                input,
                _bindingPosture,
                out string compensationDiagnostic);
            if (compensation == PlayerInputPostureRestoreOutcome.Restored)
            {
                _bindingPosture = default;
                _state = PauseProductBindingState.Failed;
                diagnostic = $"Pause binding setup failed; initial PlayerInput posture restoration was confirmed. setup='{operationDiagnostic}' compensation='{compensationDiagnostic}'.";
                _lastDiagnostic = diagnostic;
                return false;
            }

            if (compensation == PlayerInputPostureRestoreOutcome.TargetDestroyed)
            {
                _bindingPosture = default;
                _state = PauseProductBindingState.Failed;
                diagnostic = $"Pause binding setup failed and its exact PlayerInput target was destroyed; physical restoration was not confirmed. setup='{operationDiagnostic}' compensation='{compensationDiagnostic}'.";
                _lastDiagnostic = diagnostic;
                return false;
            }

            _generation++;
            _token = new PauseProductBindingToken(
                _generation,
                input.GetEntityId().GetHashCode());
            _binding = binding;
            _playerInput = input;
            _adapter = adapter;
            _pauseAction = pauseAction;
            _activeTransaction = default;
            _state = PauseProductBindingState.ReleasePending;
            token = _token;
            diagnostic = $"Pause binding setup failed and physical compensation remains pending; the binding token and initial posture receipt are retained for controlled release retry. setup='{operationDiagnostic}' compensation='{compensationDiagnostic}'.";
            _lastDiagnostic = diagnostic;
            return false;
        }

        public bool ReleaseBinding(
            PauseProductBindingToken token,
            string reason,
            out PauseBindingReleaseOutcome outcome,
            out string diagnostic)
        {
            outcome = PauseBindingReleaseOutcome.Rejected;
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

            if (_state is not (PauseProductBindingState.Bound or PauseProductBindingState.ReleasePending) ||
                _requestInFlight)
            {
                diagnostic =
                    "Pause binding release rejected because binding is not releasable.";
                return false;
            }

            bool exactPlayerInputDestroyed = _playerInput == null;
            _state = PauseProductBindingState.Unbinding;
            string pauseDiagnostic = string.Empty;
            bool hasPreviousPause = _application.TryGetApplicationPauseSnapshot(
                out PauseSnapshot snapshot) && snapshot.IsValid;
            bool pauseRestored =
                !hasPreviousPause ||
                snapshot.State == PauseState.Running ||
                _application.TryRestorePauseSnapshot(
                    PauseSnapshot.FromState(
                        PauseState.Running,
                        nameof(PauseProductBindingRuntimeContext),
                        reason,
                        Array.Empty<string>()),
                    reason,
                    out pauseDiagnostic);

            if (!pauseRestored)
            {
                bool logicalRollback = _application.TryRestorePauseSnapshot(
                    snapshot,
                    "pause-binding-release-logical-rollback",
                    out string logicalRollbackDiagnostic);
                _state = PauseProductBindingState.Bound;
                outcome = PauseBindingReleaseOutcome.FailedRecoverable;
                diagnostic = $"Pause binding release is pending because logical Pause could not be restored to Running; physical posture was left unchanged and ownership was retained. pause='{pauseDiagnostic}' rollback='{(logicalRollback ? "restored" : logicalRollbackDiagnostic)}'.";
                _lastDiagnostic = diagnostic;
                return false;
            }

            if (exactPlayerInputDestroyed)
            {
                if (_pauseAction != null)
                {
                    _pauseAction.performed -= OnPauseActionPerformed;
                }
                ClearBinding();
                _state = PauseProductBindingState.Unbound;
                outcome = PauseBindingReleaseOutcome.TargetDestroyed;
                diagnostic = $"Pause binding released because its exact PlayerInput target was destroyed; physical restoration was not confirmed. pause='{pauseDiagnostic}'.";
                _lastDiagnostic = diagnostic;
                return true;
            }

            string postureDiagnostic;
            PlayerInputPostureRestoreOutcome postureOutcome;
            if (ReferenceEquals(_adapter, null))
            {
                postureDiagnostic = "Pause binding lost its Unity PlayerInput Gate Adapter while the exact target still exists.";
                postureOutcome = PlayerInputPostureRestoreOutcome.FailedRecoverable;
            }
            else
            {
                postureOutcome = _adapter.TryRestorePosture(
                    _playerInput,
                    _bindingPosture,
                    out postureDiagnostic);
            }

            if (postureOutcome == PlayerInputPostureRestoreOutcome.TargetDestroyed)
            {
                if (_pauseAction != null)
                {
                    _pauseAction.performed -= OnPauseActionPerformed;
                }
                ClearBinding();
                _state = PauseProductBindingState.Unbound;
                outcome = PauseBindingReleaseOutcome.TargetDestroyed;
                diagnostic = $"Pause binding released because its exact PlayerInput target was destroyed; physical restoration was not confirmed. pause='{pauseDiagnostic}'.";
                _lastDiagnostic = diagnostic;
                return true;
            }

            if (postureOutcome == PlayerInputPostureRestoreOutcome.FailedRecoverable)
            {
                bool logicalRollback = true;
                string logicalRollbackDiagnostic = string.Empty;
                if (hasPreviousPause)
                {
                    logicalRollback = _application.TryRestorePauseSnapshot(
                        snapshot,
                        "pause-binding-release-physical-rollback",
                        out logicalRollbackDiagnostic);
                }
                _state = PauseProductBindingState.Bound;
                outcome = PauseBindingReleaseOutcome.FailedRecoverable;
                diagnostic = $"Pause binding release is pending; exact PlayerInput posture restoration failed and ownership was retained. posture='{postureDiagnostic}' pauseRollback='{(logicalRollback ? "restored" : logicalRollbackDiagnostic)}'.";
                _lastDiagnostic = diagnostic;
                return false;
            }

            if (_pauseAction != null)
            {
                _pauseAction.performed -= OnPauseActionPerformed;
            }
            ClearBinding();
            _state = PauseProductBindingState.Unbound;
            outcome = PauseBindingReleaseOutcome.Restored;
            diagnostic = pauseRestored
                ? "Pause binding released and exact prior PlayerInput posture restoration was confirmed."
                : $"Pause binding released after physical restoration; logical Pause release reported '{pauseDiagnostic}'.";
            _lastDiagnostic = diagnostic;
            return true;
        }

        /// <summary>
        /// Bypass entry point for framework-internal Pause lifecycle cleanup (IF-ADR-005 Pause
        /// lifecycle cleanup). Restores the exact captured Pause-time PlayerInput Action Map
        /// baseline through a Pause-owned physical transaction, without evaluating
        /// external Pause admission (GameFlow transition gate, Activity admission) and without
        /// releasing or re-registering the PlayerInput binding. Callers are responsible for
        /// resolving the logical PauseRuntime state to Running (via
        /// IPauseProductApplicationPort.TryRestorePauseSnapshot) before invoking this.
        /// </summary>
        internal bool TryRestorePhysicalPostureForLifecycleExit(
            string reason,
            out PlayerInputPostureRestoreOutcome outcome,
            out string diagnostic)
        {
            if (!HasActivePlayerInputBinding)
            {
                outcome = PlayerInputPostureRestoreOutcome.Restored;
                diagnostic =
                    "No active PlayerInput binding requires physical Pause posture restoration.";
                return true;
            }

            if (!_pauseActionMapSetReceipt.IsValid)
            {
                outcome = PlayerInputPostureRestoreOutcome.Restored;
                diagnostic =
                    "No captured physical Pause posture requires restoration.";
                return true;
            }

            if (_requestInFlight)
            {
                outcome = PlayerInputPostureRestoreOutcome.FailedRecoverable;
                diagnostic =
                    "Physical Pause posture restoration rejected because another Pause product request is in progress.";
                return false;
            }

            string resolvedReason =
                reason.NormalizeTextOrFallback("activity-exit-pause-cleanup");

            if (!_application.TryGetApplicationPauseSnapshot(
                    out PauseSnapshot previousPause) ||
                !previousPause.IsValid)
            {
                outcome = PlayerInputPostureRestoreOutcome.FailedRecoverable;
                diagnostic = "Activity-exit physical restoration requires the current Pause snapshot.";
                return false;
            }

            _requestInFlight = true;
            PauseProductTransaction transaction = BeginTransaction(
                previousPause,
                PauseState.Running,
                nameof(PauseProductBindingRuntimeContext),
                resolvedReason);
            try
            {
                if (!_adapter.TryRestoreActionMapSet(
                        _playerInput,
                        _pauseActionMapSetReceipt,
                        nameof(PauseProductBindingRuntimeContext),
                        resolvedReason,
                        out string physicalDiagnostic))
                {
                    RollbackTransaction(transaction);
                    if (_playerInput == null)
                    {
                        _pauseActionMapSetReceipt = default;
                        outcome = PlayerInputPostureRestoreOutcome.TargetDestroyed;
                        diagnostic = "The exact PlayerInput target was destroyed during Activity-exit cleanup; physical restoration was not confirmed. " + physicalDiagnostic;
                        return true;
                    }
                    outcome = PlayerInputPostureRestoreOutcome.FailedRecoverable;
                    diagnostic = "Physical Pause posture restoration failed and remains retryable. " + physicalDiagnostic;
                    return false;
                }

                if (!CommitTransaction(transaction))
                {
                    _adapter.TryApplyActionMapSet(
                        _playerInput,
                        _pauseActionMapSetReceipt.AppliedPrimaryActionMapName,
                        _pauseActionMapSetReceipt.AppliedEnabledActionMapNames,
                        nameof(PauseProductBindingRuntimeContext),
                        "pause-transaction-commit-failed",
                        out _,
                        out string physicalRollbackDiagnostic);
                    diagnostic =
                        "Physical Pause posture restoration transaction commit failed. compensation='" +
                        physicalRollbackDiagnostic + "'.";
                    outcome = _playerInput == null
                        ? PlayerInputPostureRestoreOutcome.TargetDestroyed
                        : PlayerInputPostureRestoreOutcome.FailedRecoverable;
                    if (outcome == PlayerInputPostureRestoreOutcome.TargetDestroyed)
                    {
                        _pauseActionMapSetReceipt = default;
                        diagnostic = "The exact PlayerInput target was destroyed during Activity-exit compensation; physical restoration was not confirmed. " + diagnostic;
                        return true;
                    }
                    return false;
                }

                _pauseActionMapSetReceipt = default;
                outcome = PlayerInputPostureRestoreOutcome.Restored;
                diagnostic = "Physical Pause posture restored for Activity exit.";
                return true;
            }
            finally
            {
                EndTransaction(transaction);
            }
        }

        public PauseProductRequestResult RequestPause(
            PauseRequest request)
        {
            if (_requestInFlight)
            {
                return Record(
                    PauseProductRequestStatus.Rejected,
                    default,
                    default,
                    "Pause product request rejected because another request is in progress.");
            }

            if (!request.IsValid ||
                !_application.TryGetApplicationPauseSnapshot(
                    out PauseSnapshot previousPause) ||
                !previousPause.IsValid)
            {
                return Record(
                    PauseProductRequestStatus.Rejected,
                    default,
                    default,
                    "Pause product request is invalid or application Pause is unavailable.");
            }

            if (!HasActivePlayerInputBinding)
            {
                if (!IsCleanlyUnbound())
                {
                    return Record(
                        PauseProductRequestStatus.BindingUnavailable,
                        default,
                        default,
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
            if (targetPause == previousPause.State)
            {
                return Record(
                    PauseProductRequestStatus.Ignored,
                    default,
                    default,
                    "Pause product request ignored because Pause is already in the requested state.");
            }

            bool paused =
                targetPause == PauseState.Paused;
            if (paused && _pauseActionMapSetReceipt.IsValid)
            {
                return Record(
                    PauseProductRequestStatus.Failed,
                    default,
                    default,
                    "Pause product request rejected because retained Pause-time PlayerInput baseline evidence is inconsistent.");
            }

            if (!paused && !_pauseActionMapSetReceipt.IsValid)
            {
                return Record(
                    PauseProductRequestStatus.Failed,
                    default,
                    default,
                    "Resume rejected because exact Pause-time PlayerInput baseline evidence is unavailable.");
            }

            _requestInFlight = true;
            PauseProductTransaction transaction = BeginTransaction(
                previousPause,
                targetPause,
                request.Source,
                request.Reason);
            try
            {
                if (!_application.TryApplyProductPause(
                        request,
                        out PauseResult pauseResult,
                        out string pauseDiagnostic))
                {
                    RollbackTransaction(transaction);
                    return Record(
                        pauseResult.Rejected
                            ? PauseProductRequestStatus.Rejected
                            : PauseProductRequestStatus.Failed,
                        pauseResult,
                        transaction,
                        pauseDiagnostic);
                }

                UnityPlayerInputActionMapSetWriteReceipt
                    physicalReceipt = default;
                string physicalDiagnostic;
                if (paused)
                {
                    if (!_adapter.TryApplyActionMapSet(
                            _playerInput,
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
                        RollbackTransaction(transaction);
                        _application.TryRestorePauseSnapshot(
                            previousPause,
                            "pause-product-physical-rollback",
                            out string rollbackDiagnostic);
                        return Record(
                            PauseProductRequestStatus.Failed,
                            pauseResult,
                            transaction,
                            $"Physical Pause posture failed. physical='{physicalDiagnostic}' compensation='{rollbackDiagnostic}'.");
                    }
                }
                else if (!_adapter.TryRestoreActionMapSet(
                             _playerInput,
                             _pauseActionMapSetReceipt,
                             nameof(PauseProductBindingRuntimeContext),
                             request.Reason,
                             out physicalDiagnostic))
                {
                    _adapter.TryApplyActionMapSet(
                        _playerInput,
                        _pauseActionMapSetReceipt.AppliedPrimaryActionMapName,
                        _pauseActionMapSetReceipt.AppliedEnabledActionMapNames,
                        nameof(PauseProductBindingRuntimeContext),
                        "resume-physical-restore-failed",
                        out _,
                        out string physicalRollbackDiagnostic);
                    RollbackTransaction(transaction);
                    _application.TryRestorePauseSnapshot(
                        previousPause,
                        "resume-product-physical-rollback",
                        out string rollbackDiagnostic);
                    return Record(
                        PauseProductRequestStatus.Failed,
                        pauseResult,
                        transaction,
                        $"Physical Resume posture restore failed. physical='{physicalDiagnostic}' physicalCompensation='{physicalRollbackDiagnostic}' compensation='{rollbackDiagnostic}'.");
                }

                if (!CommitTransaction(transaction))
                {
                    string physicalRollbackDiagnostic;
                    if (paused)
                    {
                        _adapter.TryRestoreActionMapSet(
                            _playerInput,
                            physicalReceipt,
                            nameof(PauseProductBindingRuntimeContext),
                            "pause-transaction-commit-failed",
                            out physicalRollbackDiagnostic);
                    }
                    else
                    {
                        _adapter.TryApplyActionMapSet(
                            _playerInput,
                            _pauseActionMapSetReceipt.AppliedPrimaryActionMapName,
                            _pauseActionMapSetReceipt.AppliedEnabledActionMapNames,
                            nameof(PauseProductBindingRuntimeContext),
                            "pause-transaction-commit-failed",
                            out _,
                            out physicalRollbackDiagnostic);
                    }
                    _application.TryRestorePauseSnapshot(
                        previousPause,
                        "pause-transaction-commit-failed",
                        out string rollbackDiagnostic);
                    return Record(
                        PauseProductRequestStatus.Failed,
                        pauseResult,
                        transaction,
                        $"Pause transaction commit failed. physicalCompensation='{physicalRollbackDiagnostic}' compensation='{rollbackDiagnostic}'.");
                }

                _pauseActionMapSetReceipt =
                    paused
                        ? physicalReceipt
                        : default;

                return Record(
                    PauseProductRequestStatus.Applied,
                    pauseResult,
                    transaction,
                    "Pause product request applied through the canonical PlayerInput binding.");
            }
            finally
            {
                EndTransaction(transaction);
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
                        pauseResult.Rejected
                            ? PauseProductRequestStatus.Rejected
                            : PauseProductRequestStatus.Failed,
                        pauseResult,
                        default,
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
                        default,
                        $"Application-only Pause returned an invalid result. compensation='{rollbackDiagnostic}'.");
                }

                if (!pauseResult.Applied)
                {
                    return Record(
                        PauseProductRequestStatus.Ignored,
                        pauseResult,
                        default,
                        "Pause product request completed without a state change and without a PlayerInput transaction.");
                }

                return Record(
                    PauseProductRequestStatus.AppliedWithoutPlayerInput,
                    pauseResult,
                    default,
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
                result.Ignored ||
                result.Status == PauseProductRequestStatus.Rejected)
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
            PauseProductTransaction transaction,
            string diagnostic)
        {
            _lastDiagnostic = diagnostic;
            _lastResult =
                new PauseProductRequestResult(
                    status,
                    pause,
                    transaction,
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
                !_activeTransaction.IsValid &&
                !_pauseActionMapSetReceipt.IsValid;
        }

        private void ClearBinding()
        {
            _token = default;
            _binding = null;
            _playerInput = null;
            _adapter = null;
            _pauseAction = null;
            _activeTransaction = default;
            _bindingPosture = default;
            _pauseActionMapSetReceipt = default;
        }

        private PauseProductTransaction BeginTransaction(
            PauseSnapshot previousPause,
            PauseState targetState,
            string source,
            string reason)
        {
            if (!_requestInFlight || _activeTransaction.IsValid)
                throw new InvalidOperationException("Pause transaction requires the exclusive Pause product request lock.");

            _transactionSequence++;
            _activeTransaction = new PauseProductTransaction(
                _transactionSequence,
                previousPause,
                targetState,
                source,
                reason);
            return _activeTransaction;
        }

        private bool CommitTransaction(PauseProductTransaction transaction)
        {
            if (!IsCurrentTransaction(transaction))
                return false;

            _activeTransaction = default;
            return true;
        }

        private void RollbackTransaction(PauseProductTransaction transaction)
        {
            if (IsCurrentTransaction(transaction))
                _activeTransaction = default;
        }

        private void EndTransaction(PauseProductTransaction transaction)
        {
            RollbackTransaction(transaction);
            _requestInFlight = false;
        }

        private bool IsCurrentTransaction(PauseProductTransaction transaction) =>
            transaction.IsValid &&
            _activeTransaction.IsValid &&
            _activeTransaction.Equals(transaction);
    }
}
