using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E multi-Slot Activity Player handoff group result.")]
    public sealed class ActivityPlayerHandoffGroupResult
    {
        internal ActivityPlayerHandoffGroupResult(
            ActivityPlayerHandoffGroupStatus status,
            string operation,
            ActivityPlayerHandoffGroupSnapshot previousSnapshot,
            ActivityPlayerHandoffGroupSnapshot currentSnapshot,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PreviousSnapshot = previousSnapshot;
            CurrentSnapshot = currentSnapshot;
            Message = message.NormalizeText();
        }
        public ActivityPlayerHandoffGroupStatus Status { get; }
        public string Operation { get; }
        public ActivityPlayerHandoffGroupSnapshot PreviousSnapshot { get; }
        public ActivityPlayerHandoffGroupSnapshot CurrentSnapshot { get; }
        public string Message { get; }
        public bool ReadyToCommit => Status is
            ActivityPlayerHandoffGroupStatus.SucceededReadyToCommit or
            ActivityPlayerHandoffGroupStatus.SucceededAlreadyReadyToCommit;
        public bool Committed => Status is
            ActivityPlayerHandoffGroupStatus.SucceededCommitted or
            ActivityPlayerHandoffGroupStatus.SucceededAlreadyCommitted;
        public bool Succeeded => ReadyToCommit || Committed ||
            Status == ActivityPlayerHandoffGroupStatus.SucceededRolledBack;
        public bool Failed => (int)Status >= (int)ActivityPlayerHandoffGroupStatus.FailedSlotBegin;
        public bool Rejected => Status != ActivityPlayerHandoffGroupStatus.None &&
            !Succeeded && !Failed;
        public string ToDiagnosticString() =>
            $"operation='{Operation}' status='{Status}' message='{Message}' " +
            $"current=[{CurrentSnapshot?.ToDiagnosticString() ?? string.Empty}]";
    }
}
