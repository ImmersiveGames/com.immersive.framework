using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7B staged Activity Player admission operation result.")]
    public sealed class ActivityPlayerAdmissionStageResult
    {
        internal ActivityPlayerAdmissionStageResult(
            ActivityPlayerAdmissionStageStatus status,
            string operation,
            ActivityPlayerAdmissionStageSnapshot previousSnapshot,
            ActivityPlayerAdmissionStageSnapshot currentSnapshot,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackIssue,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PreviousSnapshot = previousSnapshot;
            CurrentSnapshot = currentSnapshot;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            RollbackIssue = rollbackIssue.NormalizeText();
            Message = message.NormalizeText();
        }

        public ActivityPlayerAdmissionStageStatus Status { get; }
        public string Operation { get; }
        public ActivityPlayerAdmissionStageSnapshot PreviousSnapshot { get; }
        public ActivityPlayerAdmissionStageSnapshot CurrentSnapshot { get; }
        public bool RollbackAttempted { get; }
        public bool RollbackSucceeded { get; }
        public string RollbackIssue { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            ActivityPlayerAdmissionStageStatus.SucceededReadyToCommit or
            ActivityPlayerAdmissionStageStatus.SucceededCommitted or
            ActivityPlayerAdmissionStageStatus.SucceededRolledBack or
            ActivityPlayerAdmissionStageStatus.SucceededAlreadyRolledBack;

        public string ToDiagnosticString()
        {
            return
                $"operation='{Operation}' status='{Status}' " +
                $"rollbackAttempted='{RollbackAttempted}' rollbackSucceeded='{RollbackSucceeded}' " +
                $"rollbackIssue='{RollbackIssue}' message='{Message}' " +
                $"current=[{(CurrentSnapshot != null ? CurrentSnapshot.ToDiagnosticString() : string.Empty)}]";
        }
    }
}
