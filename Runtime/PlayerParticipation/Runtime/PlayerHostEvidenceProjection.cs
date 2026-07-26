using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    internal enum PlayerHostEvidenceStatus
    {
        None = 0,
        SucceededRegistered = 10,
        SucceededAlreadyRegistered = 20,
        SucceededConfirmed = 30,
        SucceededReleased = 40,
        SucceededClearedDivergent = 50,
        RejectedInvalidRequest = 100,
        RejectedNoEvidence = 110,
        RejectedInvalidAssignmentToken = 120,
        RejectedTokenSlotMismatch = 130,
        RejectedForeignAssignmentToken = 140,
        RejectedStaleAssignmentToken = 150,
        RejectedAssignmentOriginMismatch = 160,
        RejectedHostConflict = 170,
        RejectedBindingConflict = 180,
        RejectedHostMismatch = 190,
        RejectedDestroyedHost = 200,
        RejectedEvidenceStillCurrent = 210
    }

    internal readonly struct PlayerHostEvidenceSnapshot
    {
        internal PlayerHostEvidenceSnapshot(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            PlayerSlotId = playerSlotId;
            AssignmentOrigin = assignmentOrigin;
            AssignmentToken = assignmentToken;
            HostBindingIdentity = hostBindingIdentity;
            Host = host;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        internal PlayerSlotId PlayerSlotId { get; }
        internal PlayerSlotAssignmentOrigin AssignmentOrigin { get; }
        internal PlayerSlotAssignmentToken AssignmentToken { get; }
        internal PlayerHostBindingIdentity HostBindingIdentity { get; }
        internal LocalPlayerHostAuthoring Host { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal bool HasRetainedHostReference => !ReferenceEquals(Host, null);
        internal bool HostIsAvailable => HasRetainedHostReference && Host != null;
        internal bool IsRecorded =>
            PlayerSlotId.IsValid &&
            (AssignmentOrigin is
                PlayerSlotAssignmentOrigin.ManagerProvisioned or
                PlayerSlotAssignmentOrigin.SceneProvided) &&
            AssignmentToken.IsValid &&
            HostBindingIdentity.IsValid &&
            HasRetainedHostReference;
    }

    internal sealed class PlayerHostEvidenceResult
    {
        internal PlayerHostEvidenceResult(
            PlayerHostEvidenceStatus status,
            string operation,
            PlayerHostEvidenceSnapshot previousEvidence,
            PlayerHostEvidenceSnapshot currentEvidence,
            PlayerSlotAssignmentResult assignmentResult,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Operation = operation.NormalizeText();
            PreviousEvidence = previousEvidence;
            CurrentEvidence = currentEvidence;
            AssignmentResult = assignmentResult;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        internal PlayerHostEvidenceStatus Status { get; }
        internal string Operation { get; }
        internal PlayerHostEvidenceSnapshot PreviousEvidence { get; }
        internal PlayerHostEvidenceSnapshot CurrentEvidence { get; }
        internal PlayerSlotAssignmentResult AssignmentResult { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal string Message { get; }
        internal bool Succeeded => Status is
            PlayerHostEvidenceStatus.SucceededRegistered or
            PlayerHostEvidenceStatus.SucceededAlreadyRegistered or
            PlayerHostEvidenceStatus.SucceededConfirmed or
            PlayerHostEvidenceStatus.SucceededReleased or
            PlayerHostEvidenceStatus.SucceededClearedDivergent;
        internal bool HasRetainedEvidence => CurrentEvidence.IsRecorded;

        internal string ToDiagnosticString()
        {
            PlayerHostEvidenceSnapshot evidence = CurrentEvidence.IsRecorded
                ? CurrentEvidence
                : PreviousEvidence;
            return $"operation='{Operation}' status='{Status}' " +
                $"slot='{evidence.PlayerSlotId.StableText}' " +
                $"origin='{evidence.AssignmentOrigin}' " +
                $"assignment='{evidence.AssignmentToken.StableText}' " +
                $"binding='{evidence.HostBindingIdentity.StableText}' " +
                $"hostReferenceRetained='{evidence.HasRetainedHostReference}' " +
                $"hostAvailable='{evidence.HostIsAvailable}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }
    }

    /// <summary>
    /// Technical projection that correlates one physical Local Player Host with the
    /// canonical current Player Slot assignment. It never creates or replaces assignment
    /// authority and never repairs divergent evidence during reads.
    /// </summary>
    internal sealed class PlayerHostEvidenceProjection
    {
        private sealed class Record
        {
            internal Record(
                PlayerSlotId playerSlotId,
                PlayerSlotAssignmentOrigin assignmentOrigin,
                PlayerSlotAssignmentToken assignmentToken,
                PlayerHostBindingIdentity hostBindingIdentity,
                LocalPlayerHostAuthoring host,
                string source,
                string reason)
            {
                PlayerSlotId = playerSlotId;
                AssignmentOrigin = assignmentOrigin;
                AssignmentToken = assignmentToken;
                HostBindingIdentity = hostBindingIdentity;
                Host = host;
                Source = source;
                Reason = reason;
            }

            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerSlotAssignmentOrigin AssignmentOrigin { get; }
            internal PlayerSlotAssignmentToken AssignmentToken { get; }
            internal PlayerHostBindingIdentity HostBindingIdentity { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal string Source { get; }
            internal string Reason { get; }
        }

        private readonly PlayerParticipationRuntimeContext participationContext;
        private readonly string sessionContextId;
        private readonly Dictionary<PlayerSlotId, Record> records = new();

        internal PlayerHostEvidenceProjection(
            PlayerParticipationRuntimeContext participationContext)
        {
            this.participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
            PlayerParticipationSnapshot snapshot =
                participationContext.CreateSnapshot();
            sessionContextId = snapshot != null
                ? snapshot.ContextId.NormalizeText()
                : string.Empty;
            if (string.IsNullOrEmpty(sessionContextId))
            {
                throw new InvalidOperationException(
                    "Host evidence projection requires an initialized Player participation context.");
            }
        }

        internal int RetainedEvidenceCount => records.Count;
        internal string SessionContextId => sessionContextId;

        internal PlayerHostEvidenceResult RegisterHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            const string operation = "RegisterHostEvidence";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "register-host-evidence");

            PlayerHostEvidenceResult validation = ValidateRequest(
                operation,
                playerSlotId,
                assignmentOrigin,
                assignmentToken,
                hostBindingIdentity,
                host,
                resolvedSource,
                resolvedReason,
                requireJoinedHost: true);
            if (validation != null)
            {
                return validation;
            }

            PlayerSlotAssignmentResult assignment =
                participationContext.TryConfirmCurrentAssignment(
                    playerSlotId,
                    assignmentToken,
                    resolvedSource,
                    resolvedReason);
            PlayerHostEvidenceResult assignmentFailure = ValidateAssignment(
                operation,
                playerSlotId,
                assignmentOrigin,
                assignmentToken,
                hostBindingIdentity,
                assignment,
                resolvedSource,
                resolvedReason);
            if (assignmentFailure != null)
            {
                return assignmentFailure;
            }

            foreach (KeyValuePair<PlayerSlotId, Record> pair in records)
            {
                if (pair.Key == playerSlotId)
                {
                    continue;
                }

                PlayerHostEvidenceSnapshot conflicting = Snapshot(pair.Value);
                if (ReferenceEquals(pair.Value.Host, host))
                {
                    return Result(
                        PlayerHostEvidenceStatus.RejectedHostConflict,
                        operation,
                        conflicting,
                        conflicting,
                        assignment,
                        resolvedSource,
                        resolvedReason,
                        $"Local Player Host is already retained for Player Slot '{pair.Key.StableText}'.");
                }

                if (pair.Value.HostBindingIdentity == hostBindingIdentity)
                {
                    return Result(
                        PlayerHostEvidenceStatus.RejectedBindingConflict,
                        operation,
                        conflicting,
                        conflicting,
                        assignment,
                        resolvedSource,
                        resolvedReason,
                        $"Host binding is already retained for Player Slot '{pair.Key.StableText}'.");
                }
            }

            if (records.TryGetValue(playerSlotId, out Record existing))
            {
                PlayerHostEvidenceSnapshot existingSnapshot = Snapshot(existing);
                if (existing.AssignmentOrigin == assignmentOrigin &&
                    existing.AssignmentToken == assignmentToken &&
                    existing.HostBindingIdentity == hostBindingIdentity &&
                    ReferenceEquals(existing.Host, host))
                {
                    return Result(
                        PlayerHostEvidenceStatus.SucceededAlreadyRegistered,
                        operation,
                        existingSnapshot,
                        existingSnapshot,
                        assignment,
                        resolvedSource,
                        resolvedReason,
                        "The exact physical Host evidence is already registered.");
                }

                PlayerHostEvidenceStatus conflict =
                    !ReferenceEquals(existing.Host, host)
                        ? PlayerHostEvidenceStatus.RejectedHostConflict
                        : existing.HostBindingIdentity != hostBindingIdentity
                            ? PlayerHostEvidenceStatus.RejectedBindingConflict
                            : existing.AssignmentOrigin != assignmentOrigin
                                ? PlayerHostEvidenceStatus.RejectedAssignmentOriginMismatch
                                : PlayerHostEvidenceStatus.RejectedStaleAssignmentToken;
                return Result(
                    conflict,
                    operation,
                    existingSnapshot,
                    existingSnapshot,
                    assignment,
                    resolvedSource,
                    resolvedReason,
                    "Another retained physical Host evidence record already occupies this Player Slot.");
            }

            var record = new Record(
                playerSlotId,
                assignmentOrigin,
                assignmentToken,
                hostBindingIdentity,
                host,
                resolvedSource,
                resolvedReason);
            records.Add(playerSlotId, record);
            PlayerHostEvidenceSnapshot current = Snapshot(record);
            return Result(
                PlayerHostEvidenceStatus.SucceededRegistered,
                operation,
                default,
                current,
                assignment,
                resolvedSource,
                resolvedReason,
                "Physical Host evidence registered as a projection of the canonical assignment.");
        }

        internal bool TryGetHostEvidence(
            PlayerSlotId playerSlotId,
            out LocalPlayerHostAuthoring host,
            out PlayerHostEvidenceResult result)
        {
            result = ConfirmHostEvidence(
                playerSlotId,
                nameof(PlayerHostEvidenceProjection),
                "lookup-current-host-evidence");
            host = result.Succeeded
                ? result.CurrentEvidence.Host
                : null;
            return result.Succeeded && host != null;
        }

        internal bool TryGetRetainedEvidence(
            PlayerSlotId playerSlotId,
            out PlayerHostEvidenceSnapshot evidence)
        {
            if (playerSlotId.IsValid &&
                records.TryGetValue(playerSlotId, out Record record))
            {
                evidence = Snapshot(record);
                return true;
            }

            evidence = default;
            return false;
        }

        internal PlayerHostEvidenceResult ConfirmHostEvidence(
            PlayerSlotId playerSlotId,
            string source,
            string reason)
        {
            const string operation = "ConfirmHostEvidence";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "confirm-host-evidence");
            if (!playerSlotId.IsValid)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedInvalidRequest,
                    operation,
                    default,
                    default,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Host evidence confirmation requires a valid Player Slot identity.");
            }

            if (!records.TryGetValue(playerSlotId, out Record record))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedNoEvidence,
                    operation,
                    default,
                    default,
                    null,
                    resolvedSource,
                    resolvedReason,
                    $"Player Slot '{playerSlotId.StableText}' has no retained physical Host evidence.");
            }

            PlayerHostEvidenceSnapshot retained = Snapshot(record);
            if (ReferenceEquals(record.Host, null))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedHostMismatch,
                    operation,
                    retained,
                    retained,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Retained physical Host evidence has no managed Host reference.");
            }

            if (record.Host == null)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedDestroyedHost,
                    operation,
                    retained,
                    retained,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Retained physical Host evidence references a destroyed Unity Host.");
            }

            if (!record.Host.IsJoined ||
                !record.Host.HasJoinedSlot ||
                record.Host.JoinedPlayerSlotId != playerSlotId)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedHostMismatch,
                    operation,
                    retained,
                    retained,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Retained physical Host no longer carries matching Joined Slot evidence.");
            }

            PlayerSlotAssignmentResult assignment =
                participationContext.TryConfirmCurrentAssignment(
                    playerSlotId,
                    record.AssignmentToken,
                    resolvedSource,
                    resolvedReason);
            PlayerHostEvidenceResult assignmentFailure = ValidateAssignment(
                operation,
                playerSlotId,
                record.AssignmentOrigin,
                record.AssignmentToken,
                record.HostBindingIdentity,
                assignment,
                resolvedSource,
                resolvedReason,
                retained);
            return assignmentFailure ?? Result(
                PlayerHostEvidenceStatus.SucceededConfirmed,
                operation,
                retained,
                retained,
                assignment,
                resolvedSource,
                resolvedReason,
                "Physical Host evidence is current and fully correlated.");
        }

        internal PlayerHostEvidenceResult ReleaseHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring expectedHost,
            string source,
            string reason)
        {
            const string operation = "ReleaseHostEvidence";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-host-evidence");
            PlayerHostEvidenceResult exact = ValidateExactRetainedEvidence(
                operation,
                playerSlotId,
                assignmentToken,
                hostBindingIdentity,
                expectedHost,
                resolvedSource,
                resolvedReason,
                out Record record);
            if (exact != null)
            {
                return exact;
            }

            PlayerHostEvidenceResult confirmation = ConfirmHostEvidence(
                playerSlotId,
                resolvedSource,
                resolvedReason);
            if (!confirmation.Succeeded)
            {
                return confirmation;
            }

            records.Remove(playerSlotId);
            return Result(
                PlayerHostEvidenceStatus.SucceededReleased,
                operation,
                Snapshot(record),
                default,
                confirmation.AssignmentResult,
                resolvedSource,
                resolvedReason,
                "Physical Host evidence released explicitly.");
        }

        internal PlayerHostEvidenceResult ClearDivergentHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring expectedHost,
            string source,
            string reason)
        {
            const string operation = "ClearDivergentHostEvidence";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "clear-divergent-host-evidence");
            PlayerHostEvidenceResult exact = ValidateExactRetainedEvidence(
                operation,
                playerSlotId,
                assignmentToken,
                hostBindingIdentity,
                expectedHost,
                resolvedSource,
                resolvedReason,
                out Record record);
            if (exact != null)
            {
                return exact;
            }

            PlayerHostEvidenceResult confirmation = ConfirmHostEvidence(
                playerSlotId,
                resolvedSource,
                resolvedReason);
            if (confirmation.Succeeded)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedEvidenceStillCurrent,
                    operation,
                    Snapshot(record),
                    Snapshot(record),
                    confirmation.AssignmentResult,
                    resolvedSource,
                    resolvedReason,
                    "Current correlated Host evidence must use ReleaseHostEvidence.");
            }

            records.Remove(playerSlotId);
            return Result(
                PlayerHostEvidenceStatus.SucceededClearedDivergent,
                operation,
                Snapshot(record),
                default,
                confirmation.AssignmentResult,
                resolvedSource,
                resolvedReason,
                "Divergent physical Host evidence cleared explicitly.");
        }

        internal void ClearAll()
        {
            records.Clear();
        }

        private PlayerHostEvidenceResult ValidateRequest(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring host,
            string source,
            string reason,
            bool requireJoinedHost)
        {
            if (!playerSlotId.IsValid ||
                assignmentOrigin is not
                    PlayerSlotAssignmentOrigin.ManagerProvisioned and not
                    PlayerSlotAssignmentOrigin.SceneProvided)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedInvalidRequest,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Host evidence registration requires a valid Slot and supported assignment origin.");
            }

            if (!assignmentToken.IsValid)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedInvalidAssignmentToken,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Host evidence registration requires a valid assignment token.");
            }

            if (assignmentToken.PlayerSlotId != playerSlotId)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedTokenSlotMismatch,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Assignment token belongs to another Player Slot.");
            }

            if (!string.Equals(
                    assignmentToken.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedForeignAssignmentToken,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Assignment token belongs to another Session context.");
            }

            if (!hostBindingIdentity.IsValid ||
                assignmentToken.HostBindingIdentity != hostBindingIdentity)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedBindingConflict,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Host binding identity does not match the assignment token.");
            }

            if (ReferenceEquals(host, null) || host == null)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedDestroyedHost,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Host evidence registration requires an available Local Player Host.");
            }

            if (requireJoinedHost &&
                (!host.IsJoined ||
                 !host.HasJoinedSlot ||
                 host.JoinedPlayerSlotId != playerSlotId))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedHostMismatch,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Local Player Host does not carry matching Joined Slot evidence.");
            }

            return null;
        }

        private PlayerHostEvidenceResult ValidateAssignment(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            PlayerSlotAssignmentResult assignment,
            string source,
            string reason,
            PlayerHostEvidenceSnapshot retained = default)
        {
            if (assignment == null || !assignment.Succeeded)
            {
                PlayerHostEvidenceStatus status =
                    assignment?.Status == PlayerSlotAssignmentStatus.RejectedForeignToken
                        ? PlayerHostEvidenceStatus.RejectedForeignAssignmentToken
                        : assignment?.Status is
                            PlayerSlotAssignmentStatus.RejectedTokenSlotMismatch
                            ? PlayerHostEvidenceStatus.RejectedTokenSlotMismatch
                            : PlayerHostEvidenceStatus.RejectedStaleAssignmentToken;
                return Result(
                    status,
                    operation,
                    retained,
                    retained,
                    assignment,
                    source,
                    reason,
                    assignment != null
                        ? "Canonical assignment confirmation failed. " +
                          assignment.Message
                        : "Canonical assignment confirmation returned no result.");
            }

            if (assignment.CurrentAssignment.AssignmentOrigin != assignmentOrigin)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedAssignmentOriginMismatch,
                    operation,
                    retained,
                    retained,
                    assignment,
                    source,
                    reason,
                    "Physical Host origin does not match the canonical assignment origin.");
            }

            if (assignment.CurrentAssignment.AssignmentToken != assignmentToken)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedStaleAssignmentToken,
                    operation,
                    retained,
                    retained,
                    assignment,
                    source,
                    reason,
                    "Physical Host assignment token is no longer current.");
            }

            if (assignment.CurrentAssignment.HostBindingIdentity != hostBindingIdentity)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedBindingConflict,
                    operation,
                    retained,
                    retained,
                    assignment,
                    source,
                    reason,
                    "Physical Host binding does not match the canonical assignment binding.");
            }

            return null;
        }

        private PlayerHostEvidenceResult ValidateExactRetainedEvidence(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring expectedHost,
            string source,
            string reason,
            out Record record)
        {
            record = null;
            if (!playerSlotId.IsValid)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedInvalidRequest,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "Host evidence release requires a valid Player Slot identity.");
            }

            if (!records.TryGetValue(playerSlotId, out record))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedNoEvidence,
                    operation,
                    default,
                    default,
                    null,
                    source,
                    reason,
                    "No retained physical Host evidence exists for release.");
            }

            PlayerHostEvidenceSnapshot retained = Snapshot(record);
            if (!assignmentToken.IsValid)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedInvalidAssignmentToken,
                    operation,
                    retained,
                    retained,
                    null,
                    source,
                    reason,
                    "Host evidence release requires a valid assignment token.");
            }

            if (assignmentToken.PlayerSlotId != playerSlotId)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedTokenSlotMismatch,
                    operation,
                    retained,
                    retained,
                    null,
                    source,
                    reason,
                    "Assignment token belongs to another Player Slot.");
            }

            if (!string.Equals(
                    assignmentToken.SessionContextId,
                    sessionContextId,
                    StringComparison.Ordinal))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedForeignAssignmentToken,
                    operation,
                    retained,
                    retained,
                    null,
                    source,
                    reason,
                    "Assignment token belongs to another Session context.");
            }

            if (record.AssignmentToken != assignmentToken)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedStaleAssignmentToken,
                    operation,
                    retained,
                    retained,
                    null,
                    source,
                    reason,
                    "Assignment token does not match the retained Host evidence.");
            }

            if (record.HostBindingIdentity != hostBindingIdentity)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedBindingConflict,
                    operation,
                    retained,
                    retained,
                    null,
                    source,
                    reason,
                    "Host binding identity does not match the retained Host evidence.");
            }

            if (!ReferenceEquals(record.Host, expectedHost))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedHostConflict,
                    operation,
                    retained,
                    retained,
                    null,
                    source,
                    reason,
                    "Expected Host does not match the retained physical Host reference.");
            }

            return null;
        }

        private static PlayerHostEvidenceSnapshot Snapshot(Record record)
        {
            return record == null
                ? default
                : new PlayerHostEvidenceSnapshot(
                    record.PlayerSlotId,
                    record.AssignmentOrigin,
                    record.AssignmentToken,
                    record.HostBindingIdentity,
                    record.Host,
                    record.Source,
                    record.Reason);
        }

        private static PlayerHostEvidenceResult Result(
            PlayerHostEvidenceStatus status,
            string operation,
            PlayerHostEvidenceSnapshot previousEvidence,
            PlayerHostEvidenceSnapshot currentEvidence,
            PlayerSlotAssignmentResult assignmentResult,
            string source,
            string reason,
            string message)
        {
            return new PlayerHostEvidenceResult(
                status,
                operation,
                previousEvidence,
                currentEvidence,
                assignmentResult,
                source,
                reason,
                message);
        }
    }
}
