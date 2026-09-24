using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Scoped resident authority for one logical InputMode posture.
    /// It serializes requests and commits state only after the caller reports that
    /// the corresponding physical application completed. It owns no Unity object,
    /// PlayerInputManager, Pause runtime or global registration.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2 scoped resident InputMode state owner and request arbiter.")]
    public sealed class InputModeRuntimeContext
    {
        private readonly string _contextId;

        private InputModeState _currentState;
        private long _operationSequence;
        private bool _operationInFlight;
        private InputModeRuntimeTransaction _activeTransaction;
        private InputModeRuntimeOperationStatus _lastStatus =
            InputModeRuntimeOperationStatus.Unknown;
        private string _lastMessage =
            "InputMode runtime context has not processed an operation.";

        public InputModeRuntimeContext(
            string contextId,
            InputModeState initialState)
        {
            this._contextId = contextId.NormalizeText();
            if (string.IsNullOrEmpty(this._contextId))
            {
                throw new ArgumentException(
                    "InputMode runtime context requires an explicit context id.",
                    nameof(contextId));
            }

            if (!initialState.IsValid)
            {
                throw new ArgumentException(
                    "InputMode runtime context requires a valid initial state.",
                    nameof(initialState));
            }

            _currentState = initialState;
        }

        public string ContextId => _contextId;
        public InputModeState CurrentState => _currentState;
        public bool OperationInFlight => _operationInFlight;

        public InputModeRuntimeOperationResult TryBegin(
            InputModeRequest request,
            string source,
            out InputModeRuntimeTransaction transaction)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(InputModeRuntimeContext));
            transaction = default;

            if (!request.IsValid)
            {
                return Record(
                    InputModeRuntimeOperationStatus.RejectedInvalidRequest,
                    request,
                    default,
                    _currentState,
                    _currentState,
                    resolvedSource,
                    request.Reason,
                    "InputMode runtime rejected an invalid target mode.");
            }

            if (_operationInFlight)
            {
                return Record(
                    InputModeRuntimeOperationStatus.RejectedOperationInFlight,
                    request,
                    _activeTransaction,
                    _currentState,
                    _currentState,
                    resolvedSource,
                    request.Reason,
                    $"InputMode runtime already has transaction '{_activeTransaction.Sequence}' in flight.");
            }

            InputModeRequestResult preview =
                InputModeRequestEvaluator.Preview(
                    _currentState,
                    request,
                    resolvedSource);
            if (preview.Ignored)
            {
                return Record(
                    InputModeRuntimeOperationStatus.IgnoredAlreadyCurrent,
                    request,
                    default,
                    _currentState,
                    _currentState,
                    resolvedSource,
                    request.Reason,
                    "InputMode runtime is already in the requested mode.");
            }

            if (!preview.Succeeded)
            {
                return Record(
                    InputModeRuntimeOperationStatus.RejectedInvalidRequest,
                    request,
                    default,
                    _currentState,
                    _currentState,
                    resolvedSource,
                    request.Reason,
                    "InputMode runtime request preview failed.");
            }

            _operationSequence++;
            transaction = new InputModeRuntimeTransaction(
                _contextId,
                _operationSequence,
                request,
                preview.PreviousState,
                preview.CurrentState);
            _activeTransaction = transaction;
            _operationInFlight = true;

            return Record(
                InputModeRuntimeOperationStatus.SucceededPrepared,
                request,
                transaction,
                _currentState,
                _currentState,
                resolvedSource,
                request.Reason,
                $"InputMode transaction '{transaction.Sequence}' prepared.");
        }

        public InputModeRuntimeOperationResult Commit(
            InputModeRuntimeTransaction transaction,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(InputModeRuntimeContext));
            string resolvedReason = reason.NormalizeText();
            InputModeState previous = _currentState;

            if (!IsCurrentTransaction(transaction))
            {
                return Record(
                    InputModeRuntimeOperationStatus
                        .RejectedForeignOrStaleTransaction,
                    transaction.Request,
                    transaction,
                    _currentState,
                    _currentState,
                    resolvedSource,
                    resolvedReason,
                    "InputMode commit rejected missing, foreign or stale transaction evidence.");
            }

            _currentState = transaction.NextState;
            ClearActiveTransaction();
            return Record(
                InputModeRuntimeOperationStatus.SucceededCommitted,
                transaction.Request,
                transaction,
                previous,
                _currentState,
                resolvedSource,
                resolvedReason,
                $"InputMode transaction '{transaction.Sequence}' committed.");
        }

        public InputModeRuntimeOperationResult Rollback(
            InputModeRuntimeTransaction transaction,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(InputModeRuntimeContext));
            string resolvedReason = reason.NormalizeText();

            if (!IsCurrentTransaction(transaction))
            {
                return Record(
                    InputModeRuntimeOperationStatus
                        .RejectedForeignOrStaleTransaction,
                    transaction.Request,
                    transaction,
                    _currentState,
                    _currentState,
                    resolvedSource,
                    resolvedReason,
                    "InputMode rollback rejected missing, foreign or stale transaction evidence.");
            }

            InputModeState preserved = _currentState;
            ClearActiveTransaction();
            return Record(
                InputModeRuntimeOperationStatus.RolledBack,
                transaction.Request,
                transaction,
                preserved,
                preserved,
                resolvedSource,
                resolvedReason,
                $"InputMode transaction '{transaction.Sequence}' rolled back without committing logical state.");
        }

        public InputModeRuntimeSnapshot CreateSnapshot()
        {
            return new InputModeRuntimeSnapshot(
                _contextId,
                _currentState,
                _operationSequence,
                _operationInFlight,
                _activeTransaction,
                _lastStatus,
                _lastMessage);
        }

        private bool IsCurrentTransaction(
            InputModeRuntimeTransaction transaction)
        {
            return _operationInFlight &&
                   transaction.IsValid &&
                   _activeTransaction.IsValid &&
                   transaction == _activeTransaction &&
                   string.Equals(
                       transaction.ContextId,
                       _contextId,
                       StringComparison.Ordinal) &&
                   transaction.PreviousState.Equals(_currentState);
        }

        private void ClearActiveTransaction()
        {
            _operationInFlight = false;
            _activeTransaction = default;
        }

        private InputModeRuntimeOperationResult Record(
            InputModeRuntimeOperationStatus status,
            InputModeRequest request,
            InputModeRuntimeTransaction transaction,
            InputModeState previousState,
            InputModeState nextState,
            string source,
            string reason,
            string message)
        {
            _lastStatus = status;
            _lastMessage = message.NormalizeText();
            return new InputModeRuntimeOperationResult(
                status,
                request,
                transaction,
                previousState,
                nextState,
                CreateSnapshot(),
                source,
                reason,
                _lastMessage);
        }
    }
}
