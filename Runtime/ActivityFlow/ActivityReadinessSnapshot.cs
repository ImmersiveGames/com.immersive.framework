using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>Read-only presentation state published by the official Activity readiness adapter.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "M03 public activity readiness presentation snapshot.")]
    public readonly struct ActivityReadinessSnapshot
    {
        public ActivityReadinessSnapshot(
            ActivityAsset activity,
            bool isReady,
            string reason,
            int participantCount,
            int requiredCount,
            int optionalCount,
            int pendingCount,
            int completedCount,
            int failedCount,
            int revision)
            : this(
                activity,
                isReady,
                reason,
                participantCount,
                requiredCount,
                optionalCount,
                pendingCount,
                completedCount,
                failedCount,
                0,
                revision)
        {
        }

        public ActivityReadinessSnapshot(
            ActivityAsset activity,
            bool isReady,
            string reason,
            int participantCount,
            int requiredCount,
            int optionalCount,
            int pendingCount,
            int completedCount,
            int failedCount,
            int occurrence,
            int revision)
        {
            Activity = activity;
            IsReady = isReady;
            Reason = reason ?? string.Empty;
            ParticipantCount = participantCount;
            RequiredCount = requiredCount;
            OptionalCount = optionalCount;
            PendingCount = pendingCount;
            CompletedCount = completedCount;
            FailedCount = failedCount;
            Occurrence = occurrence > 0 ? occurrence : 0;
            Revision = revision;
        }

        public ActivityAsset Activity { get; }
        public bool IsReady { get; }
        public bool IsPreparing => ParticipantCount > 0 && PendingCount > 0;
        public string Reason { get; }
        public int ParticipantCount { get; }
        public int RequiredCount { get; }
        public int OptionalCount { get; }
        public int PendingCount { get; }
        public int CompletedCount { get; }
        public int FailedCount { get; }
        public int Occurrence { get; }
        public bool HasOccurrence => Occurrence > 0;
        public int Revision { get; }
    }
}
