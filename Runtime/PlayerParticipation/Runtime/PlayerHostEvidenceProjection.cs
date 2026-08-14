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
        SucceededReprojected = 35,
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
            PlayerHostProvisioningMode physicalProvisioningMode,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            PlayerSlotId = playerSlotId;
            PhysicalProvisioningMode = physicalProvisioningMode;
            AssignmentOrigin = assignmentOrigin;
            AssignmentToken = assignmentToken;
            HostBindingIdentity = hostBindingIdentity;
            Host = host;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        internal PlayerSlotId PlayerSlotId { get; }
        internal PlayerHostProvisioningMode PhysicalProvisioningMode { get; }
        internal PlayerSlotAssignmentOrigin AssignmentOrigin { get; }
        internal PlayerSlotAssignmentToken AssignmentToken { get; }
        internal PlayerHostBindingIdentity HostBindingIdentity { get; }
        internal LocalPlayerHostAuthoring Host { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal bool HasRetainedHostReference => !ReferenceEquals(Host, null);
        internal bool HostIsAvailable => HasRetainedHostReference && Host != null;
        internal bool HasContextualProjection =>
            (AssignmentOrigin is
                PlayerSlotAssignmentOrigin.ManagerProvisioned or
                PlayerSlotAssignmentOrigin.SceneProvided) &&
            AssignmentToken.IsValid &&
            HostBindingIdentity.IsValid;
        internal bool HasSessionPhysicalHost =>
            PlayerSlotId.IsValid && HasRetainedHostReference;
        internal bool IsRecorded =>
            HasSessionPhysicalHost && HasContextualProjection;
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
            PlayerHostEvidenceStatus.SucceededReprojected or
            PlayerHostEvidenceStatus.SucceededReleased or
            PlayerHostEvidenceStatus.SucceededClearedDivergent;
        internal bool HasRetainedEvidence => CurrentEvidence.IsRecorded;

        internal string ToDiagnosticString()
        {
            PlayerHostEvidenceSnapshot evidence = CurrentEvidence.IsRecorded
                ? CurrentEvidence
                : PreviousEvidence;
            return $"operation='{Operation}' status='{Status}' " +
                $"slot='{(evidence.PlayerSlotId.IsValid ? evidence.PlayerSlotId.StableText : "<invalid>")}' " +
                $"slotValid='{evidence.PlayerSlotId.IsValid}' " +
                $"physicalProvisioning='{evidence.PhysicalProvisioningMode}' " +
                $"origin='{evidence.AssignmentOrigin}' " +
                $"assignment='{(evidence.AssignmentToken.IsValid ? evidence.AssignmentToken.StableText : "<invalid>")}' " +
                $"assignmentValid='{evidence.AssignmentToken.IsValid}' " +
                $"binding='{(evidence.HostBindingIdentity.IsValid ? evidence.HostBindingIdentity.StableText : "<invalid>")}' " +
                $"bindingValid='{evidence.HostBindingIdentity.IsValid}' " +
                $"hostReferenceRetained='{evidence.HasRetainedHostReference}' " +
                $"hostAvailable='{evidence.HostIsAvailable}' " +
                $"source='{Source}' reason='{Reason}' message='{Message}'";
        }
    }

    /// <summary>
    /// Session-scoped physical Host registry with an optional current Activity contextual
    /// projection. It never creates assignment authority.
    /// </summary>
    internal sealed class PlayerHostEvidenceProjection
    {
        private sealed class Record
        {
            internal Record(
                PlayerSlotId playerSlotId,
                PlayerHostProvisioningMode physicalProvisioningMode,
                PlayerSlotAssignmentOrigin assignmentOrigin,
                PlayerSlotAssignmentToken assignmentToken,
                PlayerHostBindingIdentity hostBindingIdentity,
                LocalPlayerHostAuthoring host,
                string source,
                string reason)
            {
                PlayerSlotId = playerSlotId;
                PhysicalProvisioningMode = physicalProvisioningMode;
                AssignmentOrigin = assignmentOrigin;
                AssignmentToken = assignmentToken;
                HostBindingIdentity = hostBindingIdentity;
                Host = host;
                Source = source;
                Reason = reason;
            }

            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerHostProvisioningMode PhysicalProvisioningMode { get; }
            internal PlayerSlotAssignmentOrigin AssignmentOrigin { get; set; }
            internal PlayerSlotAssignmentToken AssignmentToken { get; set; }
            internal PlayerHostBindingIdentity HostBindingIdentity { get; set; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal string Source { get; set; }
            internal string Reason { get; set; }
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

        internal PlayerHostEvidenceResult RegisterSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            const string operation = "RegisterSessionPhysicalHost";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "register-session-physical-host");
            if (!playerSlotId.IsValid || ReferenceEquals(host, null) || host == null ||
                !host.IsJoined || !host.HasJoinedSlot ||
                host.JoinedPlayerSlotId != playerSlotId)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedHostMismatch,
                    operation,
                    default,
                    default,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Session physical Host registration requires the exact available Joined Host for the Slot.");
            }

            if (records.TryGetValue(playerSlotId, out Record existing))
            {
                PlayerHostEvidenceSnapshot snapshot = Snapshot(existing);
                return ReferenceEquals(existing.Host, host)
                    ? Result(PlayerHostEvidenceStatus.SucceededAlreadyRegistered, operation, snapshot, snapshot, null, resolvedSource, resolvedReason, "The exact Session physical Host is already registered.")
                    : Result(PlayerHostEvidenceStatus.RejectedHostConflict, operation, snapshot, snapshot, null, resolvedSource, resolvedReason, "Another Session physical Host is already registered for this Slot.");
            }

            foreach (KeyValuePair<PlayerSlotId, Record> pair in records)
            {
                if (ReferenceEquals(pair.Value.Host, host))
                {
                    PlayerHostEvidenceSnapshot conflict = Snapshot(pair.Value);
                    return Result(PlayerHostEvidenceStatus.RejectedHostConflict, operation, conflict, conflict, null, resolvedSource, resolvedReason, "The physical Host is already registered for another Slot.");
                }
            }

            var record = new Record(
                playerSlotId,
                PlayerHostProvisioningMode.ManagerProvisioned,
                default,
                default,
                default,
                host,
                resolvedSource,
                resolvedReason);
            records.Add(playerSlotId, record);
            return Result(PlayerHostEvidenceStatus.SucceededRegistered, operation, default, Snapshot(record), null, resolvedSource, resolvedReason, "Session physical Host registered without a contextual assignment.");
        }

        internal bool TryGetSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            out LocalPlayerHostAuthoring host,
            out PlayerHostEvidenceResult result)
        {
            host = null;
            if (!playerSlotId.IsValid || !records.TryGetValue(playerSlotId, out Record record))
            {
                result = Result(PlayerHostEvidenceStatus.RejectedNoEvidence, "LookupSessionPhysicalHost", default, default, null, nameof(PlayerHostEvidenceProjection), "lookup-session-physical-host", "No Session physical Host is registered for the Slot.");
                return false;
            }

            PlayerHostEvidenceSnapshot snapshot = Snapshot(record);
            if (ReferenceEquals(record.Host, null) || record.Host == null ||
                !record.Host.IsJoined || !record.Host.HasJoinedSlot ||
                record.Host.JoinedPlayerSlotId != playerSlotId)
            {
                result = Result(PlayerHostEvidenceStatus.RejectedHostMismatch, "LookupSessionPhysicalHost", snapshot, snapshot, null, nameof(PlayerHostEvidenceProjection), "lookup-session-physical-host", "Session physical Host evidence is unavailable or no longer belongs to the Slot.");
                return false;
            }

            host = record.Host;
            result = Result(PlayerHostEvidenceStatus.SucceededConfirmed, "LookupSessionPhysicalHost", snapshot, snapshot, null, nameof(PlayerHostEvidenceProjection), "lookup-session-physical-host", "Session physical Host is available.");
            return true;
        }

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
                if (!existingSnapshot.HasContextualProjection &&
                    ReferenceEquals(existing.Host, host))
                {
                    existing.AssignmentOrigin = assignmentOrigin;
                    existing.AssignmentToken = assignmentToken;
                    existing.HostBindingIdentity = hostBindingIdentity;
                    existing.Source = resolvedSource;
                    existing.Reason = resolvedReason;
                    PlayerHostEvidenceSnapshot reprojected = Snapshot(existing);
                    return Result(
                        PlayerHostEvidenceStatus.SucceededReprojected,
                        operation,
                        existingSnapshot,
                        reprojected,
                        assignment,
                        resolvedSource,
                        resolvedReason,
                        "Retained Session physical Host projected into the current contextual assignment.");
                }

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
                ToProvisioningMode(assignmentOrigin),
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

        /// <summary>
        /// Re-correlates retained Session physical Host evidence with the next Activity-owned
        /// contextual assignment. The physical Host reference is deliberately preserved; the
        /// contextual assignment token and binding are the only values that change.
        /// </summary>
        internal PlayerHostEvidenceResult ReprojectHostEvidence(
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            string source,
            string reason)
        {
            const string operation = "ReprojectHostEvidence";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "reproject-host-evidence");
            if (!playerSlotId.IsValid ||
                !records.TryGetValue(playerSlotId, out Record existing))
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedNoEvidence,
                    operation,
                    default,
                    default,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Contextual Host evidence reprojection requires retained physical Host evidence for the exact Player Slot.");
            }

            PlayerHostEvidenceSnapshot previous = Snapshot(existing);
            PlayerHostEvidenceResult validation = ValidateRequest(
                operation,
                playerSlotId,
                assignmentOrigin,
                assignmentToken,
                hostBindingIdentity,
                existing.Host,
                resolvedSource,
                resolvedReason,
                requireJoinedHost: true);
            if (validation != null)
            {
                return Result(
                    validation.Status,
                    operation,
                    previous,
                    previous,
                    validation.AssignmentResult,
                    resolvedSource,
                    resolvedReason,
                    validation.Message);
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
                resolvedReason,
                previous);
            if (assignmentFailure != null)
            {
                return assignmentFailure;
            }

            if (previous.HasContextualProjection &&
                existing.AssignmentOrigin != assignmentOrigin)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedAssignmentOriginMismatch,
                    operation,
                    previous,
                    previous,
                    assignment,
                    resolvedSource,
                    resolvedReason,
                    "Retained physical Host evidence belongs to a different provisioning origin and cannot be reprojected.");
            }

            existing.AssignmentOrigin = assignmentOrigin;
            existing.AssignmentToken = assignmentToken;
            existing.HostBindingIdentity = hostBindingIdentity;
            existing.Source = resolvedSource;
            existing.Reason = resolvedReason;
            return Result(
                PlayerHostEvidenceStatus.SucceededReprojected,
                operation,
                previous,
                Snapshot(existing),
                assignment,
                resolvedSource,
                resolvedReason,
                "Retained Session physical Host evidence re-correlated with the fresh Scene-provided contextual assignment.");
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
            if (!retained.HasContextualProjection)
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedNoEvidence,
                    operation,
                    retained,
                    retained,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Session physical Host exists, but no Activity contextual projection is current.");
            }

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

            PlayerHostEvidenceSnapshot previous = Snapshot(record);
            record.AssignmentOrigin = default;
            record.AssignmentToken = default;
            record.HostBindingIdentity = default;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            return Result(
                PlayerHostEvidenceStatus.SucceededReleased,
                operation,
                previous,
                Snapshot(record),
                confirmation.AssignmentResult,
                resolvedSource,
                resolvedReason,
                "Activity contextual Host projection released; Session physical Host remains retained.");
        }

        internal PlayerHostEvidenceResult ReleaseSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            LocalPlayerHostAuthoring expectedHost,
            string source,
            string reason)
        {
            const string operation = "ReleaseSessionPhysicalHost";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "release-session-physical-host");
            if (!playerSlotId.IsValid || !records.TryGetValue(playerSlotId, out Record record))
            {
                return Result(PlayerHostEvidenceStatus.RejectedNoEvidence, operation, default, default, null, resolvedSource, resolvedReason, "No Session physical Host evidence exists for terminal release.");
            }

            PlayerHostEvidenceSnapshot previous = Snapshot(record);
            if (!ReferenceEquals(record.Host, expectedHost))
            {
                return Result(PlayerHostEvidenceStatus.RejectedHostConflict, operation, previous, previous, null, resolvedSource, resolvedReason, "Terminal physical Host release requires the exact retained Host reference.");
            }

            records.Remove(playerSlotId);
            return Result(PlayerHostEvidenceStatus.SucceededReleased, operation, previous, default, null, resolvedSource, resolvedReason, "Session physical Host evidence released terminally.");
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
                    record.PhysicalProvisioningMode,
                    record.AssignmentOrigin,
                    record.AssignmentToken,
                    record.HostBindingIdentity,
                    record.Host,
                    record.Source,
                    record.Reason);
        }

        private static PlayerHostProvisioningMode ToProvisioningMode(
            PlayerSlotAssignmentOrigin assignmentOrigin)
        {
            return assignmentOrigin == PlayerSlotAssignmentOrigin.SceneProvided
                ? PlayerHostProvisioningMode.SceneProvided
                : PlayerHostProvisioningMode.ManagerProvisioned;
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
