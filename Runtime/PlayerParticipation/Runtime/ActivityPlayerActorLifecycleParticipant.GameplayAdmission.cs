using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        private readonly struct CurrentGameplayRecord
        {
            internal CurrentGameplayRecord(
                PlayerSlotId playerSlotId,
                PlayerGameplayAdmissionToken token)
            {
                PlayerSlotId = playerSlotId;
                Token = token;
            }

            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerGameplayAdmissionToken Token { get; }
        }

        private ActivityContentExecutionResult ExecuteGameplayReadyEnter(
            ActivityContentExecutionRequest request,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            List<PlayerSlotRuntimeSnapshot> projectedSlots)
        {
            PlayerGameplayRuntimeHostModule gameplayRuntime =
                preparationModule.GetComponent<PlayerGameplayRuntimeHostModule>();
            if (gameplayRuntime == null || !gameplayRuntime.IsReady)
            {
                string issue = gameplayRuntime != null
                    ? gameplayRuntime.Diagnostic
                    : "FrameworkRuntimeHost has no Player gameplay runtime module.";
                return FailGameplayReadyEnter(
                    request, activity, owner, projectedSlots, issue);
            }

            var prepared = new List<PreparedSlotRecord>(projectedSlots.Count);
            var selections = new List<AppliedSelectionRecord>();
            var gameplay = new List<CurrentGameplayRecord>();
            var hosts = new List<LocalPlayerHostAuthoring>(projectedSlots.Count);
            var evidence = new ActivityPlayerActorSlotLifecycleSnapshot[
                projectedSlots.Count];

            for (int index = 0; index < projectedSlots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = projectedSlots[index];
                string hostIssue = string.Empty;
                if (!slot.IsJoined || !preparationModule.TryGetRegisteredHost(
                        slot.PlayerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out hostIssue))
                {
                    return RollbackGameplayReadyEnter(
                        request, activity, owner, projectedSlots, gameplayRuntime,
                        gameplay, prepared, selections,
                        !slot.IsJoined
                            ? $"Projected Player Slot '{slot.PlayerSlotId.StableText}' is not Joined during GameplayReady entry."
                            : hostIssue);
                }

                hosts.Add(host);
                bool selectionApplied = false;
                if (!slot.HasSelectedActor)
                {
                    PlayerActorSelectionResult selection =
                        preparationModule.TrySelectDefaultActor(
                            slot.PlayerSlotId,
                            slot.SelectionRevision,
                            nameof(ActivityPlayerActorLifecycleParticipant),
                            "activity-enter-gameplay-ready-select-default-actor");
                    if (selection == null || !selection.Succeeded)
                    {
                        return RollbackGameplayReadyEnter(
                            request, activity, owner, projectedSlots, gameplayRuntime,
                            gameplay, prepared, selections,
                            selection != null
                                ? selection.ToDiagnosticString()
                                : $"Default Actor selection returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                    }

                    slot = selection.Slot;
                    selectionApplied = selection.StateChanged;
                    if (selectionApplied)
                    {
                        selections.Add(new AppliedSelectionRecord(
                            slot.PlayerSlotId, selection.SelectionRevision));
                    }
                }

                if (!slot.HasSelectedActor)
                {
                    return RollbackGameplayReadyEnter(
                        request, activity, owner, projectedSlots, gameplayRuntime,
                        gameplay, prepared, selections,
                        $"Projected Player Slot '{slot.PlayerSlotId.StableText}' has no selected Actor.");
                }

                PlayerActorPreparationResult preparation =
                    preparationModule.TryEnsureSessionPhysicalActor(
                        request.RuntimeScopeContext,
                        slot.PlayerSlotId,
                        nameof(ActivityPlayerActorLifecycleParticipant),
                        "activity-enter-gameplay-ready-ensure-session-physical-actor");
                if (preparation == null || !preparation.Succeeded ||
                    !preparation.CurrentSummary.IsPrepared ||
                    !preparation.CurrentSummary.Token.IsValid)
                {
                    return RollbackGameplayReadyEnter(
                        request, activity, owner, projectedSlots, gameplayRuntime,
                        gameplay, prepared, selections,
                        preparation != null
                            ? preparation.ToDiagnosticString()
                            : $"Session physical preparation returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                }

                if (!preparationModule.TryApplyCurrentActivityRelocation(
                        owner,
                        slot.PlayerSlotId,
                        preparation.CurrentSummary.Token,
                        out string relocationIssue))
                {
                    return RollbackGameplayReadyEnter(
                        request, activity, owner, projectedSlots, gameplayRuntime,
                        gameplay, prepared, selections,
                        "Activity explicit Player relocation failed. " + relocationIssue);
                }

                PlayerActorPreparationToken preparationToken =
                    preparation.CurrentSummary.Token;
                bool preparedNow = preparation.Status ==
                    PlayerActorPreparationStatus.SucceededPrepared;
                prepared.Add(new PreparedSlotRecord(
                    slot.PlayerSlotId, preparationToken, preparedNow));

                PlayerGameplayRuntimeOperationResult current =
                    gameplayRuntime.TryEnsureCurrentGameplay(
                        slot.PlayerSlotId,
                        owner,
                        nameof(ActivityPlayerActorLifecycleParticipant),
                        "activity-enter-gameplay-ready-ensure-current-gameplay");
                if (current == null || !current.Succeeded ||
                    !current.CurrentAdmission.IsAdmitted ||
                    !current.CurrentAdmission.Token.IsValid ||
                    current.CurrentAdmission.PreparationToken != preparationToken ||
                    current.CurrentAdmission.Owner != owner ||
                    current.CurrentAdmission.InputBindingToken.Owner != owner)
                {
                    return RollbackGameplayReadyEnter(
                        request, activity, owner, projectedSlots, gameplayRuntime,
                        gameplay, prepared, selections,
                        current != null
                            ? current.ToDiagnosticString()
                            : $"Current gameplay projection returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                }

                if (!current.PreviousAdmission.IsAdmitted)
                {
                    gameplay.Add(new CurrentGameplayRecord(
                        slot.PlayerSlotId, current.CurrentAdmission.Token));
                }

                evidence[index] = new ActivityPlayerActorSlotLifecycleSnapshot(
                    slot.PlayerSlotId,
                    true,
                    slot.SelectedActorProfileId,
                    selectionApplied,
                    preparationToken,
                    preparedNow,
                    false,
                    preparation.Status,
                    current.Message);
            }

            activeRecord = new ActiveActivityRecord(
                activity, owner, PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots.Count, projectedSlots.Count, prepared, hosts);
            lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                ActivityPlayerActorLifecycleStatus.SucceededEntered,
                activity.ActivityName,
                owner,
                PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots.Count,
                projectedSlots.Count,
                prepared.Count,
                0,
                0,
                evidence,
                "GameplayReady current contextual projection was established over retained Session physical Players.");
            return ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-gameplay-ready-entered",
                lastSnapshot.ToDiagnosticString());
        }

        private ActivityContentExecutionResult RollbackGameplayReadyEnter(
            ActivityContentExecutionRequest request,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            List<PlayerSlotRuntimeSnapshot> projectedSlots,
            PlayerGameplayRuntimeHostModule gameplayRuntime,
            List<CurrentGameplayRecord> gameplay,
            List<PreparedSlotRecord> prepared,
            List<AppliedSelectionRecord> selections,
            string issue)
        {
            var failures = new List<string>();
            for (int index = gameplay.Count - 1; index >= 0; index--)
            {
                CurrentGameplayRecord record = gameplay[index];
                PlayerGameplayRuntimeOperationResult release =
                    gameplayRuntime.TryReleaseCurrentGameplay(
                        record.PlayerSlotId,
                        record.Token,
                        nameof(ActivityPlayerActorLifecycleParticipant),
                        "activity-enter-gameplay-ready-rollback-contextual-gameplay");
                if (release == null || !release.Succeeded)
                {
                    failures.Add(release != null
                        ? release.ToDiagnosticString()
                        : $"Contextual gameplay rollback returned no result for Slot '{record.PlayerSlotId.StableText}'.");
                }
            }

            for (int index = selections.Count - 1; index >= 0; index--)
            {
                AppliedSelectionRecord selection = selections[index];
                bool physicalCommitted = prepared.Exists(preparedSlot =>
                    preparedSlot.PlayerSlotId == selection.PlayerSlotId &&
                    preparedSlot.CreatedByEnter);
                if (physicalCommitted)
                {
                    continue;
                }

                PlayerActorSelectionResult clear =
                    preparationModule.TryClearActorSelection(
                        new PlayerActorSelectionRequest(
                            selection.PlayerSlotId,
                            null,
                            nameof(ActivityPlayerActorLifecycleParticipant),
                            "activity-enter-gameplay-ready-rollback-selection",
                            selection.SelectionRevision));
                if (clear == null || !clear.Succeeded)
                {
                    failures.Add(clear != null
                        ? clear.ToDiagnosticString()
                        : $"Selection rollback returned no result for Slot '{selection.PlayerSlotId.StableText}'.");
                }
            }

            string finalIssue = failures.Count == 0
                ? issue
                : issue + " Rollback failures: " + string.Join(" | ", failures);
            activeRecord = null;
            playerReadinessRecord = null;
            lastSnapshot = FailureSnapshot(
                failures.Count == 0
                    ? ActivityPlayerActorLifecycleStatus.FailedRequirement
                    : ActivityPlayerActorLifecycleStatus.FailedRollback,
                activity,
                owner,
                PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots,
                finalIssue);
            return Blocking(
                request,
                failures.Count == 0
                    ? "activity-player-actor-gameplay-ready-enter-failed"
                    : "activity-player-actor-gameplay-ready-enter-rollback-failed",
                finalIssue);
        }

        private ActivityContentExecutionResult FailGameplayReadyEnter(
            ActivityContentExecutionRequest request,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            List<PlayerSlotRuntimeSnapshot> projectedSlots,
            string issue)
        {
            lastSnapshot = FailureSnapshot(
                ActivityPlayerActorLifecycleStatus.FailedRequirement,
                activity,
                owner,
                PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots,
                issue);
            return Blocking(
                request,
                "activity-player-actor-gameplay-ready-runtime-missing",
                issue);
        }

        private bool TryReleaseGameplayBeforePreparedActor(
            PreparedSlotRecord prepared,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            PlayerGameplayRuntimeHostModule gameplayRuntime =
                preparationModule.GetComponent<PlayerGameplayRuntimeHostModule>();
            if (gameplayRuntime == null || !gameplayRuntime.IsReady)
            {
                if (activeRecord != null && activeRecord.RequirementLevel ==
                    PlayerParticipationRequirementLevel.GameplayReady)
                {
                    issue =
                        "GameplayReady Activity exit cannot release its contextual gameplay without the current gameplay runtime.";
                    return false;
                }

                return true;
            }

            if (!gameplayRuntime.TryGetCurrentAdmission(
                    prepared.PlayerSlotId,
                    out PlayerGameplayAdmissionSummary admission) ||
                !admission.IsAdmitted)
            {
                return true;
            }

            PlayerGameplayRuntimeOperationResult release =
                gameplayRuntime.TryReleaseCurrentGameplay(
                    prepared.PlayerSlotId,
                    admission.Token,
                    source,
                    reason);
            if (release != null && release.Succeeded)
            {
                return true;
            }

            issue = release != null
                ? release.ToDiagnosticString()
                : "Current gameplay release returned no result.";
            return false;
        }
    }
}
