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
        private sealed class ActivityPlayerReadinessRecord
        {
            internal ActivityAsset Activity;
            internal RuntimeContentOwner Owner;
            internal RuntimeScopeContext ScopeContext;
            internal PlayerParticipationRequirementLevel RequirementLevel;
            internal List<PlayerReadinessSlotRecord> ProjectedSlots;
            internal int EnterSessionRevision;
            internal int AppliedSessionRevision;
            internal int Occurrence;
            internal bool Completed;
            internal bool Failed;
            internal bool Released;
            internal ActivityPlayerActorReadinessReason ReadinessReason;
            internal string Message;
        }

        private sealed class PlayerReadinessSlotRecord
        {
            internal PlayerSlotId PlayerSlotId;
            internal int SlotRevision;
            internal int SelectionRevision;
            internal bool Joined;
            internal bool Selected;
            internal bool Prepared;
            internal bool GameplayAdmitted;
            internal bool GameplayReady;
            internal bool SelectionCreatedByLifecycle;
            internal bool PreparationCreatedByLifecycle;
            internal bool GameplayCreatedByLifecycle;
            internal PlayerActorPreparationToken PreparationToken;
            internal PlayerGameplayAdmissionToken GameplayAdmissionToken;
            internal ActivityPlayerActorReadinessReason ReadinessReason;
            internal string Message;
        }

        private sealed class ReconcilePassDelta
        {
            internal PlayerSlotId PlayerSlotId;
            internal bool SelectionApplied;
            internal int SelectionRevision;
            internal bool PreparationApplied;
            internal PlayerActorPreparationToken PreparationToken;
            internal bool GameplayApplied;
            internal PlayerGameplayAdmissionToken GameplayAdmissionToken;
        }

        private ActivityPlayerReadinessRecord playerReadinessRecord;
        private ActivityPlayerActorReconcileResult lastReconcileResult;

        internal ActivityPlayerActorReconcileResult LastReconcileResult =>
            lastReconcileResult;

        private static bool ShouldDeferActivityPlayerReadiness(
            PlayerParticipationRequirementLevel requirementLevel,
            IReadOnlyList<PlayerSlotRuntimeSnapshot> projectedSlots)
        {
            if ((int)requirementLevel <
                    (int)PlayerParticipationRequirementLevel.JoinedSlots ||
                projectedSlots == null)
            {
                return false;
            }

            for (int index = 0; index < projectedSlots.Count; index++)
            {
                if (!projectedSlots[index].IsJoined)
                {
                    return true;
                }
            }

            return false;
        }

        private ActivityContentExecutionResult
            BeginDeferredActivityPlayerReadiness(
                ActivityContentExecutionRequest request,
                ActivityAsset activity,
                RuntimeContentOwner owner,
                PlayerParticipationRequirementLevel requirementLevel,
                IReadOnlyList<PlayerSlotRuntimeSnapshot> projectedSlots)
        {
            PlayerParticipationSnapshot session =
                participationContext.CreateSnapshot();
            if (session == null || !session.IsInitialized)
            {
                string issue =
                    "Deferred Activity Player readiness requires an initialized Session participation snapshot.";
                lastSnapshot = FailureSnapshot(
                    ActivityPlayerActorLifecycleStatus.FailedProjection,
                    activity,
                    owner,
                    requirementLevel,
                    projectedSlots != null
                        ? new List<PlayerSlotRuntimeSnapshot>(projectedSlots)
                        : new List<PlayerSlotRuntimeSnapshot>(),
                    issue);
                return Blocking(
                    request,
                    "activity-player-actor-deferred-session-missing",
                    issue);
            }

            var slots = new List<PlayerReadinessSlotRecord>(
                projectedSlots.Count);
            var admittedHosts = new List<LocalPlayerHostAuthoring>();
            int selectedCount = 0;
            var evidence = new ActivityPlayerActorSlotLifecycleSnapshot[
                projectedSlots.Count];
            for (int index = 0; index < projectedSlots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = projectedSlots[index];
                if (slot.IsJoined)
                {
                    if (!preparationModule.TryGetRegisteredHost(
                            slot.PlayerSlotId,
                            out LocalPlayerHostAuthoring host,
                            out string hostIssue))
                    {
                        playerReadinessRecord = null;
                        lastSnapshot = FailureSnapshot(
                            ActivityPlayerActorLifecycleStatus
                                .FailedRequirement,
                            activity,
                            owner,
                            requirementLevel,
                            new List<PlayerSlotRuntimeSnapshot>(
                                projectedSlots),
                            hostIssue);
                        return Blocking(
                            request,
                            "activity-player-actor-deferred-host-evidence-missing",
                            hostIssue);
                    }

                    admittedHosts.Add(host);
                }

                if (slot.HasSelectedActor)
                {
                    selectedCount++;
                }
                ActivityPlayerActorReadinessReason slotReason =
                    slot.IsJoined
                        ? ResolveNextReadinessReason(requirementLevel, slot)
                        : ActivityPlayerActorReadinessReason.WaitingForJoin;
                slots.Add(new PlayerReadinessSlotRecord
                {
                    PlayerSlotId = slot.PlayerSlotId,
                    SlotRevision = slot.Revision,
                    SelectionRevision = slot.SelectionRevision,
                    Joined = slot.IsJoined,
                    Selected = slot.HasSelectedActor,
                    Prepared = false,
                    GameplayAdmitted = false,
                    GameplayReady = false,
                    ReadinessReason = slotReason,
                    Message = slot.IsJoined
                        ? "Player lifecycle will continue through explicit delta reconcile."
                        : "Projected Player Slot is waiting for Join."
                });
                evidence[index] = new ActivityPlayerActorSlotLifecycleSnapshot(
                    slot.PlayerSlotId,
                    slot.IsJoined,
                    slot.SelectedActorProfileId,
                    false,
                    default,
                    false,
                    false,
                    PlayerActorPreparationStatus.None,
                    slots[index].Message);
            }

            playerReadinessRecord = new ActivityPlayerReadinessRecord
            {
                Activity = activity,
                Owner = owner,
                ScopeContext = request.RuntimeScopeContext,
                RequirementLevel = requirementLevel,
                ProjectedSlots = slots,
                EnterSessionRevision = session.Revision,
                AppliedSessionRevision = session.Revision,
                Occurrence = 0,
                Completed = false,
                Failed = false,
                Released = false,
                ReadinessReason = ResolveAggregateReadinessReason(slots),
                Message = "Activity Player lifecycle entered in a normal Preparing state and awaits explicit delta reconcile."
            };
            SynchronizePlayerReadinessContributionAfterRecordCreated();

            activeRecord = new ActiveActivityRecord(
                activity,
                owner,
                requirementLevel,
                projectedSlots.Count,
                selectedCount,
                new List<PreparedSlotRecord>(),
                admittedHosts);
            lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                ActivityPlayerActorLifecycleStatus.SucceededEnteredPreparing,
                activity.ActivityName,
                owner,
                requirementLevel,
                projectedSlots.Count,
                selectedCount,
                0,
                0,
                0,
                evidence,
                playerReadinessRecord.Message);
            return ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-entered-preparing",
                lastSnapshot.ToDiagnosticString());
        }

        private void CaptureImmediateActivityPlayerReadiness(
            ActivityContentExecutionRequest request,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            PlayerParticipationRequirementLevel requirementLevel,
            IReadOnlyList<PlayerSlotRuntimeSnapshot> projectedSlots,
            ActivityContentExecutionResult result)
        {
            if (!result.Succeeded ||
                requirementLevel == PlayerParticipationRequirementLevel.None ||
                projectedSlots == null ||
                projectedSlots.Count == 0)
            {
                playerReadinessRecord = null;
                return;
            }

            PlayerParticipationSnapshot session =
                participationContext.CreateSnapshot();
            var slots = new List<PlayerReadinessSlotRecord>(
                projectedSlots.Count);
            for (int index = 0; index < projectedSlots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = projectedSlots[index];
                PlayerActorPreparationToken preparationToken =
                    FindPreparedToken(slot.PlayerSlotId);
                PlayerGameplayAdmissionToken admissionToken =
                    FindGameplayAdmissionToken(slot.PlayerSlotId);
                slots.Add(new PlayerReadinessSlotRecord
                {
                    PlayerSlotId = slot.PlayerSlotId,
                    SlotRevision = slot.Revision,
                    SelectionRevision = slot.SelectionRevision,
                    Joined = slot.IsJoined,
                    Selected = slot.HasSelectedActor,
                    Prepared =
                        (int)requirementLevel <
                            (int)PlayerParticipationRequirementLevel
                                .LogicalActorsPrepared ||
                        preparationToken.IsValid,
                    GameplayAdmitted =
                        (int)requirementLevel <
                            (int)PlayerParticipationRequirementLevel.GameplayReady ||
                        admissionToken.IsValid,
                    GameplayReady = true,
                    PreparationToken = preparationToken,
                    GameplayAdmissionToken = admissionToken,
                    ReadinessReason =
                        ActivityPlayerActorReadinessReason.RequirementSatisfied,
                    Message = "Player requirement was satisfied during Activity enter."
                });
            }

            int revision = session != null && session.IsInitialized
                ? session.Revision
                : 0;
            playerReadinessRecord = new ActivityPlayerReadinessRecord
            {
                Activity = activity,
                Owner = owner,
                ScopeContext = request.RuntimeScopeContext,
                RequirementLevel = requirementLevel,
                ProjectedSlots = slots,
                EnterSessionRevision = revision,
                AppliedSessionRevision = revision,
                Occurrence = 0,
                Completed = true,
                Failed = false,
                Released = false,
                ReadinessReason =
                    ActivityPlayerActorReadinessReason.RequirementSatisfied,
                Message =
                    "Activity Player lifecycle requirement was satisfied during Activity enter."
            };
            SynchronizePlayerReadinessContributionAfterRecordCreated();
        }

        internal ActivityPlayerActorReconcileResult
            TryReconcileActiveActivityPlayerLifecycle(
                ActivityAsset expectedActivity,
                RuntimeContentOwner expectedOwner,
                int expectedOccurrence,
                string source,
                string reason)
        {
            string resolvedSource = string.IsNullOrWhiteSpace(source)
                ? nameof(ActivityPlayerActorLifecycleParticipant)
                : source.Trim();
            string resolvedReason = string.IsNullOrWhiteSpace(reason)
                ? "active-activity-player-reconcile"
                : reason.Trim();

            ActivityPlayerActorReconcileResult rejected =
                ValidateReconcileRequest(
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence);
            if (rejected != null)
            {
                lastReconcileResult = rejected;
                return rejected;
            }

            PlayerParticipationSnapshot session =
                participationContext.CreateSnapshot();
            if (session == null || !session.IsInitialized)
            {
                return PublishReconcileFailure(
                    ActivityPlayerActorReconcileStatus.FailedProjection,
                    0,
                    "Session Player participation snapshot is unavailable.",
                    false,
                    false);
            }

            if (session.Revision <
                playerReadinessRecord.AppliedSessionRevision)
            {
                return PublishReconcileFailure(
                    ActivityPlayerActorReconcileStatus.FailedProjection,
                    session.Revision,
                    $"Session revision regressed from '{playerReadinessRecord.AppliedSessionRevision}' to '{session.Revision}'.",
                    false,
                    false);
            }

            if (playerReadinessRecord.Completed)
            {
                lastReconcileResult = BuildReconcileResult(
                    ActivityPlayerActorReconcileStatus.SucceededNoChange,
                    session.Revision,
                    playerReadinessRecord.AppliedSessionRevision,
                    false,
                    false,
                    true,
                    "Activity Player readiness is already complete.");
                return lastReconcileResult;
            }

            if (!HasReconcileRevisionDelta(session))
            {
                lastReconcileResult = BuildReconcileResult(
                    ActivityPlayerActorReconcileStatus.SucceededNoChange,
                    session.Revision,
                    playerReadinessRecord.AppliedSessionRevision,
                    false,
                    false,
                    true,
                    "Session and projected Slot revisions have not changed.");
                return lastReconcileResult;
            }

            var deltas = new List<ReconcilePassDelta>();
            bool progressed = false;
            PlayerGameplayRuntimeHostModule gameplayRuntime = null;
            string gameplayRuntimeIssue = string.Empty;

            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord slotRecord =
                    playerReadinessRecord.ProjectedSlots[index];
                if (!TryFindSlot(
                        session,
                        slotRecord.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot slot))
                {
                    return FailAndRollbackReconcile(
                        ActivityPlayerActorReconcileStatus.FailedProjection,
                        session.Revision,
                        deltas,
                        gameplayRuntime,
                        resolvedSource,
                        resolvedReason,
                        $"Projected Player Slot '{slotRecord.PlayerSlotId.StableText}' is no longer configured in the Session.");
                }

                if (slot.Revision < slotRecord.SlotRevision ||
                    slot.SelectionRevision <
                        slotRecord.SelectionRevision)
                {
                    return FailAndRollbackReconcile(
                        ActivityPlayerActorReconcileStatus.FailedProjection,
                        session.Revision,
                        deltas,
                        gameplayRuntime,
                        resolvedSource,
                        resolvedReason,
                        $"Projected Player Slot '{slot.PlayerSlotId.StableText}' " +
                        $"revision regressed. previousSlotRevision='{slotRecord.SlotRevision}' " +
                        $"currentSlotRevision='{slot.Revision}' " +
                        $"previousSelectionRevision='{slotRecord.SelectionRevision}' " +
                        $"currentSelectionRevision='{slot.SelectionRevision}'.");
                }

                slotRecord.SlotRevision = slot.Revision;
                slotRecord.SelectionRevision = slot.SelectionRevision;
                slotRecord.Joined = slot.IsJoined;
                slotRecord.Selected = slot.HasSelectedActor;

                if ((int)playerReadinessRecord.RequirementLevel >=
                        (int)PlayerParticipationRequirementLevel.JoinedSlots &&
                    !slot.IsJoined)
                {
                    slotRecord.ReadinessReason =
                        ActivityPlayerActorReadinessReason.WaitingForJoin;
                    slotRecord.Message = "Projected Player Slot is waiting for Join.";
                    continue;
                }

                if (!preparationModule.TryGetRegisteredHost(
                        slot.PlayerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out string hostIssue))
                {
                    return FailAndRollbackReconcile(
                        ActivityPlayerActorReconcileStatus.FailedHostEvidence,
                        session.Revision,
                        deltas,
                        gameplayRuntime,
                        resolvedSource,
                        resolvedReason,
                        hostIssue);
                }

                if ((int)playerReadinessRecord.RequirementLevel >=
                    (int)PlayerParticipationRequirementLevel.SelectedActors)
                {
                    if (!slot.HasSelectedActor)
                    {
                        slotRecord.ReadinessReason =
                            ActivityPlayerActorReadinessReason
                                .WaitingForActorSelection;
                        PlayerActorSelectionResult selection =
                            preparationModule.TrySelectDefaultActor(
                                slot.PlayerSlotId,
                                slot.SelectionRevision,
                                resolvedSource,
                                resolvedReason + "; select-default-actor");
                        if (selection == null || !selection.Succeeded)
                        {
                            return FailAndRollbackReconcile(
                                ActivityPlayerActorReconcileStatus.FailedSelection,
                                session.Revision,
                                deltas,
                                gameplayRuntime,
                                resolvedSource,
                                resolvedReason,
                                selection != null
                                    ? selection.ToDiagnosticString()
                                    : $"Default Actor selection returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                        }

                        slot = selection.Slot;
                        slotRecord.Selected = slot.HasSelectedActor;
                        slotRecord.SelectionRevision = slot.SelectionRevision;
                        slotRecord.SelectionCreatedByLifecycle |=
                            selection.StateChanged;
                        if (selection.StateChanged)
                        {
                            deltas.Add(new ReconcilePassDelta
                            {
                                PlayerSlotId = slot.PlayerSlotId,
                                SelectionApplied = true,
                                SelectionRevision =
                                    selection.SelectionRevision
                            });
                            progressed = true;
                        }
                    }

                    if (!slot.HasSelectedActor)
                    {
                        return FailAndRollbackReconcile(
                            ActivityPlayerActorReconcileStatus.FailedSelection,
                            session.Revision,
                            deltas,
                            gameplayRuntime,
                            resolvedSource,
                            resolvedReason,
                            $"Projected Player Slot '{slot.PlayerSlotId.StableText}' has no selected Actor after default selection.");
                    }
                }

                if ((int)playerReadinessRecord.RequirementLevel >=
                    (int)PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared)
                {
                    slotRecord.ReadinessReason =
                        ActivityPlayerActorReadinessReason
                            .PreparingLogicalActor;
                    PlayerActorPreparationResult preparation =
                        preparationModule.TryPrepareSelectedActor(
                            playerReadinessRecord.ScopeContext,
                            slot.PlayerSlotId,
                            resolvedSource,
                            resolvedReason + "; prepare-selected-actor");
                    if (preparation == null || !preparation.Succeeded)
                    {
                        return FailAndRollbackReconcile(
                            ActivityPlayerActorReconcileStatus.FailedPreparation,
                            session.Revision,
                            deltas,
                            gameplayRuntime,
                            resolvedSource,
                            resolvedReason,
                            preparation != null
                                ? preparation.ToDiagnosticString()
                                : $"Logical Actor preparation returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                    }

                    bool preparationApplied =
                        preparation.Status ==
                        PlayerActorPreparationStatus.SucceededPrepared;
                    slotRecord.PreparationToken =
                        preparation.CurrentSummary.Token;
                    slotRecord.Prepared =
                        preparation.CurrentSummary.IsPrepared;
                    slotRecord.PreparationCreatedByLifecycle |=
                        preparationApplied;
                    if (preparationApplied)
                    {
                        deltas.Add(new ReconcilePassDelta
                        {
                            PlayerSlotId = slot.PlayerSlotId,
                            PreparationApplied = true,
                            PreparationToken =
                                preparation.CurrentSummary.Token
                        });
                        progressed = true;
                    }
                }

                if ((int)playerReadinessRecord.RequirementLevel >=
                    (int)PlayerParticipationRequirementLevel.GameplayReady)
                {
                    slotRecord.ReadinessReason =
                        ActivityPlayerActorReadinessReason
                            .PreparingGameplayAdmission;
                    if (gameplayRuntime == null &&
                        !preparationModule.TryGetPlayerGameplayRuntime(
                            out gameplayRuntime,
                            out gameplayRuntimeIssue))
                    {
                        return FailAndRollbackReconcile(
                            ActivityPlayerActorReconcileStatus
                                .FailedGameplayAdmission,
                            session.Revision,
                            deltas,
                            null,
                            resolvedSource,
                            resolvedReason,
                            gameplayRuntimeIssue);
                    }

                    PlayerGameplayRuntimeOperationResult gameplay =
                        gameplayRuntime.TryEnsureCurrentGameplay(
                            slot.PlayerSlotId,
                            resolvedSource,
                            resolvedReason + "; ensure-current-gameplay");
                    if (gameplay == null || !gameplay.Succeeded)
                    {
                        return FailAndRollbackReconcile(
                            ActivityPlayerActorReconcileStatus
                                .FailedGameplayAdmission,
                            session.Revision,
                            deltas,
                            gameplayRuntime,
                            resolvedSource,
                            resolvedReason,
                            gameplay != null
                                ? gameplay.ToDiagnosticString()
                                : $"Gameplay admission returned no result for Slot '{slot.PlayerSlotId.StableText}'.");
                    }

                    bool gameplayApplied =
                        !gameplay.PreviousAdmission.IsAdmitted &&
                        gameplay.CurrentAdmission.IsAdmitted;
                    slotRecord.GameplayAdmissionToken =
                        gameplay.CurrentAdmission.Token;
                    slotRecord.GameplayAdmitted =
                        gameplay.CurrentAdmission.IsAdmitted;
                    slotRecord.GameplayReady = gameplay.GameplayReady;
                    slotRecord.GameplayCreatedByLifecycle |=
                        gameplayApplied;
                    if (gameplayApplied)
                    {
                        deltas.Add(new ReconcilePassDelta
                        {
                            PlayerSlotId = slot.PlayerSlotId,
                            GameplayApplied = true,
                            GameplayAdmissionToken =
                                gameplay.CurrentAdmission.Token
                        });
                        progressed = true;
                    }
                }

                slotRecord.ReadinessReason =
                    ActivityPlayerActorReadinessReason.RequirementSatisfied;
                slotRecord.Message =
                    "Player lifecycle delta is satisfied for this Slot.";
            }

            PlayerParticipationSnapshot appliedSession =
                participationContext.CreateSnapshot();
            if (appliedSession == null || !appliedSession.IsInitialized)
            {
                return FailAndRollbackReconcile(
                    ActivityPlayerActorReconcileStatus.FailedProjection,
                    session.Revision,
                    deltas,
                    gameplayRuntime,
                    resolvedSource,
                    resolvedReason,
                    "Session snapshot became unavailable after applying the Player lifecycle delta.");
            }

            RefreshReadinessSlotRevisions(appliedSession);
            playerReadinessRecord.AppliedSessionRevision =
                appliedSession.Revision;
            RebuildActiveRecordFromReadiness(appliedSession);

            ActivityPlayerAdmissionEvaluationResult evaluation =
                EvaluateCurrentPlayerAdmission(
                    out PlayerParticipationSnapshot evaluatedSession,
                    out _,
                    out _);
            if (evaluation == null)
            {
                return FailAndRollbackReconcile(
                    ActivityPlayerActorReconcileStatus.FailedProjection,
                    appliedSession.Revision,
                    deltas,
                    gameplayRuntime,
                    resolvedSource,
                    resolvedReason,
                    "Activity Player admission evaluator returned no result.");
            }

            if (evaluation.CanActivate ||
                IsOnlyBlockedByCurrentEntryGate(evaluation))
            {
                string completionMessage = evaluation.CanActivate
                    ? "All projected Player requirements are satisfied."
                    : "All Player gameplay chains are authoritative and are " +
                      "blocked only by the current Activity entry gate; " +
                      "readiness may release that gate.";
                MarkAllReadinessSlotsSatisfied(completionMessage);
                CompletePlayerReadinessContribution(completionMessage);
                UpdateLifecycleSnapshot(
                    ActivityPlayerActorLifecycleStatus
                        .SucceededReconciledReady,
                    evaluatedSession,
                    evaluation,
                    playerReadinessRecord.Message);
                lastReconcileResult = BuildReconcileResult(
                    ActivityPlayerActorReconcileStatus.SucceededCompleted,
                    appliedSession.Revision,
                    playerReadinessRecord.AppliedSessionRevision,
                    true,
                    false,
                    true,
                    playerReadinessRecord.Message);
                return lastReconcileResult;
            }

            if (evaluation.IsFailed || evaluation.IsBlocked)
            {
                return FailAndRollbackReconcile(
                    ActivityPlayerActorReconcileStatus.FailedProjection,
                    appliedSession.Revision,
                    deltas,
                    gameplayRuntime,
                    resolvedSource,
                    resolvedReason,
                    evaluation.ToDiagnosticString());
            }

            ApplyPendingEvaluation(evaluation);
            UpdateLifecycleSnapshot(
                ActivityPlayerActorLifecycleStatus
                    .SucceededReconciledPreparing,
                evaluatedSession,
                evaluation,
                evaluation.Message);
            lastReconcileResult = BuildReconcileResult(
                progressed
                    ? ActivityPlayerActorReconcileStatus.SucceededProgressed
                    : ActivityPlayerActorReconcileStatus.SucceededNoChange,
                appliedSession.Revision,
                playerReadinessRecord.AppliedSessionRevision,
                progressed,
                false,
                true,
                evaluation.Message);
            return lastReconcileResult;
        }

        private ActivityPlayerActorReconcileResult ValidateReconcileRequest(
            ActivityAsset expectedActivity,
            RuntimeContentOwner expectedOwner,
            int expectedOccurrence)
        {
            if (expectedActivity == null ||
                !expectedOwner.IsValid ||
                expectedOccurrence <= 0)
            {
                return BuildRejectedResult(
                    ActivityPlayerActorReconcileStatus.RejectedInvalidRequest,
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    "Reconcile requires an exact Activity, RuntimeContentOwner and positive occurrence sequence.");
            }

            if (playerReadinessRecord == null ||
                playerReadinessRecord.Released)
            {
                return BuildRejectedResult(
                    ActivityPlayerActorReconcileStatus.RejectedNoActiveActivity,
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    "No active Activity Player readiness record is available.");
            }

            if (!ReferenceEquals(
                    playerReadinessRecord.Activity,
                    expectedActivity))
            {
                return BuildRejectedResult(
                    ActivityPlayerActorReconcileStatus
                        .RejectedForeignOrStaleActivity,
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    "Reconcile Activity is foreign or stale.");
            }

            if (playerReadinessRecord.Owner != expectedOwner ||
                !playerReadinessRecord.ScopeContext.IsValid ||
                playerReadinessRecord.ScopeContext.Owner != expectedOwner)
            {
                return BuildRejectedResult(
                    ActivityPlayerActorReconcileStatus
                        .RejectedForeignOrStaleOwner,
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    "Reconcile RuntimeContentOwner or retained Activity scope context is foreign or stale.");
            }

            if (playerReadinessRecord.Occurrence != expectedOccurrence ||
                playerReadinessParticipant == null ||
                playerReadinessParticipant.Occurrence != expectedOccurrence)
            {
                return BuildRejectedResult(
                    ActivityPlayerActorReconcileStatus
                        .RejectedForeignOrStaleOccurrence,
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    "Reconcile occurrence is foreign, stale or has not started.");
            }

            return null;
        }

        private bool HasReconcileRevisionDelta(
            PlayerParticipationSnapshot session)
        {
            if (session.Revision !=
                playerReadinessRecord.AppliedSessionRevision)
            {
                return true;
            }

            for (int recordIndex = 0;
                 recordIndex < playerReadinessRecord.ProjectedSlots.Count;
                 recordIndex++)
            {
                PlayerReadinessSlotRecord record =
                    playerReadinessRecord.ProjectedSlots[recordIndex];
                if (!TryFindSlot(
                        session,
                        record.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot slot) ||
                    slot.Revision != record.SlotRevision ||
                    slot.SelectionRevision != record.SelectionRevision)
                {
                    return true;
                }
            }

            return false;
        }

        private ActivityPlayerAdmissionEvaluationResult
            EvaluateCurrentPlayerAdmission(
                out PlayerParticipationSnapshot session,
                out PlayerActorPreparationSnapshot preparation,
                out PlayerGameplayAdmissionSnapshot gameplay)
        {
            session = participationContext.CreateSnapshot();
            preparation = null;
            gameplay = null;

            if ((int)playerReadinessRecord.RequirementLevel >=
                (int)PlayerParticipationRequirementLevel
                    .LogicalActorsPrepared)
            {
                preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot
                        preparationHost);
                preparation = preparationHost?.Preparation;
            }

            if ((int)playerReadinessRecord.RequirementLevel >=
                (int)PlayerParticipationRequirementLevel.GameplayReady &&
                preparationModule.TryGetPlayerGameplayRuntime(
                    out PlayerGameplayRuntimeHostModule gameplayRuntime,
                    out _))
            {
                gameplayRuntime.TryGetSnapshot(
                    out PlayerGameplayRuntimeHostSnapshot gameplayHost);
                gameplay = gameplayHost?.Admission;
            }

            return ActivityPlayerAdmissionEvaluator.Evaluate(
                playerReadinessRecord.Activity,
                session,
                preparation,
                gameplay);
        }

        private static bool IsOnlyBlockedByCurrentEntryGate(
            ActivityPlayerAdmissionEvaluationResult evaluation)
        {
            if (evaluation == null ||
                !evaluation.IsPendingResolution ||
                evaluation.ProjectedSlotCount == 0)
            {
                return false;
            }

            for (int index = 0; index < evaluation.Slots.Count; index++)
            {
                ActivityPlayerAdmissionSlotResult slot =
                    evaluation.Slots[index];
                if (slot.IsSatisfied)
                {
                    continue;
                }

                if (!slot.IsPendingResolution ||
                    slot.Code != ActivityPlayerAdmissionEvaluationCode
                        .GameplayAdmissionBlockedByInputGate)
                {
                    return false;
                }
            }

            return true;
        }

        private void MarkAllReadinessSlotsSatisfied(string message)
        {
            if (playerReadinessRecord == null)
            {
                return;
            }

            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    playerReadinessRecord.ProjectedSlots[index];
                record.ReadinessReason =
                    ActivityPlayerActorReadinessReason.RequirementSatisfied;
                record.Message = message ?? string.Empty;
            }
        }

        private void RefreshReadinessSlotRevisions(
            PlayerParticipationSnapshot session)
        {
            if (session == null || playerReadinessRecord == null)
            {
                return;
            }

            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    playerReadinessRecord.ProjectedSlots[index];
                if (!TryFindSlot(
                        session,
                        record.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot slot))
                {
                    continue;
                }

                record.SlotRevision = slot.Revision;
                record.SelectionRevision = slot.SelectionRevision;
                record.Joined = slot.IsJoined;
                record.Selected = slot.HasSelectedActor;
            }
        }

        private void ApplyPendingEvaluation(
            ActivityPlayerAdmissionEvaluationResult evaluation)
        {
            ActivityPlayerActorReadinessReason aggregate =
                ActivityPlayerActorReadinessReason.RequirementSatisfied;
            for (int index = 0; index < evaluation.Slots.Count; index++)
            {
                ActivityPlayerAdmissionSlotResult evaluated =
                    evaluation.Slots[index];
                PlayerReadinessSlotRecord record =
                    FindReadinessSlot(evaluated.PlayerSlotId);
                if (record == null)
                {
                    continue;
                }

                record.ReadinessReason =
                    MapMissingRequirement(evaluated.MissingRequirement);
                record.Message = evaluated.Message;
                if (record.ReadinessReason < aggregate)
                {
                    aggregate = record.ReadinessReason;
                }
            }

            playerReadinessRecord.ReadinessReason = aggregate;
            playerReadinessRecord.Message = evaluation.Message;
        }

        private ActivityPlayerActorReconcileResult
            FailAndRollbackReconcile(
                ActivityPlayerActorReconcileStatus status,
                int requestedSessionRevision,
                List<ReconcilePassDelta> deltas,
                PlayerGameplayRuntimeHostModule gameplayRuntime,
                string source,
                string reason,
                string issue)
        {
            bool rollbackAttempted = deltas.Count > 0;
            bool rollbackSucceeded = RollbackReconcileDeltas(
                deltas,
                gameplayRuntime,
                source,
                reason,
                out string rollbackIssue);
            ActivityPlayerActorReconcileStatus finalStatus =
                rollbackAttempted && !rollbackSucceeded
                    ? ActivityPlayerActorReconcileStatus.FailedRollback
                    : status;
            string finalIssue = rollbackAttempted && !rollbackSucceeded
                ? issue + " Rollback failures: " + rollbackIssue
                : issue;
            return PublishReconcileFailure(
                finalStatus,
                requestedSessionRevision,
                finalIssue,
                rollbackAttempted,
                rollbackSucceeded);
        }

        private bool RollbackReconcileDeltas(
            List<ReconcilePassDelta> deltas,
            PlayerGameplayRuntimeHostModule gameplayRuntime,
            string source,
            string reason,
            out string issue)
        {
            var issues = new List<string>();
            for (int index = deltas.Count - 1; index >= 0; index--)
            {
                ReconcilePassDelta delta = deltas[index];
                if (delta.GameplayApplied &&
                    delta.GameplayAdmissionToken.IsValid)
                {
                    if (gameplayRuntime == null)
                    {
                        issues.Add(
                            $"Gameplay rollback runtime is unavailable for Slot '{delta.PlayerSlotId.StableText}'.");
                    }
                    else
                    {
                        PlayerGameplayRuntimeOperationResult release =
                            gameplayRuntime.TryReleaseCurrentGameplay(
                                delta.PlayerSlotId,
                                delta.GameplayAdmissionToken,
                                source,
                                reason + "; rollback-gameplay");
                        if (release == null || !release.Succeeded)
                        {
                            issues.Add(release != null
                                ? release.ToDiagnosticString()
                                : $"Gameplay rollback returned no result for Slot '{delta.PlayerSlotId.StableText}'.");
                        }
                        else
                        {
                            PlayerReadinessSlotRecord record =
                                FindReadinessSlot(delta.PlayerSlotId);
                            if (record != null)
                            {
                                record.GameplayAdmitted = false;
                                record.GameplayReady = false;
                                record.GameplayAdmissionToken = default;
                                record.GameplayCreatedByLifecycle = false;
                            }
                        }
                    }
                }

                if (delta.PreparationApplied &&
                    delta.PreparationToken.IsValid)
                {
                    PlayerActorPreparationResult release =
                        preparationModule.TryReleasePreparedActor(
                            delta.PlayerSlotId,
                            delta.PreparationToken,
                            source,
                            reason + "; rollback-preparation");
                    if (release == null || !release.Succeeded)
                    {
                        issues.Add(release != null
                            ? release.ToDiagnosticString()
                            : $"Preparation rollback returned no result for Slot '{delta.PlayerSlotId.StableText}'.");
                    }
                    else
                    {
                        PlayerReadinessSlotRecord record =
                            FindReadinessSlot(delta.PlayerSlotId);
                        if (record != null)
                        {
                            record.Prepared = false;
                            record.PreparationToken = default;
                            record.PreparationCreatedByLifecycle = false;
                        }
                    }
                }

                if (delta.SelectionApplied)
                {
                    PlayerActorSelectionResult clear =
                        preparationModule.TryClearActorSelection(
                            new PlayerActorSelectionRequest(
                                delta.PlayerSlotId,
                                null,
                                source,
                                reason + "; rollback-selection",
                                delta.SelectionRevision));
                    if (clear == null || !clear.Succeeded)
                    {
                        issues.Add(clear != null
                            ? clear.ToDiagnosticString()
                            : $"Selection rollback returned no result for Slot '{delta.PlayerSlotId.StableText}'.");
                    }
                    else
                    {
                        PlayerReadinessSlotRecord record =
                            FindReadinessSlot(delta.PlayerSlotId);
                        if (record != null)
                        {
                            record.Selected = false;
                            record.SelectionRevision = clear.SelectionRevision;
                            record.SelectionCreatedByLifecycle = false;
                        }
                    }
                }
            }

            PlayerParticipationSnapshot session =
                participationContext.CreateSnapshot();
            if (session != null && session.IsInitialized)
            {
                RefreshReadinessSlotRevisions(session);
                playerReadinessRecord.AppliedSessionRevision =
                    session.Revision;
            }

            RebuildActiveRecordFromReadiness(session);
            issue = issues.Count == 0
                ? string.Empty
                : string.Join(" | ", issues);
            return issues.Count == 0;
        }

        private ActivityPlayerActorReconcileResult PublishReconcileFailure(
            ActivityPlayerActorReconcileStatus status,
            int requestedSessionRevision,
            string issue,
            bool rollbackAttempted,
            bool rollbackSucceeded)
        {
            FailPlayerReadinessContribution(issue);
            lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                status == ActivityPlayerActorReconcileStatus.FailedRollback
                    ? ActivityPlayerActorLifecycleStatus.FailedRollback
                    : ActivityPlayerActorLifecycleStatus.FailedReconcile,
                playerReadinessRecord != null
                    ? playerReadinessRecord.Activity.ActivityName
                    : string.Empty,
                playerReadinessRecord != null
                    ? playerReadinessRecord.Owner
                    : default,
                playerReadinessRecord != null
                    ? playerReadinessRecord.RequirementLevel
                    : PlayerParticipationRequirementLevel.None,
                playerReadinessRecord?.ProjectedSlots.Count ?? 0,
                CountSelectedSlots(),
                CountPreparedSlots(),
                0,
                1,
                BuildLifecycleSlotEvidence(),
                issue);
            lastReconcileResult = BuildReconcileResult(
                status,
                requestedSessionRevision,
                playerReadinessRecord?.AppliedSessionRevision ?? 0,
                true,
                rollbackAttempted,
                rollbackSucceeded,
                issue);
            return lastReconcileResult;
        }

        private ActivityPlayerActorReconcileResult BuildRejectedResult(
            ActivityPlayerActorReconcileStatus status,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            int occurrence,
            string message)
        {
            return new ActivityPlayerActorReconcileResult(
                status,
                activity != null ? activity.ActivityName : string.Empty,
                owner,
                occurrence,
                0,
                playerReadinessRecord?.AppliedSessionRevision ?? 0,
                playerReadinessRecord?.ProjectedSlots.Count ?? 0,
                CountSatisfiedSlots(),
                CountPendingSlots(),
                CountFailedSlots(),
                playerReadinessRecord?.ReadinessReason ??
                    ActivityPlayerActorReadinessReason.None,
                false,
                false,
                false,
                lastSnapshot,
                message);
        }

        private ActivityPlayerActorReconcileResult BuildReconcileResult(
            ActivityPlayerActorReconcileStatus status,
            int requestedSessionRevision,
            int appliedSessionRevision,
            bool stateChanged,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string message)
        {
            return new ActivityPlayerActorReconcileResult(
                status,
                playerReadinessRecord?.Activity != null
                    ? playerReadinessRecord.Activity.ActivityName
                    : string.Empty,
                playerReadinessRecord != null
                    ? playerReadinessRecord.Owner
                    : default,
                playerReadinessRecord?.Occurrence ?? 0,
                requestedSessionRevision,
                appliedSessionRevision,
                playerReadinessRecord?.ProjectedSlots.Count ?? 0,
                CountSatisfiedSlots(),
                CountPendingSlots(),
                CountFailedSlots(),
                playerReadinessRecord?.ReadinessReason ??
                    ActivityPlayerActorReadinessReason.None,
                stateChanged,
                rollbackAttempted,
                rollbackSucceeded,
                lastSnapshot,
                message);
        }

        private void RebuildActiveRecordFromReadiness(
            PlayerParticipationSnapshot session)
        {
            var prepared = new List<PreparedSlotRecord>();
            var hosts = new List<LocalPlayerHostAuthoring>();
            int selectedCount = 0;
            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    playerReadinessRecord.ProjectedSlots[index];
                if (record.Selected)
                {
                    selectedCount++;
                }

                if (record.PreparationToken.IsValid)
                {
                    prepared.Add(new PreparedSlotRecord(
                        record.PlayerSlotId,
                        record.PreparationToken,
                        record.PreparationCreatedByLifecycle));
                }

                if (record.Joined &&
                    preparationModule.TryGetRegisteredHost(
                        record.PlayerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out _) &&
                    host != null &&
                    !hosts.Contains(host))
                {
                    hosts.Add(host);
                }
            }

            activeRecord = new ActiveActivityRecord(
                playerReadinessRecord.Activity,
                playerReadinessRecord.Owner,
                playerReadinessRecord.RequirementLevel,
                playerReadinessRecord.ProjectedSlots.Count,
                selectedCount,
                prepared,
                hosts);
        }

        private void UpdateLifecycleSnapshot(
            ActivityPlayerActorLifecycleStatus status,
            PlayerParticipationSnapshot session,
            ActivityPlayerAdmissionEvaluationResult evaluation,
            string message)
        {
            lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                status,
                playerReadinessRecord.Activity.ActivityName,
                playerReadinessRecord.Owner,
                playerReadinessRecord.RequirementLevel,
                playerReadinessRecord.ProjectedSlots.Count,
                CountSelectedSlots(),
                CountPreparedSlots(),
                0,
                evaluation != null ? evaluation.FailedSlotCount : 0,
                BuildLifecycleSlotEvidence(),
                message);
        }

        private ActivityPlayerActorSlotLifecycleSnapshot[]
            BuildLifecycleSlotEvidence()
        {
            if (playerReadinessRecord == null)
            {
                return Array.Empty<
                    ActivityPlayerActorSlotLifecycleSnapshot>();
            }

            var evidence =
                new ActivityPlayerActorSlotLifecycleSnapshot[
                    playerReadinessRecord.ProjectedSlots.Count];
            PlayerParticipationSnapshot session =
                participationContext.CreateSnapshot();
            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    playerReadinessRecord.ProjectedSlots[index];
                TryFindSlot(
                    session,
                    record.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot slot);
                evidence[index] =
                    new ActivityPlayerActorSlotLifecycleSnapshot(
                        record.PlayerSlotId,
                        record.Joined,
                        slot.SelectedActorProfileId,
                        record.SelectionCreatedByLifecycle,
                        record.PreparationToken,
                        record.PreparationCreatedByLifecycle,
                        false,
                        record.Prepared
                            ? PlayerActorPreparationStatus
                                .SucceededAlreadyPrepared
                            : PlayerActorPreparationStatus.None,
                        record.Message);
            }

            return evidence;
        }

        private PlayerActorPreparationToken FindPreparedToken(
            PlayerSlotId playerSlotId)
        {
            if (activeRecord == null)
            {
                return default;
            }

            for (int index = 0;
                 index < activeRecord.PreparedSlots.Count;
                 index++)
            {
                PreparedSlotRecord prepared =
                    activeRecord.PreparedSlots[index];
                if (prepared.PlayerSlotId == playerSlotId)
                {
                    return prepared.Token;
                }
            }

            return default;
        }

        private PlayerGameplayAdmissionToken FindGameplayAdmissionToken(
            PlayerSlotId playerSlotId)
        {
            if (!preparationModule.TryGetPlayerGameplayRuntime(
                    out PlayerGameplayRuntimeHostModule gameplay,
                    out _) ||
                !gameplay.TryGetCurrentAdmission(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary admission))
            {
                return default;
            }

            return admission.Token;
        }

        private static bool TryFindSlot(
            PlayerParticipationSnapshot session,
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot slot)
        {
            if (session != null)
            {
                for (int index = 0;
                     index < session.Slots.Count;
                     index++)
                {
                    if (session.Slots[index].PlayerSlotId == playerSlotId)
                    {
                        slot = session.Slots[index];
                        return true;
                    }
                }
            }

            slot = default;
            return false;
        }

        private PlayerReadinessSlotRecord FindReadinessSlot(
            PlayerSlotId playerSlotId)
        {
            if (playerReadinessRecord == null)
            {
                return null;
            }

            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                if (playerReadinessRecord.ProjectedSlots[index]
                        .PlayerSlotId == playerSlotId)
                {
                    return playerReadinessRecord.ProjectedSlots[index];
                }
            }

            return null;
        }

        private static ActivityPlayerActorReadinessReason
            ResolveNextReadinessReason(
                PlayerParticipationRequirementLevel requirementLevel,
                PlayerSlotRuntimeSnapshot slot)
        {
            if ((int)requirementLevel >=
                    (int)PlayerParticipationRequirementLevel.JoinedSlots &&
                !slot.IsJoined)
            {
                return ActivityPlayerActorReadinessReason.WaitingForJoin;
            }

            if ((int)requirementLevel >=
                    (int)PlayerParticipationRequirementLevel.SelectedActors &&
                !slot.HasSelectedActor)
            {
                return ActivityPlayerActorReadinessReason
                    .WaitingForActorSelection;
            }

            if ((int)requirementLevel >=
                (int)PlayerParticipationRequirementLevel
                    .LogicalActorsPrepared)
            {
                return ActivityPlayerActorReadinessReason
                    .PreparingLogicalActor;
            }

            return ActivityPlayerActorReadinessReason
                .RequirementSatisfied;
        }

        private static ActivityPlayerActorReadinessReason
            ResolveAggregateReadinessReason(
                IReadOnlyList<PlayerReadinessSlotRecord> slots)
        {
            ActivityPlayerActorReadinessReason result =
                ActivityPlayerActorReadinessReason.RequirementSatisfied;
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].ReadinessReason < result)
                {
                    result = slots[index].ReadinessReason;
                }
            }

            return result;
        }

        private static ActivityPlayerActorReadinessReason
            MapMissingRequirement(
                ActivityPlayerAdmissionMissingRequirement missing)
        {
            return missing switch
            {
                ActivityPlayerAdmissionMissingRequirement.JoinedSlot =>
                    ActivityPlayerActorReadinessReason.WaitingForJoin,
                ActivityPlayerAdmissionMissingRequirement.SelectedActor =>
                    ActivityPlayerActorReadinessReason
                        .WaitingForActorSelection,
                ActivityPlayerAdmissionMissingRequirement
                    .LogicalActorPrepared =>
                    ActivityPlayerActorReadinessReason
                        .PreparingLogicalActor,
                ActivityPlayerAdmissionMissingRequirement.GameplayReady =>
                    ActivityPlayerActorReadinessReason
                        .PreparingGameplayAdmission,
                _ => ActivityPlayerActorReadinessReason
                    .RequirementSatisfied
            };
        }

        private int CountSelectedSlots()
        {
            int count = 0;
            if (playerReadinessRecord == null)
            {
                return count;
            }

            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                if (playerReadinessRecord.ProjectedSlots[index].Selected)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountPreparedSlots()
        {
            int count = 0;
            if (playerReadinessRecord == null)
            {
                return count;
            }

            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                if (playerReadinessRecord.ProjectedSlots[index].Prepared)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountSatisfiedSlots()
        {
            int count = 0;
            if (playerReadinessRecord == null)
            {
                return count;
            }

            for (int index = 0;
                 index < playerReadinessRecord.ProjectedSlots.Count;
                 index++)
            {
                if (playerReadinessRecord.ProjectedSlots[index]
                        .ReadinessReason ==
                    ActivityPlayerActorReadinessReason
                        .RequirementSatisfied)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountPendingSlots()
        {
            if (playerReadinessRecord == null)
            {
                return 0;
            }

            return Math.Max(
                0,
                playerReadinessRecord.ProjectedSlots.Count -
                    CountSatisfiedSlots() -
                    CountFailedSlots());
        }

        private int CountFailedSlots()
        {
            if (playerReadinessRecord == null ||
                !playerReadinessRecord.Failed)
            {
                return 0;
            }

            return 1;
        }
    }
}
