using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable evidence for one occurrence-scoped Player lifecycle delta reconcile pass.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-M07-10 explicit active-Activity Player lifecycle reconcile result.")]
    public sealed class ActivityPlayerActorReconcileResult
    {
        internal ActivityPlayerActorReconcileResult(
            ActivityPlayerActorReconcileStatus status,
            string activityName,
            RuntimeContentOwner owner,
            int occurrence,
            int requestedSessionRevision,
            int appliedSessionRevision,
            int projectedSlotCount,
            int satisfiedSlotCount,
            int pendingSlotCount,
            int failedSlotCount,
            ActivityPlayerActorReadinessReason readinessReason,
            bool stateChanged,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            ActivityPlayerActorLifecycleSnapshot lifecycleSnapshot,
            string message)
        {
            Status = status;
            ActivityName = activityName ?? string.Empty;
            Owner = owner;
            Occurrence = occurrence;
            RequestedSessionRevision = requestedSessionRevision;
            AppliedSessionRevision = appliedSessionRevision;
            ProjectedSlotCount = projectedSlotCount;
            SatisfiedSlotCount = satisfiedSlotCount;
            PendingSlotCount = pendingSlotCount;
            FailedSlotCount = failedSlotCount;
            ReadinessReason = readinessReason;
            StateChanged = stateChanged;
            RollbackAttempted = rollbackAttempted;
            RollbackSucceeded = rollbackSucceeded;
            LifecycleSnapshot = lifecycleSnapshot;
            Message = message ?? string.Empty;
        }

        public ActivityPlayerActorReconcileStatus Status { get; }
        public string ActivityName { get; }
        public RuntimeContentOwner Owner { get; }
        public int Occurrence { get; }
        public int RequestedSessionRevision { get; }
        public int AppliedSessionRevision { get; }
        public int ProjectedSlotCount { get; }
        public int SatisfiedSlotCount { get; }
        public int PendingSlotCount { get; }
        public int FailedSlotCount { get; }
        public ActivityPlayerActorReadinessReason ReadinessReason { get; }
        public bool StateChanged { get; }
        public bool RollbackAttempted { get; }
        public bool RollbackSucceeded { get; }
        public ActivityPlayerActorLifecycleSnapshot LifecycleSnapshot { get; }
        public string Message { get; }

        public bool Succeeded =>
            Status == ActivityPlayerActorReconcileStatus.SucceededNoChange ||
            Status == ActivityPlayerActorReconcileStatus.SucceededProgressed ||
            Status == ActivityPlayerActorReconcileStatus.SucceededCompleted;

        public bool Completed =>
            Status == ActivityPlayerActorReconcileStatus.SucceededCompleted;

        public bool Failed =>
            Status >= ActivityPlayerActorReconcileStatus.FailedProjection;

        public string ToDiagnosticString()
        {
            return
                $"status='{Status}' activity='{ActivityName}' " +
                $"owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"occurrence='{Occurrence}' requestedSessionRevision='{RequestedSessionRevision}' " +
                $"appliedSessionRevision='{AppliedSessionRevision}' projected='{ProjectedSlotCount}' " +
                $"satisfied='{SatisfiedSlotCount}' pending='{PendingSlotCount}' failed='{FailedSlotCount}' " +
                $"readinessReason='{ReadinessReason}' stateChanged='{StateChanged}' " +
                $"rollbackAttempted='{RollbackAttempted}' rollbackSucceeded='{RollbackSucceeded}' " +
                $"message='{Message}'";
        }
    }
}
