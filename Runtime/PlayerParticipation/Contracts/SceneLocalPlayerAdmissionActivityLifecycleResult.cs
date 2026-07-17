using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B2A immutable Scene Local Player Activity lifecycle result.")]
    public sealed class SceneLocalPlayerAdmissionActivityLifecycleResult
    {
        internal SceneLocalPlayerAdmissionActivityLifecycleResult(
            SceneLocalPlayerAdmissionActivityLifecycleStatus status,
            SceneLocalPlayerAdmissionActivityLifecycleStatus originalStatus,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            int affectedCount,
            int blockingIssueCount,
            string source,
            string reason,
            string message)
        {
            Status = status;
            OriginalStatus = originalStatus;
            Activity = activity;
            Owner = owner;
            AffectedCount = affectedCount;
            BlockingIssueCount = blockingIssueCount;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public SceneLocalPlayerAdmissionActivityLifecycleStatus Status { get; }
        public SceneLocalPlayerAdmissionActivityLifecycleStatus OriginalStatus { get; }
        public ActivityAsset Activity { get; }
        public RuntimeContentOwner Owner { get; }
        public int AffectedCount { get; }
        public int BlockingIssueCount { get; }
        public string Source { get; }
        public string Reason { get; }
        public string Message { get; }

        public bool Succeeded => Status is
            SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededEntered or
            SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyEntered or
            SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededNoAutomaticPlayers or
            SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededExited or
            SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyExited or
            SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededRolledBack;

        public bool Failed => !Succeeded &&
            Status != SceneLocalPlayerAdmissionActivityLifecycleStatus.None;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public string ToDiagnosticString()
        {
            return $"status='{Status}' originalStatus='{OriginalStatus}' " +
                $"activity='{(Activity != null ? Activity.ActivityName : string.Empty)}' " +
                $"owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"affected='{AffectedCount}' blockingIssues='{BlockingIssueCount}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }
    }
}
