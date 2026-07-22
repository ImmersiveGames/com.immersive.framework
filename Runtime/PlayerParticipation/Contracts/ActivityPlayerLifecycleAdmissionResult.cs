using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Activity Player lifecycle admission operation result.")]
    public sealed class ActivityPlayerLifecycleAdmissionResult
    {
        internal ActivityPlayerLifecycleAdmissionResult(
            ActivityPlayerLifecycleAdmissionStatus status,
            string operation,
            ActivityPlayerLifecycleAdmissionSnapshot previousSnapshot,
            ActivityPlayerLifecycleAdmissionSnapshot currentSnapshot,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PreviousSnapshot = previousSnapshot;
            CurrentSnapshot = currentSnapshot;
            Message = message.NormalizeText();
        }

        public ActivityPlayerLifecycleAdmissionStatus Status { get; }
        public string Operation { get; }
        public ActivityPlayerLifecycleAdmissionSnapshot PreviousSnapshot { get; }
        public ActivityPlayerLifecycleAdmissionSnapshot CurrentSnapshot { get; }
        public string Message { get; }

        public bool NotRequired =>
            Status == ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired;

        public bool ReadyForTransition => Status is
            ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired or
            ActivityPlayerLifecycleAdmissionStatus.SucceededReadyToCommit or
            ActivityPlayerLifecycleAdmissionStatus.SucceededAlreadyReadyToCommit or
            ActivityPlayerLifecycleAdmissionStatus.SucceededTransitionAuthorized or
            ActivityPlayerLifecycleAdmissionStatus.SucceededAlreadyTransitionAuthorized;

        public bool CanActivate => Status is
            ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired or
            ActivityPlayerLifecycleAdmissionStatus.SucceededCommitted or
            ActivityPlayerLifecycleAdmissionStatus.SucceededAlreadyCommitted or
            ActivityPlayerLifecycleAdmissionStatus.SucceededCommitCleanupPending or
            ActivityPlayerLifecycleAdmissionStatus.SucceededLifecycleCompleted;

        public bool Succeeded => ReadyForTransition || CanActivate ||
            Status is ActivityPlayerLifecycleAdmissionStatus.SucceededRolledBack or
                ActivityPlayerLifecycleAdmissionStatus.SucceededAlreadyRolledBack;

        public bool Failed =>
            (int)Status >=
            (int)ActivityPlayerLifecycleAdmissionStatus.FailedScopePreparation;

        public bool Rejected =>
            Status != ActivityPlayerLifecycleAdmissionStatus.None &&
            !Succeeded &&
            !Failed;

        public string ToDiagnosticString() =>
            $"operation='{Operation}' status='{Status}' message='{Message}' " +
            $"current=[{CurrentSnapshot?.ToDiagnosticString() ?? string.Empty}]";

        internal static ActivityPlayerLifecycleAdmissionResult NotRequiredResult(
            string operation,
            string source,
            string reason,
            string message)
        {
            ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                ActivityPlayerLifecycleAdmissionSnapshot.NotRequired(
                    source,
                    reason,
                    message);
            return new ActivityPlayerLifecycleAdmissionResult(
                ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                operation,
                snapshot,
                snapshot,
                message);
        }

        internal static ActivityPlayerLifecycleAdmissionResult RejectedRuntimeUnavailable(
            string operation,
            string source,
            string reason,
            string message)
        {
            ActivityPlayerLifecycleAdmissionSnapshot snapshot =
                ActivityPlayerLifecycleAdmissionSnapshot.Empty(
                    ActivityPlayerLifecycleAdmissionStatus.RejectedRuntimeUnavailable,
                    source,
                    reason,
                    message);
            return new ActivityPlayerLifecycleAdmissionResult(
                ActivityPlayerLifecycleAdmissionStatus.RejectedRuntimeUnavailable,
                operation,
                snapshot,
                snapshot,
                message);
        }
    }
}
