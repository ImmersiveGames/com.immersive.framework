using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7C contextual Logical Player Actor candidate staging operation result.")]
    public sealed class PlayerActorCandidateStageResult
    {
        internal PlayerActorCandidateStageResult(
            PlayerActorCandidateStageStatus status,
            string operation,
            PlayerActorCandidateStageSnapshot previousSnapshot,
            PlayerActorCandidateStageSnapshot currentSnapshot,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PreviousSnapshot = previousSnapshot;
            CurrentSnapshot = currentSnapshot;
            Message = message.NormalizeText();
        }

        public PlayerActorCandidateStageStatus Status { get; }
        public string Operation { get; }
        public PlayerActorCandidateStageSnapshot PreviousSnapshot { get; }
        public PlayerActorCandidateStageSnapshot CurrentSnapshot { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            PlayerActorCandidateStageStatus.SucceededStaged or
            PlayerActorCandidateStageStatus.SucceededAlreadyStaged or
            PlayerActorCandidateStageStatus.SucceededRolledBack or
            PlayerActorCandidateStageStatus.SucceededAlreadyRolledBack;

        public bool Failed => Status is
            PlayerActorCandidateStageStatus.FailedMaterialization or
            PlayerActorCandidateStageStatus.FailedRollback;

        public bool Rejected =>
            Status != PlayerActorCandidateStageStatus.None &&
            !Succeeded &&
            !Failed;

        public string ToDiagnosticString()
        {
            return
                $"operation='{Operation}' status='{Status}' message='{Message}' " +
                $"current=[{(CurrentSnapshot != null ? CurrentSnapshot.ToDiagnosticString() : string.Empty)}]";
        }
    }
}
