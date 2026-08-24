using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        private sealed class SessionPlayerActivityRepresentationReleaseProgress
        {
            internal SessionPlayerLeaveToken leaveToken;
            internal string activityName;
            internal RuntimeContentOwner activityOwner;
            internal PlayerActorPreparationToken preparationToken;
            internal bool hadActivityRepresentation;
            internal bool hadPreparedActor;
            internal bool hadGameplayChain;
            internal bool gameplayAdmissionReleased;
            internal bool cameraReleased;
            internal bool inputReleased;
            internal bool occupancyReleased;
            internal bool preparedActorReleased;
            internal bool actorRetainedCleanupPending;
            internal PlayerActorPreparationResult lastActorRelease;
            internal bool activityLedgerRetired;
            internal bool readinessContributionRetired;
            internal bool completed;
        }

        private readonly Dictionary<SessionPlayerLeaveToken,
            SessionPlayerActivityRepresentationReleaseProgress>
            _sessionPlayerActivityRepresentationReleaseProgress = new();

        /// <summary>
        /// Retires the exact current Activity representation for a staged Session Player Leave.
        /// The Player is first removed from the Activity/readiness ledger so a Leaving occurrence
        /// stops contributing immediately. Resource release then proceeds monotonically and is
        /// retried by the same Leave token without compensation or representation recreation.
        /// Session Actor selection, provisioning resources and terminal Slot vacancy remain later
        /// ADR-020 stages.
        /// </summary>
        internal SessionPlayerActivityRepresentationReleaseResult
            TryReleaseActivityRepresentationForSessionPlayerLeave(
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            string resolvedSource = string.IsNullOrWhiteSpace(source)
                ? nameof(ActivityPlayerActorLifecycleParticipant)
                : source.Trim();
            string resolvedReason = string.IsNullOrWhiteSpace(reason)
                ? "session-player-leave-release-activity-representation"
                : reason.Trim();

            if (!leaveToken.IsValid)
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.RejectedInvalidRequest,
                    leaveToken,
                    null,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Activity representation release requires a valid Session Player Leave token.");
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                _participationContext.TryConfirmSessionPlayerLeave(
                    leaveToken,
                    resolvedSource,
                    resolvedReason);
            if (leaveConfirmation == null || !leaveConfirmation.Succeeded)
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.RejectedLeaveCorrelation,
                    leaveToken,
                    leaveConfirmation,
                    null,
                    resolvedSource,
                    resolvedReason,
                    leaveConfirmation != null
                        ? leaveConfirmation.ToDiagnosticString()
                        : "Session Player Leave confirmation returned no result.");
            }

            if (_sessionPlayerActivityRepresentationReleaseProgress.TryGetValue(
                    leaveToken,
                    out SessionPlayerActivityRepresentationReleaseProgress progress) &&
                progress.completed)
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.SucceededAlreadyReleased,
                    leaveToken,
                    leaveConfirmation,
                    progress,
                    resolvedSource,
                    resolvedReason,
                    "The exact Leave occurrence already retired its Activity representation.");
            }

            if (!_preparationModule.TryGetPlayerGameplayRuntime(
                    out PlayerGameplayRuntimeHostModule gameplayRuntime,
                    out string gameplayRuntimeIssue))
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.RejectedRuntimeUnavailable,
                    leaveToken,
                    leaveConfirmation,
                    progress,
                    resolvedSource,
                    resolvedReason,
                    gameplayRuntimeIssue);
            }

            if (progress == null)
            {
                SessionPlayerActivityRepresentationReleaseResult captureFailure =
                    TryCaptureSessionPlayerActivityRepresentationRelease(
                        leaveToken,
                        leaveConfirmation,
                        gameplayRuntime,
                        resolvedSource,
                        resolvedReason,
                        out progress);
                if (captureFailure != null)
                {
                    return captureFailure;
                }

                _sessionPlayerActivityRepresentationReleaseProgress.Add(
                    leaveToken,
                    progress);
            }

            if (!progress.activityLedgerRetired)
            {
                RetirePlayerSlotFromActivityLifecycle(
                    leaveToken.PlayerSlotId,
                    out bool activityLedgerRetired,
                    out bool readinessContributionRetired);
                progress.activityLedgerRetired = activityLedgerRetired;
                progress.readinessContributionRetired |= readinessContributionRetired;
                if (!progress.activityLedgerRetired)
                {
                    return Result(
                        SessionPlayerActivityRepresentationReleaseStatus.FailedInvariant,
                        leaveToken,
                        leaveConfirmation,
                        progress,
                        resolvedSource,
                        resolvedReason,
                        "The Leaving Session Player could not be retired from the current Activity lifecycle ledger before contextual resource release.");
                }
            }

            PlayerGameplayRuntimeHostModule.SessionPlayerLeaveGameplayReleaseResult
                gameplay = gameplayRuntime.TryReleaseActivityGameplayForSessionPlayerLeave(
                    leaveToken,
                    progress.preparationToken,
                    resolvedSource,
                    resolvedReason);
            if (gameplay != null)
            {
                progress.hadGameplayChain |= gameplay.HadGameplayChain;
                progress.gameplayAdmissionReleased = gameplay.AdmissionReleased;
                progress.cameraReleased = gameplay.CameraReleased;
                progress.inputReleased = gameplay.InputReleased;
                progress.occupancyReleased = gameplay.OccupancyReleased;
            }

            if (gameplay == null || !gameplay.Succeeded)
            {
                SessionPlayerActivityRepresentationReleaseStatus status =
                    gameplay != null &&
                    gameplay.Status == PlayerGameplayRuntimeHostModule
                        .SessionPlayerLeaveGameplayReleaseStatus
                        .RejectedPreparationCorrelation
                        ? SessionPlayerActivityRepresentationReleaseStatus
                            .RejectedRepresentationCorrelation
                        : gameplay != null &&
                          gameplay.Status == PlayerGameplayRuntimeHostModule
                            .SessionPlayerLeaveGameplayReleaseStatus.FailedInvariant
                            ? SessionPlayerActivityRepresentationReleaseStatus
                                .FailedInvariant
                            : SessionPlayerActivityRepresentationReleaseStatus
                                .FailedGameplayRelease;
                return Result(
                    status,
                    leaveToken,
                    leaveConfirmation,
                    progress,
                    resolvedSource,
                    resolvedReason,
                    gameplay != null
                        ? gameplay.Message
                        : "Activity gameplay release returned no result.");
            }

            if (!progress.hadActivityRepresentation)
            {
                progress.completed = true;
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.SucceededNoCurrentRepresentation,
                    leaveToken,
                    leaveConfirmation,
                    progress,
                    resolvedSource,
                    resolvedReason,
                    "The exact Leaving Session Player has no current Activity representation; Stage C performed only terminal cleanup of retained Session gameplay occupancy when present and did not create contextual state.");
            }

            if (!_preparationModule.TryReleaseManagerContextualProjection(
                    progress.activityOwner,
                    leaveToken.PlayerSlotId,
                    resolvedSource,
                    resolvedReason + "; release-manager-contextual-projection",
                    out string contextualReleaseIssue))
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.FailedInvariant,
                    leaveToken,
                    leaveConfirmation,
                    progress,
                    resolvedSource,
                    resolvedReason,
                    contextualReleaseIssue);
            }

            // Stage C deliberately ends at contextual retirement. The prepared Actor and its
            // RuntimeContent handle remain Session physical resources until stage D in the
            // Session Leave coordinator; Activity release must never destroy them.

            progress.completed = true;
            return Result(
                SessionPlayerActivityRepresentationReleaseStatus.SucceededReleased,
                leaveToken,
                leaveConfirmation,
                progress,
                resolvedSource,
                resolvedReason,
                "Current Activity representation retired for the exact Leaving Session Player. Physical Actor release, provisioning resources and Slot vacancy remain downstream stages.");
        }

        private SessionPlayerActivityRepresentationReleaseResult
            TryCaptureSessionPlayerActivityRepresentationRelease(
                SessionPlayerLeaveToken leaveToken,
                SessionPlayerLeaveRuntimeResult leaveConfirmation,
                PlayerGameplayRuntimeHostModule gameplayRuntime,
                string source,
                string reason,
                out SessionPlayerActivityRepresentationReleaseProgress progress)
        {
            progress = null;
            PlayerSlotId playerSlotId = leaveToken.PlayerSlotId;
            PlayerReadinessSlotRecord readinessSlot = FindReadinessSlot(playerSlotId);
            PlayerActorPreparationToken preparationToken = FindPreparedToken(playerSlotId);
            bool hasPreparedActor = preparationToken.IsValid;
            bool snapshotContainsSlot = _activeRecord != null &&
                LastSnapshotContainsSlot(playerSlotId);
            bool activeHostRecorded = ActiveRecordContainsHostForSlot(playerSlotId);
            bool hasActivityRepresentation = _activeRecord != null &&
                (readinessSlot != null ||
                 hasPreparedActor ||
                 activeHostRecorded ||
                 snapshotContainsSlot);

            if (_activeRecord == null && readinessSlot != null)
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.FailedInvariant,
                    leaveToken,
                    leaveConfirmation,
                    null,
                    source,
                    reason,
                    "Activity readiness retains the Leaving Player without a current active Activity lifecycle record.");
            }

            if (hasPreparedActor)
            {
                if (!_preparationModule.TryGetRetainedActorEvidence(
                        playerSlotId,
                        out PlayerActorCorrelationEvidence actorEvidence) ||
                    !actorEvidence.IsValid ||
                    actorEvidence.PreparationToken != preparationToken ||
                    actorEvidence.Owner.Scope != RuntimeContentScope.Session)
                {
                    return Result(
                        SessionPlayerActivityRepresentationReleaseStatus.RejectedRepresentationCorrelation,
                        leaveToken,
                        leaveConfirmation,
                        null,
                        source,
                        reason,
                        "Activity lifecycle preparation token does not match the retained current Actor representation evidence.");
                }

                if (!_participationContext.TryGetCurrentAssignment(
                        playerSlotId,
                        out PlayerSlotAssignmentSnapshot assignment) ||
                    !assignment.IsAssigned)
                {
                    return Result(
                        SessionPlayerActivityRepresentationReleaseStatus.RejectedRepresentationCorrelation,
                        leaveToken,
                        leaveConfirmation,
                        null,
                        source,
                        reason,
                        "Current Slot assignment is unavailable for the active contextual representation.");
                }

                if (!_preparationModule.TryGetRetainedHostEvidence(
                        playerSlotId,
                        out PlayerHostEvidenceSnapshot hostEvidence) ||
                    !hostEvidence.IsRecorded ||
                    hostEvidence.AssignmentToken != assignment.AssignmentToken ||
                    hostEvidence.HostBindingIdentity != assignment.HostBindingIdentity)
                {
                    return Result(
                        SessionPlayerActivityRepresentationReleaseStatus.RejectedRepresentationCorrelation,
                        leaveToken,
                        leaveConfirmation,
                        null,
                        source,
                        reason,
                        "Prepared Activity Actor representation does not resolve to the exact retained Host evidence for the same assignment occurrence.");
                }
            }
            else if (_activeRecord != null &&
                     snapshotContainsSlot &&
                     (int)_activeRecord.RequirementLevel >=
                         (int)PlayerParticipationRequirementLevel.LogicalActorsPrepared &&
                     _preparationModule.TryGetRetainedActorEvidence(
                         playerSlotId,
                         out PlayerActorCorrelationEvidence divergentActor) &&
                     divergentActor.IsValid)
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.FailedInvariant,
                    leaveToken,
                    leaveConfirmation,
                    null,
                    source,
                    reason,
                    "Current Activity requires a prepared Actor representation for the Leaving Slot, but retained Session physical Actor evidence exists without its matching Activity lifecycle preparation token.");
            }

            if (!gameplayRuntime.TryInspectActivityGameplayForSessionPlayerLeave(
                    leaveToken,
                    preparationToken,
                    source,
                    reason,
                    out bool hadGameplayChain,
                    out PlayerGameplayRuntimeHostModule.SessionPlayerLeaveGameplayReleaseStatus
                        gameplayInspectionFailure,
                    out string gameplayInspectionIssue))
            {
                SessionPlayerActivityRepresentationReleaseStatus status =
                    gameplayInspectionFailure == PlayerGameplayRuntimeHostModule
                        .SessionPlayerLeaveGameplayReleaseStatus
                        .RejectedPreparationCorrelation
                        ? SessionPlayerActivityRepresentationReleaseStatus
                            .RejectedRepresentationCorrelation
                        : gameplayInspectionFailure == PlayerGameplayRuntimeHostModule
                            .SessionPlayerLeaveGameplayReleaseStatus.FailedInvariant
                            ? SessionPlayerActivityRepresentationReleaseStatus
                                .FailedInvariant
                            : SessionPlayerActivityRepresentationReleaseStatus
                                .FailedGameplayRelease;
                return Result(
                    status,
                    leaveToken,
                    leaveConfirmation,
                    null,
                    source,
                    reason,
                    gameplayInspectionIssue);
            }

            if (!hasActivityRepresentation && hadGameplayChain)
            {
                return Result(
                    SessionPlayerActivityRepresentationReleaseStatus.FailedInvariant,
                    leaveToken,
                    leaveConfirmation,
                    null,
                    source,
                    reason,
                    "Activity gameplay capability evidence exists without a matching current Activity lifecycle representation ledger entry.");
            }

            progress = new SessionPlayerActivityRepresentationReleaseProgress
            {
                leaveToken = leaveToken,
                activityName = _activeRecord != null && _activeRecord.Activity != null
                    ? _activeRecord.Activity.ActivityName
                    : _lastSnapshot != null
                        ? _lastSnapshot.ActivityName
                        : string.Empty,
                activityOwner = _activeRecord != null
                    ? _activeRecord.Owner
                    : _lastSnapshot != null
                        ? _lastSnapshot.Owner
                        : default,
                preparationToken = preparationToken,
                hadActivityRepresentation = hasActivityRepresentation,
                hadPreparedActor = hasPreparedActor,
                hadGameplayChain = hadGameplayChain,
                gameplayAdmissionReleased = !hadGameplayChain,
                cameraReleased = !hadGameplayChain,
                inputReleased = !hadGameplayChain,
                occupancyReleased = !hadGameplayChain,
                preparedActorReleased = !hasPreparedActor,
                actorRetainedCleanupPending = false,
                lastActorRelease = null,
                activityLedgerRetired = !hasActivityRepresentation,
                readinessContributionRetired = false,
                completed = false
            };
            return null;
        }

        private void RetirePlayerSlotFromActivityLifecycle(
            PlayerSlotId playerSlotId,
            out bool activityLedgerRetired,
            out bool readinessContributionRetired)
        {
            readinessContributionRetired = false;
            if (_playerReadinessRecord != null)
            {
                for (int index = _playerReadinessRecord.projectedSlots.Count - 1;
                     index >= 0;
                     index--)
                {
                    PlayerReadinessSlotRecord slot =
                        _playerReadinessRecord.projectedSlots[index];
                    if (slot.playerSlotId == playerSlotId)
                    {
                        // A projeção configurada permanece na Activity atual, mas nenhuma
                        // evidência da ocorrência que saiu pode continuar autoritativa.
                        slot.joined = false;
                        slot.selected = false;
                        slot.prepared = false;
                        slot.gameplayAdmitted = false;
                        slot.gameplayReady = false;
                        slot.selectionCreatedByLifecycle = false;
                        slot.preparationCreatedByLifecycle = false;
                        slot.gameplayCreatedByLifecycle = false;
                        slot.preparationToken = default;
                        slot.gameplayAdmissionToken = default;
                        slot.readinessReason =
                            ActivityPlayerActorReadinessReason.WaitingForJoin;
                        slot.message =
                            "Projected Player Slot is waiting for Join after the prior Session Player left.";
                        readinessContributionRetired = true;
                    }
                }

                PlayerParticipationSnapshot session =
                    _participationContext.CreateSnapshot();
                if (session != null && session.IsInitialized)
                {
                    _playerReadinessRecord.appliedSessionRevision = session.Revision;
                }

                if (!_playerReadinessRecord.failed)
                {
                    _playerReadinessRecord.readinessReason =
                        ResolveAggregateReadinessReason(
                            _playerReadinessRecord.projectedSlots);
                    if (CountPendingSlots() == 0 && CountFailedSlots() == 0)
                    {
                        CompletePlayerReadinessContribution(
                            "Activity Player readiness remains satisfied after the Leaving Session Player contribution was retired.");
                    }
                    else
                    {
                        _playerReadinessRecord.completed = false;
                        _playerReadinessRecord.message =
                            "Leaving Session Player contribution retired; remaining projected Players continue under the existing Activity readiness occurrence.";
                    }
                }

                RebuildActiveRecordFromReadiness(session);
                UpdateLifecycleSnapshot(
                    _playerReadinessRecord.completed
                        ? ActivityPlayerActorLifecycleStatus.SucceededEntered
                        : ActivityPlayerActorLifecycleStatus.SucceededEnteredPreparing,
                    session,
                    null,
                    "Leaving Session Player retired from the current Activity lifecycle projection.");
            }
            else if (_activeRecord != null)
            {
                var prepared = new List<PreparedSlotRecord>();
                for (int index = 0; index < _activeRecord.PreparedSlots.Count; index++)
                {
                    PreparedSlotRecord item = _activeRecord.PreparedSlots[index];
                    if (item.PlayerSlotId != playerSlotId)
                    {
                        prepared.Add(item);
                    }
                }

                var hosts = new List<LocalPlayerHostAuthoring>();
                for (int index = 0; index < _activeRecord.AdmittedHosts.Count; index++)
                {
                    LocalPlayerHostAuthoring host = _activeRecord.AdmittedHosts[index];
                    if (host == null ||
                        (host.HasJoinedSlot &&
                         host.JoinedPlayerSlotId == playerSlotId))
                    {
                        continue;
                    }

                    hosts.Add(host);
                }

                bool hadSlot = LastSnapshotContainsSlot(playerSlotId) ||
                    _activeRecord.PreparedSlots.Count != prepared.Count ||
                    _activeRecord.AdmittedHosts.Count != hosts.Count;
                int projectedCount = hadSlot
                    ? Math.Max(0, _activeRecord.ProjectedSlotCount - 1)
                    : _activeRecord.ProjectedSlotCount;
                int selectedCount = _activeRecord.SelectedCount;
                if (hadSlot &&
                    _participationContext.TryGetSlotSnapshot(
                        playerSlotId,
                        out PlayerSlotRuntimeSnapshot slot) &&
                    slot.HasSelectedActor)
                {
                    selectedCount = Math.Max(0, selectedCount - 1);
                }

                _activeRecord = new ActiveActivityRecord(
                    _activeRecord.Activity,
                    _activeRecord.Owner,
                    _activeRecord.RequirementLevel,
                    projectedCount,
                    selectedCount,
                    prepared,
                    hosts);
                _lastSnapshot = FilterLifecycleSnapshotForLeave(
                    _lastSnapshot,
                    playerSlotId,
                    projectedCount,
                    selectedCount,
                    prepared.Count);
            }

            activityLedgerRetired = !ActivityLedgerContainsSlot(playerSlotId);
        }

        private bool ActivityLedgerContainsSlot(PlayerSlotId playerSlotId)
        {
            PlayerReadinessSlotRecord readinessSlot =
                FindReadinessSlot(playerSlotId);
            if ((readinessSlot != null &&
                 (readinessSlot.joined ||
                  readinessSlot.prepared ||
                  readinessSlot.gameplayAdmitted ||
                  readinessSlot.gameplayReady ||
                  readinessSlot.preparationToken.IsValid ||
                  readinessSlot.gameplayAdmissionToken.IsValid)) ||
                FindPreparedToken(playerSlotId).IsValid ||
                ActiveRecordContainsHostForSlot(playerSlotId))
            {
                return true;
            }

            return false;
        }

        private bool ActiveRecordContainsHostForSlot(PlayerSlotId playerSlotId)
        {
            if (_activeRecord == null)
            {
                return false;
            }

            for (int index = 0; index < _activeRecord.AdmittedHosts.Count; index++)
            {
                LocalPlayerHostAuthoring host = _activeRecord.AdmittedHosts[index];
                if (host != null &&
                    host.HasJoinedSlot &&
                    host.JoinedPlayerSlotId == playerSlotId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool LastSnapshotContainsSlot(PlayerSlotId playerSlotId)
        {
            if (_lastSnapshot == null)
            {
                return false;
            }

            for (int index = 0; index < _lastSnapshot.Slots.Count; index++)
            {
                if (_lastSnapshot.Slots[index].PlayerSlotId == playerSlotId)
                {
                    return true;
                }
            }

            return false;
        }

        private static ActivityPlayerActorLifecycleSnapshot
            FilterLifecycleSnapshotForLeave(
                ActivityPlayerActorLifecycleSnapshot snapshot,
                PlayerSlotId playerSlotId,
                int projectedCount,
                int selectedCount,
                int preparedCount)
        {
            if (snapshot == null)
            {
                return ActivityPlayerActorLifecycleSnapshot.Empty(
                    "Leaving Session Player retired from Activity lifecycle.");
            }

            var slots = new List<ActivityPlayerActorSlotLifecycleSnapshot>();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                ActivityPlayerActorSlotLifecycleSnapshot slot = snapshot.Slots[index];
                if (slot.PlayerSlotId != playerSlotId)
                {
                    slots.Add(slot);
                }
            }

            return new ActivityPlayerActorLifecycleSnapshot(
                snapshot.Status,
                snapshot.ActivityName,
                snapshot.Owner,
                snapshot.RequirementLevel,
                projectedCount,
                selectedCount,
                preparedCount,
                snapshot.ReleasedCount,
                snapshot.FailedCount,
                slots.ToArray(),
                "Leaving Session Player retired from the current Activity lifecycle projection.");
        }

        private SessionPlayerActivityRepresentationReleaseResult Result(
            SessionPlayerActivityRepresentationReleaseStatus status,
            SessionPlayerLeaveToken leaveToken,
            SessionPlayerLeaveRuntimeResult leaveConfirmation,
            SessionPlayerActivityRepresentationReleaseProgress progress,
            string source,
            string reason,
            string message)
        {
            return new SessionPlayerActivityRepresentationReleaseResult(
                status,
                leaveToken,
                leaveConfirmation,
                progress != null ? progress.activityName : string.Empty,
                progress != null ? progress.activityOwner : default,
                progress != null ? progress.preparationToken : default,
                progress != null ? progress.lastActorRelease : null,
                progress != null && progress.hadActivityRepresentation,
                progress != null && progress.hadPreparedActor,
                progress != null && progress.gameplayAdmissionReleased,
                progress != null && progress.cameraReleased,
                progress != null && progress.inputReleased,
                progress != null && progress.occupancyReleased,
                progress != null && progress.preparedActorReleased,
                progress != null && progress.actorRetainedCleanupPending,
                progress != null && progress.activityLedgerRetired,
                progress != null && progress.readinessContributionRetired,
                source,
                reason,
                message);
        }
    }
}
