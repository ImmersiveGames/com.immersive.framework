using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Plain host-owned coordinator. It observes committed Session snapshots
    /// after the frame's synchronous Player operations and invokes the exact
    /// IF-M07-10 active-Activity reconcile endpoint without recursive calls.
    /// </summary>
    internal sealed class PlayerActivityReconciliationRuntimeHostModule
    {
        private int observedSessionRevision = -1;
        private ActivityAsset observedActivity;
        private RuntimeContentOwner observedOwner;
        private int observedOccurrence;
        private ActivityPlayerActorReconcileTargetStatus observedTargetStatus;
        private bool reconciling;
        private PlayerActivityReconciliationRuntimeHostSnapshot lastSnapshot;

        internal PlayerActivityReconciliationRuntimeHostSnapshot LastSnapshot =>
            lastSnapshot ??
            new PlayerActivityReconciliationRuntimeHostSnapshot(
                PlayerActivityReconciliationCoordinatorStatus.Ready,
                PlayerActivityReconciliationStableChangeKind.None,
                observedSessionRevision,
                null,
                default,
                0,
                ActivityPlayerActorReconcileTargetStatus.None,
                null,
                "Player Activity reconciliation observer is ready.");

        internal bool ObserveAndReconcile(
            PlayerParticipationSnapshot session,
            ActivityPlayerActorLifecycleParticipant lifecycle,
            string source,
            string reason)
        {
            if (session == null || !session.IsInitialized)
            {
                lastSnapshot =
                    PlayerActivityReconciliationRuntimeHostSnapshot.Unavailable(
                        "Session Player participation snapshot is unavailable.");
                return false;
            }

            if (lifecycle == null)
            {
                lastSnapshot =
                    PlayerActivityReconciliationRuntimeHostSnapshot.Unavailable(
                        "Activity Player Actor lifecycle participant is unavailable.");
                return false;
            }

            ActivityPlayerActorReconcileTarget target =
                lifecycle.CaptureActiveReconcileTarget();
            bool revisionChanged =
                session.Revision != observedSessionRevision;
            bool targetChanged = HasTargetChanged(target);
            if (!revisionChanged && !targetChanged)
            {
                return false;
            }

            PlayerActivityReconciliationStableChangeKind changeKind =
                ResolveChangeKind(revisionChanged, targetChanged);

            if (reconciling)
            {
                return false;
            }

            reconciling = true;
            try
            {
                RecordObservedState(session.Revision, target);

                if (target.Status ==
                    ActivityPlayerActorReconcileTargetStatus.NoActiveActivity)
                {
                    lastSnapshot =
                        new PlayerActivityReconciliationRuntimeHostSnapshot(
                            PlayerActivityReconciliationCoordinatorStatus
                                .SucceededNoActiveActivity,
                            changeKind,
                            session.Revision,
                            null,
                            default,
                            0,
                            target.Status,
                            null,
                            target.Message);
                    return true;
                }

                if (target.Status ==
                    ActivityPlayerActorReconcileTargetStatus
                        .WaitingForOccurrence)
                {
                    lastSnapshot =
                        new PlayerActivityReconciliationRuntimeHostSnapshot(
                            PlayerActivityReconciliationCoordinatorStatus
                                .SucceededWaitingForOccurrence,
                            changeKind,
                            session.Revision,
                            target.Activity,
                            target.Owner,
                            target.Occurrence,
                            target.Status,
                            null,
                            target.Message);
                    return true;
                }

                if (!target.IsReady)
                {
                    lastSnapshot =
                        new PlayerActivityReconciliationRuntimeHostSnapshot(
                            PlayerActivityReconciliationCoordinatorStatus
                                .FailedInvalidTarget,
                            changeKind,
                            session.Revision,
                            target.Activity,
                            target.Owner,
                            target.Occurrence,
                            target.Status,
                            null,
                            target.Message);
                    return true;
                }

                ActivityPlayerActorReconcileResult result =
                    lifecycle.TryReconcileActiveActivityPlayerLifecycle(
                        target.Activity,
                        target.Owner,
                        target.Occurrence,
                        string.IsNullOrWhiteSpace(source)
                            ? nameof(
                                PlayerActivityReconciliationRuntimeHostModule)
                            : source.Trim(),
                        string.IsNullOrWhiteSpace(reason)
                            ? "stable-session-revision-or-activity-occurrence"
                            : reason.Trim());

                PlayerActivityReconciliationCoordinatorStatus status =
                    result == null || !result.Succeeded
                        ? PlayerActivityReconciliationCoordinatorStatus
                            .FailedReconcile
                        : PlayerActivityReconciliationCoordinatorStatus
                            .SucceededReconciled;
                string message = result != null
                    ? result.ToDiagnosticString()
                    : "Active-Activity Player reconcile returned no result.";

                lastSnapshot =
                    new PlayerActivityReconciliationRuntimeHostSnapshot(
                        status,
                        changeKind,
                        session.Revision,
                        target.Activity,
                        target.Owner,
                        target.Occurrence,
                        target.Status,
                        result,
                        message);
                return true;
            }
            catch (Exception exception)
            {
                lastSnapshot =
                    new PlayerActivityReconciliationRuntimeHostSnapshot(
                        PlayerActivityReconciliationCoordinatorStatus
                            .FailedException,
                        changeKind,
                        session.Revision,
                        target.Activity,
                        target.Owner,
                        target.Occurrence,
                        target.Status,
                        null,
                        "Active-Activity Player reconciliation threw an exception. " +
                        exception.Message);
                return true;
            }
            finally
            {
                reconciling = false;
            }
        }

        private bool HasTargetChanged(
            ActivityPlayerActorReconcileTarget target)
        {
            return
                target.Status != observedTargetStatus ||
                !ReferenceEquals(target.Activity, observedActivity) ||
                target.Owner != observedOwner ||
                target.Occurrence != observedOccurrence;
        }

        private void RecordObservedState(
            int sessionRevision,
            ActivityPlayerActorReconcileTarget target)
        {
            observedSessionRevision = sessionRevision;
            observedActivity = target.Activity;
            observedOwner = target.Owner;
            observedOccurrence = target.Occurrence;
            observedTargetStatus = target.Status;
        }

        private static PlayerActivityReconciliationStableChangeKind
            ResolveChangeKind(
                bool revisionChanged,
                bool targetChanged)
        {
            if (revisionChanged && targetChanged)
            {
                return PlayerActivityReconciliationStableChangeKind
                    .SessionRevisionAndActivityOccurrenceChanged;
            }

            return revisionChanged
                ? PlayerActivityReconciliationStableChangeKind
                    .SessionRevisionChanged
                : PlayerActivityReconciliationStableChangeKind
                    .ActivityOccurrenceChanged;
        }
    }
}
