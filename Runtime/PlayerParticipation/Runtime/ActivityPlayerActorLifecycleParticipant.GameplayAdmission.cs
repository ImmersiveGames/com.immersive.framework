using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class ActivityPlayerActorLifecycleParticipant
    {
        private readonly struct ColdStartGameplayRecord
        {
            internal ColdStartGameplayRecord(
                PlayerSlotId playerSlotId,
                PlayerGameplayAdmissionToken token)
            {
                PlayerSlotId = playerSlotId;
                Token = token;
            }

            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerGameplayAdmissionToken Token { get; }
        }

        private IActivityPlayerGameplayLifecycleRuntime
            gameplayLifecycleRuntime;

        internal void SetActivityPlayerGameplayLifecycleRuntime(
            IActivityPlayerGameplayLifecycleRuntime runtime)
        {
            gameplayLifecycleRuntime = runtime;
        }

        private ActivityContentExecutionResult
            ExecuteGameplayReadyAdoptionEnter(
                ActivityContentExecutionRequest request,
                ActivityAsset activity,
                RuntimeContentOwner owner,
                List<PlayerSlotRuntimeSnapshot> projectedSlots)
        {
            if (gameplayLifecycleRuntime == null)
            {
                string issue =
                    "GameplayReady Activity enter requires the official Player Gameplay Admission lifecycle adoption runtime.";
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

            if (!gameplayLifecycleRuntime.TryAdoptCommittedTarget(
                    request,
                    projectedSlots,
                    out IReadOnlyList<
                        ActivityPlayerGameplayAdoptedSlot> adopted,
                    out string adoptionIssue))
            {
                bool noActiveHandoff =
                    gameplayLifecycleRuntime is
                        ActivityPlayerLifecycleAdmissionRuntimeContext
                            admissionRuntime &&
                    !admissionRuntime.HasActiveTransaction;
                if (noActiveHandoff)
                {
                    return ExecuteGameplayReadyColdStartEnter(
                        request,
                        activity,
                        owner,
                        projectedSlots);
                }

                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedRequirement,
                    activity,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots,
                    adoptionIssue);
                return Blocking(
                    request,
                    "activity-player-actor-gameplay-ready-adoption-failed",
                    adoptionIssue);
            }

            if (adopted == null ||
                adopted.Count != projectedSlots.Count)
            {
                string issue =
                    "GameplayReady adoption returned an invalid Slot count.";
                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedRequirement,
                    activity,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots,
                    issue);
                return Blocking(
                    request,
                    "activity-player-actor-gameplay-ready-adoption-count",
                    issue);
            }

            var prepared =
                new List<PreparedSlotRecord>(adopted.Count);
            var admittedHosts =
                new List<LocalPlayerHostAuthoring>(adopted.Count);
            var evidence =
                new ActivityPlayerActorSlotLifecycleSnapshot[
                    adopted.Count];

            for (int index = 0;
                 index < adopted.Count;
                 index++)
            {
                ActivityPlayerGameplayAdoptedSlot slot =
                    adopted[index];
                if (!slot.IsValid ||
                    slot.PlayerSlotId !=
                        projectedSlots[index].PlayerSlotId)
                {
                    string issue =
                        $"GameplayReady adoption Slot order or evidence is invalid at index '{index}'.";
                    lastSnapshot = FailureSnapshot(
                        ActivityPlayerActorLifecycleStatus.FailedRequirement,
                        activity,
                        owner,
                        PlayerParticipationRequirementLevel.GameplayReady,
                        projectedSlots,
                        issue);
                    return Blocking(
                        request,
                        "activity-player-actor-gameplay-ready-adoption-invalid",
                        issue);
                }

                prepared.Add(
                    new PreparedSlotRecord(
                        slot.PlayerSlotId,
                        slot.PreparationToken,
                        false));
                if (!preparationModule.TryGetRegisteredHost(
                        slot.PlayerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out string hostIssue))
                {
                    string issue =
                        $"Committed GameplayReady admission has no exact Local Player Host evidence for Slot '{slot.PlayerSlotId.StableText}'. {hostIssue}";
                    lastSnapshot = FailureSnapshot(
                        ActivityPlayerActorLifecycleStatus.FailedRequirement,
                        activity,
                        owner,
                        PlayerParticipationRequirementLevel.GameplayReady,
                        projectedSlots,
                        issue);
                    return Blocking(
                        request,
                        "activity-player-actor-gameplay-ready-host-evidence-missing",
                        issue);
                }

                admittedHosts.Add(host);
                evidence[index] =
                    new ActivityPlayerActorSlotLifecycleSnapshot(
                        slot.PlayerSlotId,
                        true,
                        slot.ActorProfileId,
                        false,
                        slot.PreparationToken,
                        false,
                        false,
                        PlayerActorPreparationStatus
                            .SucceededAlreadyPrepared,
                        slot.Message);
            }

            activeRecord = new ActiveActivityRecord(
                activity,
                owner,
                PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots.Count,
                projectedSlots.Count,
                prepared,
                admittedHosts);
            lastSnapshot =
                new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus
                        .SucceededEntered,
                    activity.ActivityName,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots.Count,
                    projectedSlots.Count,
                    prepared.Count,
                    0,
                    0,
                    evidence,
                    "Activity Player Actor lifecycle adopted the committed P3J/P3K GameplayReady handoff.");
            return ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-gameplay-ready-adopted",
                lastSnapshot.ToDiagnosticString());
        }

        private ActivityContentExecutionResult
            ExecuteGameplayReadyColdStartEnter(
                ActivityContentExecutionRequest request,
                ActivityAsset activity,
                RuntimeContentOwner owner,
                List<PlayerSlotRuntimeSnapshot> projectedSlots)
        {
            PlayerGameplayRuntimeHostModule gameplayRuntime =
                preparationModule.GetComponent<
                    PlayerGameplayRuntimeHostModule>();
            if (gameplayRuntime == null || !gameplayRuntime.IsReady)
            {
                string gameplayRuntimeIssue =
                    gameplayRuntime != null
                        ? gameplayRuntime.Diagnostic
                        : "FrameworkRuntimeHost has no Player gameplay runtime module.";
                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedRequirement,
                    activity,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots,
                    gameplayRuntimeIssue);
                return Blocking(
                    request,
                    "activity-player-actor-gameplay-ready-cold-start-runtime-missing",
                    gameplayRuntimeIssue);
            }

            var prepared =
                new List<PreparedSlotRecord>(projectedSlots.Count);
            var appliedSelections =
                new List<AppliedSelectionRecord>();
            var appliedGameplay =
                new List<ColdStartGameplayRecord>();
            var admittedHosts =
                new List<LocalPlayerHostAuthoring>(projectedSlots.Count);
            var evidence =
                new ActivityPlayerActorSlotLifecycleSnapshot[
                    projectedSlots.Count];

            for (int index = 0;
                 index < projectedSlots.Count;
                 index++)
            {
                PlayerSlotRuntimeSnapshot slot = projectedSlots[index];
                if (!slot.IsJoined)
                {
                    return FailGameplayReadyColdStartAndRollback(
                        request,
                        activity,
                        owner,
                        projectedSlots,
                        gameplayRuntime,
                        appliedGameplay,
                        prepared,
                        appliedSelections,
                        $"Projected Player Slot '{slot.PlayerSlotId.StableText}' changed to a non-Joined state during GameplayReady cold start.");
                }

                if (!preparationModule.TryGetRegisteredHost(
                        slot.PlayerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out string hostIssue))
                {
                    return FailGameplayReadyColdStartAndRollback(
                        request,
                        activity,
                        owner,
                        projectedSlots,
                        gameplayRuntime,
                        appliedGameplay,
                        prepared,
                        appliedSelections,
                        hostIssue);
                }

                admittedHosts.Add(host);

                bool selectionApplied = false;
                if (!slot.HasSelectedActor)
                {
                    PlayerActorSelectionResult selection =
                        preparationModule.TrySelectDefaultActor(
                            slot.PlayerSlotId,
                            slot.SelectionRevision,
                            nameof(
                                ActivityPlayerActorLifecycleParticipant),
                            "activity-enter-gameplay-ready-cold-start-select-default-actor");
                    if (selection == null || !selection.Succeeded)
                    {
                        return FailGameplayReadyColdStartAndRollback(
                            request,
                            activity,
                            owner,
                            projectedSlots,
                            gameplayRuntime,
                            appliedGameplay,
                            prepared,
                            appliedSelections,
                            selection != null
                                ? selection.ToDiagnosticString()
                                : $"Default Actor selection returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                    }

                    slot = selection.Slot;
                    selectionApplied = selection.StateChanged;
                    if (selectionApplied)
                    {
                        appliedSelections.Add(
                            new AppliedSelectionRecord(
                                slot.PlayerSlotId,
                                selection.SelectionRevision));
                    }
                }

                if (!slot.HasSelectedActor)
                {
                    return FailGameplayReadyColdStartAndRollback(
                        request,
                        activity,
                        owner,
                        projectedSlots,
                        gameplayRuntime,
                        appliedGameplay,
                        prepared,
                        appliedSelections,
                        $"Projected Player Slot '{slot.PlayerSlotId.StableText}' has no selected Actor after default selection.");
                }

                PlayerActorPreparationResult preparation =
                    preparationModule.TryEnsureSessionPhysicalActor(
                        request.RuntimeScopeContext,
                        slot.PlayerSlotId,
                        nameof(
                            ActivityPlayerActorLifecycleParticipant),
                        "activity-enter-gameplay-ready-cold-start-prepare-selected-actor");
                if (preparation == null ||
                    !preparation.Succeeded ||
                    !preparation.CurrentSummary.IsPrepared ||
                    !preparation.CurrentSummary.Token.IsValid)
                {
                    return FailGameplayReadyColdStartAndRollback(
                        request,
                        activity,
                        owner,
                        projectedSlots,
                        gameplayRuntime,
                        appliedGameplay,
                        prepared,
                        appliedSelections,
                        preparation != null
                            ? preparation.ToDiagnosticString()
                            : $"Logical Actor preparation returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                }

                bool preparationApplied =
                    preparation.Status ==
                        PlayerActorPreparationStatus.SucceededPrepared;
                PlayerActorPreparationToken preparationToken =
                    preparation.CurrentSummary.Token;
                prepared.Add(
                    new PreparedSlotRecord(
                        slot.PlayerSlotId,
                        preparationToken,
                        preparationApplied));

                PlayerGameplayRuntimeOperationResult gameplay =
                    gameplayRuntime.TryEnsureCurrentGameplay(
                        slot.PlayerSlotId,
                        owner,
                        nameof(
                            ActivityPlayerActorLifecycleParticipant),
                        "activity-enter-gameplay-ready-cold-start-ensure-current-gameplay");
                if (gameplay == null ||
                    !gameplay.Succeeded ||
                    !gameplay.CurrentAdmission.IsAdmitted ||
                    !gameplay.CurrentAdmission.Token.IsValid ||
                    gameplay.CurrentAdmission.PreparationToken !=
                        preparationToken ||
                    gameplay.CurrentAdmission.Owner != owner ||
                    gameplay.CurrentAdmission.InputBindingToken.Owner != owner)
                {
                    return FailGameplayReadyColdStartAndRollback(
                        request,
                        activity,
                        owner,
                        projectedSlots,
                        gameplayRuntime,
                        appliedGameplay,
                        prepared,
                        appliedSelections,
                        gameplay != null
                            ? gameplay.ToDiagnosticString()
                            : $"Gameplay admission returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                }

                bool gameplayApplied =
                    !gameplay.PreviousAdmission.IsAdmitted &&
                    gameplay.CurrentAdmission.IsAdmitted;
                if (gameplayApplied)
                {
                    appliedGameplay.Add(
                        new ColdStartGameplayRecord(
                            slot.PlayerSlotId,
                            gameplay.CurrentAdmission.Token));
                }

                evidence[index] =
                    new ActivityPlayerActorSlotLifecycleSnapshot(
                        slot.PlayerSlotId,
                        true,
                        slot.SelectedActorProfileId,
                        selectionApplied,
                        preparationToken,
                        preparationApplied,
                        false,
                        preparation.Status,
                        gameplay.Message);
            }

            activeRecord = new ActiveActivityRecord(
                activity,
                owner,
                PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots.Count,
                projectedSlots.Count,
                prepared,
                admittedHosts);
            lastSnapshot =
                new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus
                        .SucceededEntered,
                    activity.ActivityName,
                    owner,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    projectedSlots.Count,
                    projectedSlots.Count,
                    prepared.Count,
                    0,
                    0,
                    evidence,
                    "Activity Player Actor lifecycle calculated GameplayReady readiness without a transferable handoff.");
            return ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-gameplay-ready-cold-started",
                lastSnapshot.ToDiagnosticString());
        }

        private ActivityContentExecutionResult
            FailGameplayReadyColdStartAndRollback(
                ActivityContentExecutionRequest request,
                ActivityAsset activity,
                RuntimeContentOwner owner,
                List<PlayerSlotRuntimeSnapshot> projectedSlots,
                PlayerGameplayRuntimeHostModule gameplayRuntime,
                List<ColdStartGameplayRecord> appliedGameplay,
                List<PreparedSlotRecord> prepared,
                List<AppliedSelectionRecord> appliedSelections,
                string issue)
        {
            bool rollbackSucceeded =
                RollbackGameplayReadyColdStart(
                    gameplayRuntime,
                    appliedGameplay,
                    prepared,
                    appliedSelections,
                    out string rollbackIssue);
            string finalIssue = rollbackSucceeded
                ? issue
                : issue + " Rollback failures: " + rollbackIssue;

            activeRecord = null;
            playerReadinessRecord = null;
            lastSnapshot = FailureSnapshot(
                rollbackSucceeded
                    ? ActivityPlayerActorLifecycleStatus.FailedRequirement
                    : ActivityPlayerActorLifecycleStatus.FailedRollback,
                activity,
                owner,
                PlayerParticipationRequirementLevel.GameplayReady,
                projectedSlots,
                finalIssue);
            return Blocking(
                request,
                rollbackSucceeded
                    ? "activity-player-actor-gameplay-ready-cold-start-failed"
                    : "activity-player-actor-gameplay-ready-cold-start-rollback-failed",
                finalIssue);
        }

        private bool RollbackGameplayReadyColdStart(
            PlayerGameplayRuntimeHostModule gameplayRuntime,
            List<ColdStartGameplayRecord> appliedGameplay,
            List<PreparedSlotRecord> prepared,
            List<AppliedSelectionRecord> appliedSelections,
            out string issue)
        {
            var failures = new List<string>();

            for (int index = appliedGameplay.Count - 1;
                 index >= 0;
                 index--)
            {
                ColdStartGameplayRecord record =
                    appliedGameplay[index];
                PlayerGameplayRuntimeOperationResult release =
                    gameplayRuntime.TryReleaseCurrentGameplay(
                        record.PlayerSlotId,
                        record.Token,
                        nameof(
                            ActivityPlayerActorLifecycleParticipant),
                        "activity-enter-gameplay-ready-cold-start-rollback-gameplay");
                if (release == null || !release.Succeeded)
                {
                    failures.Add(
                        release != null
                            ? release.ToDiagnosticString()
                            : $"Gameplay rollback returned no result for Slot '{record.PlayerSlotId.StableText}'.");
                }
            }

            for (int index = appliedSelections.Count - 1;
                 index >= 0;
                 index--)
            {
                AppliedSelectionRecord record =
                    appliedSelections[index];
                bool physicalCommitted = false;
                for (int preparedIndex = 0;
                     preparedIndex < prepared.Count;
                     preparedIndex++)
                {
                    PreparedSlotRecord preparedSlot = prepared[preparedIndex];
                    if (preparedSlot.PlayerSlotId == record.PlayerSlotId &&
                        preparedSlot.CreatedByEnter)
                    {
                        physicalCommitted = true;
                        break;
                    }
                }

                if (physicalCommitted)
                {
                    continue;
                }

                PlayerActorSelectionResult clear =
                    preparationModule.TryClearActorSelection(
                        new PlayerActorSelectionRequest(
                            record.PlayerSlotId,
                            null,
                            nameof(
                                ActivityPlayerActorLifecycleParticipant),
                            "activity-enter-gameplay-ready-cold-start-rollback-selection",
                            record.SelectionRevision));
                if (clear == null || !clear.Succeeded)
                {
                    failures.Add(
                        clear != null
                            ? clear.ToDiagnosticString()
                            : $"Selection rollback returned no result for Slot '{record.PlayerSlotId.StableText}'.");
                }
            }

            issue = failures.Count == 0
                ? string.Empty
                : string.Join(" | ", failures);
            return failures.Count == 0;
        }

        private bool TryExecuteCommittedGameplayHandoffExit(
            ActivityContentExecutionRequest request,
            out ActivityContentExecutionResult result)
        {
            result = default;
            if (gameplayLifecycleRuntime == null)
            {
                return false;
            }

            if (!gameplayLifecycleRuntime
                    .TryHandleSupersededPreviousExit(
                        request,
                        out bool handled,
                        out ActivityPlayerPreviousExitDisposition
                            disposition,
                        out string issue))
            {
                result = Blocking(
                    request,
                    "activity-player-actor-handoff-exit-failed",
                    issue);
                return true;
            }

            if (!handled)
            {
                return false;
            }

            if (activeRecord == null)
            {
                lastSnapshot =
                    new ActivityPlayerActorLifecycleSnapshot(
                        ActivityPlayerActorLifecycleStatus
                            .SucceededExitedNoActors,
                        request.Activity.ActivityName,
                        request.Owner,
                        ResolveRequirementLevel(request.Activity),
                        0,
                        0,
                        0,
                        0,
                        0,
                        Array.Empty<
                            ActivityPlayerActorSlotLifecycleSnapshot>(),
                        disposition ==
                            ActivityPlayerPreviousExitDisposition
                                .SupersededAwaitingCommit
                            ? "Previous Activity Player lifecycle exit transferred to the reversible Route Startup handoff without a retained P3J.6 Activity record."
                            : "Previous Activity Player lifecycle exit was acknowledged by the committed P3K.7E handoff without a retained P3J.6 Activity record.");
                result =
                    ActivityContentExecutionResult.SucceededNoOp(
                        request,
                        nameof(
                            ActivityPlayerActorLifecycleParticipant),
                        disposition ==
                            ActivityPlayerPreviousExitDisposition
                                .SupersededAwaitingCommit
                            ? "activity-player-actor-exit-transferred-to-route-handoff"
                            : "activity-player-actor-exit-superseded-without-retained-record",
                        lastSnapshot.ToDiagnosticString());
                return true;
            }

            var evidence =
                new ActivityPlayerActorSlotLifecycleSnapshot[
                    activeRecord.PreparedSlots.Count];
            for (int index = 0;
                 index < activeRecord.PreparedSlots.Count;
                 index++)
            {
                PreparedSlotRecord prepared =
                    activeRecord.PreparedSlots[index];
                evidence[index] =
                    new ActivityPlayerActorSlotLifecycleSnapshot(
                        prepared.PlayerSlotId,
                        true,
                        default,
                        false,
                        prepared.Token,
                        prepared.CreatedByEnter,
                        disposition ==
                            ActivityPlayerPreviousExitDisposition
                                .SupersededByCommittedHandoff,
                        disposition ==
                            ActivityPlayerPreviousExitDisposition
                                .SupersededAwaitingCommit
                            ? PlayerActorPreparationStatus
                                .SucceededAlreadyPrepared
                            : PlayerActorPreparationStatus
                                .SucceededAlreadyReleased,
                        disposition ==
                            ActivityPlayerPreviousExitDisposition
                                .SupersededAwaitingCommit
                            ? "Previous Actor remains retained by the reversible Route Startup handoff until commit."
                            : "Previous Actor and gameplay chain were released by the committed P3K.7E handoff.");
            }

            PlayerParticipationRequirementLevel requirementLevel =
                activeRecord.RequirementLevel;
            int projectedSlotCount =
                activeRecord.ProjectedSlotCount;
            int selectedCount =
                activeRecord.SelectedCount;
            int preparedCount =
                activeRecord.PreparedSlots.Count;
            activeRecord = null;

            lastSnapshot =
                new ActivityPlayerActorLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus
                        .SucceededExited,
                    request.Activity.ActivityName,
                    request.Owner,
                    requirementLevel,
                    projectedSlotCount,
                    selectedCount,
                    preparedCount,
                    disposition ==
                        ActivityPlayerPreviousExitDisposition
                            .SupersededByCommittedHandoff
                        ? preparedCount
                        : 0,
                    0,
                    evidence,
                    disposition ==
                        ActivityPlayerPreviousExitDisposition
                            .SupersededAwaitingCommit
                        ? "Previous Activity Player Actor lifecycle exit transferred to the reversible Route Startup handoff."
                        : "Previous Activity Player Actor lifecycle exit was superseded by the committed P3K.7E handoff.");
            result = ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                disposition ==
                    ActivityPlayerPreviousExitDisposition
                        .SupersededAwaitingCommit
                    ? "activity-player-actor-exit-transferred-to-route-handoff"
                    : "activity-player-actor-exit-superseded-by-handoff",
                lastSnapshot.ToDiagnosticString());
            return true;
        }

        private bool TryReleaseGameplayBeforePreparedActor(
            PreparedSlotRecord prepared,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (gameplayLifecycleRuntime == null)
            {
                if (activeRecord != null &&
                    activeRecord.RequirementLevel ==
                        PlayerParticipationRequirementLevel
                            .GameplayReady)
                {
                    issue =
                        "GameplayReady Actor exit cannot release P3J before the official gameplay lifecycle runtime is available.";
                    return false;
                }

                return true;
            }

            return gameplayLifecycleRuntime
                .TryReleaseGameplayBeforeActor(
                    prepared.PlayerSlotId,
                    prepared.Token,
                    activeRecord != null ? activeRecord.Owner : default,
                    source,
                    reason,
                    out _,
                    out issue);
        }
    }
}
