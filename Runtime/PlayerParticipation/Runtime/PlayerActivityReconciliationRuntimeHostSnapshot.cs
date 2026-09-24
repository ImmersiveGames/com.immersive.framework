using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed class PlayerActivityReconciliationRuntimeHostSnapshot
    {
        internal PlayerActivityReconciliationRuntimeHostSnapshot(
            PlayerActivityReconciliationCoordinatorStatus status,
            PlayerActivityReconciliationStableChangeKind changeKind,
            int observedSessionRevision,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            int occurrence,
            ActivityPlayerActorReconcileTargetStatus targetStatus,
            ActivityPlayerActorReconcileResult reconcileResult,
            string message)
        {
            Status = status;
            ChangeKind = changeKind;
            ObservedSessionRevision = observedSessionRevision;
            Activity = activity;
            Owner = owner;
            Occurrence = occurrence;
            TargetStatus = targetStatus;
            ReconcileResult = reconcileResult;
            Message = message ?? string.Empty;
        }

        internal PlayerActivityReconciliationCoordinatorStatus Status { get; }
        internal PlayerActivityReconciliationStableChangeKind ChangeKind { get; }
        internal int ObservedSessionRevision { get; }
        internal ActivityAsset Activity { get; }
        internal RuntimeContentOwner Owner { get; }
        internal int Occurrence { get; }
        internal ActivityPlayerActorReconcileTargetStatus TargetStatus { get; }
        internal ActivityPlayerActorReconcileResult ReconcileResult { get; }
        internal string Message { get; }

        internal static PlayerActivityReconciliationRuntimeHostSnapshot
            Unavailable(string message)
        {
            return new PlayerActivityReconciliationRuntimeHostSnapshot(
                PlayerActivityReconciliationCoordinatorStatus
                    .FailedRuntimeUnavailable,
                PlayerActivityReconciliationStableChangeKind.None,
                -1,
                null,
                default,
                0,
                ActivityPlayerActorReconcileTargetStatus.None,
                null,
                message);
        }

        internal string ToDiagnosticString()
        {
            return
                $"status='{Status}' change='{ChangeKind}' " +
                $"sessionRevision='{ObservedSessionRevision}' " +
                $"activity='{(Activity != null ? Activity.ActivityName : string.Empty)}' " +
                $"owner='{(Owner.IsValid ? Owner.StableText : string.Empty)}' " +
                $"occurrence='{Occurrence}' target='{TargetStatus}' " +
                $"reconcile='{ReconcileResult?.Status}' message='{Message}'";
        }
    }
}
