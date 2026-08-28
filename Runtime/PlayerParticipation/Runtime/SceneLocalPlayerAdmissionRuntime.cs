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
    /// Session-scoped plain C# authority for admitting original Scene-Provided Player
    /// candidates. Successful Actor adoption promotes the same composition to Session
    /// lifetime; Activity records remain contextual only.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-019 Session-scoped Scene Local Player host admission and contextual representation authority.")]
    internal sealed partial class SceneLocalPlayerAdmissionRuntime
    {
        private enum ContextualReleaseAuthorization
        {
            ActivityExit = 10,
            SessionPlayerLeave = 20,
            SessionTermination = 30
        }

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

        private readonly PlayerParticipationRuntimeContext _participationContext;
        private readonly ISceneLocalPlayerAssignmentReleaseRuntimePort _assignmentReleasePort;
        private readonly List<AdmissionRecord> _records = new();
        private readonly Dictionary<PlayerSlotId, AdmissionRecord> _recordsBySlot = new();
        private int _operationSequence;

        internal SceneLocalPlayerAdmissionRuntime(
            PlayerParticipationRuntimeContext participationContext)
            : this(participationContext, participationContext)
        {
        }

        internal SceneLocalPlayerAdmissionRuntime(
            PlayerParticipationRuntimeContext participationContext,
            ISceneLocalPlayerAssignmentReleaseRuntimePort assignmentReleasePort)
        {
            this._participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
            this._assignmentReleasePort = assignmentReleasePort ??
                throw new ArgumentNullException(nameof(assignmentReleasePort));
        }

        internal int ActiveAdmissionCount => _records.Count;

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
                    _participationContext.TryGetSlotSnapshot(
                        existing.Token.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot currentSlot) &&
                    currentSlot.IsJoined;
                PlayerSlotAssignmentResult assignmentConfirmation =
                    _participationContext.TryConfirmCurrentAssignment(
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
                _recordsBySlot.TryGetValue(playerSlotId, out AdmissionRecord conflictingSlotRecord);
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
            if (_participationContext.TryGetSlotSnapshot(
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot currentSessionSlot) &&
                currentSessionSlot.IsJoined)
            {
                if (!_participationContext.TryGetEffectiveHostProvisioningMode(
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
                    _participationContext.CreateHostBindingIdentity();
                PlayerSlotAssignmentResult assignment =
                    _participationContext.BeginAssignment(
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
                        allowExistingActorRuntime: true,
                        expectedSceneRuntimeHost: authoring.ScenePlayerActorRuntimeHost,
                        out string hostIssue))
                {
                    PlayerSlotAssignmentResult assignmentCompensation =
                        _participationContext.ReleaseAssignment(
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

                _operationSequence++;
                var token = new SceneLocalPlayerAdmissionToken(
                    _participationContext.CreateSnapshot().ContextId,
                    _operationSequence,
                    playerSlotId,
                    currentSessionSlot.Revision,
                    assignment.CurrentAssignment.AssignmentToken);
                var record = new AdmissionRecord(
                    authoring,
                    host,
                    currentSessionSlot,
                    token,
                    assignment.CurrentAssignment);
                _records.Add(record);
                _recordsBySlot.Add(playerSlotId, record);

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
                _participationContext.TryReserveSceneLocalPlayerSlot(
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
                    allowExistingActorRuntime: true,
                    expectedSceneRuntimeHost: authoring.ScenePlayerActorRuntimeHost,
                    out string hostStageIssue))
            {
                PlayerParticipationOperationResult rollback =
                    _participationContext.TryReleaseReservation(
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
                _participationContext.TryMarkJoined(
                    reservation.ReservationToken,
                    resolvedSource,
                    resolvedReason);
            if (commit == null || !commit.Succeeded)
            {
                host.RollbackStagedAdmission(
                    resolvedSource,
                    "scene-slot-commit-failed");
                PlayerParticipationOperationResult rollback =
                    _participationContext.TryReleaseReservation(
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

            _operationSequence++;
            var slotAdmissionToken = new SceneLocalPlayerAdmissionToken(
                commit.Snapshot.ContextId,
                _operationSequence,
                commit.Slot.PlayerSlotId,
                commit.Slot.Revision);

            PlayerHostBindingIdentity initialHostBindingIdentity =
                _participationContext.CreateHostBindingIdentity();
            PlayerSlotAssignmentResult initialAssignment =
                _participationContext.BeginAssignment(
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
                    _participationContext.TryAbandonCommittedSceneAdmission(
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
                _operationSequence,
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
                    _participationContext.ReleaseAssignment(
                        commit.Slot.PlayerSlotId,
                        initialToken.AssignmentToken,
                        resolvedSource,
                        "scene-host-commit-failed");
                PlayerParticipationOperationResult compensation =
                    _participationContext.TryAbandonCommittedSceneAdmission(
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
            _records.Add(initialRecord);
            _recordsBySlot.Add(playerSlotId, initialRecord);

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
                "Scene Local Player Host admitted to the exact ordered Session Slot. The original composition remains candidate-owned until successful Actor adoption.",
                assignmentResult: initialAssignment);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryRelease(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            return TryReleaseCore(
                authoring,
                expectedToken,
                ContextualReleaseAuthorization.ActivityExit,
                default,
                source,
                reason);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryReleaseForSessionPlayerLeave(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason)
        {
            return TryReleaseCore(
                authoring,
                expectedToken,
                ContextualReleaseAuthorization.SessionPlayerLeave,
                leaveToken,
                source,
                reason);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult TryReleaseForSessionTermination(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            return TryReleaseCore(
                authoring,
                expectedToken,
                ContextualReleaseAuthorization.SessionTermination,
                default,
                source,
                reason);
        }

        private SceneLocalPlayerAdmissionRuntimeResult TryReleaseCore(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            ContextualReleaseAuthorization authorization,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason)
        {
            string operation = authorization switch
            {
                ContextualReleaseAuthorization.SessionPlayerLeave =>
                    "RetireSceneLocalPlayerForSessionLeave",
                ContextualReleaseAuthorization.SessionTermination =>
                    "RetireSceneLocalPlayerForSessionTermination",
                _ => "ReleaseSceneLocalPlayer"
            };
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
                _participationContext.TryConfirmCurrentAssignment(
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

            if (!TryResolveContextualReleaseSlot(
                    record,
                    authorization,
                    leaveToken,
                    resolvedSource,
                    resolvedReason,
                    out PlayerSlotRuntimeSnapshot currentSessionSlot,
                    out SceneLocalPlayerAdmissionRuntimeStatus slotStatus,
                    out string slotIssue))
            {
                return Result(
                    slotStatus,
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
                    slotIssue);
            }

            bool hostAdmissionAlreadyRetired =
                (authorization is ContextualReleaseAuthorization.SessionPlayerLeave or
                    ContextualReleaseAuthorization.SessionTermination) &&
                record.Host != null &&
                record.Host.IsAdmissionReleased;
            if (record.Host == null ||
                (!hostAdmissionAlreadyRetired &&
                 (!record.Host.IsJoined ||
                  !record.Host.HasJoinedSlot ||
                  record.Host.JoinedPlayerSlotId != record.Token.PlayerSlotId)))
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

            if (!hostAdmissionAlreadyRetired &&
                !record.Host.TryValidateCommittedAdmissionRelease(
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
            if (!hostAdmissionAlreadyRetired &&
                !record.Host.TryReleaseCommittedAdmission(
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
                _assignmentReleasePort.ReleaseAssignment(
                    record.Token.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    resolvedSource,
                    resolvedReason);
            if (assignmentRelease == null || !assignmentRelease.Succeeded)
            {
                if (authorization != ContextualReleaseAuthorization.ActivityExit)
                {
                    return Result(
                        SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit,
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
                            ? authorization == ContextualReleaseAuthorization.SessionPlayerLeave
                                ? "Session Leave contextual assignment release failed after local admission retirement. The exact Leaving occurrence retains only residual assignment cleanup for retry. " + assignmentRelease.Message
                                : "Session termination contextual assignment release failed after local admission retirement. Only residual shutdown cleanup remains. " + assignmentRelease.Message
                            : authorization == ContextualReleaseAuthorization.SessionPlayerLeave
                                ? "Session Leave contextual assignment release returned no result after local admission retirement. The exact Leaving occurrence retains only residual assignment cleanup for retry."
                                : "Session termination contextual assignment release returned no result after local admission retirement. Only residual shutdown cleanup remains.",
                        SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit,
                        assignmentRelease);
                }

                bool hostRestored = record.Host.TryRestoreCommittedAdmission(
                    currentSessionSlot,
                    resolvedSource,
                    "scene-assignment-release-failed",
                    allowExistingActorRuntime: true,
                    expectedSceneRuntimeHost: authoring.ScenePlayerActorRuntimeHost,
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

            _records.Remove(record);
            _recordsBySlot.Remove(record.Token.PlayerSlotId);

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
                authorization == ContextualReleaseAuthorization.SessionPlayerLeave
                    ? "Non-adopted Scene Local Player contextual representation and current Slot assignment retired for the exact Leaving Session Player occurrence. Session physical release remains downstream."
                    : authorization == ContextualReleaseAuthorization.SessionTermination
                        ? "Non-adopted Scene Local Player contextual representation and current Slot assignment retired by Session termination authority."
                        : "Non-adopted Scene Local Player contextual representation and current Slot assignment released. Session Player Slot remains Joined.",
                assignmentResult: assignmentRelease);
        }

        /// <summary>
        /// Retires the contextual Activity admission after physical Scene Actor adoption.
        /// The contextual Slot assignment is released; Session-owned physical preparation
        /// and Host evidence deliberately remain outside this operation.
        /// </summary>
        internal SceneLocalPlayerAdmissionRuntimeResult TryRetireContextualRepresentation(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            string source,
            string reason)
        {
            return TryRetireContextualRepresentationCore(
                authoring,
                expectedToken,
                ContextualReleaseAuthorization.ActivityExit,
                default,
                source,
                reason);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult
            TryRetireContextualRepresentationForSessionPlayerLeave(
                SceneLocalPlayerAdmissionAuthoring authoring,
                SceneLocalPlayerAdmissionToken expectedToken,
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            return TryRetireContextualRepresentationCore(
                authoring,
                expectedToken,
                ContextualReleaseAuthorization.SessionPlayerLeave,
                leaveToken,
                source,
                reason);
        }

        internal SceneLocalPlayerAdmissionRuntimeResult
            TryRetireContextualRepresentationForSessionTermination(
                SceneLocalPlayerAdmissionAuthoring authoring,
                SceneLocalPlayerAdmissionToken expectedToken,
                string source,
                string reason)
        {
            return TryRetireContextualRepresentationCore(
                authoring,
                expectedToken,
                ContextualReleaseAuthorization.SessionTermination,
                default,
                source,
                reason);
        }

        private SceneLocalPlayerAdmissionRuntimeResult TryRetireContextualRepresentationCore(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken expectedToken,
            ContextualReleaseAuthorization authorization,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason)
        {
            string operation = authorization switch
            {
                ContextualReleaseAuthorization.SessionPlayerLeave =>
                    "RetireSceneLocalPlayerContextForSessionLeave",
                ContextualReleaseAuthorization.SessionTermination =>
                    "RetireSceneLocalPlayerContextForSessionTermination",
                _ => "RetireSceneLocalPlayerContext"
            };
            string resolvedSource = source.NormalizeTextOrFallback(nameof(SceneLocalPlayerAdmissionRuntime));
            string resolvedReason = reason.NormalizeTextOrFallback("retire-scene-local-player-context");
            AdmissionRecord record = authoring != null ? FindRecordByAuthoring(authoring) : null;
            if (record == null)
            {
                return Result(
                    expectedToken.IsValid
                        ? SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken
                        : SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyReleased,
                    operation, authoring, expectedToken, null, null, null, default, default,
                    resolvedSource, resolvedReason,
                    "Scene Local Player contextual representation has no active admission record.");
            }

            PlayerSlotAssignmentResult assignmentConfirmation =
                _participationContext.TryConfirmCurrentAssignment(
                    record.Token.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    resolvedSource,
                    "confirm-contextual-retirement");
            if (!expectedToken.IsValid || record.Token != expectedToken ||
                assignmentConfirmation == null || !assignmentConfirmation.Succeeded ||
                assignmentConfirmation.CurrentAssignment.AssignmentOrigin !=
                    PlayerSlotAssignmentOrigin.SceneProvided ||
                assignmentConfirmation.CurrentAssignment.HostBindingIdentity !=
                    record.Assignment.HostBindingIdentity ||
                assignmentConfirmation.CurrentAssignment.AssignmentOwner.Scope is not
                    (RuntimeContentScope.Activity or RuntimeContentScope.Route))
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                    operation, authoring, expectedToken, null, null, null,
                    record.JoinedSlot, record.JoinedSlot, resolvedSource, resolvedReason,
                    assignmentConfirmation != null
                        ? "Contextual retirement requires the exact current Scene-provided Activity/Route assignment. " +
                          assignmentConfirmation.Message
                        : "Contextual retirement assignment confirmation returned no result.",
                    assignmentResult: assignmentConfirmation);
            }

            if (!TryResolveContextualReleaseSlot(
                    record,
                    authorization,
                    leaveToken,
                    resolvedSource,
                    resolvedReason,
                    out PlayerSlotRuntimeSnapshot currentSessionSlot,
                    out SceneLocalPlayerAdmissionRuntimeStatus slotStatus,
                    out string slotIssue))
            {
                return Result(
                    slotStatus,
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
                    slotIssue,
                    assignmentResult: assignmentConfirmation);
            }

            PlayerSlotAssignmentResult assignmentRelease =
                _assignmentReleasePort.ReleaseAssignment(
                    record.Token.PlayerSlotId,
                    expectedToken.AssignmentToken,
                    resolvedSource,
                    resolvedReason);
            if (assignmentRelease == null || !assignmentRelease.Succeeded)
            {
                return Result(
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit,
                    operation, authoring, record.Token, null, null, null,
                    record.JoinedSlot, currentSessionSlot, resolvedSource, resolvedReason,
                    assignmentRelease != null
                        ? "Contextual retirement could not release its exact current Slot assignment. " +
                          assignmentRelease.Message
                        : "Contextual retirement assignment release returned no result.",
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit,
                    assignmentRelease);
            }

            _records.Remove(record);
            _recordsBySlot.Remove(record.Token.PlayerSlotId);
            return Result(
                SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased,
                operation, authoring, record.Token, null, null, null,
                record.JoinedSlot, currentSessionSlot, resolvedSource, resolvedReason,
                authorization == ContextualReleaseAuthorization.SessionPlayerLeave
                    ? "Scene Local Player Activity admission and contextual Slot assignment retired for the exact Leaving Session Player occurrence; Session physical preparation and Host evidence remain retained until terminal release."
                    : authorization == ContextualReleaseAuthorization.SessionTermination
                        ? "Scene Local Player Activity admission and contextual Slot assignment retired by Session termination authority."
                        : "Scene Local Player Activity admission and contextual Slot assignment retired; Session physical preparation and Host evidence remain retained.",
                assignmentResult: assignmentRelease);
        }

        private bool TryResolveContextualReleaseSlot(
            AdmissionRecord record,
            ContextualReleaseAuthorization authorization,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason,
            out PlayerSlotRuntimeSnapshot currentSlot,
            out SceneLocalPlayerAdmissionRuntimeStatus status,
            out string issue)
        {
            currentSlot = default;
            status = SceneLocalPlayerAdmissionRuntimeStatus.FailedInvariant;
            issue = string.Empty;
            if (!_participationContext.TryGetSlotSnapshot(
                    record.Token.PlayerSlotId,
                    out currentSlot))
            {
                issue = "Scene contextual release could not resolve the represented Session Player Slot.";
                return false;
            }

            if (authorization == ContextualReleaseAuthorization.ActivityExit)
            {
                if (currentSlot.IsJoined)
                {
                    return true;
                }

                issue = "Scene contextual release requires the represented Session Player Slot to remain Joined.";
                return false;
            }

            if (authorization == ContextualReleaseAuthorization.SessionTermination)
            {
                return true;
            }

            SessionPlayerLeaveRuntimeResult leaveConfirmation =
                _participationContext.TryConfirmSessionPlayerLeave(
                    leaveToken,
                    source,
                    reason + "; confirm-session-player-leave-contextual-retirement");
            if (leaveConfirmation == null || !leaveConfirmation.Succeeded)
            {
                status = SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken;
                issue = leaveConfirmation != null
                    ? "Scene contextual retirement rejected a foreign or stale Session Player Leave correlation. " +
                      leaveConfirmation.Message
                    : "Scene contextual retirement received no Session Player Leave confirmation result.";
                return false;
            }

            if (leaveToken.PlayerSlotId != record.Token.PlayerSlotId ||
                leaveToken.ExpectedOccurrenceRevision != record.Token.JoinedSlotRevision ||
                record.JoinedSlot.Revision != leaveToken.ExpectedOccurrenceRevision ||
                currentSlot.AllocationState != PlayerSlotAllocationState.Leaving ||
                currentSlot.Revision != leaveToken.LeavingSlotRevision)
            {
                status = SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken;
                issue = "Scene contextual retirement Leave correlation does not match the exact admitted Session Player occurrence.";
                return false;
            }

            return true;
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
            for (int index = 0; index < _records.Count; index++)
            {
                AdmissionRecord candidate = _records[index];
                if (candidate != null && ReferenceEquals(candidate.Authoring, authoring))
                {
                    return candidate;
                }
            }

            return null;
        }

        private AdmissionRecord FindRecordByHost(LocalPlayerHostAuthoring host)
        {
            for (int index = 0; index < _records.Count; index++)
            {
                AdmissionRecord candidate = _records[index];
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
