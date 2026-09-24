using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Result from one InputMode runtime authority operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2 scoped InputMode runtime operation result.")]
    public sealed class InputModeRuntimeOperationResult
    {
        internal InputModeRuntimeOperationResult(
            InputModeRuntimeOperationStatus status,
            InputModeRequest request,
            InputModeRuntimeTransaction transaction,
            InputModeState previousState,
            InputModeState currentState,
            InputModeRuntimeSnapshot snapshot,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Request = request;
            Transaction = transaction;
            PreviousState = previousState;
            CurrentState = currentState;
            Snapshot = snapshot;
            Source = source.NormalizeTextOrFallback(
                nameof(InputModeRuntimeOperationResult));
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public InputModeRuntimeOperationStatus Status { get; }
        public InputModeRequest Request { get; }
        public InputModeRuntimeTransaction Transaction { get; }
        public InputModeState PreviousState { get; }
        public InputModeState CurrentState { get; }
        public InputModeRuntimeSnapshot Snapshot { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool Prepared =>
            Status == InputModeRuntimeOperationStatus.SucceededPrepared;
        public bool Committed =>
            Status == InputModeRuntimeOperationStatus.SucceededCommitted;
        public bool Ignored =>
            Status == InputModeRuntimeOperationStatus.IgnoredAlreadyCurrent;
        public bool RolledBack =>
            Status == InputModeRuntimeOperationStatus.RolledBack;
        public bool Succeeded => Prepared || Committed || Ignored || RolledBack;
        public bool Failed => !Succeeded;

        public string ToDiagnosticString()
        {
            return
                $"status='{Status}' prepared='{Prepared}' committed='{Committed}' " +
                $"ignored='{Ignored}' rolledBack='{RolledBack}' " +
                $"previous='{PreviousState.CurrentKind}' " +
                $"current='{CurrentState.CurrentKind}' " +
                $"revision='{CurrentState.Revision}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'.";
        }
    }
}
