using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal enum ActivityPlayerActorReconcileTargetStatus
    {
        None = 0,
        Ready = 10,
        NoActiveActivity = 20,
        WaitingForOccurrence = 30,
        InvalidState = 100
    }

    internal sealed class ActivityPlayerActorReconcileTarget
    {
        internal ActivityPlayerActorReconcileTarget(
            ActivityPlayerActorReconcileTargetStatus status,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            int occurrence,
            int appliedSessionRevision,
            string message)
        {
            Status = status;
            Activity = activity;
            Owner = owner;
            Occurrence = occurrence;
            AppliedSessionRevision = appliedSessionRevision;
            Message = message ?? string.Empty;
        }

        internal ActivityPlayerActorReconcileTargetStatus Status { get; }
        internal ActivityAsset Activity { get; }
        internal RuntimeContentOwner Owner { get; }
        internal int Occurrence { get; }
        internal int AppliedSessionRevision { get; }
        internal string Message { get; }

        internal bool IsReady =>
            Status == ActivityPlayerActorReconcileTargetStatus.Ready;
    }

    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        internal ActivityPlayerActorReconcileTarget
            CaptureActiveReconcileTarget()
        {
            if (playerReadinessRecord == null ||
                playerReadinessRecord.Released)
            {
                return new ActivityPlayerActorReconcileTarget(
                    ActivityPlayerActorReconcileTargetStatus.NoActiveActivity,
                    null,
                    default,
                    0,
                    0,
                    "No active Activity Player readiness record is available.");
            }

            ActivityAsset activity = playerReadinessRecord.Activity;
            RuntimeContentOwner owner = playerReadinessRecord.Owner;
            int occurrence = playerReadinessRecord.Occurrence;
            int appliedSessionRevision =
                playerReadinessRecord.AppliedSessionRevision;

            if (activity == null)
            {
                return new ActivityPlayerActorReconcileTarget(
                    ActivityPlayerActorReconcileTargetStatus.InvalidState,
                    null,
                    owner,
                    occurrence,
                    appliedSessionRevision,
                    "The active Activity Player readiness record has no Activity.");
            }

            if (!owner.IsValid ||
                !playerReadinessRecord.ScopeContext.IsValid ||
                playerReadinessRecord.ScopeContext.Owner != owner)
            {
                return new ActivityPlayerActorReconcileTarget(
                    ActivityPlayerActorReconcileTargetStatus.InvalidState,
                    activity,
                    owner,
                    occurrence,
                    appliedSessionRevision,
                    "The active Activity Player readiness record has no exact valid RuntimeContentOwner.");
            }

            if (occurrence <= 0 || playerReadinessParticipant == null)
            {
                return new ActivityPlayerActorReconcileTarget(
                    ActivityPlayerActorReconcileTargetStatus
                        .WaitingForOccurrence,
                    activity,
                    owner,
                    occurrence,
                    appliedSessionRevision,
                    "Activity Player readiness is waiting for its occurrence to start.");
            }

            if (playerReadinessParticipant.Occurrence != occurrence)
            {
                return new ActivityPlayerActorReconcileTarget(
                    ActivityPlayerActorReconcileTargetStatus.InvalidState,
                    activity,
                    owner,
                    occurrence,
                    appliedSessionRevision,
                    "The Activity Player readiness occurrence is foreign or stale.");
            }

            return new ActivityPlayerActorReconcileTarget(
                ActivityPlayerActorReconcileTargetStatus.Ready,
                activity,
                owner,
                occurrence,
                appliedSessionRevision,
                "Exact active Activity Player reconcile target captured.");
        }
    }
}
