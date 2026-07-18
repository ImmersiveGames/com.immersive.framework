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
        private readonly string contextId;

        private InputModeState currentState;
        private long operationSequence;
        private bool operationInFlight;
        private InputModeRuntimeTransaction activeTransaction;
        private InputModeRuntimeOperationStatus lastStatus =
            InputModeRuntimeOperationStatus.Unknown;
        private string lastMessage =
            "InputMode runtime context has not processed an operation.";

        public InputModeRuntimeContext(
            string contextId,
            InputModeState initialState)
        {
            this.contextId = contextId.NormalizeText();
            if (string.IsNullOrEmpty(this.contextId))
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

            currentState = initialState;
        }

        public string ContextId => contextId;
        public InputModeState CurrentState => currentState;
        public bool OperationInFlight => operationInFlight;

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
                    currentState,
                    currentState,
                    resolvedSource,
                    request.Reason,
                    "InputMode runtime rejected an invalid target mode.");
            }

            if (operationInFlight)
            {
                return Record(
                    InputModeRuntimeOperationStatus.RejectedOperationInFlight,
                    request,
                    activeTransaction,
                    currentState,
                    currentState,
                    resolvedSource,
                    request.Reason,
                    $"InputMode runtime already has transaction '{activeTransaction.Sequence}' in flight.");
            }

            InputModeRequestResult preview =
                InputModeRequestEvaluator.Preview(
                    currentState,
                    request,
                    resolvedSource);
            if (preview.Ignored)
            {
                return Record(
                    InputModeRuntimeOperationStatus.IgnoredAlreadyCurrent,
                    request,
                    default,
                    currentState,
                    currentState,
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
                    currentState,
                    currentState,
                    resolvedSource,
                    request.Reason,
                    "InputMode runtime request preview failed.");
            }

            operationSequence++;
            transaction = new InputModeRuntimeTransaction(
                contextId,
                operationSequence,
                request,
                preview.PreviousState,
                preview.CurrentState);
            activeTransaction = transaction;
            operationInFlight = true;

            return Record(
                InputModeRuntimeOperationStatus.SucceededPrepared,
                request,
                transaction,
                currentState,
                currentState,
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
            InputModeState previous = currentState;

            if (!IsCurrentTransaction(transaction))
            {
                return Record(
                    InputModeRuntimeOperationStatus
                        .RejectedForeignOrStaleTransaction,
                    transaction.Request,
                    transaction,
                    currentState,
                    currentState,
                    resolvedSource,
                    resolvedReason,
                    "InputMode commit rejected missing, foreign or stale transaction evidence.");
            }

            currentState = transaction.NextState;
            ClearActiveTransaction();
            return Record(
                InputModeRuntimeOperationStatus.SucceededCommitted,
                transaction.Request,
                transaction,
                previous,
                currentState,
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
                    currentState,
                    currentState,
                    resolvedSource,
                    resolvedReason,
                    "InputMode rollback rejected missing, foreign or stale transaction evidence.");
            }

            InputModeState preserved = currentState;
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
                contextId,
                currentState,
                operationSequence,
                operationInFlight,
                activeTransaction,
                lastStatus,
                lastMessage);
        }

        private bool IsCurrentTransaction(
            InputModeRuntimeTransaction transaction)
        {
            return operationInFlight &&
                   transaction.IsValid &&
                   activeTransaction.IsValid &&
                   transaction == activeTransaction &&
                   string.Equals(
                       transaction.ContextId,
                       contextId,
                       StringComparison.Ordinal) &&
                   transaction.PreviousState.Equals(currentState);
        }

        private void ClearActiveTransaction()
        {
            operationInFlight = false;
            activeTransaction = default;
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
            lastStatus = status;
            lastMessage = message.NormalizeText();
            return new InputModeRuntimeOperationResult(
                status,
                request,
                transaction,
                previousState,
                nextState,
                CreateSnapshot(),
                source,
                reason,
                lastMessage);
        }
    }
}
