using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Result for one gameplay admission, refresh, release or rollback operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.5 gameplay admission operation result.")]
    public readonly struct PlayerGameplayAdmissionResult
    {
        internal PlayerGameplayAdmissionResult(
            PlayerGameplayAdmissionStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionSummary previousSummary,
            PlayerGameplayAdmissionSummary currentSummary,
            PlayerGameplayAdmissionSnapshot snapshot,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackIssue,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PlayerSlotId = playerSlotId;
            PreviousSummary = previousSummary;
            CurrentSummary = currentSummary;
            Snapshot = snapshot;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            RollbackIssue = rollbackIssue.NormalizeText();
            Message = message.NormalizeText();
        }

        public PlayerGameplayAdmissionStatus Status { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayAdmissionSummary PreviousSummary { get; }
        public PlayerGameplayAdmissionSummary CurrentSummary { get; }
        public PlayerGameplayAdmissionSnapshot Snapshot { get; }
        public bool RollbackAttempted { get; }
        public bool RollbackSucceeded { get; }
        public string RollbackIssue { get; }
        public string Message { get; }

        public bool Succeeded =>
            Status == PlayerGameplayAdmissionStatus.SucceededReady ||
            Status == PlayerGameplayAdmissionStatus.SucceededBlockedByInputGate ||
            Status == PlayerGameplayAdmissionStatus.SucceededAlreadyAdmitted ||
            Status == PlayerGameplayAdmissionStatus.SucceededReadinessRefreshed ||
            Status == PlayerGameplayAdmissionStatus.SucceededReleased ||
            Status == PlayerGameplayAdmissionStatus.SucceededAlreadyReleased;

        public string ToDiagnosticString()
        {
            return
                $"operation='{Operation}' status='{Status}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"rollbackAttempted='{RollbackAttempted}' rollbackSucceeded='{RollbackSucceeded}' " +
                $"rollbackIssue='{RollbackIssue}' message='{Message}' " +
                $"current=[{CurrentSummary.ToDiagnosticString()}]";
        }
    }
}
