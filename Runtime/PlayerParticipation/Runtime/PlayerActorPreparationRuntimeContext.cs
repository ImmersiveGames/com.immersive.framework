using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped authority for Logical Player Actor preparation state.
    /// It coordinates selected ActorProfiles, attached Unity materialization and immutable
    /// per-Slot preparation evidence without becoming a global service or physical object registry.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3J.4 Session Logical Player Actor preparation authority.")]
    internal sealed partial class PlayerActorPreparationRuntimeContext
    {
        private sealed class PreparationRecord
        {
            internal PreparationRecord(
                PlayerActorMaterializationHandle handle,
                PlayerActorPreparationSummary summary)
            {
                Handle = handle ?? throw new ArgumentNullException(nameof(handle));
                Summary = summary;
            }

            internal PlayerActorMaterializationHandle Handle { get; }
            internal PlayerActorPreparationSummary Summary { get; set; }
        }

        private sealed class RetainedReleaseFailure
        {
            internal RetainedReleaseFailure(
                PlayerActorMaterializationHandle handle,
                string issue)
            {
                Handle = handle ?? throw new ArgumentNullException(nameof(handle));
                Issue = issue.NormalizeText();
            }

            internal PlayerActorMaterializationHandle Handle { get; }
            internal string Issue { get; set; }
        }

        private readonly PlayerParticipationRuntimeContext participationContext;
        private readonly AttachedPlayerActorMaterializationAdapter materializationAdapter;
        private readonly string sessionContextId;
        private readonly Dictionary<PlayerSlotId, PreparationRecord> records =
            new Dictionary<PlayerSlotId, PreparationRecord>();
        private readonly List<RetainedReleaseFailure> retainedReleaseFailures =
            new List<RetainedReleaseFailure>();

        private int revision;
        private PlayerActorPreparationStatus lastOperationStatus;
        private string lastOperationMessage;

        private PlayerActorPreparationRuntimeContext(
            PlayerParticipationRuntimeContext participationContext,
            AttachedPlayerActorMaterializationAdapter materializationAdapter,
            string sessionContextId)
        {
            this.participationContext = participationContext;
            this.materializationAdapter = materializationAdapter;
            this.sessionContextId = sessionContextId;
            revision = 1;
            lastOperationStatus = PlayerActorPreparationStatus.None;
            lastOperationMessage = "Player Actor preparation runtime context initialized.";
        }

        internal string SessionContextId => sessionContextId;
        internal int Revision => revision;

        internal static bool TryCreate(
            PlayerParticipationRuntimeContext participationContext,
            AttachedPlayerActorMaterializationAdapter materializationAdapter,
            out PlayerActorPreparationRuntimeContext context,
            out string issue)
        {
            context = null;
            issue = string.Empty;

            if (participationContext == null)
            {
                issue = "Player Actor preparation requires a Player participation runtime context.";
                return false;
            }

            if (materializationAdapter == null)
            {
                issue = "Player Actor preparation requires an attached materialization adapter.";
                return false;
            }

            PlayerParticipationSnapshot participationSnapshot =
                participationContext.CreateSnapshot();
            if (participationSnapshot == null ||
                !participationSnapshot.IsInitialized ||
                string.IsNullOrEmpty(participationSnapshot.ContextId))
            {
                issue = "Player Actor preparation requires an initialized Session participation snapshot.";
                return false;
            }

            if (!string.Equals(
                    participationSnapshot.ContextId,
                    materializationAdapter.SessionContextId,
                    StringComparison.Ordinal))
            {
                issue = "Player Actor preparation context and materialization adapter belong to different Session identities.";
                return false;
            }

            context = new PlayerActorPreparationRuntimeContext(
                participationContext,
                materializationAdapter,
                participationSnapshot.ContextId);
            return true;
        }

        internal PlayerActorPreparationResult TryPrepareSelectedActor(
            RuntimeScopeContext scopeContext,
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring localPlayerHost,
            string source,
            string reason)
        {
            const string operation = "PrepareSelectedActor";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "prepare-selected-player-actor");

            if (!scopeContext.IsValid || !playerSlotId.IsValid)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    default,
                    default,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Prepare Selected Actor requires a valid Runtime Scope Context and Player Slot identity.");
            }

            if (!participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedSlotNotConfigured,
                    operation,
                    playerSlotId,
                    default,
                    default,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this Session context.");
            }

            PlayerActorPreparationSummary unprepared = CreateUnpreparedSummary(
                slot,
                resolvedSource,
                resolvedReason,
                "Logical Player Actor is not prepared.");

            if (!slot.IsJoined)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedSlotNotJoined,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Prepare Selected Actor requires a Joined Player Slot.");
            }

            if (!slot.HasSelectedActor || slot.SelectedActorProfile == null)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedActorSelectionMissing,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Prepare Selected Actor requires an explicit ActorProfile selection for the Joined Slot.");
            }

            if (localPlayerHost == null)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedHostUnavailable,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Prepare Selected Actor requires the explicit joined Local Player Host.");
            }

            if (!localPlayerHost.HasJoinedSlot ||
                localPlayerHost.JoinedPlayerSlotId != playerSlotId)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedHostSlotMismatch,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Local Player Host joined Slot evidence does not match the requested Player Slot.");
            }

            if (records.TryGetValue(playerSlotId, out PreparationRecord existing))
            {
                if (IsCurrentIdempotentPreparation(
                        existing,
                        scopeContext,
                        slot,
                        localPlayerHost))
                {
                    return CreateResult(
                        PlayerActorPreparationStatus.SucceededAlreadyPrepared,
                        operation,
                        playerSlotId,
                        existing.Summary,
                        existing.Summary,
                        null,
                        null,
                        false,
                        false,
                        string.Empty,
                        false,
                        false,
                        string.Empty,
                        "Selected Logical Player Actor is already prepared with the same owner, Profile, host and functional identities.");
                }

                return CreateResult(
                    PlayerActorPreparationStatus.RejectedPreparedActorConflict,
                    operation,
                    playerSlotId,
                    existing.Summary,
                    existing.Summary,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Player Slot already has a different or failed prepared Logical Actor. Release or replace it explicitly.");
            }

            PlayerActorMaterializationResult materializationResult =
                materializationAdapter.TryMaterialize(
                    scopeContext,
                    slot,
                    slot.SelectedActorProfile,
                    localPlayerHost,
                    resolvedSource,
                    resolvedReason);
            if (materializationResult == null || !materializationResult.Succeeded ||
                materializationResult.Handle == null)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.FailedMaterialization,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    materializationResult,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    materializationResult != null
                        ? materializationResult.Message
                        : "Logical Player Actor materialization returned no result.");
            }

            PlayerActorMaterializationHandle handle = materializationResult.Handle;
            if (!handle.TryActivate(resolvedSource, resolvedReason, out string activationIssue))
            {
                bool rollbackSucceeded = materializationAdapter.TryReleaseMaterialization(
                    handle,
                    resolvedSource,
                    "prepare-activation-rollback",
                    out string rollbackIssue);
                if (!rollbackSucceeded)
                {
                    RetainReleaseFailure(handle, rollbackIssue);
                }

                return CreateResult(
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.FailedActivation
                        : PlayerActorPreparationStatus.FailedRollback,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    materializationResult,
                    null,
                    true,
                    rollbackSucceeded,
                    rollbackIssue,
                    false,
                    false,
                    string.Empty,
                    rollbackSucceeded
                        ? activationIssue
                        : $"{activationIssue} Rollback failed. {rollbackIssue}",
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.None
                        : PlayerActorPreparationStatus.FailedActivation);
            }

            PlayerActorPreparationSummary prepared = CreatePreparedSummary(
                slot,
                handle,
                PlayerActorPreparationState.Prepared,
                resolvedSource,
                resolvedReason,
                "Selected Logical Player Actor prepared and activated.");
            records.Add(playerSlotId, new PreparationRecord(handle, prepared));
            revision++;
            return CreateResult(
                PlayerActorPreparationStatus.SucceededPrepared,
                operation,
                playerSlotId,
                unprepared,
                prepared,
                materializationResult,
                null,
                false,
                false,
                string.Empty,
                false,
                false,
                string.Empty,
                "Selected Logical Player Actor prepared and activated.");
        }

        internal PlayerActorPreparationResult TryReleasePreparedActor(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason)
        {
            const string operation = "ReleasePreparedActor";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-prepared-player-actor");

            if (!playerSlotId.IsValid)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    default,
                    default,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Release Prepared Actor requires a valid Player Slot identity.");
            }

            if (!participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedSlotNotConfigured,
                    operation,
                    playerSlotId,
                    default,
                    default,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this Session context.");
            }

            PlayerActorPreparationSummary unprepared = CreateUnpreparedSummary(
                slot,
                resolvedSource,
                resolvedReason,
                "Logical Player Actor is not prepared.");

            if (!records.TryGetValue(playerSlotId, out PreparationRecord record))
            {
                if (expectedPreparation.IsValid)
                {
                    return CreateResult(
                        PlayerActorPreparationStatus.RejectedForeignOrStalePreparation,
                        operation,
                        playerSlotId,
                        unprepared,
                        unprepared,
                        null,
                        null,
                        false,
                        false,
                        string.Empty,
                        false,
                        false,
                        string.Empty,
                        "Expected preparation token is stale because the Player Slot has no current prepared Actor.");
                }

                bool retainedReleaseAttempted = HasRetainedReleaseFailure(playerSlotId);
                bool missingRecordRetainedReleased = TryReleaseRetainedForSlot(
                    playerSlotId,
                    resolvedSource,
                    resolvedReason,
                    out string missingRecordRetainedIssue);
                return CreateResult(
                    missingRecordRetainedReleased
                        ? PlayerActorPreparationStatus.SucceededAlreadyReleased
                        : PlayerActorPreparationStatus.FailedPreviousRelease,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    retainedReleaseAttempted,
                    missingRecordRetainedReleased,
                    missingRecordRetainedIssue,
                    missingRecordRetainedReleased
                        ? "Logical Player Actor is already unprepared."
                        : $"Logical Player Actor is unprepared, but retained previous cleanup failed. {missingRecordRetainedIssue}");
            }

            if (!MatchesExpectedPreparation(record, expectedPreparation))
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedForeignOrStalePreparation,
                    operation,
                    playerSlotId,
                    record.Summary,
                    record.Summary,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Expected preparation token is foreign or stale for the current prepared Actor.");
            }

            PlayerActorPreparationSummary previous = record.Summary;
            bool released = materializationAdapter.TryReleaseMaterialization(
                record.Handle,
                resolvedSource,
                resolvedReason,
                out string releaseIssue);
            if (!released)
            {
                PlayerActorPreparationSummary failedSummary = CreatePreparedSummary(
                    slot,
                    record.Handle,
                    PlayerActorPreparationState.ReleaseFailed,
                    resolvedSource,
                    resolvedReason,
                    releaseIssue);
                record.Summary = failedSummary;
                revision++;
                return CreateResult(
                    PlayerActorPreparationStatus.FailedRelease,
                    operation,
                    playerSlotId,
                    previous,
                    failedSummary,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    true,
                    false,
                    releaseIssue,
                    releaseIssue);
            }

            records.Remove(playerSlotId);
            revision++;

            bool retainedReleased = TryReleaseRetainedForSlot(
                playerSlotId,
                resolvedSource,
                resolvedReason,
                out string retainedIssue);
            PlayerActorPreparationSummary current = CreateUnpreparedSummary(
                slot,
                resolvedSource,
                resolvedReason,
                retainedReleased
                    ? "Logical Player Actor released."
                    : "Current Logical Player Actor released; an earlier retained release still failed.");
            return CreateResult(
                retainedReleased
                    ? PlayerActorPreparationStatus.SucceededReleased
                    : PlayerActorPreparationStatus.FailedPreviousRelease,
                operation,
                playerSlotId,
                previous,
                current,
                null,
                null,
                false,
                false,
                string.Empty,
                true,
                retainedReleased,
                retainedIssue,
                retainedReleased
                    ? "Logical Player Actor released and RuntimeContent evidence unregistered."
                    : $"Current Logical Player Actor released, but retained previous cleanup failed. {retainedIssue}");
        }

        internal PlayerActorPreparationResult TryReplacePreparedActor(
            RuntimeScopeContext scopeContext,
            PlayerActorSelectionRequest replacementRequest,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason)
        {
            const string operation = "ReplacePreparedActor";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "replace-prepared-player-actor");
            PlayerSlotId playerSlotId = replacementRequest.PlayerSlotId;

            if (!scopeContext.IsValid || !replacementRequest.IsValid ||
                replacementRequest.ActorProfile == null)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedInvalidRequest,
                    operation,
                    playerSlotId,
                    default,
                    default,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Replace Prepared Actor requires a valid scope, Slot, replacement ActorProfile, source and reason.");
            }

            if (!participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot currentSlot))
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedSlotNotConfigured,
                    operation,
                    playerSlotId,
                    default,
                    default,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this Session context.");
            }

            if (!currentSlot.IsJoined)
            {
                PlayerActorPreparationSummary unprepared = CreateUnpreparedSummary(
                    currentSlot,
                    resolvedSource,
                    resolvedReason,
                    "Logical Player Actor is not prepared.");
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedSlotNotJoined,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Replace Prepared Actor requires a Joined Player Slot.");
            }

            if (!records.TryGetValue(playerSlotId, out PreparationRecord currentRecord) ||
                !currentRecord.Summary.IsPrepared)
            {
                PlayerActorPreparationSummary unprepared = records.TryGetValue(
                        playerSlotId,
                        out PreparationRecord failedRecord)
                    ? failedRecord.Summary
                    : CreateUnpreparedSummary(
                        currentSlot,
                        resolvedSource,
                        resolvedReason,
                        "Logical Player Actor is not prepared.");
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedPreparedActorConflict,
                    operation,
                    playerSlotId,
                    unprepared,
                    unprepared,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Replace Prepared Actor requires one current successfully prepared Logical Actor.");
            }

            if (!MatchesExpectedPreparation(currentRecord, expectedPreparation))
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedForeignOrStalePreparation,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Expected preparation token is foreign or stale for the current prepared Actor.");
            }

            if (currentRecord.Handle.Request.Owner != scopeContext.Owner)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedScopeMismatch,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Prepared Actor replacement must use the same Runtime Content owner scope as the current Actor.");
            }

            if (!currentSlot.HasSelectedActor ||
                currentSlot.SelectedActorProfile == null ||
                currentSlot.SelectedActorProfileId != currentRecord.Summary.PreparedActorProfileId)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedPreparedActorConflict,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Session Actor selection no longer matches the current prepared Actor. Direct selection mutation bypassed preparation authority.");
            }

            if (replacementRequest.HasExpectedSelectionRevision &&
                replacementRequest.ExpectedSelectionRevision != currentSlot.SelectionRevision)
            {
                PlayerActorSelectionResult staleSelection = CreateSelectionRejection(
                    PlayerActorSelectionStatus.RejectedStaleSelectionRevision,
                    "ReplaceActorSelection",
                    currentSlot,
                    replacementRequest.Source,
                    replacementRequest.Reason,
                    $"Expected selection revision '{replacementRequest.ExpectedSelectionRevision}' does not match current revision '{currentSlot.SelectionRevision}'.");
                return CreateResult(
                    PlayerActorPreparationStatus.FailedSelectionCommit,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    null,
                    staleSelection,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    staleSelection.Message);
            }

            if (replacementRequest.ActorProfile.TryGetActorProfileId(
                    out ActorProfileId requestedProfileId,
                    out _) &&
                requestedProfileId == currentSlot.SelectedActorProfileId)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.SucceededAlreadyPrepared,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Replacement ActorProfile already matches the current prepared Actor.");
            }

            PlayerActorMaterializationResult replacementMaterialization =
                materializationAdapter.TryMaterialize(
                    scopeContext,
                    currentSlot,
                    replacementRequest.ActorProfile,
                    currentRecord.Handle.LocalPlayerHost,
                    resolvedSource,
                    resolvedReason);
            if (replacementMaterialization == null ||
                !replacementMaterialization.Succeeded ||
                replacementMaterialization.Handle == null)
            {
                return CreateResult(
                    PlayerActorPreparationStatus.FailedMaterialization,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    replacementMaterialization,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    replacementMaterialization != null
                        ? replacementMaterialization.Message
                        : "Replacement Logical Player Actor materialization returned no result.");
            }

            PlayerActorMaterializationHandle replacementHandle =
                replacementMaterialization.Handle;
            var canonicalSelectionRequest = new PlayerActorSelectionRequest(
                playerSlotId,
                replacementRequest.ActorProfile,
                resolvedSource,
                resolvedReason,
                currentSlot.SelectionRevision);
            PlayerActorSelectionResult selectionResult =
                participationContext.TryReplaceActorSelection(canonicalSelectionRequest);
            if (selectionResult == null || !selectionResult.Succeeded)
            {
                bool rollbackSucceeded = materializationAdapter.TryReleaseMaterialization(
                    replacementHandle,
                    resolvedSource,
                    "replacement-selection-rollback",
                    out string rollbackIssue);
                if (!rollbackSucceeded)
                {
                    RetainReleaseFailure(replacementHandle, rollbackIssue);
                }

                return CreateResult(
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.FailedSelectionCommit
                        : PlayerActorPreparationStatus.FailedRollback,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    replacementMaterialization,
                    selectionResult,
                    true,
                    rollbackSucceeded,
                    rollbackIssue,
                    false,
                    false,
                    string.Empty,
                    selectionResult != null
                        ? selectionResult.Message
                        : "Replacement Actor selection returned no result.",
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.None
                        : PlayerActorPreparationStatus.FailedSelectionCommit);
            }

            if (!replacementHandle.TryActivate(
                    resolvedSource,
                    resolvedReason,
                    out string activationIssue))
            {
                var restoreRequest = new PlayerActorSelectionRequest(
                    playerSlotId,
                    currentSlot.SelectedActorProfile,
                    resolvedSource,
                    "replacement-activation-selection-rollback",
                    selectionResult.SelectionRevision);
                PlayerActorSelectionResult restoreSelection =
                    participationContext.TryReplaceActorSelection(restoreRequest);
                bool physicalRollbackSucceeded = materializationAdapter.TryReleaseMaterialization(
                    replacementHandle,
                    resolvedSource,
                    "replacement-activation-physical-rollback",
                    out string physicalRollbackIssue);
                if (!physicalRollbackSucceeded)
                {
                    RetainReleaseFailure(replacementHandle, physicalRollbackIssue);
                }

                bool rollbackSucceeded =
                    restoreSelection != null &&
                    restoreSelection.Succeeded &&
                    physicalRollbackSucceeded;
                return CreateResult(
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.FailedActivation
                        : PlayerActorPreparationStatus.FailedRollback,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    replacementMaterialization,
                    selectionResult,
                    true,
                    rollbackSucceeded,
                    JoinMessages(
                        restoreSelection != null ? restoreSelection.Message : "Selection rollback returned no result.",
                        physicalRollbackIssue),
                    false,
                    false,
                    string.Empty,
                    rollbackSucceeded
                        ? activationIssue
                        : $"{activationIssue} Replacement rollback failed.",
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.None
                        : PlayerActorPreparationStatus.FailedActivation);
            }

            PlayerSlotRuntimeSnapshot committedSlot = selectionResult.Slot;
            PlayerActorPreparationSummary replacementSummary = CreatePreparedSummary(
                committedSlot,
                replacementHandle,
                PlayerActorPreparationState.Prepared,
                resolvedSource,
                resolvedReason,
                "Replacement Logical Player Actor prepared and activated.");
            PlayerActorPreparationSummary previousSummary = currentRecord.Summary;
            records[playerSlotId] = new PreparationRecord(
                replacementHandle,
                replacementSummary);
            revision++;

            bool previousReleased = materializationAdapter.TryReleaseMaterialization(
                currentRecord.Handle,
                resolvedSource,
                "release-previous-prepared-player-actor",
                out string previousReleaseIssue);
            if (!previousReleased)
            {
                RetainReleaseFailure(currentRecord.Handle, previousReleaseIssue);
                return CreateResult(
                    PlayerActorPreparationStatus.FailedPreviousRelease,
                    operation,
                    playerSlotId,
                    previousSummary,
                    replacementSummary,
                    replacementMaterialization,
                    selectionResult,
                    false,
                    false,
                    string.Empty,
                    true,
                    false,
                    previousReleaseIssue,
                    "Replacement Actor is current and active, but the previous Actor release failed and remains retained for diagnostics.");
            }

            return CreateResult(
                PlayerActorPreparationStatus.SucceededReplaced,
                operation,
                playerSlotId,
                previousSummary,
                replacementSummary,
                replacementMaterialization,
                selectionResult,
                false,
                false,
                string.Empty,
                true,
                true,
                string.Empty,
                "Prepared Logical Player Actor replaced transactionally without replacing the stable Local Player Host.");
        }

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return HasPreparedOrFailedRecord(request.PlayerSlotId)
                ? CreatePreparedSelectionRejection(
                    "SelectActorProfile",
                    request,
                    "Actor selection cannot change while a Logical Player Actor is prepared. Use ReplacePreparedActor.")
                : participationContext.TrySelectActorProfile(request);
        }

        internal PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return HasPreparedOrFailedRecord(request.PlayerSlotId)
                ? CreatePreparedSelectionRejection(
                    "ReplaceActorSelection",
                    request,
                    "Actor selection cannot change while a Logical Player Actor is prepared. Use ReplacePreparedActor.")
                : participationContext.TryReplaceActorSelection(request);
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return HasPreparedOrFailedRecord(request.PlayerSlotId)
                ? CreatePreparedSelectionRejection(
                    "ClearActorSelection",
                    request,
                    "Actor selection cannot be cleared while a Logical Player Actor is prepared. Release it first.")
                : participationContext.TryClearActorSelection(request);
        }

        internal PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            if (HasPreparedOrFailedRecord(playerSlotId))
            {
                var request = new PlayerActorSelectionRequest(
                    playerSlotId,
                    null,
                    source,
                    reason,
                    expectedSelectionRevision);
                return CreatePreparedSelectionRejection(
                    "SelectDefaultActor",
                    request,
                    "Default Actor selection cannot change while a Logical Player Actor is prepared.");
            }

            return participationContext.TrySelectDefaultActor(
                playerSlotId,
                expectedSelectionRevision,
                source,
                reason);
        }

        internal bool TryGetPreparationSummary(
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary summary)
        {
            if (!playerSlotId.IsValid ||
                !participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                summary = default;
                return false;
            }

            if (records.TryGetValue(playerSlotId, out PreparationRecord record))
            {
                summary = record.Summary;
                return true;
            }

            summary = CreateUnpreparedSummary(
                slot,
                nameof(PlayerActorPreparationRuntimeContext),
                "snapshot",
                "Logical Player Actor is not prepared.");
            return true;
        }

        internal PlayerActorPreparationSnapshot CreateSnapshot()
        {
            PlayerParticipationSnapshot participationSnapshot =
                participationContext.CreateSnapshot();
            var summaries = new PlayerActorPreparationSummary[
                participationSnapshot.ConfiguredSlotCount];
            for (int index = 0; index < participationSnapshot.ConfiguredSlotCount; index++)
            {
                PlayerSlotRuntimeSnapshot slot = participationSnapshot.Slots[index];
                summaries[index] = records.TryGetValue(
                        slot.PlayerSlotId,
                        out PreparationRecord record)
                    ? record.Summary
                    : CreateUnpreparedSummary(
                        slot,
                        nameof(PlayerActorPreparationRuntimeContext),
                        "snapshot",
                        "Logical Player Actor is not prepared.");
            }

            var retained = new PlayerActorMaterializationSnapshot[
                retainedReleaseFailures.Count];
            for (int index = 0; index < retainedReleaseFailures.Count; index++)
            {
                retained[index] = retainedReleaseFailures[index]
                    .Handle
                    .CreateSnapshot();
            }

            return new PlayerActorPreparationSnapshot(
                sessionContextId,
                revision,
                summaries,
                retained,
                lastOperationStatus,
                lastOperationMessage);
        }

        private bool IsCurrentIdempotentPreparation(
            PreparationRecord record,
            RuntimeScopeContext scopeContext,
            PlayerSlotRuntimeSnapshot slot,
            LocalPlayerHostAuthoring host)
        {
            return record != null &&
                record.Summary.IsPrepared &&
                record.Handle.State == PlayerActorMaterializationState.Active &&
                record.Handle.Request.Owner == scopeContext.Owner &&
                record.Handle.Request.ActorProfileId == slot.SelectedActorProfileId &&
                record.Summary.SelectionRevision == slot.SelectionRevision &&
                ReferenceEquals(record.Handle.LocalPlayerHost, host);
        }

        private bool MatchesExpectedPreparation(
            PreparationRecord record,
            PlayerActorPreparationToken expectedPreparation)
        {
            return !expectedPreparation.IsValid ||
                (record != null &&
                 expectedPreparation.SessionContextId == sessionContextId &&
                 expectedPreparation == record.Summary.Token);
        }

        private bool HasPreparedOrFailedRecord(PlayerSlotId playerSlotId)
        {
            return playerSlotId.IsValid && records.ContainsKey(playerSlotId);
        }

        private PlayerActorSelectionResult CreatePreparedSelectionRejection(
            string operation,
            PlayerActorSelectionRequest request,
            string message)
        {
            if (!participationContext.TryGetActorSelection(
                    request.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                return participationContext.TrySelectActorProfile(request);
            }

            return CreateSelectionRejection(
                PlayerActorSelectionStatus.RejectedLogicalActorAlreadyPrepared,
                operation,
                slot,
                request.Source,
                request.Reason,
                message);
        }

        private PlayerActorSelectionResult CreateSelectionRejection(
            PlayerActorSelectionStatus status,
            string operation,
            PlayerSlotRuntimeSnapshot slot,
            string source,
            string reason,
            string message)
        {
            PlayerParticipationSnapshot snapshot =
                participationContext.CreateSnapshot();
            return new PlayerActorSelectionResult(
                status,
                operation,
                slot.PlayerSlotId,
                slot.Profile,
                slot.SelectedActorProfile,
                slot.SelectedActorProfile,
                slot.SelectionRevision,
                slot.SelectionRevision,
                snapshot.ActorSelectionPolicyProfile,
                default,
                source.NormalizeText(),
                reason.NormalizeText(),
                message,
                slot,
                snapshot);
        }

        private PlayerActorPreparationSummary CreateUnpreparedSummary(
            PlayerSlotRuntimeSnapshot slot,
            string source,
            string reason,
            string message)
        {
            return new PlayerActorPreparationSummary(
                sessionContextId,
                slot.PlayerSlotId,
                PlayerActorPreparationState.Unprepared,
                slot.SelectedActorProfileId,
                slot.SelectionRevision,
                default,
                source,
                reason,
                message);
        }

        private PlayerActorPreparationSummary CreatePreparedSummary(
            PlayerSlotRuntimeSnapshot slot,
            PlayerActorMaterializationHandle handle,
            PlayerActorPreparationState state,
            string source,
            string reason,
            string message)
        {
            return new PlayerActorPreparationSummary(
                sessionContextId,
                slot.PlayerSlotId,
                state,
                slot.SelectedActorProfileId,
                slot.SelectionRevision,
                handle.CreateSnapshot(),
                source,
                reason,
                message);
        }

        private void RetainReleaseFailure(
            PlayerActorMaterializationHandle handle,
            string issue)
        {
            for (int index = 0; index < retainedReleaseFailures.Count; index++)
            {
                if (ReferenceEquals(retainedReleaseFailures[index].Handle, handle))
                {
                    retainedReleaseFailures[index].Issue = issue.NormalizeText();
                    return;
                }
            }

            retainedReleaseFailures.Add(
                new RetainedReleaseFailure(handle, issue));
            revision++;
        }

        private bool HasRetainedReleaseFailure(PlayerSlotId playerSlotId)
        {
            for (int index = 0; index < retainedReleaseFailures.Count; index++)
            {
                if (retainedReleaseFailures[index].Handle.Request.Slot.PlayerSlotId == playerSlotId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryReleaseRetainedForSlot(
            PlayerSlotId playerSlotId,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            for (int index = retainedReleaseFailures.Count - 1; index >= 0; index--)
            {
                RetainedReleaseFailure retained = retainedReleaseFailures[index];
                if (retained.Handle.Request.Slot.PlayerSlotId != playerSlotId)
                {
                    continue;
                }

                bool released = materializationAdapter.TryReleaseMaterialization(
                    retained.Handle,
                    source,
                    "retry-retained-previous-release",
                    out string retainedIssue);
                if (released)
                {
                    retainedReleaseFailures.RemoveAt(index);
                    revision++;
                    continue;
                }

                retained.Issue = retainedIssue.NormalizeText();
                issue = JoinMessages(issue, retainedIssue);
            }

            return string.IsNullOrEmpty(issue);
        }

        private PlayerActorPreparationResult CreateResult(
            PlayerActorPreparationStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationSummary previousSummary,
            PlayerActorPreparationSummary currentSummary,
            PlayerActorMaterializationResult materializationResult,
            PlayerActorSelectionResult selectionResult,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackMessage,
            bool previousReleaseAttempted,
            bool previousReleaseSucceeded,
            string previousReleaseMessage,
            string message,
            PlayerActorPreparationStatus originalStatus = PlayerActorPreparationStatus.None)
        {
            lastOperationStatus = status;
            lastOperationMessage = message.NormalizeText();
            return new PlayerActorPreparationResult(
                status,
                operation,
                playerSlotId,
                previousSummary,
                currentSummary,
                materializationResult,
                materializationResult != null,
                selectionResult,
                selectionResult != null,
                rollbackAttempted,
                rollbackSucceeded,
                rollbackMessage,
                previousReleaseAttempted,
                previousReleaseSucceeded,
                previousReleaseMessage,
                CreateSnapshot(),
                message,
                originalStatus);
        }

        private static string JoinMessages(string left, string right)
        {
            string normalizedLeft = left.NormalizeText();
            string normalizedRight = right.NormalizeText();
            if (string.IsNullOrEmpty(normalizedLeft))
            {
                return normalizedRight;
            }

            if (string.IsNullOrEmpty(normalizedRight))
            {
                return normalizedLeft;
            }

            return $"{normalizedLeft} {normalizedRight}";
        }
    }
}
