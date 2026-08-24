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
            internal ActivityAsset activity;
            internal RuntimeContentOwner owner;
            internal RuntimeScopeContext scopeContext;
            internal PlayerParticipationRequirementLevel requirementLevel;
            internal List<PlayerReadinessSlotRecord> projectedSlots;
            internal int enterSessionRevision;
            internal int appliedSessionRevision;
            internal int occurrence;
            internal bool completed;
            internal bool failed;
            internal bool released;
            internal ActivityPlayerActorReadinessReason readinessReason;
            internal string message;
        }

        private sealed class PlayerReadinessSlotRecord
        {
            internal PlayerSlotId playerSlotId;
            internal int slotRevision;
            internal int selectionRevision;
            internal bool joined;
            internal bool selected;
            internal bool prepared;
            internal bool gameplayAdmitted;
            internal bool gameplayReady;
            internal bool selectionCreatedByLifecycle;
            internal bool preparationCreatedByLifecycle;
            internal bool gameplayCreatedByLifecycle;
            internal PlayerActorPreparationToken preparationToken;
            internal PlayerGameplayAdmissionToken gameplayAdmissionToken;
            internal ActivityPlayerActorReadinessReason readinessReason;
            internal string message;
        }

        private sealed class ReconcilePassDelta
        {
            internal PlayerSlotId playerSlotId;
            internal bool selectionApplied;
            internal int selectionRevision;
            internal bool preparationApplied;
            internal PlayerActorPreparationToken preparationToken;
            internal bool gameplayApplied;
            internal PlayerGameplayAdmissionToken gameplayAdmissionToken;
        }

        private ActivityPlayerReadinessRecord _playerReadinessRecord;
        private ActivityPlayerActorReconcileResult _lastReconcileResult;

        internal ActivityPlayerActorReconcileResult LastReconcileResult =>
            _lastReconcileResult;

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
                _participationContext.CreateSnapshot();
            if (session == null || !session.IsInitialized)
            {
                string issue =
                    "Deferred Activity Player readiness requires an initialized Session participation snapshot.";
                _lastSnapshot = FailureSnapshot(
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
                    if (RequiresActivityActorRepresentation(requirementLevel))
                    {
                        if (!_preparationModule.TryGetRegisteredHost(
                                slot.PlayerSlotId,
                                out LocalPlayerHostAuthoring host,
                                out string hostIssue))
                        {
                            string representationIssue =
                                "Activity Actor representation is required but its " +
                                "Host evidence is unavailable. " + hostIssue;
                            _playerReadinessRecord = null;
                            _lastSnapshot = FailureSnapshot(
                                ActivityPlayerActorLifecycleStatus
                                    .FailedRequirement,
                                activity,
                                owner,
                                requirementLevel,
                                new List<PlayerSlotRuntimeSnapshot>(
                                    projectedSlots),
                                representationIssue);
                            return Blocking(
                                request,
                                "activity-player-actor-deferred-representation-evidence-missing",
                                representationIssue);
                        }

                        admittedHosts.Add(host);
                    }
                    else if (_preparationModule.TryGetRegisteredHost(
                                 slot.PlayerSlotId,
                                 out LocalPlayerHostAuthoring optionalHost,
                                 out _))
                    {
                        admittedHosts.Add(optionalHost);
                    }
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
                    playerSlotId = slot.PlayerSlotId,
                    slotRevision = slot.Revision,
                    selectionRevision = slot.SelectionRevision,
                    joined = slot.IsJoined,
                    selected = slot.HasSelectedActor,
                    prepared = false,
                    gameplayAdmitted = false,
                    gameplayReady = false,
                    readinessReason = slotReason,
                    message = slot.IsJoined
                        ? RequiresActivityActorRepresentation(requirementLevel)
                            ? "Player lifecycle will continue through explicit delta reconcile; this Activity requires an Actor representation."
                            : "Player lifecycle will continue through explicit delta reconcile; Session membership does not require an Activity Actor representation at this requirement level."
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
                    slots[index].message);
            }

            _playerReadinessRecord = new ActivityPlayerReadinessRecord
            {
                activity = activity,
                owner = owner,
                scopeContext = request.RuntimeScopeContext,
                requirementLevel = requirementLevel,
                projectedSlots = slots,
                enterSessionRevision = session.Revision,
                appliedSessionRevision = session.Revision,
                occurrence = 0,
                completed = false,
                failed = false,
                released = false,
                readinessReason = ResolveAggregateReadinessReason(slots),
                message = "Activity Player lifecycle entered in a normal Preparing state and awaits explicit delta reconcile."
            };
            SynchronizePlayerReadinessContributionAfterRecordCreated();

            _activeRecord = new ActiveActivityRecord(
                activity,
                owner,
                requirementLevel,
                projectedSlots.Count,
                selectedCount,
                new List<PreparedSlotRecord>(),
                admittedHosts);
            _lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
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
                _playerReadinessRecord.message);
            return ActivityContentExecutionResult.Success(
                request,
                nameof(ActivityPlayerActorLifecycleParticipant),
                "activity-player-actor-entered-preparing",
                _lastSnapshot.ToDiagnosticString());
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
                _playerReadinessRecord = null;
                return;
            }

            PlayerParticipationSnapshot session =
                _participationContext.CreateSnapshot();
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
                    playerSlotId = slot.PlayerSlotId,
                    slotRevision = slot.Revision,
                    selectionRevision = slot.SelectionRevision,
                    joined = slot.IsJoined,
                    selected = slot.HasSelectedActor,
                    prepared =
                        (int)requirementLevel <
                            (int)PlayerParticipationRequirementLevel
                                .LogicalActorsPrepared ||
                        preparationToken.IsValid,
                    gameplayAdmitted =
                        (int)requirementLevel <
                            (int)PlayerParticipationRequirementLevel.GameplayReady ||
                        admissionToken.IsValid,
                    gameplayReady = true,
                    preparationToken = preparationToken,
                    gameplayAdmissionToken = admissionToken,
                    readinessReason =
                        ActivityPlayerActorReadinessReason.RequirementSatisfied,
                    message = RequiresActivityActorRepresentation(requirementLevel)
                        ? "Player requirement was satisfied with an Activity Actor representation prepared for this occurrence."
                        : "Session Player requirement was satisfied without requiring an Activity Actor representation."
                });
            }

            int revision = session != null && session.IsInitialized
                ? session.Revision
                : 0;
            _playerReadinessRecord = new ActivityPlayerReadinessRecord
            {
                activity = activity,
                owner = owner,
                scopeContext = request.RuntimeScopeContext,
                requirementLevel = requirementLevel,
                projectedSlots = slots,
                enterSessionRevision = revision,
                appliedSessionRevision = revision,
                occurrence = 0,
                completed = true,
                failed = false,
                released = false,
                readinessReason =
                    ActivityPlayerActorReadinessReason.RequirementSatisfied,
                message =
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
                _lastReconcileResult = rejected;
                return rejected;
            }

            PlayerParticipationSnapshot session =
                _participationContext.CreateSnapshot();
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
                _playerReadinessRecord.appliedSessionRevision)
            {
                return PublishReconcileFailure(
                    ActivityPlayerActorReconcileStatus.FailedProjection,
                    session.Revision,
                    $"Session revision regressed from '{_playerReadinessRecord.appliedSessionRevision}' to '{session.Revision}'.",
                    false,
                    false);
            }

            if (_playerReadinessRecord.completed)
            {
                bool projectedSlotRevisionChanged =
                    HasProjectedSlotRevisionDelta(session);
                if (!projectedSlotRevisionChanged)
                {
                    _playerReadinessRecord.appliedSessionRevision =
                        session.Revision;
                }

                _lastReconcileResult = BuildReconcileResult(
                    ActivityPlayerActorReconcileStatus.SucceededNoChange,
                    session.Revision,
                    _playerReadinessRecord.appliedSessionRevision,
                    false,
                    false,
                    true,
                    projectedSlotRevisionChanged
                        ? "Activity Player readiness is already complete; the completed occurrence retains its existing applied revision because a projected Slot changed."
                        : "Activity Player readiness is already complete; the Session revision changed only outside the frozen Activity projection and was acknowledged without lifecycle mutation.");
                return _lastReconcileResult;
            }

            if (!HasReconcileRevisionDelta(session))
            {
                _lastReconcileResult = BuildReconcileResult(
                    ActivityPlayerActorReconcileStatus.SucceededNoChange,
                    session.Revision,
                    _playerReadinessRecord.appliedSessionRevision,
                    false,
                    false,
                    true,
                    "Session and projected Slot revisions have not changed.");
                return _lastReconcileResult;
            }

            var deltas = new List<ReconcilePassDelta>();
            bool progressed = false;
            PlayerGameplayRuntimeHostModule gameplayRuntime = null;
            string gameplayRuntimeIssue = string.Empty;

            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord slotRecord =
                    _playerReadinessRecord.projectedSlots[index];
                if (!TryFindSlot(
                        session,
                        slotRecord.playerSlotId,
                        out PlayerSlotRuntimeSnapshot slot))
                {
                    return FailAndRollbackReconcile(
                        ActivityPlayerActorReconcileStatus.FailedProjection,
                        session.Revision,
                        deltas,
                        gameplayRuntime,
                        resolvedSource,
                        resolvedReason,
                        $"Projected Player Slot '{slotRecord.playerSlotId.StableText}' is no longer configured in the Session.");
                }

                if (slot.Revision < slotRecord.slotRevision ||
                    slot.SelectionRevision <
                        slotRecord.selectionRevision)
                {
                    return FailAndRollbackReconcile(
                        ActivityPlayerActorReconcileStatus.FailedProjection,
                        session.Revision,
                        deltas,
                        gameplayRuntime,
                        resolvedSource,
                        resolvedReason,
                        $"Projected Player Slot '{slot.PlayerSlotId.StableText}' " +
                        $"revision regressed. previousSlotRevision='{slotRecord.slotRevision}' " +
                        $"currentSlotRevision='{slot.Revision}' " +
                        $"previousSelectionRevision='{slotRecord.selectionRevision}' " +
                        $"currentSelectionRevision='{slot.SelectionRevision}'.");
                }

                slotRecord.slotRevision = slot.Revision;
                slotRecord.selectionRevision = slot.SelectionRevision;
                slotRecord.joined = slot.IsJoined;
                slotRecord.selected = slot.HasSelectedActor;

                if ((int)_playerReadinessRecord.requirementLevel >=
                        (int)PlayerParticipationRequirementLevel.JoinedSlots &&
                    !slot.IsJoined)
                {
                    slotRecord.readinessReason =
                        ActivityPlayerActorReadinessReason.WaitingForJoin;
                    slotRecord.message = "Projected Player Slot is waiting for Join.";
                    continue;
                }

                if (RequiresActivityActorRepresentation(
                        _playerReadinessRecord.requirementLevel) &&
                    !_preparationModule.TryGetRegisteredHost(
                        slot.PlayerSlotId,
                        out _,
                        out string hostIssue))
                {
                    return FailAndRollbackReconcile(
                        ActivityPlayerActorReconcileStatus.FailedHostEvidence,
                        session.Revision,
                        deltas,
                        gameplayRuntime,
                        resolvedSource,
                        resolvedReason,
                        "Activity Actor representation is required but its " +
                        "Host evidence is unavailable. " + hostIssue);
                }

                if ((int)_playerReadinessRecord.requirementLevel >=
                    (int)PlayerParticipationRequirementLevel.SelectedActors)
                {
                    if (!slot.HasSelectedActor)
                    {
                        slotRecord.readinessReason =
                            ActivityPlayerActorReadinessReason
                                .WaitingForActorSelection;
                        PlayerActorSelectionResult selection =
                            _preparationModule.TrySelectDefaultActor(
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
                        slotRecord.selected = slot.HasSelectedActor;
                        slotRecord.selectionRevision = slot.SelectionRevision;
                        slotRecord.selectionCreatedByLifecycle |=
                            selection.StateChanged;
                        if (selection.StateChanged)
                        {
                            deltas.Add(new ReconcilePassDelta
                            {
                                playerSlotId = slot.PlayerSlotId,
                                selectionApplied = true,
                                selectionRevision =
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

                if ((int)_playerReadinessRecord.requirementLevel >=
                    (int)PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared)
                {
                    slotRecord.readinessReason =
                        ActivityPlayerActorReadinessReason
                            .PreparingLogicalActor;
                    PlayerActorPreparationResult preparation =
                        _preparationModule.TryEnsureSessionPhysicalActor(
                            _playerReadinessRecord.scopeContext,
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

                    if (!_preparationModule.TryApplyCurrentActivityRelocation(
                            _playerReadinessRecord.scopeContext.Owner,
                            slot.PlayerSlotId,
                            preparation.CurrentSummary.Token,
                            out string relocationIssue))
                    {
                        return FailAndRollbackReconcile(
                            ActivityPlayerActorReconcileStatus.FailedPreparation,
                            session.Revision, deltas, gameplayRuntime,
                            resolvedSource, resolvedReason,
                            "Activity explicit Player relocation failed. " + relocationIssue);
                    }

                    bool preparationApplied =
                        preparation.Status ==
                        PlayerActorPreparationStatus.SucceededPrepared;
                    slotRecord.preparationToken =
                        preparation.CurrentSummary.Token;
                    slotRecord.prepared =
                        preparation.CurrentSummary.IsPrepared;
                    slotRecord.preparationCreatedByLifecycle |=
                        preparationApplied;
                    if (preparationApplied)
                    {
                        deltas.Add(new ReconcilePassDelta
                        {
                            playerSlotId = slot.PlayerSlotId,
                            preparationApplied = true,
                            preparationToken =
                                preparation.CurrentSummary.Token
                        });
                        progressed = true;
                    }
                }

                if ((int)_playerReadinessRecord.requirementLevel >=
                    (int)PlayerParticipationRequirementLevel.GameplayReady)
                {
                    slotRecord.readinessReason =
                        ActivityPlayerActorReadinessReason
                            .PreparingGameplayAdmission;
                    if (gameplayRuntime == null &&
                        !_preparationModule.TryGetPlayerGameplayRuntime(
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
                            _playerReadinessRecord.owner,
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
                    slotRecord.gameplayAdmissionToken =
                        gameplay.CurrentAdmission.Token;
                    slotRecord.gameplayAdmitted =
                        gameplay.CurrentAdmission.IsAdmitted;
                    slotRecord.gameplayReady = gameplay.GameplayReady;
                    slotRecord.gameplayCreatedByLifecycle |=
                        gameplayApplied;
                    if (gameplayApplied)
                    {
                        deltas.Add(new ReconcilePassDelta
                        {
                            playerSlotId = slot.PlayerSlotId,
                            gameplayApplied = true,
                            gameplayAdmissionToken =
                                gameplay.CurrentAdmission.Token
                        });
                        progressed = true;
                    }
                }

                slotRecord.readinessReason =
                    ActivityPlayerActorReadinessReason.RequirementSatisfied;
                slotRecord.message = RequiresActivityActorRepresentation(
                        _playerReadinessRecord.requirementLevel)
                    ? "Player lifecycle delta is satisfied with an Activity Actor representation prepared for this occurrence."
                    : "Session Player requirement is satisfied; no physical Activity Actor representation is required at this level.";
            }

            PlayerParticipationSnapshot appliedSession =
                _participationContext.CreateSnapshot();
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
            _playerReadinessRecord.appliedSessionRevision =
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
                    _playerReadinessRecord.message);
                _lastReconcileResult = BuildReconcileResult(
                    ActivityPlayerActorReconcileStatus.SucceededCompleted,
                    appliedSession.Revision,
                    _playerReadinessRecord.appliedSessionRevision,
                    true,
                    false,
                    true,
                    _playerReadinessRecord.message);
                return _lastReconcileResult;
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
            _lastReconcileResult = BuildReconcileResult(
                progressed
                    ? ActivityPlayerActorReconcileStatus.SucceededProgressed
                    : ActivityPlayerActorReconcileStatus.SucceededNoChange,
                appliedSession.Revision,
                _playerReadinessRecord.appliedSessionRevision,
                progressed,
                false,
                true,
                evaluation.Message);
            return _lastReconcileResult;
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

            if (_playerReadinessRecord == null ||
                _playerReadinessRecord.released)
            {
                return BuildRejectedResult(
                    ActivityPlayerActorReconcileStatus.RejectedNoActiveActivity,
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    "No active Activity Player readiness record is available.");
            }

            if (!ReferenceEquals(
                    _playerReadinessRecord.activity,
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

            if (_playerReadinessRecord.owner != expectedOwner ||
                !_playerReadinessRecord.scopeContext.IsValid ||
                _playerReadinessRecord.scopeContext.Owner != expectedOwner)
            {
                return BuildRejectedResult(
                    ActivityPlayerActorReconcileStatus
                        .RejectedForeignOrStaleOwner,
                    expectedActivity,
                    expectedOwner,
                    expectedOccurrence,
                    "Reconcile RuntimeContentOwner or retained Activity scope context is foreign or stale.");
            }

            if (_playerReadinessRecord.occurrence != expectedOccurrence ||
                _playerReadinessParticipant == null ||
                _playerReadinessParticipant.Occurrence != expectedOccurrence)
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

        private bool HasProjectedSlotRevisionDelta(
            PlayerParticipationSnapshot session)
        {
            if (session == null)
            {
                return true;
            }

            for (int recordIndex = 0;
                 recordIndex < _playerReadinessRecord.projectedSlots.Count;
                 recordIndex++)
            {
                PlayerReadinessSlotRecord record =
                    _playerReadinessRecord.projectedSlots[recordIndex];
                if (!TryFindSlot(
                        session,
                        record.playerSlotId,
                        out PlayerSlotRuntimeSnapshot slot) ||
                    slot.Revision != record.slotRevision ||
                    slot.SelectionRevision != record.selectionRevision)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasReconcileRevisionDelta(
            PlayerParticipationSnapshot session)
        {
            return
                session.Revision !=
                    _playerReadinessRecord.appliedSessionRevision ||
                HasProjectedSlotRevisionDelta(session);
        }

        private ActivityPlayerAdmissionEvaluationResult
            EvaluateCurrentPlayerAdmission(
                out PlayerParticipationSnapshot session,
                out PlayerActorPreparationSnapshot preparation,
                out PlayerGameplayAdmissionSnapshot gameplay)
        {
            session = _participationContext.CreateSnapshot();
            preparation = null;
            gameplay = null;

            if ((int)_playerReadinessRecord.requirementLevel >=
                (int)PlayerParticipationRequirementLevel
                    .LogicalActorsPrepared)
            {
                _preparationModule.TryGetSnapshot(
                    out PlayerActorPreparationRuntimeHostSnapshot
                        preparationHost);
                preparation = preparationHost?.Preparation;
            }

            if ((int)_playerReadinessRecord.requirementLevel >=
                (int)PlayerParticipationRequirementLevel.GameplayReady &&
                _preparationModule.TryGetPlayerGameplayRuntime(
                    out PlayerGameplayRuntimeHostModule gameplayRuntime,
                    out _))
            {
                gameplayRuntime.TryGetSnapshot(
                    out PlayerGameplayRuntimeHostSnapshot gameplayHost);
                gameplay = gameplayHost?.Admission;
            }

            return ActivityPlayerAdmissionEvaluator.Evaluate(
                _playerReadinessRecord.activity,
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
            if (_playerReadinessRecord == null)
            {
                return;
            }

            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    _playerReadinessRecord.projectedSlots[index];
                record.readinessReason =
                    ActivityPlayerActorReadinessReason.RequirementSatisfied;
                record.message = message ?? string.Empty;
            }
        }

        private void RefreshReadinessSlotRevisions(
            PlayerParticipationSnapshot session)
        {
            if (session == null || _playerReadinessRecord == null)
            {
                return;
            }

            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    _playerReadinessRecord.projectedSlots[index];
                if (!TryFindSlot(
                        session,
                        record.playerSlotId,
                        out PlayerSlotRuntimeSnapshot slot))
                {
                    continue;
                }

                record.slotRevision = slot.Revision;
                record.selectionRevision = slot.SelectionRevision;
                record.joined = slot.IsJoined;
                record.selected = slot.HasSelectedActor;
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

                record.readinessReason =
                    MapMissingRequirement(evaluated.MissingRequirement);
                record.message = evaluated.Message;
                if (record.readinessReason < aggregate)
                {
                    aggregate = record.readinessReason;
                }
            }

            _playerReadinessRecord.readinessReason = aggregate;
            _playerReadinessRecord.message = evaluation.Message;
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
                if (delta.gameplayApplied &&
                    delta.gameplayAdmissionToken.IsValid)
                {
                    if (gameplayRuntime == null)
                    {
                        issues.Add(
                            $"Gameplay rollback runtime is unavailable for Slot '{delta.playerSlotId.StableText}'.");
                    }
                    else
                    {
                        PlayerGameplayRuntimeOperationResult release =
                            gameplayRuntime.TryReleaseCurrentGameplay(
                                delta.playerSlotId,
                                delta.gameplayAdmissionToken,
                                source,
                                reason + "; rollback-gameplay");
                        if (release == null || !release.Succeeded)
                        {
                            issues.Add(release != null
                                ? release.ToDiagnosticString()
                                : $"Gameplay rollback returned no result for Slot '{delta.playerSlotId.StableText}'.");
                        }
                        else
                        {
                            PlayerReadinessSlotRecord record =
                                FindReadinessSlot(delta.playerSlotId);
                            if (record != null)
                            {
                                record.gameplayAdmitted = false;
                                record.gameplayReady = false;
                                record.gameplayAdmissionToken = default;
                                record.gameplayCreatedByLifecycle = false;
                            }
                        }
                    }
                }

                if (delta.selectionApplied)
                {
                    bool physicalCommitted = false;
                    for (int deltaIndex = 0;
                         deltaIndex < deltas.Count;
                         deltaIndex++)
                    {
                        ReconcilePassDelta preparedDelta = deltas[deltaIndex];
                        if (preparedDelta.playerSlotId == delta.playerSlotId &&
                            preparedDelta.preparationApplied &&
                            preparedDelta.preparationToken.IsValid)
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
                        _preparationModule.TryClearActorSelection(
                            new PlayerActorSelectionRequest(
                                delta.playerSlotId,
                                null,
                                source,
                                reason + "; rollback-selection",
                                delta.selectionRevision));
                    if (clear == null || !clear.Succeeded)
                    {
                        issues.Add(clear != null
                            ? clear.ToDiagnosticString()
                            : $"Selection rollback returned no result for Slot '{delta.playerSlotId.StableText}'.");
                    }
                    else
                    {
                        PlayerReadinessSlotRecord record =
                            FindReadinessSlot(delta.playerSlotId);
                        if (record != null)
                        {
                            record.selected = false;
                            record.selectionRevision = clear.SelectionRevision;
                            record.selectionCreatedByLifecycle = false;
                        }
                    }
                }
            }

            PlayerParticipationSnapshot session =
                _participationContext.CreateSnapshot();
            if (session != null && session.IsInitialized)
            {
                RefreshReadinessSlotRevisions(session);
                _playerReadinessRecord.appliedSessionRevision =
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
            _lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                status == ActivityPlayerActorReconcileStatus.FailedRollback
                    ? ActivityPlayerActorLifecycleStatus.FailedRollback
                    : ActivityPlayerActorLifecycleStatus.FailedReconcile,
                _playerReadinessRecord != null
                    ? _playerReadinessRecord.activity.ActivityName
                    : string.Empty,
                _playerReadinessRecord != null
                    ? _playerReadinessRecord.owner
                    : default,
                _playerReadinessRecord != null
                    ? _playerReadinessRecord.requirementLevel
                    : PlayerParticipationRequirementLevel.None,
                _playerReadinessRecord?.projectedSlots.Count ?? 0,
                CountSelectedSlots(),
                CountPreparedSlots(),
                0,
                1,
                BuildLifecycleSlotEvidence(),
                issue);
            _lastReconcileResult = BuildReconcileResult(
                status,
                requestedSessionRevision,
                _playerReadinessRecord?.appliedSessionRevision ?? 0,
                true,
                rollbackAttempted,
                rollbackSucceeded,
                issue);
            return _lastReconcileResult;
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
                _playerReadinessRecord?.appliedSessionRevision ?? 0,
                _playerReadinessRecord?.projectedSlots.Count ?? 0,
                CountSatisfiedSlots(),
                CountPendingSlots(),
                CountFailedSlots(),
                _playerReadinessRecord?.readinessReason ??
                    ActivityPlayerActorReadinessReason.None,
                false,
                false,
                false,
                _lastSnapshot,
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
                _playerReadinessRecord?.activity != null
                    ? _playerReadinessRecord.activity.ActivityName
                    : string.Empty,
                _playerReadinessRecord != null
                    ? _playerReadinessRecord.owner
                    : default,
                _playerReadinessRecord?.occurrence ?? 0,
                requestedSessionRevision,
                appliedSessionRevision,
                _playerReadinessRecord?.projectedSlots.Count ?? 0,
                CountSatisfiedSlots(),
                CountPendingSlots(),
                CountFailedSlots(),
                _playerReadinessRecord?.readinessReason ??
                    ActivityPlayerActorReadinessReason.None,
                stateChanged,
                rollbackAttempted,
                rollbackSucceeded,
                _lastSnapshot,
                message);
        }

        private void RebuildActiveRecordFromReadiness(
            PlayerParticipationSnapshot session)
        {
            var prepared = new List<PreparedSlotRecord>();
            var hosts = new List<LocalPlayerHostAuthoring>();
            int selectedCount = 0;
            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    _playerReadinessRecord.projectedSlots[index];
                if (record.selected)
                {
                    selectedCount++;
                }

                if (record.preparationToken.IsValid)
                {
                    prepared.Add(new PreparedSlotRecord(
                        record.playerSlotId,
                        record.preparationToken,
                        record.preparationCreatedByLifecycle));
                }

                if (record.joined &&
                    _preparationModule.TryGetRegisteredHost(
                        record.playerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out _) &&
                    host != null &&
                    !hosts.Contains(host))
                {
                    hosts.Add(host);
                }
            }

            _activeRecord = new ActiveActivityRecord(
                _playerReadinessRecord.activity,
                _playerReadinessRecord.owner,
                _playerReadinessRecord.requirementLevel,
                _playerReadinessRecord.projectedSlots.Count,
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
            _lastSnapshot = new ActivityPlayerActorLifecycleSnapshot(
                status,
                _playerReadinessRecord.activity.ActivityName,
                _playerReadinessRecord.owner,
                _playerReadinessRecord.requirementLevel,
                _playerReadinessRecord.projectedSlots.Count,
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
            if (_playerReadinessRecord == null)
            {
                return Array.Empty<
                    ActivityPlayerActorSlotLifecycleSnapshot>();
            }

            var evidence =
                new ActivityPlayerActorSlotLifecycleSnapshot[
                    _playerReadinessRecord.projectedSlots.Count];
            PlayerParticipationSnapshot session =
                _participationContext.CreateSnapshot();
            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                PlayerReadinessSlotRecord record =
                    _playerReadinessRecord.projectedSlots[index];
                TryFindSlot(
                    session,
                    record.playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot);
                evidence[index] =
                    new ActivityPlayerActorSlotLifecycleSnapshot(
                        record.playerSlotId,
                        record.joined,
                        slot.SelectedActorProfileId,
                        record.selectionCreatedByLifecycle,
                        record.preparationToken,
                        record.preparationCreatedByLifecycle,
                        false,
                        record.prepared
                            ? PlayerActorPreparationStatus
                                .SucceededAlreadyPrepared
                            : PlayerActorPreparationStatus.None,
                        record.message);
            }

            return evidence;
        }

        private PlayerActorPreparationToken FindPreparedToken(
            PlayerSlotId playerSlotId)
        {
            if (_activeRecord == null)
            {
                return default;
            }

            for (int index = 0;
                 index < _activeRecord.PreparedSlots.Count;
                 index++)
            {
                PreparedSlotRecord prepared =
                    _activeRecord.PreparedSlots[index];
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
            if (!_preparationModule.TryGetPlayerGameplayRuntime(
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
            if (_playerReadinessRecord == null)
            {
                return null;
            }

            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                if (_playerReadinessRecord.projectedSlots[index]
                        .playerSlotId == playerSlotId)
                {
                    return _playerReadinessRecord.projectedSlots[index];
                }
            }

            return null;
        }

        private static bool RequiresActivityActorRepresentation(
            PlayerParticipationRequirementLevel requirementLevel)
        {
            return (int)requirementLevel >=
                (int)PlayerParticipationRequirementLevel
                    .LogicalActorsPrepared;
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
                if (slots[index].readinessReason < result)
                {
                    result = slots[index].readinessReason;
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
            if (_playerReadinessRecord == null)
            {
                return count;
            }

            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                if (_playerReadinessRecord.projectedSlots[index].selected)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountPreparedSlots()
        {
            int count = 0;
            if (_playerReadinessRecord == null)
            {
                return count;
            }

            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                if (_playerReadinessRecord.projectedSlots[index].prepared)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountSatisfiedSlots()
        {
            int count = 0;
            if (_playerReadinessRecord == null)
            {
                return count;
            }

            for (int index = 0;
                 index < _playerReadinessRecord.projectedSlots.Count;
                 index++)
            {
                if (_playerReadinessRecord.projectedSlots[index]
                        .readinessReason ==
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
            if (_playerReadinessRecord == null)
            {
                return 0;
            }

            return Math.Max(
                0,
                _playerReadinessRecord.projectedSlots.Count -
                    CountSatisfiedSlots() -
                    CountFailedSlots());
        }

        private int CountFailedSlots()
        {
            if (_playerReadinessRecord == null ||
                !_playerReadinessRecord.failed)
            {
                return 0;
            }

            return 1;
        }
    }
}
