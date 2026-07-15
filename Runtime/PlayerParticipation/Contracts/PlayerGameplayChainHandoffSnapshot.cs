using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7D immutable Player gameplay handoff progress evidence.")]
    public sealed class PlayerGameplayChainHandoffSnapshot
    {
        internal PlayerGameplayChainHandoffSnapshot(
            PlayerGameplayChainHandoffToken token,
            PlayerGameplayChainHandoffState state,
            PlayerActorPreparationToken currentPreparationToken,
            PlayerGameplayAdmissionToken currentAdmissionToken,
            bool currentChainReleased,
            bool preparationSwapped,
            bool candidateChainReady,
            bool candidateOwnershipCompleted,
            bool previousActorReleased,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string source,
            string reason,
            string message)
        {
            Token = token;
            State = state;
            CurrentPreparationToken = currentPreparationToken;
            CurrentAdmissionToken = currentAdmissionToken;
            CurrentChainReleased = currentChainReleased;
            PreparationSwapped = preparationSwapped;
            CandidateChainReady = candidateChainReady;
            CandidateOwnershipCompleted = candidateOwnershipCompleted;
            PreviousActorReleased = previousActorReleased;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PlayerGameplayChainHandoffToken Token { get; }
        public PlayerGameplayChainHandoffState State { get; }
        public PlayerActorPreparationToken CurrentPreparationToken { get; }
        public PlayerGameplayAdmissionToken CurrentAdmissionToken { get; }
        public bool CurrentChainReleased { get; }
        public bool PreparationSwapped { get; }
        public bool CandidateChainReady { get; }
        public bool CandidateOwnershipCompleted { get; }
        public bool PreviousActorReleased { get; }
        public bool RollbackAttempted { get; }
        public bool RollbackSucceeded { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }
        public bool IsCommitted => State == PlayerGameplayChainHandoffState.Committed;
        public bool IsRollbackFailed => State == PlayerGameplayChainHandoffState.RollbackFailed;
        public bool IsCommitCleanupFailed => State == PlayerGameplayChainHandoffState.CommitCleanupFailed;

        public string ToDiagnosticString() =>
            $"handoff='{Token.StableText}' state='{State}' currentPreparation='{CurrentPreparationToken.StableText}' " +
            $"currentAdmission='{CurrentAdmissionToken.StableText}' currentChainReleased='{CurrentChainReleased}' " +
            $"preparationSwapped='{PreparationSwapped}' candidateChainReady='{CandidateChainReady}' " +
            $"candidateOwnershipCompleted='{CandidateOwnershipCompleted}' previousActorReleased='{PreviousActorReleased}' " +
            $"rollbackAttempted='{RollbackAttempted}' rollbackSucceeded='{RollbackSucceeded}' " +
            $"source='{Source}' reason='{Reason}' message='{Message}'";

        internal static PlayerGameplayChainHandoffSnapshot Empty(
            string source,
            string reason,
            string message) =>
            new PlayerGameplayChainHandoffSnapshot(
                default,
                PlayerGameplayChainHandoffState.None,
                default,
                default,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                source,
                reason,
                message);
    }
}
