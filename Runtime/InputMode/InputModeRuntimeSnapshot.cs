using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Immutable diagnostics for one scoped InputMode runtime context.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC2 resident InputMode runtime snapshot.")]
    public sealed class InputModeRuntimeSnapshot
    {
        internal InputModeRuntimeSnapshot(
            string contextId,
            InputModeState currentState,
            long operationSequence,
            bool operationInFlight,
            InputModeRuntimeTransaction activeTransaction,
            InputModeRuntimeOperationStatus lastStatus,
            string lastMessage)
        {
            ContextId = contextId.NormalizeText();
            CurrentState = currentState;
            OperationSequence = operationSequence;
            OperationInFlight = operationInFlight;
            ActiveTransaction = activeTransaction;
            LastStatus = lastStatus;
            LastMessage = lastMessage.NormalizeText();
        }

        public string ContextId { get; }
        public InputModeState CurrentState { get; }
        public long OperationSequence { get; }
        public bool OperationInFlight { get; }
        public InputModeRuntimeTransaction ActiveTransaction { get; }
        public InputModeRuntimeOperationStatus LastStatus { get; }
        public string LastMessage { get; }

        public bool IsInitialized =>
            !string.IsNullOrEmpty(ContextId) && CurrentState.IsValid;

        public InputModeKind CurrentMode => CurrentState.CurrentKind;
        public int Revision => CurrentState.Revision;
        public long ActiveSequence =>
            OperationInFlight ? ActiveTransaction.Sequence : 0;
        public string ActiveRequester =>
            OperationInFlight
                ? ActiveTransaction.Request.Requester
                : string.Empty;

        public string ToDiagnosticString()
        {
            return
                $"context='{ContextId}' initialized='{IsInitialized}' " +
                $"mode='{CurrentMode}' revision='{Revision}' " +
                $"operationSequence='{OperationSequence}' " +
                $"operationInFlight='{OperationInFlight}' " +
                $"activeSequence='{ActiveSequence}' " +
                $"activeRequester='{ActiveRequester}' " +
                $"lastStatus='{LastStatus}' " +
                $"lastMessage='{LastMessage}'.";
        }
    }
}
