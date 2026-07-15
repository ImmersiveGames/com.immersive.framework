using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete host-scoped operation result for creating or releasing the current
    /// P3K.2-P3K.5 gameplay chain of one Player Slot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7F current Player gameplay chain operation result.")]
    public sealed class PlayerGameplayRuntimeOperationResult
    {
        internal PlayerGameplayRuntimeOperationResult(
            PlayerGameplayRuntimeOperationStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary previousAdmission,
            PlayerGameplayAdmissionSummary currentAdmission,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            PlayerGameplayRuntimeHostSnapshot snapshot,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PlayerSlotId = playerSlotId;
            PreviousAdmission = previousAdmission;
            CurrentAdmission = currentAdmission;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            RollbackMessage = rollbackMessage.NormalizeText();
            Snapshot = snapshot;
            Message = message.NormalizeText();
        }

        public PlayerGameplayRuntimeOperationStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayAdmissionSummary PreviousAdmission { get; }
        public PlayerGameplayAdmissionSummary CurrentAdmission { get; }
        public bool RollbackAttempted { get; }
        public bool RollbackSucceeded { get; }
        public string RollbackMessage { get; }
        public PlayerGameplayRuntimeHostSnapshot Snapshot { get; }
        public string Message { get; }

        public bool Succeeded =>
            Status == PlayerGameplayRuntimeOperationStatus.SucceededReady ||
            Status == PlayerGameplayRuntimeOperationStatus.SucceededAlreadyReady ||
            Status == PlayerGameplayRuntimeOperationStatus.SucceededBlockedByInputGate ||
            Status == PlayerGameplayRuntimeOperationStatus.SucceededAlreadyBlockedByInputGate ||
            Status == PlayerGameplayRuntimeOperationStatus.SucceededReleased ||
            Status == PlayerGameplayRuntimeOperationStatus.SucceededAlreadyReleased;

        public bool GameplayReady => CurrentAdmission.GameplayReady;

        public string ToDiagnosticString()
        {
            return
                $"operation='{Operation}' status='{Status}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"gameplayReady='{GameplayReady}' rollbackAttempted='{RollbackAttempted}' " +
                $"rollbackSucceeded='{RollbackSucceeded}' rollbackMessage='{RollbackMessage}' " +
                $"message='{Message}' snapshot=[{(Snapshot != null ? Snapshot.ToDiagnosticString() : string.Empty)}]";
        }
    }
}
