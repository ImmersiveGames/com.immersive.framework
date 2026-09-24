using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
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
        private int _observedSessionRevision = -1;
        private ActivityAsset _observedActivity;
        private RuntimeContentOwner _observedOwner;
        private int _observedOccurrence;
        private ActivityPlayerActorReconcileTargetStatus _observedTargetStatus;
        private bool _reconciling;
        private PlayerActivityReconciliationRuntimeHostSnapshot _lastSnapshot;
        private FrameworkLogger _logger;

        internal PlayerActivityReconciliationRuntimeHostSnapshot LastSnapshot =>
            _lastSnapshot ??
            new PlayerActivityReconciliationRuntimeHostSnapshot(
                PlayerActivityReconciliationCoordinatorStatus.Ready,
                PlayerActivityReconciliationStableChangeKind.None,
                _observedSessionRevision,
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
                _lastSnapshot =
                    PlayerActivityReconciliationRuntimeHostSnapshot.Unavailable(
                        "Session Player participation snapshot is unavailable.");
                return false;
            }

            if (lifecycle == null)
            {
                _lastSnapshot =
                    PlayerActivityReconciliationRuntimeHostSnapshot.Unavailable(
                        "Activity Player Actor lifecycle participant is unavailable.");
                return false;
            }

            ActivityPlayerActorReconcileTarget target =
                lifecycle.CaptureActiveReconcileTarget();
            bool revisionChanged =
                session.Revision != _observedSessionRevision;
            bool targetChanged = HasTargetChanged(target);
            if (!revisionChanged && !targetChanged)
            {
                return false;
            }

            PlayerActivityReconciliationStableChangeKind changeKind =
                ResolveChangeKind(revisionChanged, targetChanged);

            if (_reconciling)
            {
                return false;
            }

            _reconciling = true;
            try
            {
                RecordObservedState(session.Revision, target);

                if (target.Status ==
                    ActivityPlayerActorReconcileTargetStatus.NoActiveActivity)
                {
                    _lastSnapshot =
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
                    _lastSnapshot =
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
                    _lastSnapshot =
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

                if (result != null && result.Failed)
                {
                    (_logger ??= FrameworkLogger.Create<
                        PlayerActivityReconciliationRuntimeHostModule>())
                        .Error("Activity Player reconciliation failed. " +
                            result.Message);
                }

                _lastSnapshot =
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
                _lastSnapshot =
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
                _reconciling = false;
            }
        }

        private bool HasTargetChanged(
            ActivityPlayerActorReconcileTarget target)
        {
            return
                target.Status != _observedTargetStatus ||
                !ReferenceEquals(target.Activity, _observedActivity) ||
                target.Owner != _observedOwner ||
                target.Occurrence != _observedOccurrence;
        }

        private void RecordObservedState(
            int sessionRevision,
            ActivityPlayerActorReconcileTarget target)
        {
            _observedSessionRevision = sessionRevision;
            _observedActivity = target.Activity;
            _observedOwner = target.Owner;
            _observedOccurrence = target.Occurrence;
            _observedTargetStatus = target.Status;
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
