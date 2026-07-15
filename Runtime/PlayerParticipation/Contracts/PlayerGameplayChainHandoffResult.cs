using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7D complete reversible Player gameplay handoff operation result.")]
    public sealed class PlayerGameplayChainHandoffResult
    {
        internal PlayerGameplayChainHandoffResult(
            PlayerGameplayChainHandoffStatus status,
            string operation,
            PlayerGameplayChainHandoffSnapshot previousSnapshot,
            PlayerGameplayChainHandoffSnapshot currentSnapshot,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PreviousSnapshot = previousSnapshot;
            CurrentSnapshot = currentSnapshot;
            Message = message.NormalizeText();
        }

        public PlayerGameplayChainHandoffStatus Status { get; }
        public string Operation { get; }
        public PlayerGameplayChainHandoffSnapshot PreviousSnapshot { get; }
        public PlayerGameplayChainHandoffSnapshot CurrentSnapshot { get; }
        public string Message { get; }
        public bool Succeeded => Status is
            PlayerGameplayChainHandoffStatus.SucceededCommitted or
            PlayerGameplayChainHandoffStatus.SucceededAlreadyCommitted or
            PlayerGameplayChainHandoffStatus.SucceededRolledBack;
        public bool Failed => Status is
            PlayerGameplayChainHandoffStatus.FailedCurrentChainRelease or
            PlayerGameplayChainHandoffStatus.FailedPreparationHandoff or
            PlayerGameplayChainHandoffStatus.FailedCandidateChain or
            PlayerGameplayChainHandoffStatus.FailedRollback or
            PlayerGameplayChainHandoffStatus.FailedPreviousActorRelease;
        public bool Rejected =>
            Status != PlayerGameplayChainHandoffStatus.None && !Succeeded && !Failed;
        public string ToDiagnosticString() =>
            $"operation='{Operation}' status='{Status}' message='{Message}' " +
            $"current=[{(CurrentSnapshot != null ? CurrentSnapshot.ToDiagnosticString() : string.Empty)}]";
    }
}
