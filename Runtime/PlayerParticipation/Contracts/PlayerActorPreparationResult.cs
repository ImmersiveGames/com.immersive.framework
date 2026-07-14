using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Complete immutable evidence for one Session Logical Player Actor preparation operation.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.4 Session Logical Player Actor prepare, release and replace result.")]
    public sealed class PlayerActorPreparationResult
    {
        internal PlayerActorPreparationResult(
            PlayerActorPreparationStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationSummary previousSummary,
            PlayerActorPreparationSummary currentSummary,
            PlayerActorMaterializationResult materializationResult,
            bool hasMaterializationResult,
            PlayerActorSelectionResult selectionResult,
            bool hasSelectionResult,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            bool previousReleaseAttempted,
            bool previousReleaseSucceeded,
            string previousReleaseMessage,
            PlayerActorPreparationSnapshot snapshot,
            string message,
            PlayerActorPreparationStatus originalStatus = PlayerActorPreparationStatus.None)
        {
            Status = status;
            OriginalStatus = originalStatus == PlayerActorPreparationStatus.None
                ? status
                : originalStatus;
            Operation = operation ?? string.Empty;
            PlayerSlotId = playerSlotId;
            PreviousSummary = previousSummary;
            CurrentSummary = currentSummary;
            MaterializationResult = materializationResult;
            HasMaterializationResult = hasMaterializationResult;
            SelectionResult = selectionResult;
            HasSelectionResult = hasSelectionResult;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            RollbackMessage = rollbackMessage ?? string.Empty;
            PreviousReleaseAttempted = previousReleaseAttempted;
            PreviousReleaseSucceeded = previousReleaseSucceeded;
            PreviousReleaseMessage = previousReleaseMessage ?? string.Empty;
            Snapshot = snapshot;
            Message = message ?? string.Empty;
        }

        public PlayerActorPreparationStatus Status { get; }
        public PlayerActorPreparationStatus OriginalStatus { get; }
        public string Operation { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerActorPreparationSummary PreviousSummary { get; }
        public PlayerActorPreparationSummary CurrentSummary { get; }
        public PlayerActorMaterializationResult MaterializationResult { get; }
        public bool HasMaterializationResult { get; }
        public PlayerActorSelectionResult SelectionResult { get; }
        public bool HasSelectionResult { get; }
        public bool RollbackAttempted { get; }
        public bool RollbackSucceeded { get; }
        public string RollbackMessage { get; }
        public bool PreviousReleaseAttempted { get; }
        public bool PreviousReleaseSucceeded { get; }
        public string PreviousReleaseMessage { get; }
        public PlayerActorPreparationSnapshot Snapshot { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            PlayerActorPreparationStatus.SucceededPrepared or
            PlayerActorPreparationStatus.SucceededReleased or
            PlayerActorPreparationStatus.SucceededReplaced or
            PlayerActorPreparationStatus.SucceededAlreadyPrepared or
            PlayerActorPreparationStatus.SucceededAlreadyReleased;

        public bool Failed => Status is
            PlayerActorPreparationStatus.FailedMaterialization or
            PlayerActorPreparationStatus.FailedActivation or
            PlayerActorPreparationStatus.FailedSelectionCommit or
            PlayerActorPreparationStatus.FailedRelease or
            PlayerActorPreparationStatus.FailedRollback or
            PlayerActorPreparationStatus.FailedPreviousRelease;

        public bool Rejected => !Succeeded && !Failed && Status != PlayerActorPreparationStatus.None;
        public bool Completed => Status != PlayerActorPreparationStatus.None;
        public bool StateChanged =>
            PreviousSummary.State != CurrentSummary.State ||
            PreviousSummary.Token != CurrentSummary.Token;

        public string ToDiagnosticString()
        {
            return $"operation='{Operation}' status='{Status}' originalStatus='{OriginalStatus}' " +
                $"slot='{(PlayerSlotId.IsValid ? PlayerSlotId.StableText : string.Empty)}' " +
                $"previous=({PreviousSummary.ToDiagnosticString()}) current=({CurrentSummary.ToDiagnosticString()}) " +
                $"materializationStatus='{(HasMaterializationResult ? MaterializationResult.Status.ToString() : string.Empty)}' " +
                $"selectionStatus='{(HasSelectionResult ? SelectionResult.Status.ToString() : string.Empty)}' " +
                $"rollbackAttempted='{RollbackAttempted}' rollbackSucceeded='{RollbackSucceeded}' rollbackMessage='{RollbackMessage}' " +
                $"previousReleaseAttempted='{PreviousReleaseAttempted}' previousReleaseSucceeded='{PreviousReleaseSucceeded}' " +
                $"previousReleaseMessage='{PreviousReleaseMessage}' message='{Message}'";
        }
    }
}
