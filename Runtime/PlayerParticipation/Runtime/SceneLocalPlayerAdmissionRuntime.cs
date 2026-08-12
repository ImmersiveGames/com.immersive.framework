using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal interface ISceneLocalPlayerAssignmentReleaseRuntimePort
    {
        PlayerSlotAssignmentResult ReleaseAssignment(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken expectedToken,
            string source,
            string reason);
    }

    /// <summary>
    /// Session-scoped plain C# authority for admitting and releasing externally owned scene
    /// Local Player Hosts. Session membership is retained independently from Activity/Route
    /// representation. Physical object creation/destruction, Actor selection and gameplay
    /// readiness remain outside this transaction.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-019 Session-scoped Scene Local Player host admission and contextual representation authority.")]
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
        private readonly ISceneLocalPlayerAssignmentReleaseRuntimePort assignmentReleasePort;
        private readonly List<AdmissionRecord> records = new();
        private readonly Dictionary<PlayerSlotId, AdmissionRecord> recordsBySlot = new();
        private int operationSequence;

        internal SceneLocalPlayerAdmissionRuntime(
            PlayerParticipationRuntimeContext participationContext)
            : this(participationContext, participationContext)
        {
        }

        internal SceneLocalPlayerAdmissionRuntime(
            PlayerParticipationRuntimeContext participationContext,
            ISceneLocalPlayerAssignmentReleaseRuntimePort assignmentReleasePort)
        {
            this.participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
            this.assignmentReleasePort = assignmentReleasePort ??
                throw new ArgumentNullException(nameof(assignmentReleasePort));
        }

        internal int ActiveAdmissionCount => records.Count;

        internal SceneLocalPlayerAdmissionRuntimeResult TryAdmit(
            SceneLocalPlayerAdmissionAuthoring authoring,
            RuntimeContentOwner assignmentOwner,
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

            if (!assignmentOwner.IsValid ||
                assignmentOwner.Scope is not RuntimeContentScope.Activity and not
                    RuntimeContentScope.Route)
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
                    "Scene Local Player admission requires an explicit Activity or Route assignment owner.");
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
                    assignmentConfirmation != null &&
                    assignmentConfirmation.Succeeded &&
                    assignmentConfirmation.CurrentAssignment.AssignmentOrigin ==
                        PlayerSlotAssignmentOrigin.SceneProvided &&
                    assignmentConfirmation.CurrentAssignment.AssignmentOwner ==
                        assignmentOwner)
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
                        "Scene Local Player contextual representation is already admitted by the same authoring surface.",
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
                    authoringIssue);
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

            // ADR-019: a Joined Slot is Session membership. A new Activity/Route scene surface
            // reprojects that Session Player instead of reserving or joining the Slot again.
            if (participationContext.TryGetSlotSnapshot(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot currentSessionSlot) &&
                currentSessionSlot.IsJoined)
            {
                if (!participationContext.TryGetEffectiveHostProvisioningMode(
                        playerSlotId,
                        out PlayerHostProvisioningMode hostProvisioningMode) ||
                    hostProvisioningMode != PlayerHostProvisioningMode.SceneProvided)
                {
                    return Result(
                        SceneLocalPlayerAdmissionRuntimeStatus.RejectedConflict,
                        operation,
                        authoring,
                        default,
                        null,
                        null,
                        null,
                        currentSessionSlot,
                        currentSessionSlot,
                        resolvedSource,
                        resolvedReason,
                        $"Scene Local Player reprojection requires SceneProvided provisioning for Joined Slot '{playerSlotId.StableText}'. No provisioning fallback was applied.");
                }

                if (currentSessionSlot.HasSelectedActor &&
                    !AreSameActorProfileIdentity(
                        currentSessionSlot.SelectedActorProfile,
                        authoring.ActorProfile))
                {
                    return Result(
                        SceneLocalPlayerAdmissionRuntimeStatus.RejectedConflict,
                        operation,
                        authoring,
                        default,
                        null,
                        null,
                        null,
                        currentSessionSlot,
                        currentSessionSlot,
                        resolvedSource,
                        resolvedReason,
                        $"Scene Local Player reprojection conflicts with the Session Actor selection for Slot '{playerSlotId.StableText}'. No Actor replacement or fallback was applied.");
                }

                PlayerHostBindingIdentity hostBindingIdentity =
                    participationContext.CreateHostBindingIdentity();
                PlayerSlotAssignmentResult assignment =
                    participationContext.BeginAssignment(
                        playerSlotId,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        assignmentOwner,
                        hostBindingIdentity,
                        resolvedSource,
                        $"{resolvedReason}:reproject");
                if (assignment == null || !assignment.Succeeded)
                {
                    return Result(
                        SceneLocalPlayerAdmissionRuntimeStatus.RejectedConflict,
                        operation,
                        authoring,
                        default,
                        null,
                        null,
                        null,
                        currentSessionSlot,
                        currentSessionSlot,
                        resolvedSource,
                        resolvedReason,
                        assignment != null
                            ? "Scene Local Player reprojection could not acquire the contextual current Slot assignment. " + assignment.Message
                            : "Scene Local Player reprojection current Slot assignment returned no result.",
                        assignmentResult: assignment);
                }

                if (!host.TryRestoreCommittedAdmission(
                        currentSessionSlot,
                        resolvedSource,
                        $"{resolvedReason}:reproject",
                        allowExistingLogicalActor: true,
                        expectedSceneActor: authoring.SceneLogicalPlayerActor,
                        out string hostIssue))
                {
                    PlayerSlotAssignmentResult assignmentCompensation =
                        participationContext.ReleaseAssignment(
                            playerSlotId,
                            assignment.CurrentAssignment.AssignmentToken,
                            resolvedSource,
                            "scene-reprojection-host-bind-failed");
                    bool assignmentRestored =
                        assignmentCompensation != null && assignmentCompensation.Succeeded;
                    return Result(
                        assignmentRestored
                            ? SceneLocalPlayerAdmissionRuntimeStatus.FailedHostCommit
                            : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation,
                        operation,
                        authoring,
                        default,
                        null,
                        null,
                        null,
                        currentSessionSlot,
                        currentSessionSlot,
                        resolvedSource,
                        resolvedReason,
                        assignmentRestored
                            ? hostIssue
                            : $"{hostIssue} Contextual assignment compensation failed. {(assignmentCompensation != null ? assignmentCompensation.Message : "No assignment compensation result.")}",
                        SceneLocalPlayerAdmissionRuntimeStatus.FailedHostCommit,
                        assignment,
                        assignmentCompensation);
                }

                operationSequence++;
                var token = new SceneLocalPlayerAdmissionToken(
                    participationContext.CreateSnapshot().ContextId,
                    operationSequence,
                    playerSlotId,
                    currentSessionSlot.Revision,
                    assignment.CurrentAssignment.AssignmentToken);
                var record = new AdmissionRecord(
                    authoring,
                    host,
                    currentSessionSlot,
                    token,
                    assignment.CurrentAssignment);
                records.Add(record);
                recordsBySlot.Add(playerSlotId, record);

                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted,
                    operation,
                    authoring,
                    token,
                    null,
                    null,
                    null,
                    currentSessionSlot,
                    currentSessionSlot,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player representation reprojected onto the existing Joined Session Player without reserve, vacate or re-Join.",
                    assignmentResult: assignment);
            }

            // First Scene-Provided occurrence still performs the one Session Join.
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

            PlayerHostBindingIdentity initialHostBindingIdentity =
                participationContext.CreateHostBindingIdentity();
            PlayerSlotAssignmentResult initialAssignment =
                participationContext.BeginAssignment(
                    commit.Slot.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    assignmentOwner,
                    initialHostBindingIdentity,
                    resolvedSource,
                    resolvedReason);
            if (initialAssignment == null || !initialAssignment.Succeeded)
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
                    initialAssignment != null
                        ? "Canonical current Slot assignment failed. " + initialAssignment.Message
                        : "Canonical current Slot assignment returned no result.",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedSlotCommit,
                    initialAssignment);
            }

            var initialToken = new SceneLocalPlayerAdmissionToken(
                commit.Snapshot.ContextId,
                operationSequence,
                commit.Slot.PlayerSlotId,
                commit.Slot.Revision,
                initialAssignment.CurrentAssignment.AssignmentToken);

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
                        initialToken.AssignmentToken,
                        resolvedSource,
                        "scene-host-commit-failed");
                PlayerParticipationOperationResult compensation =
                    participationContext.TryAbandonCommittedSceneAdmission(
                        slotAdmissionToken,
                        resolvedSource,
                        "scene-host-commit-failed");
                bool assignmentReleased =
                    assignmentCompensation != null && assignmentCompensation.Succeeded;
                bool slotReleased = compensation != null && compensation.Succeeded;
                SceneLocalPlayerAdmissionRuntimeStatus status =
                    assignmentReleased && slotReleased
                        ? SceneLocalPlayerAdmissionRuntimeStatus.FailedHostCommit
                        : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation;
                return Result(
                    status,
                    operation,
                    authoring,
                    initialToken,
                    reservation,
                    commit,
                    compensation,
                    reservation.Slot,
                    slotReleased ? compensation.Slot : commit.Slot,
                    resolvedSource,
                    resolvedReason,
                    assignmentReleased && slotReleased
                        ? $"Local Player Host commit failed. {exception.Message}"
                        : $"Local Player Host commit failed and explicit compensation failed. assignmentReleased='{assignmentReleased}' slotReleased='{slotReleased}'. {exception.Message} {(compensation != null ? compensation.Message : "No compensation result.")}",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedHostCommit,
                    initialAssignment,
                    assignmentCompensation);
            }

            var initialRecord = new AdmissionRecord(
                authoring,
                host,
                commit.Slot,
                initialToken,
                initialAssignment.CurrentAssignment);
            records.Add(initialRecord);
            recordsBySlot.Add(playerSlotId, initialRecord);

            return Result(
                SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted,
                operation,
                authoring,
                initialToken,
                reservation,
                commit,
                null,
                reservation.Slot,
                commit.Slot,
                resolvedSource,
                resolvedReason,
                "Scene Local Player Host admitted to the exact ordered Session Slot. Physical Host and Logical Actor remain externally owned.",
                assignmentResult: initialAssignment);
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
                        "Scene Local Player contextual representation is already released.")
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
                        "Expected Scene Local Player admission token is stale because no active contextual record exists.");
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
                    "Scene Local Player contextual release rejected a foreign or stale admission token.");
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
                        ? "Scene contextual release rejected current assignment evidence. " + assignmentConfirmation.Message
                        : "Scene contextual release assignment confirmation returned no result.",
                    assignmentResult: assignmentConfirmation);
            }

            if (!participationContext.TryGetSlotSnapshot(
                    record.Token.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot currentSessionSlot) ||
                !currentSessionSlot.IsJoined)
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
                    "Scene contextual release requires the represented Session Player Slot to remain Joined.");
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
                    currentSessionSlot,
                    currentSessionSlot,
                    resolvedSource,
                    resolvedReason,
                    "Active Scene Local Player contextual record has no matching committed Host evidence.");
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
                    currentSessionSlot,
                    currentSessionSlot,
                    resolvedSource,
                    resolvedReason,
                    hostValidationIssue);
            }

            // ADR-019: Activity/Route release retires contextual Host evidence and the current
            // SceneProvided assignment only. It does not transition the Session Slot to Leaving
            // or Available and it does not clear Session Actor selection intent.
            if (!record.Host.TryReleaseCommittedAdmission(
                    record.Token.PlayerSlotId,
                    resolvedSource,
                    resolvedReason,
                    out string hostReleaseIssue))
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedHostRelease,
                    operation,
                    authoring,
                    record.Token,
                    null,
                    null,
                    null,
                    currentSessionSlot,
                    currentSessionSlot,
                    resolvedSource,
                    resolvedReason,
                    hostReleaseIssue);
            }

            PlayerSlotAssignmentResult assignmentRelease =
                assignmentReleasePort.ReleaseAssignment(
                    record.Token.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    resolvedSource,
                    resolvedReason);
            if (assignmentRelease == null || !assignmentRelease.Succeeded)
            {
                bool hostRestored = record.Host.TryRestoreCommittedAdmission(
                    currentSessionSlot,
                    resolvedSource,
                    "scene-assignment-release-failed",
                    allowExistingLogicalActor: true,
                    expectedSceneActor: authoring.SceneLogicalPlayerActor,
                    out string hostRestoreIssue);

                return Result(
                    hostRestored
                        ? SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit
                        : SceneLocalPlayerAdmissionRuntimeStatus.FailedCompensation,
                    operation,
                    authoring,
                    record.Token,
                    null,
                    null,
                    null,
                    currentSessionSlot,
                    currentSessionSlot,
                    resolvedSource,
                    resolvedReason,
                    assignmentRelease != null
                        ? "Canonical contextual assignment release failed. " + assignmentRelease.Message +
                          $" hostRestored='{hostRestored}' hostIssue='{hostRestoreIssue}'."
                        : $"Canonical contextual assignment release returned no result. hostRestored='{hostRestored}' hostIssue='{hostRestoreIssue}'.",
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
                null,
                null,
                record.JoinedSlot,
                currentSessionSlot,
                resolvedSource,
                resolvedReason,
                "Scene Local Player contextual representation and current Slot assignment released. Session Player Slot remains Joined; Scene Host and Logical Actor remain externally owned.",
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

        private static bool AreSameActorProfileIdentity(
            ActorProfile left,
            ActorProfile right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return left.TryGetActorProfileId(out ActorProfileId leftId, out _) &&
                   right.TryGetActorProfileId(out ActorProfileId rightId, out _) &&
                   leftId == rightId;
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
                PlayerParticipationOperationStatus.RejectedNoAvailableSlot =>
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedSlotUnavailable,
                _ => SceneLocalPlayerAdmissionRuntimeStatus.FailedReservation
            };
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
