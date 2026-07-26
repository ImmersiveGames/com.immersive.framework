using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped plain C# authority for admitting and releasing externally owned scene
    /// Local Player Hosts. Physical object creation/destruction, Actor selection and gameplay
    /// readiness remain outside this transaction.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3M4B1 Session-scoped Scene Local Player host and Slot admission transaction authority.")]
    internal sealed class SceneLocalPlayerAdmissionRuntime
    {
        private sealed class AdmissionRecord
        {
            internal AdmissionRecord(
                SceneLocalPlayerAdmissionAuthoring authoring,
                LocalPlayerHostAuthoring host,
                PlayerSlotRuntimeSnapshot joinedSlot,
                SceneLocalPlayerAdmissionToken token,
                PlayerSlotAssignmentSnapshot assignment)
            {
                Authoring = authoring;
                Host = host;
                JoinedSlot = joinedSlot;
                Token = token;
                Assignment = assignment;
            }

            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerSlotRuntimeSnapshot JoinedSlot { get; set; }
            internal SceneLocalPlayerAdmissionToken Token { get; set; }
            internal PlayerSlotAssignmentSnapshot Assignment { get; set; }
        }

        private readonly PlayerParticipationRuntimeContext participationContext;
        private readonly List<AdmissionRecord> records = new();
        private readonly Dictionary<PlayerSlotId, AdmissionRecord> recordsBySlot = new();
        private int operationSequence;

        internal SceneLocalPlayerAdmissionRuntime(
            PlayerParticipationRuntimeContext participationContext)
        {
            this.participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
        }

        internal int ActiveAdmissionCount => records.Count;

        internal SceneLocalPlayerAdmissionRuntimeResult TryAdmit(
            SceneLocalPlayerAdmissionAuthoring authoring,
            string source,
            string reason)
        {
            const string operation = "AdmitSceneLocalPlayer";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(SceneLocalPlayerAdmissionRuntime));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "scene-local-player-admission");

            if (authoring == null)
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedInvalidRequest,
                    operation,
                    null,
                    default,
                    null,
                    null,
                    null,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player admission requires an explicit authoring surface.");
            }

            AdmissionRecord existing = FindRecordByAuthoring(authoring);
            if (existing != null)
            {
                bool currentSlotMatches =
                    participationContext.TryGetSlotSnapshot(
                        existing.Token.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot currentSlot) &&
                    currentSlot.IsJoined;
                PlayerSlotAssignmentResult assignmentConfirmation =
                    participationContext.TryConfirmCurrentAssignment(
                        existing.Token.PlayerSlotId,
                        existing.Token.AssignmentToken,
                        resolvedSource,
                        "confirm-idempotent-scene-admission");
                if (ReferenceEquals(existing.Authoring, authoring) &&
                    existing.Host != null &&
                    existing.Host.IsJoined &&
                    existing.Host.JoinedPlayerSlotId == existing.Token.PlayerSlotId &&
                    currentSlotMatches &&
                    assignmentConfirmation.Succeeded &&
                    assignmentConfirmation.CurrentAssignment.AssignmentOrigin ==
                        PlayerSlotAssignmentOrigin.SceneProvided)
                {
                    existing.JoinedSlot = currentSlot;
                    return Result(
                        SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyAdmitted,
                        operation,
                        authoring,
                        existing.Token,
                        null,
                        null,
                        null,
                        existing.JoinedSlot,
                        existing.JoinedSlot,
                        resolvedSource,
                        resolvedReason,
                        "Scene Local Player is already admitted by the same authoring surface.",
                        assignmentResult: assignmentConfirmation);
                }

                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedConflict,
                    operation,
                    authoring,
                    existing.Token,
                    null,
                    null,
                    null,
                    existing.JoinedSlot,
                    existing.JoinedSlot,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player authoring identity already owns a conflicting admission record.");
            }

            if (!authoring.TryValidateRuntimeEvidence(out string authoringIssue))
            {
                string issue = authoringIssue;
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedInvalidRequest,
                    operation,
                    authoring,
                    default,
                    null,
                    null,
                    null,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            if (!authoring.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string slotIssue))
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedInvalidRequest,
                    operation,
                    authoring,
                    default,
                    null,
                    null,
                    null,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    slotIssue);
            }

            LocalPlayerHostAuthoring host = authoring.LocalPlayerHost;
            bool hasSlotConflict =
                recordsBySlot.TryGetValue(playerSlotId, out AdmissionRecord conflictingSlotRecord);
            AdmissionRecord conflictingHostRecord = FindRecordByHost(host);
            bool hasHostConflict = conflictingHostRecord != null;
            if (hasSlotConflict || hasHostConflict)
            {
                string slotOwner = hasSlotConflict && conflictingSlotRecord.Authoring != null
                    ? conflictingSlotRecord.Authoring.name
                    : string.Empty;
                string hostOwner = hasHostConflict && conflictingHostRecord.Authoring != null
                    ? conflictingHostRecord.Authoring.name
                    : string.Empty;
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedConflict,
                    operation,
                    authoring,
                    default,
                    null,
                    null,
                    null,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    $"Scene Local Player Slot or Host is already owned by another admission surface. slotOwner='{slotOwner}' hostOwner='{hostOwner}'.");
            }

            if (host.IsJoined || host.IsAdmissionStaged || host.IsReleaseStaged)
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedConflict,
                    operation,
                    authoring,
                    default,
                    null,
                    null,
                    null,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player Host already carries admission state owned by another operation.");
            }

            PlayerParticipationOperationResult reservation =
                participationContext.TryReserveSceneLocalPlayerSlot(
                    playerSlotId,
                    resolvedSource,
                    resolvedReason,
                    out bool orderedSlotMismatch);
            if (reservation == null || !reservation.Succeeded)
            {
                SceneLocalPlayerAdmissionRuntimeStatus status = MapReservationFailure(
                    reservation,
                    orderedSlotMismatch);
                return Result(
                    status,
                    operation,
                    authoring,
                    default,
                    reservation,
                    reservation,
                    null,
                    default,
                    reservation != null ? reservation.Slot : default,
                    resolvedSource,
                    resolvedReason,
                    reservation != null
                        ? reservation.Message
                        : "Scene Local Player Slot reservation returned no result.");
            }

            if (!host.TryStageAdmission(
                    reservation.Slot,
                    resolvedSource,
                    resolvedReason,
                    allowExistingLogicalActor: true,
                    expectedSceneActor: authoring.SceneLogicalPlayerActor,
                    out string hostStageIssue))
            {
                PlayerParticipationOperationResult rollback =
                    participationContext.TryReleaseReservation(
                        reservation.ReservationToken,
                        resolvedSource,
                        "scene-host-stage-failed");
                SceneLocalPlayerAdmissionRuntimeStatus status = rollback != null && rollback.Succeeded
                    ? SceneLocalPlayerAdmissionRuntimeStatus.FailedHostStage
                    : SceneLocalPlayerAdmissionRuntimeStatus.FailedReservationRollback;
                return Result(
                    status,
                    operation,
                    authoring,
                    default,
                    reservation,
                    null,
                    rollback,
                    reservation.Slot,
                    rollback != null ? rollback.Slot : reservation.Slot,
                    resolvedSource,
                    resolvedReason,
                    rollback != null && rollback.Succeeded
                        ? hostStageIssue
                        : $"{hostStageIssue} Reservation rollback failed. {(rollback != null ? rollback.Message : "No rollback result.")}",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedHostStage);
            }

            PlayerParticipationOperationResult commit =
                participationContext.TryMarkJoined(
                    reservation.ReservationToken,
                    resolvedSource,
                    resolvedReason);
            if (commit == null || !commit.Succeeded)
            {
                host.RollbackStagedAdmission(
                    resolvedSource,
                    "scene-slot-commit-failed");
                PlayerParticipationOperationResult rollback =
                    participationContext.TryReleaseReservation(
                        reservation.ReservationToken,
                        resolvedSource,
                        "scene-slot-commit-failed");
                SceneLocalPlayerAdmissionRuntimeStatus status = rollback != null && rollback.Succeeded
                    ? SceneLocalPlayerAdmissionRuntimeStatus.FailedSlotCommit
                    : SceneLocalPlayerAdmissionRuntimeStatus.FailedReservationRollback;
                return Result(
                    status,
                    operation,
                    authoring,
                    default,
                    reservation,
                    commit,
                    rollback,
                    reservation.Slot,
                    rollback != null ? rollback.Slot : reservation.Slot,
                    resolvedSource,
                    resolvedReason,
                    commit != null
                        ? commit.Message
                        : "Scene Local Player Slot commit returned no result.",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedSlotCommit);
            }

            operationSequence++;
            var slotAdmissionToken = new SceneLocalPlayerAdmissionToken(
                commit.Snapshot.ContextId,
                operationSequence,
                commit.Slot.PlayerSlotId,
                commit.Slot.Revision);

            PlayerHostBindingIdentity hostBindingIdentity =
                participationContext.CreateHostBindingIdentity();
            RuntimeContentOwner assignmentOwner =
                participationContext.CreateSessionAssignmentOwner();
            PlayerSlotAssignmentResult assignment =
                participationContext.BeginAssignment(
                    commit.Slot.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    assignmentOwner,
                    hostBindingIdentity,
                    resolvedSource,
                    resolvedReason);
            if (assignment == null || !assignment.Succeeded)
            {
                host.RollbackStagedAdmission(
                    resolvedSource,
                    "scene-assignment-begin-failed");
                PlayerParticipationOperationResult compensation =
                    participationContext.TryAbandonCommittedSceneAdmission(
                        slotAdmissionToken,
                        resolvedSource,
                        "scene-assignment-begin-failed");
                SceneLocalPlayerAdmissionRuntimeStatus status =
                    compensation != null && compensation.Succeeded
                        ? SceneLocalPlayerAdmissionRuntimeStatus.FailedSlotCommit
                        : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation;
                return Result(
                    status,
                    operation,
                    authoring,
                    slotAdmissionToken,
                    reservation,
                    commit,
                    compensation,
                    reservation.Slot,
                    compensation != null ? compensation.Slot : commit.Slot,
                    resolvedSource,
                    resolvedReason,
                    assignment != null
                        ? "Canonical current Slot assignment failed. " +
                          assignment.Message
                        : "Canonical current Slot assignment returned no result.",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedSlotCommit,
                    assignment);
            }

            var token = new SceneLocalPlayerAdmissionToken(
                commit.Snapshot.ContextId,
                operationSequence,
                commit.Slot.PlayerSlotId,
                commit.Slot.Revision,
                assignment.CurrentAssignment.AssignmentToken);

            try
            {
                host.CommitStagedAdmission(
                    commit.Slot,
                    resolvedSource,
                    resolvedReason);
            }
            catch (Exception exception)
            {
                host.RollbackStagedAdmission(
                    resolvedSource,
                    "scene-host-commit-failed");
                PlayerSlotAssignmentResult assignmentCompensation =
                    participationContext.ReleaseAssignment(
                        commit.Slot.PlayerSlotId,
                        token.AssignmentToken,
                        resolvedSource,
                        "scene-host-commit-failed");
                PlayerParticipationOperationResult compensation =
                    participationContext.TryAbandonCommittedSceneAdmission(
                        slotAdmissionToken,
                        resolvedSource,
                        "scene-host-commit-failed");
                SceneLocalPlayerAdmissionRuntimeStatus status =
                    assignmentCompensation.Succeeded &&
                    compensation != null &&
                    compensation.Succeeded
                        ? SceneLocalPlayerAdmissionRuntimeStatus.FailedHostCommit
                        : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation;
                return Result(
                    status,
                    operation,
                    authoring,
                    token,
                    reservation,
                    commit,
                    compensation,
                    reservation.Slot,
                    compensation != null ? compensation.Slot : commit.Slot,
                    resolvedSource,
                    resolvedReason,
                    assignmentCompensation.Succeeded &&
                    compensation != null &&
                    compensation.Succeeded
                        ? $"Local Player Host commit failed. {exception.Message}"
                        : $"Local Player Host commit failed and explicit compensation failed. assignmentReleased='{assignmentCompensation.Succeeded}' slotReleased='{(compensation != null && compensation.Succeeded)}'. {exception.Message} {(compensation != null ? compensation.Message : "No compensation result.")}",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedHostCommit,
                    assignment,
                    assignmentCompensation);
            }

            var record = new AdmissionRecord(
                authoring,
                host,
                commit.Slot,
                token,
                assignment.CurrentAssignment);
            records.Add(record);
            recordsBySlot.Add(playerSlotId, record);

            return Result(
                SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted,
                operation,
                authoring,
                token,
                reservation,
                commit,
                null,
                reservation.Slot,
                commit.Slot,
                resolvedSource,
                resolvedReason,
                "Scene Local Player Host admitted to the exact ordered Session Slot. Physical Host and Logical Actor remain externally owned.",
                assignmentResult: assignment);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryRelease(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            const string operation = "ReleaseSceneLocalPlayer";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(SceneLocalPlayerAdmissionRuntime));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "scene-local-player-release");

            if (authoring == null)
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedInvalidRequest,
                    operation,
                    null,
                    expectedToken,
                    null,
                    null,
                    null,
                    default,
                    default,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player release requires an explicit authoring surface.");
            }

            AdmissionRecord record = FindRecordByAuthoring(authoring);
            if (record == null)
            {
                return !expectedToken.IsValid
                    ? Result(
                        SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyReleased,
                        operation,
                        authoring,
                        default,
                        null,
                        null,
                        null,
                        default,
                        default,
                        resolvedSource,
                        resolvedReason,
                        "Scene Local Player is already released.")
                    : Result(
                        SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                        operation,
                        authoring,
                        expectedToken,
                        null,
                        null,
                        null,
                        default,
                        default,
                        resolvedSource,
                        resolvedReason,
                        "Expected Scene Local Player admission token is stale because no active record exists.");
            }

            if (!expectedToken.IsValid || expectedToken != record.Token)
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                    operation,
                    authoring,
                    expectedToken,
                    null,
                    null,
                    null,
                    record.JoinedSlot,
                    record.JoinedSlot,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player release rejected a foreign or stale admission token.");
            }

            PlayerSlotAssignmentResult assignmentConfirmation =
                participationContext.TryConfirmCurrentAssignment(
                    record.Token.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    resolvedSource,
                    "confirm-scene-assignment-release");
            if (assignmentConfirmation == null ||
                !assignmentConfirmation.Succeeded ||
                assignmentConfirmation.CurrentAssignment.AssignmentOrigin !=
                    PlayerSlotAssignmentOrigin.SceneProvided ||
                assignmentConfirmation.CurrentAssignment.HostBindingIdentity !=
                    record.Assignment.HostBindingIdentity)
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                    operation,
                    authoring,
                    expectedToken,
                    null,
                    null,
                    null,
                    record.JoinedSlot,
                    record.JoinedSlot,
                    resolvedSource,
                    resolvedReason,
                    assignmentConfirmation != null
                        ? "Scene admission release rejected current assignment evidence. " +
                          assignmentConfirmation.Message
                        : "Scene admission release assignment confirmation returned no result.",
                    assignmentResult: assignmentConfirmation);
            }

            if (record.Host == null ||
                !record.Host.IsJoined ||
                !record.Host.HasJoinedSlot ||
                record.Host.JoinedPlayerSlotId != record.Token.PlayerSlotId)
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedInvariant,
                    operation,
                    authoring,
                    record.Token,
                    null,
                    null,
                    null,
                    record.JoinedSlot,
                    record.JoinedSlot,
                    resolvedSource,
                    resolvedReason,
                    "Active Scene Local Player admission record has no matching committed Host evidence.");
            }

            if (!record.Host.TryValidateCommittedAdmissionRelease(
                    record.Token.PlayerSlotId,
                    out string hostValidationIssue))
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedHostRelease,
                    operation,
                    authoring,
                    record.Token,
                    null,
                    null,
                    null,
                    record.JoinedSlot,
                    record.JoinedSlot,
                    resolvedSource,
                    resolvedReason,
                    hostValidationIssue);
            }

            PlayerParticipationOperationResult begin =
                participationContext.TryBeginSceneLocalPlayerRelease(
                    record.Token,
                    resolvedSource,
                    resolvedReason,
                    out SceneLocalPlayerAdmissionReleaseToken releaseToken);
            if (begin == null || !begin.Succeeded || !releaseToken.IsValid)
            {
                return Result(
                    MapReleaseBeginFailure(begin),
                    operation,
                    authoring,
                    record.Token,
                    null,
                    begin,
                    null,
                    record.JoinedSlot,
                    begin != null ? begin.Slot : record.JoinedSlot,
                    resolvedSource,
                    resolvedReason,
                    begin != null ? begin.Message : "Scene Local Player release begin returned no result.");
            }

            if (!record.Host.TryReleaseCommittedAdmission(
                    record.Token.PlayerSlotId,
                    resolvedSource,
                    resolvedReason,
                    out string hostReleaseIssue))
            {
                PlayerParticipationOperationResult rollback =
                    participationContext.TryRollbackSceneLocalPlayerRelease(
                        releaseToken,
                        resolvedSource,
                        "scene-host-release-failed");
                UpdateRecordAfterReleaseRollback(record, rollback);
                SceneLocalPlayerAdmissionRuntimeStatus status = rollback != null && rollback.Succeeded
                    ? SceneLocalPlayerAdmissionRuntimeStatus.FailedHostRelease
                    : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation;
                return Result(
                    status,
                    operation,
                    authoring,
                    record.Token,
                    null,
                    begin,
                    rollback,
                    record.JoinedSlot,
                    rollback != null ? rollback.Slot : begin.Slot,
                    resolvedSource,
                    resolvedReason,
                    rollback != null && rollback.Succeeded
                        ? hostReleaseIssue
                        : $"{hostReleaseIssue} Slot rollback failed. {(rollback != null ? rollback.Message : "No rollback result.")}",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedHostRelease);
            }

            PlayerParticipationOperationResult commit =
                participationContext.TryCommitSceneLocalPlayerRelease(
                    releaseToken,
                    resolvedSource,
                    resolvedReason);
            if (commit == null || !commit.Succeeded)
            {
                PlayerParticipationOperationResult rollback =
                    participationContext.TryRollbackSceneLocalPlayerRelease(
                        releaseToken,
                        resolvedSource,
                        "scene-slot-release-commit-failed");
                bool slotRestored = rollback != null && rollback.Succeeded;
                PlayerSlotRuntimeSnapshot restoreSlot = slotRestored
                    ? rollback.Slot
                    : record.JoinedSlot;
                bool hostRestored = record.Host.TryRestoreCommittedAdmission(
                    restoreSlot,
                    resolvedSource,
                    "scene-slot-release-commit-failed",
                    allowExistingLogicalActor: true,
                    expectedSceneActor: authoring.SceneLogicalPlayerActor,
                    out string hostRestoreIssue);
                if (slotRestored && hostRestored)
                {
                    UpdateRecordAfterReleaseRollback(record, rollback);
                }

                SceneLocalPlayerAdmissionRuntimeStatus status = slotRestored && hostRestored
                    ? SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit
                    : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation;
                string commitIssue = commit != null
                    ? commit.Message
                    : "Scene Local Player Slot release commit returned no result.";
                return Result(
                    status,
                    operation,
                    authoring,
                    record.Token,
                    null,
                    commit,
                    rollback,
                    record.JoinedSlot,
                    slotRestored ? rollback.Slot : begin.Slot,
                    resolvedSource,
                    resolvedReason,
                    status == SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit
                        ? commitIssue
                        : $"{commitIssue} Compensation failed. slotRestored='{slotRestored}' hostRestored='{hostRestored}' hostIssue='{hostRestoreIssue}'.",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit);
            }

            PlayerSlotAssignmentResult assignmentRelease =
                participationContext.ReleaseAssignment(
                    record.Token.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    resolvedSource,
                    resolvedReason);
            if (assignmentRelease == null || !assignmentRelease.Succeeded)
            {
                PlayerParticipationOperationResult slotRestore =
                    participationContext.TryRestoreJoinedSlotAfterAssignmentReleaseFailure(
                        record.Token.PlayerSlotId,
                        expectedToken.AssignmentToken,
                        resolvedSource,
                        "scene-assignment-release-failed");
                bool slotRestored =
                    slotRestore != null &&
                    slotRestore.Succeeded &&
                    slotRestore.Slot.IsJoined;
                string hostRestoreIssue = string.Empty;
                bool hostRestored = slotRestored &&
                    record.Host.TryRestoreCommittedAdmission(
                        slotRestore.Slot,
                        resolvedSource,
                        "scene-assignment-release-failed",
                        allowExistingLogicalActor: true,
                        expectedSceneActor: authoring.SceneLogicalPlayerActor,
                        out hostRestoreIssue);
                if (slotRestored && hostRestored)
                {
                    record.JoinedSlot = slotRestore.Slot;
                    record.Token = new SceneLocalPlayerAdmissionToken(
                        record.Token.ContextId,
                        record.Token.OperationSequence,
                        record.Token.PlayerSlotId,
                        slotRestore.Slot.Revision,
                        record.Token.AssignmentToken);
                }

                return Result(
                    slotRestored && hostRestored
                        ? SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit
                        : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation,
                    operation,
                    authoring,
                    record.Token,
                    null,
                    commit,
                    slotRestore,
                    record.JoinedSlot,
                    slotRestored ? slotRestore.Slot : commit.Slot,
                    resolvedSource,
                    resolvedReason,
                    assignmentRelease != null
                        ? "Canonical assignment release failed. " +
                          assignmentRelease.Message +
                          $" slotRestored='{slotRestored}' hostRestored='{hostRestored}' hostIssue='{hostRestoreIssue}'."
                        : "Canonical assignment release returned no result.",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit,
                    assignmentRelease);
            }

            records.Remove(record);
            recordsBySlot.Remove(record.Token.PlayerSlotId);

            return Result(
                SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased,
                operation,
                authoring,
                record.Token,
                null,
                commit,
                null,
                record.JoinedSlot,
                commit.Slot,
                resolvedSource,
                resolvedReason,
                "Scene Local Player admission and canonical current Slot assignment released. Slot returned to Available; physical Host and Logical Actor were preserved.",
                assignmentResult: assignmentRelease);
        }

        internal bool TryGetActiveToken(
            SceneLocalPlayerAdmissionAuthoring authoring,
            out SceneLocalPlayerAdmissionToken token)
        {
            AdmissionRecord record = authoring != null
                ? FindRecordByAuthoring(authoring)
                : null;
            if (record != null)
            {
                token = record.Token;
                return token.IsValid;
            }

            token = default;
            return false;
        }

        private AdmissionRecord FindRecordByAuthoring(
            SceneLocalPlayerAdmissionAuthoring authoring)
        {
            for (int index = 0; index < records.Count; index++)
            {
                AdmissionRecord candidate = records[index];
                if (candidate != null && ReferenceEquals(candidate.Authoring, authoring))
                {
                    return candidate;
                }
            }

            return null;
        }

        private AdmissionRecord FindRecordByHost(LocalPlayerHostAuthoring host)
        {
            for (int index = 0; index < records.Count; index++)
            {
                AdmissionRecord candidate = records[index];
                if (candidate != null && ReferenceEquals(candidate.Host, host))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static SceneLocalPlayerAdmissionRuntimeStatus MapReservationFailure(
            PlayerParticipationOperationResult reservation,
            bool orderedSlotMismatch)
        {
            if (orderedSlotMismatch)
            {
                return SceneLocalPlayerAdmissionRuntimeStatus.RejectedSlotOrderMismatch;
            }

            return reservation?.Status switch
            {
                PlayerParticipationOperationStatus.RejectedInvalidRequest =>
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedInvalidRequest,
                PlayerParticipationOperationStatus.RejectedCapacityReached =>
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedCapacityReached,
                PlayerParticipationOperationStatus.RejectedNoAvailableSlot =>
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedSlotUnavailable,
                _ => SceneLocalPlayerAdmissionRuntimeStatus.FailedReservation
            };
        }

        private static SceneLocalPlayerAdmissionRuntimeStatus MapReleaseBeginFailure(
            PlayerParticipationOperationResult begin)
        {
            return begin?.Status switch
            {
                PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation =>
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                PlayerParticipationOperationStatus.RejectedInvalidState =>
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedDependentState,
                _ => SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseBegin
            };
        }

        private static void UpdateRecordAfterReleaseRollback(
            AdmissionRecord record,
            PlayerParticipationOperationResult rollback)
        {
            if (record == null || rollback == null || !rollback.Succeeded || !rollback.Slot.IsJoined)
            {
                return;
            }

            record.JoinedSlot = rollback.Slot;
            record.Token = new SceneLocalPlayerAdmissionToken(
                record.Token.ContextId,
                record.Token.OperationSequence,
                record.Token.PlayerSlotId,
                rollback.Slot.Revision,
                record.Token.AssignmentToken);
        }

        private static SceneLocalPlayerAdmissionRuntimeResult Result(
            SceneLocalPlayerAdmissionRuntimeStatus status,
            string operation,
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken token,
            PlayerParticipationOperationResult reservationResult,
            PlayerParticipationOperationResult slotOperationResult,
            PlayerParticipationOperationResult compensationResult,
            PlayerSlotRuntimeSnapshot previousSlot,
            PlayerSlotRuntimeSnapshot currentSlot,
            string source,
            string reason,
            string message,
            SceneLocalPlayerAdmissionRuntimeStatus originalStatus =
                SceneLocalPlayerAdmissionRuntimeStatus.None,
            PlayerSlotAssignmentResult assignmentResult = null,
            PlayerSlotAssignmentResult assignmentCompensationResult = null)
        {
            return new SceneLocalPlayerAdmissionRuntimeResult(
                status,
                originalStatus,
                operation,
                authoring,
                token,
                reservationResult,
                slotOperationResult,
                compensationResult,
                previousSlot,
                currentSlot,
                source,
                reason,
                message,
                assignmentResult,
                assignmentCompensationResult);
        }
    }
}
