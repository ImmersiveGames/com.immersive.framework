using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

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
                LocalPlayerHostAuthoring host,
                PlayerActorPreparationSummary summary)
            {
                Handle = handle ?? throw new ArgumentNullException(nameof(handle));
                Host = host != null
                    ? host
                    : throw new ArgumentNullException(nameof(host));
                Summary = summary;
            }

            internal PlayerActorMaterializationHandle Handle { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorPreparationSummary Summary { get; set; }
            internal PlayerActorCorrelationEvidence ActorEvidence =>
                Summary.ActorEvidence;
            internal PlayerActorPhysicalOwnership PhysicalOwnership =>
                Summary.ActorEvidence.PhysicalOwnership;
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

        private readonly PlayerParticipationRuntimeContext _participationContext;
        private readonly PlayerHostEvidenceProjection _hostEvidenceProjection;
        private readonly AttachedPlayerActorMaterializationAdapter _materializationAdapter;
        private readonly ActorOccurrenceIdSequence _occurrenceIds;
        private readonly string _sessionContextId;
        private readonly Dictionary<PlayerSlotId, PreparationRecord> _records =
            new Dictionary<PlayerSlotId, PreparationRecord>();
        private readonly List<RetainedReleaseFailure> _retainedReleaseFailures =
            new List<RetainedReleaseFailure>();
        private readonly HashSet<PlayerSlotId> _actorReplacementTransitions =
            new HashSet<PlayerSlotId>();
        private readonly HashSet<PlayerSlotId> _invalidatedActorReplacementTransitions =
            new HashSet<PlayerSlotId>();

        private int _revision;
        private int _actorCorrelationRevision;
        private PlayerActorPreparationStatus _lastOperationStatus;
        private string _lastOperationMessage;

        private PlayerActorPreparationRuntimeContext(
            PlayerParticipationRuntimeContext participationContext,
            PlayerHostEvidenceProjection hostEvidenceProjection,
            AttachedPlayerActorMaterializationAdapter materializationAdapter,
            ActorOccurrenceIdSequence occurrenceIds,
            string sessionContextId)
        {
            this._participationContext = participationContext;
            this._hostEvidenceProjection = hostEvidenceProjection;
            this._materializationAdapter = materializationAdapter;
            this._occurrenceIds = occurrenceIds;
            this._sessionContextId = sessionContextId;
            _revision = 1;
            _actorCorrelationRevision = 0;
            _lastOperationStatus = PlayerActorPreparationStatus.None;
            _lastOperationMessage = "Player Actor preparation runtime context initialized.";
        }

        internal string SessionContextId => _sessionContextId;
        internal int Revision => _revision;

        internal static bool TryCreate(
            PlayerParticipationRuntimeContext participationContext,
            PlayerHostEvidenceProjection hostEvidenceProjection,
            AttachedPlayerActorMaterializationAdapter materializationAdapter,
            ActorOccurrenceIdSequence occurrenceIds,
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

            if (occurrenceIds == null)
            {
                issue = "Player Actor preparation requires the Framework Host occurrence identity sequence.";
                return false;
            }

            if (hostEvidenceProjection == null)
            {
                issue = "Player Actor preparation requires the correlated physical Host evidence projection.";
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

            if (!string.Equals(
                    participationSnapshot.ContextId,
                    hostEvidenceProjection.SessionContextId,
                    StringComparison.Ordinal))
            {
                issue =
                    "Player Actor preparation context and Host evidence projection belong to different Session identities.";
                return false;
            }

            context = new PlayerActorPreparationRuntimeContext(
                participationContext,
                hostEvidenceProjection,
                materializationAdapter,
                occurrenceIds,
                participationSnapshot.ContextId);
            return true;
        }

        internal PlayerActorPreparationResult TryPrepareSelectedActor(
            RuntimeScopeContext activityScopeContext,
            RuntimeScopeContext physicalScopeContext,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            const string operation = "PrepareSelectedActor";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "prepare-selected-player-actor");

            if (!activityScopeContext.IsValid ||
                !physicalScopeContext.IsValid ||
                physicalScopeContext.Scope != RuntimeContentScope.Session ||
                !playerSlotId.IsValid)
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
                "Prepare Selected Actor requires valid Activity contextual and Session physical scope contexts plus a Player Slot identity.");
            }

            if (!_participationContext.TryGetActorSelection(
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

            _hostEvidenceProjection.LogContextDiagnostic("before", "TryEnsureContextualProjection",
                playerSlotId, activityScopeContext.Owner, resolvedSource, resolvedReason);
            if (!TryEnsureContextualProjection(
                    activityScopeContext,
                    slot,
                    resolvedSource,
                    resolvedReason,
                    out string contextualProjectionIssue))
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
                    contextualProjectionIssue);
            }

            _hostEvidenceProjection.LogContextDiagnostic("after", "TryEnsureContextualProjection",
                playerSlotId, activityScopeContext.Owner, resolvedSource, resolvedReason, "succeeded");
            if (_records.TryGetValue(playerSlotId, out PreparationRecord existing))
            {
                PlayerActorCorrelationEvidence actorEvidence =
                    existing.Summary.ActorEvidence;
                if (!existing.Summary.IsPrepared || !actorEvidence.IsValid)
                {
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

                if (!TryResolveCurrentActorCorrelation(
                        activityScopeContext,
                        playerSlotId,
                        existing.Host,
                        out _,
                        out PlayerHostEvidenceSnapshot existingHostEvidence,
                        out string existingCorrelationIssue))
                {
                    return CreateResult(
                        PlayerActorPreparationStatus.RejectedHostUnavailable,
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
                        existingCorrelationIssue);
                }

                if (IsCurrentIdempotentPreparation(
                        existing,
                        slot,
                        existingHostEvidence))
                {
                    if (existing.Handle.State ==
                        PlayerActorMaterializationState.StagedInactive &&
                        !existing.Handle.TryActivate(
                            resolvedSource,
                            resolvedReason,
                            out string reactivationIssue))
                    {
                        return CreateResult(
                            PlayerActorPreparationStatus.FailedActivation,
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
                            reactivationIssue);
                    }

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
                        "Selected Session-owned Logical Player Actor is already prepared with the same Profile, Host and Session correlation and was reused for the current Activity representation.");
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

            if (!TryResolveCurrentActorCorrelation(
                    activityScopeContext,
                    playerSlotId,
                    null,
                    out _,
                    out PlayerHostEvidenceSnapshot hostEvidence,
                    out string correlationIssue))
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
                    correlationIssue);
            }

            LocalPlayerHostAuthoring localPlayerHost = hostEvidence.Host;
            ActorId logicalActorId = CreateLogicalActorId();

            PlayerActorMaterializationResult materializationResult =
                _materializationAdapter.TryMaterialize(
                    physicalScopeContext,
                    slot,
                    slot.SelectedActorProfile,
                    logicalActorId,
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
                bool rollbackSucceeded = _materializationAdapter.TryReleaseMaterialization(
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

            CommitOccurrenceIdentity(handle);
            PlayerActorPreparationSummary prepared = CreatePreparedSummary(
                slot,
                handle,
                hostEvidence,
                PlayerActorPhysicalOwnership.FrameworkOwned,
                PlayerActorPreparationState.Prepared,
                resolvedSource,
                resolvedReason,
                "Selected Logical Player Actor prepared and activated.");
            _records.Add(
                playerSlotId,
                new PreparationRecord(handle, localPlayerHost, prepared));
            _revision++;
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

        /// <summary>
        /// Canonical Session-physical availability request. An Activity supplies only the
        /// contextual scope used to correlate its current representation; the prepared Actor
        /// itself is created once and then reused by its Session/Slot occurrence.
        /// </summary>
        internal PlayerActorPreparationResult TryEnsureSessionPhysicalActor(
            RuntimeScopeContext activityScopeContext,
            RuntimeScopeContext physicalScopeContext,
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            return TryPrepareSelectedActor(
                activityScopeContext,
                physicalScopeContext,
                playerSlotId,
                source,
                reason);
        }

        internal bool TryReleaseContextualProjection(
            RuntimeContentOwner activityOwner,
            PlayerSlotId playerSlotId,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            _hostEvidenceProjection.LogContextDiagnostic("before", "TryReleaseContextualProjection",
                playerSlotId, activityOwner, source, reason);
            if (!activityOwner.IsValid || !playerSlotId.IsValid)
            {
                issue = "Contextual projection release requires a valid Activity owner and Player Slot.";
                _hostEvidenceProjection.LogContextDiagnostic("after", "TryReleaseContextualProjection",
                    playerSlotId, activityOwner, source, reason, "failed", issue);
                return false;
            }

            if (!_participationContext.TryGetCurrentAssignment(
                    playerSlotId,
                    out PlayerSlotAssignmentSnapshot assignment) ||
                !assignment.IsAssigned)
            {
                _hostEvidenceProjection.LogContextDiagnostic("after", "TryReleaseContextualProjection",
                    playerSlotId, activityOwner, source, reason, "succeeded-or-no-op", issue);
                return true;
            }

            if (assignment.AssignmentOwner != activityOwner ||
                !_hostEvidenceProjection.TryGetRetainedEvidence(
                    playerSlotId,
                    out PlayerHostEvidenceSnapshot hostEvidence) ||
                !hostEvidence.HasContextualProjection ||
                hostEvidence.AssignmentToken != assignment.AssignmentToken ||
                hostEvidence.HostBindingIdentity != assignment.HostBindingIdentity)
            {
                issue = "Contextual projection does not match the exiting Activity occurrence.";
                _hostEvidenceProjection.LogContextDiagnostic("after", "TryReleaseContextualProjection",
                    playerSlotId, activityOwner, source, reason, "failed", issue);
                return false;
            }

            PlayerHostEvidenceResult projectionRelease =
                _hostEvidenceProjection.ReleaseHostEvidence(
                    playerSlotId,
                    assignment.AssignmentToken,
                    assignment.HostBindingIdentity,
                    hostEvidence.Host,
                    source,
                    reason + "; release-contextual-host");
            if (projectionRelease == null || !projectionRelease.Succeeded)
            {
                issue = projectionRelease != null
                    ? projectionRelease.ToDiagnosticString()
                    : "Contextual Host projection release returned no result.";
                _hostEvidenceProjection.LogContextDiagnostic("after", "TryReleaseContextualProjection",
                    playerSlotId, activityOwner, source, reason, "failed", issue);
                return false;
            }

            PlayerSlotAssignmentResult assignmentRelease =
                _participationContext.ReleaseAssignment(
                    playerSlotId,
                    assignment.AssignmentToken,
                    source,
                    reason + "; release-contextual-assignment");
            if (assignmentRelease == null || !assignmentRelease.Succeeded)
            {
                issue = assignmentRelease != null
                    ? assignmentRelease.ToDiagnosticString()
                    : "Contextual assignment release returned no result.";
                _hostEvidenceProjection.LogContextDiagnostic("after", "TryReleaseContextualProjection",
                    playerSlotId, activityOwner, source, reason, "failed", issue);
                return false;
            }

            _hostEvidenceProjection.LogContextDiagnostic("after", "TryReleaseContextualProjection",
                playerSlotId, activityOwner, source, reason, "succeeded", issue);

            return true;
        }

        internal PlayerActorPreparationResult TryReleasePreparedActor(
            PlayerSlotId playerSlotId,
            PlayerActorPreparationToken expectedPreparation,
            string source,
            string reason)
        {
            const string operation = "ReleasePreparedActor";
            if (playerSlotId.IsValid &&
                _actorReplacementTransitions.Contains(playerSlotId))
            {
                _invalidatedActorReplacementTransitions.Add(playerSlotId);
            }

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

            if (!_participationContext.TryGetActorSelection(
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

            if (!_records.TryGetValue(playerSlotId, out PreparationRecord record))
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
            bool released = _materializationAdapter.TryReleaseMaterialization(
                record.Handle,
                resolvedSource,
                resolvedReason,
                out string releaseIssue);
            if (!released)
            {
                PlayerActorPreparationSummary failedSummary =
                    CreateFailedReleaseSummary(
                    record.Summary,
                    resolvedSource,
                    resolvedReason,
                    releaseIssue);
                record.Summary = failedSummary;
                _revision++;
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

            _records.Remove(playerSlotId);
            // A successful physical release is terminal for an adopted Scene composition.
            // Do not retain provenance that could later masquerade as an Activity admission.
            _sceneAdoptions.Remove(playerSlotId);
            _revision++;

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
            RuntimeScopeContext activityScopeContext,
            RuntimeScopeContext physicalScopeContext,
            PlayerActorSelectionRequest replacementRequest,
            PlayerActorPreparationToken expectedPreparation,
            PlayerActorReplacementIdentityPolicy identityPolicy,
            string source,
            string reason,
            Func<bool> prepareHandoff,
            Func<bool> isHandoffTargetCurrent)
        {
            PlayerSlotId playerSlotId = replacementRequest.PlayerSlotId;
            if (!playerSlotId.IsValid)
            {
                return TryReplacePreparedActorCore(
                    activityScopeContext,
                    physicalScopeContext,
                    replacementRequest,
                    expectedPreparation,
                    identityPolicy,
                    source,
                    reason,
                    prepareHandoff,
                    isHandoffTargetCurrent);
            }

            if (!_actorReplacementTransitions.Add(playerSlotId))
            {
                PreparationRecord current = _records.TryGetValue(playerSlotId, out PreparationRecord found)
                    ? found
                    : null;
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedTransitionInProgress,
                    "ReplacePreparedActor",
                    playerSlotId,
                    current != null ? current.Summary : default,
                    current != null ? current.Summary : default,
                    null,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    "Another Actor transition for this Player Slot is already in progress.");
            }

            _invalidatedActorReplacementTransitions.Remove(playerSlotId);
            try
            {
                return TryReplacePreparedActorCore(
                    activityScopeContext,
                    physicalScopeContext,
                    replacementRequest,
                    expectedPreparation,
                    identityPolicy,
                    source,
                    reason,
                    prepareHandoff,
                    isHandoffTargetCurrent);
            }
            finally
            {
                _actorReplacementTransitions.Remove(playerSlotId);
                _invalidatedActorReplacementTransitions.Remove(playerSlotId);
            }
        }

        private PlayerActorPreparationResult TryReplacePreparedActorCore(
            RuntimeScopeContext activityScopeContext,
            RuntimeScopeContext physicalScopeContext,
            PlayerActorSelectionRequest replacementRequest,
            PlayerActorPreparationToken expectedPreparation,
            PlayerActorReplacementIdentityPolicy identityPolicy,
            string source,
            string reason,
            Func<bool> prepareHandoff,
            Func<bool> isHandoffTargetCurrent)
        {
            const string operation = "ReplacePreparedActor";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerActorPreparationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "replace-prepared-player-actor");
            PlayerSlotId playerSlotId = replacementRequest.PlayerSlotId;

            if (!activityScopeContext.IsValid ||
                !physicalScopeContext.IsValid ||
                physicalScopeContext.Scope != RuntimeContentScope.Session ||
                !replacementRequest.IsValid || replacementRequest.ActorProfile == null)
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
                    "Replace Prepared Actor requires valid Activity contextual and Session physical scope contexts plus a Slot, replacement ActorProfile, source and reason.");
            }

            if (!_participationContext.TryGetActorSelection(
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

            if (!_records.TryGetValue(playerSlotId, out PreparationRecord currentRecord) ||
                !currentRecord.Summary.IsPrepared)
            {
                PlayerActorPreparationSummary unprepared = _records.TryGetValue(
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

            if (currentRecord.Handle.Request.Owner != physicalScopeContext.Owner)
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
                    "Prepared Actor replacement must use the same Session physical Runtime Content owner as the current Actor.");
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
                requestedProfileId == currentSlot.SelectedActorProfileId &&
                identityPolicy == PlayerActorReplacementIdentityPolicy.PreserveLogicalActor)
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

            if (!TryResolveCurrentActorCorrelation(
                    activityScopeContext,
                    playerSlotId,
                    currentRecord.Host,
                    out _,
                    out PlayerHostEvidenceSnapshot hostEvidence,
                    out string correlationIssue))
            {
                return CreateResult(
                    PlayerActorPreparationStatus.RejectedHostUnavailable,
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
                    correlationIssue);
            }

            Transform previousActorRoot = currentRecord.Handle.ActorRoot;
            if (previousActorRoot == null)
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
                    "Prepared Actor replacement requires the current Actor occurrence root.");
            }

            Vector3 previousActorPosition = previousActorRoot.position;
            Quaternion previousActorRotation = previousActorRoot.rotation;

            if (!TryResolveReplacementActorId(
                    currentRecord.Handle.Request.ActorId,
                    identityPolicy,
                    out ActorId replacementActorId,
                    out string identityIssue))
            {
                return CreateResult(
                    PlayerActorPreparationStatus.FailedMaterialization,
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
                    identityIssue);
            }

            PlayerActorMaterializationResult replacementMaterialization =
                _materializationAdapter.TryMaterialize(
                    physicalScopeContext,
                    currentSlot,
                    replacementRequest.ActorProfile,
                    replacementActorId,
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
            if (!replacementHandle.TrySetActorRootPose(previousActorPosition, previousActorRotation))
            {
                bool rollbackSucceeded = _materializationAdapter.TryReleaseMaterialization(
                    replacementHandle,
                    resolvedSource,
                    "replacement-actor-root-evidence-rollback",
                    out string rollbackIssue);
                if (!rollbackSucceeded)
                {
                    RetainReleaseFailure(replacementHandle, rollbackIssue);
                }

                return CreateResult(
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.FailedMaterialization
                        : PlayerActorPreparationStatus.FailedRollback,
                    operation,
                    playerSlotId,
                    currentRecord.Summary,
                    currentRecord.Summary,
                    replacementMaterialization,
                    null,
                    true,
                    rollbackSucceeded,
                    rollbackIssue,
                    false,
                    false,
                    string.Empty,
                    "Replacement materialization returned no Actor occurrence root.",
                    rollbackSucceeded
                        ? PlayerActorPreparationStatus.None
                        : PlayerActorPreparationStatus.FailedMaterialization);
            }

            PlayerActorPreparationSummary previousSummary = currentRecord.Summary;
            bool IsPreviousActorOperational()
            {
                Transform previousRoot = currentRecord.Handle.ActorRoot;
                return currentRecord.Handle.State == PlayerActorMaterializationState.Active &&
                    previousRoot != null &&
                    previousRoot.gameObject.activeInHierarchy;
            }

            bool IsHandoffStillCurrent()
            {
                if (_invalidatedActorReplacementTransitions.Contains(playerSlotId) ||
                    !activityScopeContext.IsValid ||
                    !physicalScopeContext.IsValid ||
                    physicalScopeContext.Scope != RuntimeContentScope.Session ||
                    isHandoffTargetCurrent == null ||
                    !isHandoffTargetCurrent() ||
                    !_records.TryGetValue(playerSlotId, out PreparationRecord current) ||
                    !ReferenceEquals(current, currentRecord) ||
                    !MatchesExpectedPreparation(current, expectedPreparation) ||
                    current.Handle.Request.Owner != physicalScopeContext.Owner ||
                    !_participationContext.TryGetActorSelection(
                        playerSlotId,
                        out PlayerSlotRuntimeSnapshot latestSlot) ||
                    !latestSlot.IsJoined ||
                    latestSlot.SelectionRevision != currentSlot.SelectionRevision ||
                    !ReferenceEquals(latestSlot.SelectedActorProfile, currentSlot.SelectedActorProfile))
                {
                    return false;
                }

                return TryResolveCurrentActorCorrelation(
                        activityScopeContext,
                        playerSlotId,
                        currentRecord.Host,
                        out _,
                        out PlayerHostEvidenceSnapshot latestHostEvidence,
                        out _) &&
                    latestHostEvidence.AssignmentToken == hostEvidence.AssignmentToken &&
                    latestHostEvidence.HostBindingIdentity == hostEvidence.HostBindingIdentity &&
                    ReferenceEquals(latestHostEvidence.Host, hostEvidence.Host);
            }

            bool TryReleaseReplacementCandidate(out string cleanupIssue)
            {
                bool released = _materializationAdapter.TryReleaseMaterialization(
                    replacementHandle,
                    resolvedSource,
                    "release-uncommitted-replacement-actor-candidate",
                    out cleanupIssue);
                if (!released)
                {
                    RetainReleaseFailure(replacementHandle, cleanupIssue);
                }

                return released;
            }

            PlayerActorPreparationResult HandoffFailedBeforePreviousDeactivation(
                PlayerActorPreparationStatus status,
                string failure)
            {
                if (!IsPreviousActorOperational())
                {
                    return HandoffFailedAfterPreviousDeactivation(
                        failure + " Actor A is no longer physically available.");
                }

                bool candidateReleased = TryReleaseReplacementCandidate(out string cleanupIssue);
                string message = candidateReleased
                    ? failure
                    : JoinMessages(failure, "Uncommitted replacement candidate cleanup failed: " + cleanupIssue);
                return CreateResult(
                    status,
                    operation,
                    playerSlotId,
                    previousSummary,
                    previousSummary,
                    replacementMaterialization,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    message,
                    candidateReleased ? PlayerActorPreparationStatus.None : status,
                    cleanupPending: !candidateReleased);
            }

            PlayerActorPreparationResult HandoffFailedAfterPreviousDeactivation(
                string failure)
            {
                bool candidateReleased = TryReleaseReplacementCandidate(out string cleanupIssue);
                string message = candidateReleased
                    ? failure + " Actor A remains the last committed binding but may be physically unavailable. Retry replacement or close the Session owner."
                    : JoinMessages(
                        failure + " Actor A remains the last committed binding but may be physically unavailable.",
                        "Uncommitted replacement candidate cleanup failed and was retained: " + cleanupIssue);
                return CreateResult(
                    PlayerActorPreparationStatus.FailedHandoffAfterPreviousDeactivation,
                    operation,
                    playerSlotId,
                    previousSummary,
                    previousSummary,
                    replacementMaterialization,
                    null,
                    false,
                    false,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    message,
                    PlayerActorPreparationStatus.FailedHandoffAfterPreviousDeactivation,
                    cleanupPending: !candidateReleased);
            }

            if (!IsHandoffStillCurrent())
            {
                return HandoffFailedBeforePreviousDeactivation(
                    PlayerActorPreparationStatus.FailedHandoffPreparation,
                    "The replacement target changed while Actor B was being staged; Actor A was not deactivated.");
            }

            if (prepareHandoff == null || !prepareHandoff())
            {
                return HandoffFailedBeforePreviousDeactivation(
                    PlayerActorPreparationStatus.FailedHandoffPreparation,
                    "Gameplay/occupancy preparation for the physical handoff failed; Actor A was not deactivated.");
            }

            if (!IsHandoffStillCurrent())
            {
                return HandoffFailedBeforePreviousDeactivation(
                    PlayerActorPreparationStatus.FailedHandoffPreparation,
                    "The replacement owner, Session or expected Actor changed before the handoff barrier; Actor A was not deactivated.");
            }

            bool deactivated = currentRecord.Handle.TryDeactivate(
                resolvedSource,
                resolvedReason + "; deactivate-previous-actor",
                out string deactivationIssue);
            if (!deactivated)
            {
                return currentRecord.Handle.State == PlayerActorMaterializationState.Active
                    ? HandoffFailedBeforePreviousDeactivation(
                        PlayerActorPreparationStatus.FailedActivation,
                        "Actor A could not be deactivated: " + deactivationIssue)
                    : HandoffFailedAfterPreviousDeactivation(
                        "Actor A became unavailable while its deactivation callback was running: " + deactivationIssue);
            }

            if (!IsHandoffStillCurrent())
            {
                return HandoffFailedAfterPreviousDeactivation(
                    "The Session/Activity owner closed or the expected Actor changed during Actor A deactivation.");
            }

            if (!replacementHandle.TryActivateReplacement(
                    resolvedSource,
                    resolvedReason + "; activate-replacement-actor",
                    out string activationIssue))
            {
                return HandoffFailedAfterPreviousDeactivation(
                    "Actor B activation failed after Actor A was deactivated: " + activationIssue);
            }

            if (!IsHandoffStillCurrent())
            {
                return HandoffFailedAfterPreviousDeactivation(
                    "The Session/Activity owner closed or the expected Actor changed during Actor B activation.");
            }

            PlayerActorPreparationSummary replacementSummary = default;
            bool physicalBindingCommitted = false;
            void CommitPhysicalBinding(PlayerSlotRuntimeSnapshot committedSlot)
            {
                CommitOccurrenceIdentity(replacementHandle);
                replacementSummary = CreatePreparedSummary(
                    committedSlot,
                    replacementHandle,
                    hostEvidence,
                    PlayerActorPhysicalOwnership.FrameworkOwned,
                    PlayerActorPreparationState.Prepared,
                    resolvedSource,
                    resolvedReason,
                    "Replacement Logical Player Actor prepared and activated.");
                _records[playerSlotId] = new PreparationRecord(
                    replacementHandle,
                    currentRecord.Host,
                    replacementSummary);
                _revision++;
                physicalBindingCommitted = true;
            }

            var canonicalSelectionRequest = new PlayerActorSelectionRequest(
                playerSlotId,
                replacementRequest.ActorProfile,
                resolvedSource,
                resolvedReason,
                currentSlot.SelectionRevision);
            PlayerActorSelectionResult selectionResult =
                _participationContext.TryReplaceActorSelection(
                    canonicalSelectionRequest,
                    CommitPhysicalBinding);
            if (selectionResult == null || !selectionResult.Succeeded)
            {
                return HandoffFailedAfterPreviousDeactivation(
                    selectionResult != null
                        ? "Actor selection commit failed after Actor A was deactivated: " + selectionResult.Message
                        : "Actor selection commit returned no result after Actor A was deactivated.");
            }

            if (!physicalBindingCommitted)
            {
                // Replacing with the already selected profile can be a valid explicit
                // physical handoff with no ActorSelectionChanged publication.
                CommitPhysicalBinding(selectionResult.Slot);
            }

            PlayerActorPreparationSummary committedSummary = replacementSummary;

            bool previousReleased = _materializationAdapter.TryReleaseMaterialization(
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
                    committedSummary,
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
                committedSummary,
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
            if (TryCreateActorTransitionSelectionRejection(
                    "SelectActorProfile",
                    request,
                    out PlayerActorSelectionResult transitionRejection))
            {
                return transitionRejection;
            }

            return HasPreparedOrFailedRecord(request.PlayerSlotId)
                ? CreatePreparedSelectionRejection(
                    "SelectActorProfile",
                    request,
                    "Actor selection cannot change while a Logical Player Actor is prepared. Use ReplacePreparedActor.")
                : _participationContext.TrySelectActorProfile(request);
        }

        internal PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            if (TryCreateActorTransitionSelectionRejection(
                    "ReplaceActorSelection",
                    request,
                    out PlayerActorSelectionResult transitionRejection))
            {
                return transitionRejection;
            }

            return HasPreparedOrFailedRecord(request.PlayerSlotId)
                ? CreatePreparedSelectionRejection(
                    "ReplaceActorSelection",
                    request,
                    "Actor selection cannot change while a Logical Player Actor is prepared. Use ReplacePreparedActor.")
                : _participationContext.TryReplaceActorSelection(request);
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            if (TryCreateActorTransitionSelectionRejection(
                    "ClearActorSelection",
                    request,
                    out PlayerActorSelectionResult transitionRejection))
            {
                return transitionRejection;
            }

            return HasPreparedOrFailedRecord(request.PlayerSlotId)
                ? CreatePreparedSelectionRejection(
                    "ClearActorSelection",
                    request,
                    "Actor selection cannot be cleared while a Logical Player Actor is prepared. Release it first.")
                : _participationContext.TryClearActorSelection(request);
        }

        internal PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            if (_actorReplacementTransitions.Contains(playerSlotId) &&
                _participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot transitionSlot))
            {
                return CreateSelectionRejection(
                    PlayerActorSelectionStatus.RejectedTransitionInProgress,
                    "SelectDefaultActor",
                    transitionSlot,
                    source,
                    reason,
                    "Actor selection cannot change during the Player Slot's guarded physical Actor transition.");
            }

            if (HasPreparedOrFailedRecord(playerSlotId))
            {
                var request = new PlayerActorSelectionRequest(
                    playerSlotId,
                    null,
                    source,
                    reason,
                    expectedSelectionRevision);
                if (!_participationContext.TryGetActorSelection(
                        playerSlotId,
                        out PlayerSlotRuntimeSnapshot slot))
                {
                    return CreatePreparedSelectionRejection(
                        "SelectDefaultActor",
                        request,
                        "Default Actor selection cannot change while a Logical Player Actor is prepared.");
                }

                if (request.HasExpectedSelectionRevision &&
                    request.ExpectedSelectionRevision != slot.SelectionRevision)
                {
                    return CreateSelectionRejection(
                        PlayerActorSelectionStatus.RejectedStaleSelectionRevision,
                        "SelectDefaultActor",
                        slot,
                        source,
                        reason,
                        $"Expected selection revision '{request.ExpectedSelectionRevision}' does not match current revision '{slot.SelectionRevision}'.");
                }

                ActorProfile defaultActorProfile =
                    slot.Profile != null ? slot.Profile.DefaultActorProfile : null;
                if (slot.HasSelectedActor &&
                    defaultActorProfile != null &&
                    defaultActorProfile.TryGetActorProfileId(
                        out ActorProfileId defaultActorProfileId,
                        out _) &&
                    slot.SelectedActorProfileId == defaultActorProfileId)
                {
                    return CreateCurrentSelectionResult(
                        PlayerActorSelectionStatus.SucceededSelected,
                        "SelectDefaultActor",
                        slot,
                        source,
                        reason,
                        "Requested default ActorProfile is already selected; no runtime state changed.");
                }

                return CreatePreparedSelectionRejection(
                    "SelectDefaultActor",
                    request,
                    "Default Actor selection cannot change while a Logical Player Actor is prepared.");
            }

            return _participationContext.TrySelectDefaultActor(
                playerSlotId,
                expectedSelectionRevision,
                source,
                reason);
        }

        private bool TryCreateActorTransitionSelectionRejection(
            string operation,
            PlayerActorSelectionRequest request,
            out PlayerActorSelectionResult result)
        {
            result = null;
            if (!_actorReplacementTransitions.Contains(request.PlayerSlotId) ||
                !_participationContext.TryGetActorSelection(
                    request.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                return false;
            }

            result = CreateSelectionRejection(
                PlayerActorSelectionStatus.RejectedTransitionInProgress,
                operation,
                slot,
                request.Source,
                request.Reason,
                "Actor selection cannot change during the Player Slot's guarded physical Actor transition.");
            return true;
        }

        internal bool TryGetPreparationSummary(
            PlayerSlotId playerSlotId,
            out PlayerActorPreparationSummary summary)
        {
            if (!playerSlotId.IsValid ||
                !_participationContext.TryGetActorSelection(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                summary = default;
                return false;
            }

            if (_records.TryGetValue(playerSlotId, out PreparationRecord record))
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
                _participationContext.CreateSnapshot();
            var summaries = new PlayerActorPreparationSummary[
                participationSnapshot.ConfiguredSlotCount];
            for (int index = 0; index < participationSnapshot.ConfiguredSlotCount; index++)
            {
                PlayerSlotRuntimeSnapshot slot = participationSnapshot.Slots[index];
                summaries[index] = _records.TryGetValue(
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
                _retainedReleaseFailures.Count];
            for (int index = 0; index < _retainedReleaseFailures.Count; index++)
            {
                retained[index] = _retainedReleaseFailures[index]
                    .Handle
                    .CreateSnapshot();
            }

            return new PlayerActorPreparationSnapshot(
                _sessionContextId,
                _revision,
                summaries,
                retained,
                _lastOperationStatus,
                _lastOperationMessage);
        }

        private bool IsCurrentIdempotentPreparation(
            PreparationRecord record,
            PlayerSlotRuntimeSnapshot slot,
            PlayerHostEvidenceSnapshot hostEvidence)
        {
            return record != null &&
                record.Summary.IsPrepared &&
                (record.Handle.State is PlayerActorMaterializationState.Active or
                    PlayerActorMaterializationState.StagedInactive) &&
                record.Handle.Request.Owner.Scope == RuntimeContentScope.Session &&
                record.Handle.Request.ActorProfileId == slot.SelectedActorProfileId &&
                record.Summary.SelectionRevision == slot.SelectionRevision &&
                record.Summary.ActorEvidence.ProvisioningOrigin ==
                    hostEvidence.PhysicalProvisioningMode &&
                ReferenceEquals(record.Host, hostEvidence.Host) &&
                ReferenceEquals(record.Handle.LocalPlayerHost, hostEvidence.Host);
        }

        private bool MatchesExpectedPreparation(
            PreparationRecord record,
            PlayerActorPreparationToken expectedPreparation)
        {
            return expectedPreparation.IsValid &&
                record != null &&
                expectedPreparation.SessionContextId == _sessionContextId &&
                expectedPreparation.PlayerSlotId == record.Summary.PlayerSlotId &&
                expectedPreparation == record.Summary.Token;
        }

        private bool HasPreparedOrFailedRecord(PlayerSlotId playerSlotId)
        {
            return playerSlotId.IsValid &&
                (_records.ContainsKey(playerSlotId) ||
                    HasRetainedReleaseFailure(playerSlotId));
        }

        private PlayerActorSelectionResult CreatePreparedSelectionRejection(
            string operation,
            PlayerActorSelectionRequest request,
            string message)
        {
            if (!_participationContext.TryGetActorSelection(
                    request.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                return _participationContext.TrySelectActorProfile(request);
            }

            return CreateSelectionRejection(
                PlayerActorSelectionStatus.RejectedLogicalActorAlreadyPrepared,
                operation,
                slot,
                request.Source,
                request.Reason,
                message);
        }

        private PlayerActorSelectionResult CreateCurrentSelectionResult(
            PlayerActorSelectionStatus status,
            string operation,
            PlayerSlotRuntimeSnapshot slot,
            string source,
            string reason,
            string message)
        {
            PlayerParticipationSnapshot snapshot =
                _participationContext.CreateSnapshot();
            return new PlayerActorSelectionResult(
                status,
                operation,
                slot.PlayerSlotId,
                slot.Profile,
                slot.SelectedActorProfile,
                slot.SelectedActorProfile,
                slot.SelectionRevision,
                slot.SelectionRevision,
                snapshot.ActorSelectionDuplicatePolicy,
                default,
                source.NormalizeText(),
                reason.NormalizeText(),
                message,
                slot,
                snapshot);
        }

        private PlayerActorSelectionResult CreateSelectionRejection(
            PlayerActorSelectionStatus status,
            string operation,
            PlayerSlotRuntimeSnapshot slot,
            string source,
            string reason,
            string message)
        {
            return CreateCurrentSelectionResult(
                status,
                operation,
                slot,
                source,
                reason,
                message);
        }

        private bool TryResolveCurrentActorCorrelation(
            RuntimeScopeContext scopeContext,
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring expectedHost,
            out PlayerSlotAssignmentSnapshot assignment,
            out PlayerHostEvidenceSnapshot hostEvidence,
            out string issue)
        {
            assignment = default;
            hostEvidence = default;
            issue = string.Empty;

            if (!scopeContext.IsValid ||
                scopeContext.Owner.Scope is not
                    RuntimeContentScope.Activity and not
                    RuntimeContentScope.Route)
            {
                issue =
                    "Logical Player Actor preparation requires an explicit Activity or Route Runtime Content owner.";
                _hostEvidenceProjection.LogContextDiagnostic("correlation", "TryResolveCurrentActorCorrelation",
                    playerSlotId, scopeContext.Owner, nameof(PlayerActorPreparationRuntimeContext),
                    "validate-contextual-correlation", "failed", issue, expectedHost);
                return false;
            }

            if (!_participationContext.TryGetCurrentAssignment(
                    playerSlotId,
                    out assignment) ||
                !assignment.IsAssigned ||
                assignment.AssignmentOwner != scopeContext.Owner)
            {
                issue =
                    $"Player Slot '{playerSlotId.StableText}' has no current assignment for the expected Activity/Route owner.";
                _hostEvidenceProjection.LogContextDiagnostic("correlation", "TryResolveCurrentActorCorrelation",
                    playerSlotId, scopeContext.Owner, nameof(PlayerActorPreparationRuntimeContext),
                    "validate-contextual-correlation", "failed", issue, expectedHost);
                return false;
            }

            PlayerHostEvidenceResult confirmation =
                _hostEvidenceProjection.ConfirmHostEvidence(
                    playerSlotId,
                    nameof(PlayerActorPreparationRuntimeContext),
                    "confirm-host-before-actor-correlation");
            if (confirmation == null || !confirmation.Succeeded)
            {
                issue = confirmation != null
                    ? confirmation.ToDiagnosticString()
                    : "Physical Host evidence confirmation returned no result.";
                _hostEvidenceProjection.LogContextDiagnostic("correlation", "TryResolveCurrentActorCorrelation",
                    playerSlotId, scopeContext.Owner, nameof(PlayerActorPreparationRuntimeContext),
                    "validate-contextual-correlation", "failed", issue, expectedHost);
                return false;
            }

            hostEvidence = confirmation.CurrentEvidence;
            if (hostEvidence.AssignmentToken != assignment.AssignmentToken ||
                hostEvidence.HostBindingIdentity != assignment.HostBindingIdentity ||
                !hostEvidence.HostIsAvailable ||
                !hostEvidence.Host.IsJoined ||
                !hostEvidence.Host.HasJoinedSlot ||
                hostEvidence.Host.JoinedPlayerSlotId != playerSlotId ||
                (expectedHost != null &&
                 !ReferenceEquals(expectedHost, hostEvidence.Host)))
            {
                issue =
                    "Physical Host evidence does not match the current assignment, binding or expected Host.";
                _hostEvidenceProjection.LogContextDiagnostic("correlation", "TryResolveCurrentActorCorrelation",
                    playerSlotId, scopeContext.Owner, nameof(PlayerActorPreparationRuntimeContext),
                    "validate-contextual-correlation", "failed", issue, expectedHost);
                return false;
            }

            _hostEvidenceProjection.LogContextDiagnostic("correlation", "TryResolveCurrentActorCorrelation",
                playerSlotId, scopeContext.Owner, nameof(PlayerActorPreparationRuntimeContext),
                "validate-contextual-correlation", "succeeded", issue, expectedHost);

            return true;
        }

        private bool TryEnsureContextualProjection(
            RuntimeScopeContext activityScopeContext,
            PlayerSlotRuntimeSnapshot slot,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!activityScopeContext.IsValid ||
                activityScopeContext.Owner.Scope is not RuntimeContentScope.Activity and not RuntimeContentScope.Route)
            {
                issue =
                    "Contextual projection requires an explicit Activity or Route Runtime Content owner.";
                return false;
            }

            if (_participationContext.TryGetCurrentAssignment(
                    slot.PlayerSlotId,
                    out PlayerSlotAssignmentSnapshot current))
            {
                if (!current.IsAssigned)
                {
                    issue = "The current Player Slot contextual assignment is not readable.";
                    return false;
                }

                if (current.AssignmentOwner != activityScopeContext.Owner)
                {
                    issue =
                        "Contextual projection is still owned by another Activity/Route occurrence and must be retired before a new occurrence acquires the Slot.";
                    return false;
                }

                PlayerHostEvidenceResult confirmation =
                    _hostEvidenceProjection.ConfirmHostEvidence(
                        slot.PlayerSlotId,
                        source,
                        reason + "; confirm-contextual-host");
                if (confirmation != null && confirmation.Succeeded &&
                    confirmation.CurrentEvidence.AssignmentToken == current.AssignmentToken &&
                    confirmation.CurrentEvidence.HostBindingIdentity == current.HostBindingIdentity)
                {
                    return true;
                }

                issue = confirmation != null
                    ? confirmation.ToDiagnosticString()
                    : "Contextual Host projection confirmation returned no result.";
                return false;
            }

            PlayerHostEvidenceResult hostResult = null;
            if (!_hostEvidenceProjection.TryGetSessionPhysicalHost(
                    slot.PlayerSlotId,
                    out LocalPlayerHostAuthoring host,
                    out hostResult))
            {
                issue = hostResult != null
                    ? hostResult.ToDiagnosticString()
                    : "Contextual projection requires a retained Session physical Host.";
                return false;
            }

            PlayerHostBindingIdentity binding =
                _participationContext.CreateHostBindingIdentity();
            PlayerSlotAssignmentResult assignment = _participationContext.BeginAssignment(
                slot.PlayerSlotId,
                activityScopeContext.Owner,
                binding,
                source,
                reason + "; acquire-contextual-assignment");
            if (assignment == null || !assignment.Succeeded ||
                !assignment.HasCurrentAssignment)
            {
                issue = assignment != null
                    ? assignment.ToDiagnosticString()
                    : "Contextual assignment acquisition returned no result.";
                return false;
            }

            PlayerHostEvidenceResult projection = _hostEvidenceProjection.ReprojectHostEvidence(
                slot.PlayerSlotId,
                assignment.CurrentAssignment.AssignmentToken,
                binding,
                source,
                reason + "; project-contextual-host");
            if (projection != null && projection.Succeeded)
            {
                return true;
            }

            _participationContext.ReleaseAssignment(
                slot.PlayerSlotId,
                assignment.CurrentAssignment.AssignmentToken,
                source,
                reason + "; rollback-contextual-assignment");
            issue = projection != null
                ? projection.ToDiagnosticString()
                : "Contextual Host projection returned no result.";
            return false;
        }

        private PlayerActorPreparationSummary CreateUnpreparedSummary(
            PlayerSlotRuntimeSnapshot slot,
            string source,
            string reason,
            string message)
        {
            return new PlayerActorPreparationSummary(
                _sessionContextId,
                slot.PlayerSlotId,
                PlayerActorPreparationState.Unprepared,
                slot.SelectedActorProfileId,
                slot.SelectionRevision,
                default,
                default,
                source,
                reason,
                message);
        }

        private void CommitOccurrenceIdentity(PlayerActorMaterializationHandle handle)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));
            if (handle.OccurrenceId.IsValid)
                return;

            ActorOccurrenceId occurrenceId = _occurrenceIds.Next();
            if (!handle.TryCommitOccurrenceIdentity(occurrenceId))
                throw new InvalidOperationException(
                    "Player Actor occurrence identity can only be committed once its exact materialization is active.");
        }

        internal static ActorId CreateLogicalActorId()
        {
            return ActorId.From($"player-actor:{Guid.NewGuid():N}");
        }

        internal static bool TryResolveReplacementActorId(
            ActorId currentActorId,
            PlayerActorReplacementIdentityPolicy identityPolicy,
            out ActorId replacementActorId,
            out string issue)
        {
            switch (identityPolicy)
            {
                case PlayerActorReplacementIdentityPolicy.PreserveLogicalActor:
                    replacementActorId = currentActorId;
                    if (!replacementActorId.IsValid)
                    {
                        issue = "Prepared Actor replacement cannot preserve a missing logical ActorId.";
                        return false;
                    }

                    issue = string.Empty;
                    return true;

                case PlayerActorReplacementIdentityPolicy.StartNewLogicalActor:
                    replacementActorId = CreateLogicalActorId();
                    issue = string.Empty;
                    return true;

                default:
                    replacementActorId = default;
                    issue = $"Prepared Actor replacement identity policy '{identityPolicy}' is unsupported.";
                    return false;
            }
        }

        private PlayerActorPreparationSummary CreatePreparedSummary(
            PlayerSlotRuntimeSnapshot slot,
            PlayerActorMaterializationHandle handle,
            PlayerHostEvidenceSnapshot hostEvidence,
            PlayerActorPhysicalOwnership physicalOwnership,
            PlayerActorPreparationState state,
            string source,
            string reason,
            string message)
        {
            _actorCorrelationRevision++;
            var actorEvidence = new PlayerActorCorrelationEvidence(
                _sessionContextId,
                slot.PlayerSlotId,
                hostEvidence.PhysicalProvisioningMode,
                slot.SelectedActorProfileId,
                slot.SelectionRevision,
                handle.Request.ActorId,
                handle.Request.RuntimeContentIdentity,
                handle.Request.MaterializationRevision,
                physicalOwnership,
                _actorCorrelationRevision,
                source,
                reason);
            return new PlayerActorPreparationSummary(
                _sessionContextId,
                slot.PlayerSlotId,
                state,
                slot.SelectedActorProfileId,
                slot.SelectionRevision,
                handle.CreateSnapshot(),
                actorEvidence,
                source,
                reason,
                message);
        }

        private static PlayerActorPreparationSummary CreateFailedReleaseSummary(
            PlayerActorPreparationSummary current,
            string source,
            string reason,
            string message)
        {
            return new PlayerActorPreparationSummary(
                current.SessionContextId,
                current.PlayerSlotId,
                PlayerActorPreparationState.ReleaseFailed,
                current.SelectedActorProfileId,
                current.SelectionRevision,
                current.Materialization,
                current.ActorEvidence,
                source,
                reason,
                message);
        }

        private void RetainReleaseFailure(
            PlayerActorMaterializationHandle handle,
            string issue)
        {
            for (int index = 0; index < _retainedReleaseFailures.Count; index++)
            {
                if (ReferenceEquals(_retainedReleaseFailures[index].Handle, handle))
                {
                    _retainedReleaseFailures[index].Issue = issue.NormalizeText();
                    return;
                }
            }

            _retainedReleaseFailures.Add(
                new RetainedReleaseFailure(handle, issue));
            _revision++;
        }

        private bool HasRetainedReleaseFailure(PlayerSlotId playerSlotId)
        {
            for (int index = 0; index < _retainedReleaseFailures.Count; index++)
            {
                if (_retainedReleaseFailures[index].Handle.Request.Slot.PlayerSlotId == playerSlotId)
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
            for (int index = _retainedReleaseFailures.Count - 1; index >= 0; index--)
            {
                RetainedReleaseFailure retained = _retainedReleaseFailures[index];
                if (retained.Handle.Request.Slot.PlayerSlotId != playerSlotId)
                {
                    continue;
                }

                bool released = _materializationAdapter.TryReleaseMaterialization(
                    retained.Handle,
                    source,
                    "retry-retained-previous-release",
                    out string retainedIssue);
                if (released)
                {
                    _retainedReleaseFailures.RemoveAt(index);
                    _revision++;
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
            PlayerActorPreparationStatus originalStatus = PlayerActorPreparationStatus.None,
            bool cleanupPending = false)
        {
            _lastOperationStatus = status;
            _lastOperationMessage = message.NormalizeText();
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
                originalStatus,
                cleanupPending);
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
