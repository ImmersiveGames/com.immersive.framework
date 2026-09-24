using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        internal PlayerPreparedActorReplacementResult TryReplacePreparedActor(
            PlayerPreparedActorReplacementRequest request)
        {
            ActivityPlayerActorReconcileTarget target = CaptureActiveReconcileTarget();
            PlayerParticipationSnapshot session = _participationContext.CreateSnapshot();
            if (!request.IsValid || session == null || !session.IsInitialized)
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
            if ((int)_playerReadinessRecord.requirementLevel >= (int)PlayerParticipationRequirementLevel.GameplayReady)
            {
                if (!_preparationModule.TryGetPlayerGameplayRuntime(out gameplay, out string gameplayIssue))
                    return Replacement(PlayerPreparedActorReplacementStatus.FailedGameplayRelease, request, previousActor, previousActor, default, default, target.Occurrence, false, false, false, gameplayIssue);
                if (gameplay.TryGetCurrentAdmission(request.PlayerSlotId, out previousGameplay) && previousGameplay.IsAdmitted)
                {
                    gameplayReleaseStarted = true;
                    PlayerGameplayRuntimeOperationResult release = gameplay.TryReleaseCurrentGameplay(request.PlayerSlotId, previousGameplay.Token, request.Source, request.Reason + "; release-previous-gameplay");
                    if (release == null || !release.Succeeded)
                    {
                        return RestorePreviousGameplayAfterPreCommitFailure(
                            PlayerPreparedActorReplacementStatus.FailedGameplayRelease,
                            request,
                            previousActor,
                            previousGameplay,
                            target,
                            gameplay,
                            release != null
                                ? release.Message
                                : "Current gameplay release returned no result.");
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
                    return RestorePreviousGameplayAfterPreCommitFailure(
                        PlayerPreparedActorReplacementStatus.FailedGameplayRelease,
                        request,
                        previousActor,
                        previousGameplay,
                        target,
                        gameplay,
                        occupancyReleaseIssue);
                }
            }

            PlayerActorPreparationResult replacement =
                _preparationModule.TryReplacePreparedActor(
                    _playerReadinessRecord.scopeContext,
                    new PlayerActorSelectionRequest(
                        request.PlayerSlotId,
                        request.ReplacementActorProfile,
                        request.Source,
                        request.Reason,
                        slot.SelectionRevision),
                    previousActor.Token,
                    request.Source,
                    request.Reason);
            bool physicalCommitted = replacement != null &&
                (replacement.Succeeded || replacement.Status ==
                    PlayerActorPreparationStatus.FailedPreviousRelease);
            if (!physicalCommitted)
            {
                PlayerPreparedActorReplacementStatus failureStatus = replacement != null &&
                    replacement.Status == PlayerActorPreparationStatus.FailedRollback
                    ? PlayerPreparedActorReplacementStatus.FailedRollback
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
                        false,
                        replacement != null
                            ? replacement.Message
                            : "Prepared Actor replacement returned no result.");
                }

                return RestorePreviousGameplayAfterPreCommitFailure(
                    failureStatus,
                    request,
                    previousActor,
                    previousGameplay,
                    target,
                    gameplay,
                    replacement != null
                        ? replacement.Message
                        : "Prepared Actor replacement returned no result.");
            }

            record.selectionRevision = replacement.CurrentSummary.SelectionRevision;
            record.slotRevision = replacement.SelectionResult != null ? replacement.SelectionResult.Slot.Revision : record.slotRevision;
            record.selected = true;
            record.prepared = replacement.CurrentSummary.IsPrepared;
            record.preparationToken = replacement.CurrentSummary.Token;
            record.gameplayAdmitted = false;
            record.gameplayReady = false;
            record.gameplayAdmissionToken = default;
            _playerReadinessRecord.completed = false;
            _playerReadinessRecord.failed = false;
            RebuildActiveRecordFromReadiness(_participationContext.CreateSnapshot());

            if ((int)_playerReadinessRecord.requirementLevel < (int)PlayerParticipationRequirementLevel.GameplayReady)
            {
                CompletePlayerReadinessContribution("Prepared Actor replacement committed for the current Activity occurrence.");
                return Replacement(replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded ? PlayerPreparedActorReplacementStatus.SucceededReplacedCleanupPending : PlayerPreparedActorReplacementStatus.SucceededReplacedAndGameplayReady, request, previousActor, replacement.CurrentSummary, previousGameplay, default, target.Occurrence, true, true, replacement.PreviousReleaseAttempted && !replacement.PreviousReleaseSucceeded, replacement.Message);
            }

            _preparationModule.TryGetPlayerGameplayRuntime(out PlayerGameplayRuntimeHostModule currentGameplay, out _);
            PlayerGameplayRuntimeOperationResult ensure = currentGameplay != null ? currentGameplay.TryEnsureCurrentGameplay(request.PlayerSlotId, target.Owner, request.Source, request.Reason + "; ensure-replacement-gameplay") : null;
            PlayerGameplayAdmissionSummary currentGameplaySummary = ensure != null ? ensure.CurrentAdmission : default;
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
                string failureMessage)
        {
            PlayerActorPreparationSummary currentActor = previousActor;
            string preparationIssue = string.Empty;
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
                    false,
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
                    false,
                    failureMessage + " Previous gameplay restoration failed. " +
                    (restoration != null
                        ? restoration.Message
                        : "Gameplay restoration returned no result."));
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
                false,
                failureMessage +
                " Previous gameplay authority was restored for the same prepared Actor and Activity occurrence.");
        }

        private static PlayerPreparedActorReplacementResult Replacement(PlayerPreparedActorReplacementStatus status, PlayerPreparedActorReplacementRequest request, PlayerActorPreparationSummary previousActor, PlayerActorPreparationSummary currentActor, PlayerGameplayAdmissionSummary previousGameplay, PlayerGameplayAdmissionSummary currentGameplay, int occurrence, bool committed, bool gameplayReprojected, bool cleanupPending, string message) => new PlayerPreparedActorReplacementResult(status, request.PlayerSlotId, previousActor, currentActor, previousGameplay, currentGameplay, occurrence, committed, gameplayReprojected, cleanupPending, message);
    }
}
