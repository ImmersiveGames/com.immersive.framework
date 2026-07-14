using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete immutable evidence for one gameplay input binding operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.3 gameplay input binding operation result.")]
    public sealed class PlayerGameplayInputBindingResult
    {
        internal PlayerGameplayInputBindingResult(
            PlayerGameplayInputBindingStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingSummary previousSummary,
            PlayerGameplayInputBindingSummary currentSummary,
            PlayerGameplayInputBindingSnapshot snapshot,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            string message)
        {
            Status = status;
            Operation = operation ?? string.Empty;
            PlayerSlotId = playerSlotId;
            PreviousSummary = previousSummary;
            CurrentSummary = currentSummary;
            Snapshot = snapshot;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            RollbackMessage = rollbackMessage ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public PlayerGameplayInputBindingStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayInputBindingSummary PreviousSummary { get; }
        public PlayerGameplayInputBindingSummary CurrentSummary { get; }
        public PlayerGameplayInputBindingSnapshot Snapshot { get; }
        public bool RollbackAttempted { get; }
        public bool RollbackSucceeded { get; }
        public string RollbackMessage { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            PlayerGameplayInputBindingStatus.SucceededBound or
            PlayerGameplayInputBindingStatus.SucceededReleased or
            PlayerGameplayInputBindingStatus.SucceededAlreadyBound or
            PlayerGameplayInputBindingStatus.SucceededAlreadyReleased or
            PlayerGameplayInputBindingStatus.SucceededAvailabilityRefreshed;

        public bool Failed => Status is
            PlayerGameplayInputBindingStatus.FailedActionMapActivation or
            PlayerGameplayInputBindingStatus.FailedRollback or
            PlayerGameplayInputBindingStatus.FailedRelease;

        public bool Rejected => !Succeeded && !Failed && Status != PlayerGameplayInputBindingStatus.None;
        public bool Completed => Status != PlayerGameplayInputBindingStatus.None;
        public bool StateChanged =>
            PreviousSummary.State != CurrentSummary.State ||
            PreviousSummary.Availability != CurrentSummary.Availability ||
            PreviousSummary.Token != CurrentSummary.Token;

        public string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"previous=({PreviousSummary.ToDiagnosticString()}) current=({CurrentSummary.ToDiagnosticString()}) " +
                $"rollbackAttempted='{RollbackAttempted}' rollbackSucceeded='{RollbackSucceeded}' " +
                $"rollbackMessage='{RollbackMessage}' message='{Message}'";
        }
    }
}
