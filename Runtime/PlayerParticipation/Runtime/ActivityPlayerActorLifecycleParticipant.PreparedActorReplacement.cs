using System;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        internal PlayerPreparedActorReplacementResult TryReplacePreparedActor(
            PlayerPreparedActorReplacementRequest request)
        {
            ActivityPlayerReadinessRecord readinessRecord =
                _playerReadinessRecord;
            ActivityPlayerActorReconcileTarget target = CaptureActiveReconcileTarget();
            PlayerParticipationSnapshot session = _participationContext.CreateSnapshot();
            if (!request.IsValid || session == null || !session.IsInitialized ||
                readinessRecord == null || readinessRecord.released)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedInvalidRequest, request, default, default, default, default, target.Occurrence, false, false, false, "Prepared Actor replacement requires a valid scoped request and Session.");
            if (!target.IsReady)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedNoActiveActivity, request, default, default, default, default, target.Occurrence, false, false, false, target.Message);
            if (request.HasExpectedSessionRevision && request.ExpectedSessionRevision != session.Revision)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedStalePublicRevision, request, default, default, default, default, target.Occurrence, false, false, false, "Expected Session revision is stale.");
            if (!_participationContext.TryGetActorSelection(request.PlayerSlotId, out PlayerSlotRuntimeSnapshot slot) || !slot.IsJoined)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedPreparedActorUnavailable, request, default, default, default, default, target.Occurrence, false, false, false, "Prepared Actor replacement requires one joined configured Slot.");
            if (request.HasExpectedSelectionRevision && request.ExpectedSelectionRevision != slot.SelectionRevision)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedStalePublicRevision, request, default, default, default, default, target.Occurrence, false, false, false, "Expected Actor selection revision is stale.");
            if (!_participationContext.TryGetHostProvisioningMode(request.PlayerSlotId, out PlayerHostProvisioningMode provisioning) || provisioning != PlayerHostProvisioningMode.ManagerProvisioned)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedUnsupportedProvisioning, request, default, default, default, default, target.Occurrence, false, false, false, "Prepared Actor replacement V1 supports Manager-Provisioned Slots only.");
            if (!_preparationModule.TryGetCurrentPreparation(request.PlayerSlotId, out PlayerActorPreparationSummary previousActor, out string preparationIssue) || !previousActor.IsPrepared)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedPreparedActorUnavailable, request, previousActor, previousActor, default, default, target.Occurrence, false, false, false, preparationIssue);
            PlayerReadinessSlotRecord record = FindReadinessSlot(request.PlayerSlotId);
            if (record == null)
                return Replacement(PlayerPreparedActorReplacementStatus.RejectedPreparedActorUnavailable, request, previousActor, previousActor, default, default, target.Occurrence, false, false, false, "The requested Slot is not projected by the current Activity occurrence.");

            PlayerGameplayAdmissionSummary previousGameplay = default;
            PlayerGameplayRuntimeHostModule gameplay = null;
            bool gameplayReleaseStarted = false;
            bool gameplayPreparationRequired =
                (int)readinessRecord.requirementLevel >=
                (int)PlayerParticipationRequirementLevel.GameplayReady;
            if (gameplayPreparationRequired &&
                !_preparationModule.TryGetPlayerGameplayRuntime(
                    out gameplay,
                    out string gameplayRuntimeIssue))
            {
                return Replacement(
                    PlayerPreparedActorReplacementStatus.FailedGameplayRelease,
                    request,
                    previousActor,
                    previousActor,
                    default,
                    default,
                    target.Occurrence,
                    false,
                    false,
                    false,
                    gameplayRuntimeIssue);
            }

            bool IsActivityOccurrenceCurrent()
            {
                ActivityPlayerActorReconcileTarget currentTarget =
                    CaptureActiveReconcileTarget();
                PlayerParticipationSnapshot currentSession =
                    _participationContext.CreateSnapshot();
                return currentTarget.IsReady &&
                    ReferenceEquals(_playerReadinessRecord, readinessRecord) &&
                    ReferenceEquals(currentTarget.Activity, target.Activity) &&
                    currentTarget.Owner == target.Owner &&
                    currentTarget.Occurrence == target.Occurrence &&
                    currentSession != null &&
                    currentSession.IsInitialized &&
                    string.Equals(
                        currentSession.ContextId,
                        session.ContextId,
                        StringComparison.Ordinal) &&
                    _participationContext.TryGetActorSelection(
                        request.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot currentSlot) &&
                    currentSlot.IsJoined;
            }

            bool IsHandoffTargetCurrent()
            {
                PlayerParticipationSnapshot currentSession =
                    _participationContext.CreateSnapshot();
                return IsActivityOccurrenceCurrent() &&
                    currentSession != null &&
                    (!request.HasExpectedSessionRevision ||
                     currentSession.Revision == request.ExpectedSessionRevision) &&
                    _participationContext.TryGetActorSelection(
                        request.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot currentSlot) &&
                    currentSlot.IsJoined &&
                    currentSlot.SelectionRevision == slot.SelectionRevision &&
                    ReferenceEquals(
                        currentSlot.SelectedActorProfile,
                        slot.SelectedActorProfile);
            }

            string handoffPreparationIssue = string.Empty;
            bool gameplayReleaseFailed = false;
            bool PrepareGameplayHandoff()
            {
                if (!IsHandoffTargetCurrent())
                {
                    handoffPreparationIssue =
                        "The exact joined Player, Session or Activity target closed before the gameplay handoff.";
                    return false;
                }

                if (!gameplayPreparationRequired)
                {
                    return true;
                }

                if (gameplay == null)
                {
                    gameplayReleaseFailed = true;
                    handoffPreparationIssue =
                        "Gameplay runtime became unavailable before the physical Actor handoff.";
                    return false;
                }

                if (gameplay.TryGetCurrentAdmission(request.PlayerSlotId, out previousGameplay) && previousGameplay.IsAdmitted)
                {
                    gameplayReleaseStarted = true;
                    PlayerGameplayRuntimeOperationResult release = gameplay.TryReleaseCurrentGameplay(request.PlayerSlotId, previousGameplay.Token, request.Source, request.Reason + "; release-previous-gameplay");
                    if (release == null || !release.Succeeded)
                    {
                        gameplayReleaseFailed = true;
                        handoffPreparationIssue = release != null
                            ? release.Message
                            : "Current gameplay release returned no result.";
                        return false;
                    }
                }

                gameplayReleaseStarted = true;
                if (!gameplay.TryReleaseCurrentOccupancyForPreparation(
                        request.PlayerSlotId,
                        previousActor.Token,
                        request.Source,
                        request.Reason + "; release-previous-gameplay-occupancy",
                        out string occupancyReleaseIssue))
                {
                    gameplayReleaseFailed = true;
                    handoffPreparationIssue = occupancyReleaseIssue;
                    return false;
                }

                return true;
            }

            PlayerActorPreparationResult replacement =
                _preparationModule.TryReplacePreparedActor(
                    readinessRecord.scopeContext,
                    new PlayerActorSelectionRequest(
                        request.PlayerSlotId,
                        request.ReplacementActorProfile,
                        request.Source,
                        request.Reason,
                        slot.SelectionRevision),
                    previousActor.Token,
                    request.IdentityPolicy,
                    request.Source,
                    request.Reason,
                    PrepareGameplayHandoff,
                    IsHandoffTargetCurrent);
            bool physicalCommitted = replacement != null &&
                (replacement.Succeeded || replacement.Status ==
                    PlayerActorPreparationStatus.FailedPreviousRelease);
            if (!physicalCommitted)
            {
                if (replacement != null && replacement.Status ==
                    PlayerActorPreparationStatus.FailedHandoffAfterPreviousDeactivation)
                {
                    record.gameplayAdmitted = false;
                    record.gameplayReady = false;
                    record.gameplayAdmissionToken = default;
                    if (ReferenceEquals(_playerReadinessRecord, readinessRecord))
                    {
                        readinessRecord.completed = false;
                        readinessRecord.failed = true;
                        readinessRecord.readinessReason =
                            ActivityPlayerActorReadinessReason.Failed;
                        readinessRecord.message = replacement.Message;
                        RebuildActiveRecordFromReadiness(
                            _participationContext.CreateSnapshot());
                        ApplyPlayerReadinessRecordTerminalState();
                    }
                    return Replacement(
                        PlayerPreparedActorReplacementStatus.FailedAfterPreviousDeactivation,
                        request,
                        previousActor,
                        replacement.CurrentSummary,
                        previousGameplay,
                        default,
                        target.Occurrence,
                        false,
                        false,
                        replacement.CleanupPending,
                        replacement.Message);
                }

                if (replacement != null && replacement.Status ==
                    PlayerActorPreparationStatus.RejectedTransitionInProgress)
                {
                    return Replacement(
                        PlayerPreparedActorReplacementStatus.RejectedTransitionInProgress,
                        request,
                        previousActor,
                        replacement.CurrentSummary,
                        previousGameplay,
                        default,
                        target.Occurrence,
                        false,
                        false,
                        replacement.CleanupPending,
                        replacement.Message);
                }

                PlayerPreparedActorReplacementStatus failureStatus =
                    replacement != null && replacement.Status ==
                        PlayerActorPreparationStatus.FailedRollback
                        ? PlayerPreparedActorReplacementStatus.FailedRollback
                        : gameplayReleaseFailed
                            ? PlayerPreparedActorReplacementStatus.FailedGameplayRelease
                            : PlayerPreparedActorReplacementStatus.FailedBeforeCommit;
                if (!gameplayReleaseStarted ||
                    failureStatus == PlayerPreparedActorReplacementStatus.FailedRollback)
                {
                    return Replacement(
                        failureStatus,
                        request,
                        previousActor,
                        replacement != null ? replacement.CurrentSummary : previousActor,
                        previousGameplay,
                        default,
                        target.Occurrence,
                        false,
                        false,
                        replacement != null && replacement.CleanupPending,
                        replacement != null
                            ? JoinReplacementMessages(
                                replacement.Message,
                                handoffPreparationIssue)
                            : "Prepared Actor replacement returned no result.");
                }

                return RestorePreviousGameplayAfterPreCommitFailure(
                    failureStatus,
                    request,
                    previousActor,
                    previousGameplay,
                    target,
                    gameplay,
                    IsActivityOccurrenceCurrent,
                    replacement != null && replacement.CleanupPending,
                    JoinReplacementMessages(
                        replacement != null
                            ? replacement.Message
                            : "Prepared Actor replacement returned no result.",
                        handoffPreparationIssue));
            }

            if (!IsActivityOccurrenceCurrent())
            {
                record.gameplayAdmitted = false;
                record.gameplayReady = false;
                record.gameplayAdmissionToken = default;
                return Replacement(
                    PlayerPreparedActorReplacementStatus.SucceededCommittedGameplayReprojectionFailed,
                    request,
                    previousActor,
                    replacement.CurrentSummary,
                    previousGameplay,
                    default,
                    target.Occurrence,
                    true,
                    false,
                    replacement.CleanupPending ||
                        (replacement.PreviousReleaseAttempted &&
                         !replacement.PreviousReleaseSucceeded),
                    "Actor B committed, but its original Session/Activity occurrence closed before gameplay reprojection.");
            }

            record.selectionRevision = replacement.CurrentSummary.SelectionRevision;
            record.slotRevision = replacement.SelectionResult != null ? replacement.SelectionResult.Slot.Revision : record.slotRevision;
            record.selected = true;
            record.prepared = replacement.CurrentSummary.IsPrepared;
            record.preparationToken = replacement.CurrentSummary.Token;
            record.gameplayAdmitted = false;
            record.gameplayReady = false;
            record.gameplayAdmissionToken = default;
            readinessRecord.completed = false;
            readinessRecord.failed = false;
            RebuildActiveRecordFromReadiness(_participationContext.CreateSnapshot());

            if ((int)readinessRecord.requirementLevel < (int)PlayerParticipationRequirementLevel.GameplayReady)
            {
                CompletePlayerReadinessContribution("Prepared Actor replacement committed for the current Activity occurrence.");
                return Replacement(replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded ? PlayerPreparedActorReplacementStatus.SucceededReplacedCleanupPending : PlayerPreparedActorReplacementStatus.SucceededReplacedAndGameplayReady, request, previousActor, replacement.CurrentSummary, previousGameplay, default, target.Occurrence, true, true, replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded, replacement.Message);
            }

            _preparationModule.TryGetPlayerGameplayRuntime(out PlayerGameplayRuntimeHostModule currentGameplay, out _);
            PlayerGameplayRuntimeOperationResult ensure = currentGameplay != null ? currentGameplay.TryEnsureCurrentGameplay(request.PlayerSlotId, target.Owner, request.Source, request.Reason + "; ensure-replacement-gameplay") : null;
            PlayerGameplayAdmissionSummary currentGameplaySummary = ensure != null ? ensure.CurrentAdmission : default;
            if (!IsActivityOccurrenceCurrent())
            {
                record.gameplayAdmitted = false;
                record.gameplayReady = false;
                record.gameplayAdmissionToken = default;
                return Replacement(
                    PlayerPreparedActorReplacementStatus.SucceededCommittedGameplayReprojectionFailed,
                    request,
                    previousActor,
                    replacement.CurrentSummary,
                    previousGameplay,
                    currentGameplaySummary,
                    target.Occurrence,
                    true,
                    false,
                    replacement.CleanupPending ||
                        (replacement.PreviousReleaseAttempted &&
                         !replacement.PreviousReleaseSucceeded),
                    "Actor B committed, but its original Session/Activity occurrence closed during gameplay reprojection.");
            }

            if (ensure == null || !ensure.Succeeded)
            {
                ContinuePlayerReadinessContribution();
                return Replacement(replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded ? PlayerPreparedActorReplacementStatus.SucceededReplacedCleanupPending : PlayerPreparedActorReplacementStatus.SucceededCommittedGameplayReprojectionFailed, request, previousActor, replacement.CurrentSummary, previousGameplay, currentGameplaySummary, target.Occurrence, true, ensure != null && ensure.Succeeded, replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded, ensure != null ? ensure.Message : "Replacement gameplay reprojection runtime is unavailable.");
            }

            record.gameplayAdmitted = currentGameplaySummary.IsAdmitted;
            record.gameplayReady = currentGameplaySummary.GameplayReady;
            record.gameplayAdmissionToken = currentGameplaySummary.Token;
            if (!currentGameplaySummary.GameplayReady)
            {
                ContinuePlayerReadinessContribution();
                return Replacement(replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded ? PlayerPreparedActorReplacementStatus.SucceededReplacedCleanupPending : PlayerPreparedActorReplacementStatus.SucceededReplacedGameplayBlocked, request, previousActor, replacement.CurrentSummary, previousGameplay, currentGameplaySummary, target.Occurrence, true, true, replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded, replacement.Message);
            }

            CompletePlayerReadinessContribution("Prepared Actor replacement gameplay was reprojected for the current Activity occurrence.");
            return Replacement(replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded ? PlayerPreparedActorReplacementStatus.SucceededReplacedCleanupPending : PlayerPreparedActorReplacementStatus.SucceededReplacedAndGameplayReady, request, previousActor, replacement.CurrentSummary, previousGameplay, currentGameplaySummary, target.Occurrence, true, true, replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded, replacement.Message);
        }

        private PlayerPreparedActorReplacementResult
            RestorePreviousGameplayAfterPreCommitFailure(
                PlayerPreparedActorReplacementStatus failureStatus,
                PlayerPreparedActorReplacementRequest request,
                PlayerActorPreparationSummary previousActor,
                PlayerGameplayAdmissionSummary previousGameplay,
                ActivityPlayerActorReconcileTarget target,
                PlayerGameplayRuntimeHostModule gameplay,
                Func<bool> isActivityOccurrenceCurrent,
                bool cleanupPending,
                string failureMessage)
        {
            PlayerActorPreparationSummary currentActor = previousActor;
            string preparationIssue = string.Empty;
            if (isActivityOccurrenceCurrent == null ||
                !isActivityOccurrenceCurrent())
            {
                return Replacement(
                    PlayerPreparedActorReplacementStatus.FailedRollback,
                    request,
                    previousActor,
                    currentActor,
                    previousGameplay,
                    default,
                    target.Occurrence,
                    false,
                    false,
                    cleanupPending,
                    failureMessage +
                    " The original Activity occurrence is no longer current, so its gameplay was not restored.");
            }

            if (gameplay == null ||
                !_preparationModule.TryGetCurrentPreparation(
                    request.PlayerSlotId,
                    out currentActor,
                    out preparationIssue) ||
                !currentActor.IsPrepared ||
                currentActor.Token != previousActor.Token)
            {
                return Replacement(
                    PlayerPreparedActorReplacementStatus.FailedRollback,
                    request,
                    previousActor,
                    currentActor,
                    previousGameplay,
                    default,
                    target.Occurrence,
                    false,
                    false,
                    cleanupPending,
                    failureMessage + " Previous gameplay restoration could not confirm Actor A as the current prepared Actor. " + preparationIssue);
            }

            PlayerGameplayRuntimeOperationResult restoration =
                gameplay.TryEnsureCurrentGameplay(
                    request.PlayerSlotId,
                    target.Owner,
                    request.Source,
                    request.Reason + "; restore-previous-gameplay-after-precommit-failure");
            PlayerGameplayAdmissionSummary currentGameplay = restoration != null
                ? restoration.CurrentAdmission
                : default;
            if (restoration == null || !restoration.Succeeded ||
                !currentGameplay.IsAdmitted || !currentGameplay.GameplayReady ||
                currentGameplay.PreparationToken != previousActor.Token)
            {
                return Replacement(
                    PlayerPreparedActorReplacementStatus.FailedRollback,
                    request,
                    previousActor,
                    currentActor,
                    previousGameplay,
                    currentGameplay,
                    target.Occurrence,
                    false,
                    false,
                    cleanupPending,
                    failureMessage + " Previous gameplay restoration failed. " +
                    (restoration != null
                        ? restoration.Message
                        : "Gameplay restoration returned no result."));
            }

            if (!isActivityOccurrenceCurrent())
            {
                return Replacement(
                    PlayerPreparedActorReplacementStatus.FailedRollback,
                    request,
                    previousActor,
                    currentActor,
                    previousGameplay,
                    currentGameplay,
                    target.Occurrence,
                    false,
                    false,
                    false,
                    failureMessage +
                    " The Activity occurrence closed during gameplay restoration; its owner now controls cleanup.");
            }

            PlayerReadinessSlotRecord readiness =
                FindReadinessSlot(request.PlayerSlotId);
            if (readiness != null)
            {
                readiness.gameplayAdmitted = currentGameplay.IsAdmitted;
                readiness.gameplayReady = currentGameplay.GameplayReady;
                readiness.gameplayAdmissionToken = currentGameplay.Token;
            }

            return Replacement(
                failureStatus,
                request,
                previousActor,
                currentActor,
                previousGameplay,
                currentGameplay,
                target.Occurrence,
                false,
                false,
                cleanupPending,
                failureMessage +
                " Previous gameplay authority was restored for the same prepared Actor and Activity occurrence.");
        }

        private static string JoinReplacementMessages(
            string primary,
            string detail)
        {
            string resolvedPrimary = string.IsNullOrWhiteSpace(primary)
                ? string.Empty
                : primary.Trim();
            string resolvedDetail = string.IsNullOrWhiteSpace(detail)
                ? string.Empty
                : detail.Trim();
            if (resolvedPrimary.Length == 0)
            {
                return resolvedDetail;
            }

            return resolvedDetail.Length == 0
                ? resolvedPrimary
                : resolvedPrimary + " " + resolvedDetail;
        }

        private static PlayerPreparedActorReplacementResult Replacement(PlayerPreparedActorReplacementStatus status, PlayerPreparedActorReplacementRequest request, PlayerActorPreparationSummary previousActor, PlayerActorPreparationSummary currentActor, PlayerGameplayAdmissionSummary previousGameplay, PlayerGameplayAdmissionSummary currentGameplay, int occurrence, bool committed, bool gameplayReprojected, bool cleanupPending, string message) => new PlayerPreparedActorReplacementResult(status, request.PlayerSlotId, previousActor, currentActor, previousGameplay, currentGameplay, occurrence, committed, gameplayReprojected, cleanupPending, message);
    }
}
