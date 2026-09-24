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
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            PlayerSlotId = playerSlotId;
            PhysicalProvisioningMode = physicalProvisioningMode;
            AssignmentToken = assignmentToken;
            HostBindingIdentity = hostBindingIdentity;
            Host = host;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        internal PlayerSlotId PlayerSlotId { get; }
        internal PlayerHostProvisioningMode PhysicalProvisioningMode { get; }
        internal PlayerSlotAssignmentToken AssignmentToken { get; }
        internal PlayerHostBindingIdentity HostBindingIdentity { get; }
        internal LocalPlayerHostAuthoring Host { get; }
        internal string Source { get; }
        internal string Reason { get; }
        internal bool HasRetainedHostReference => !ReferenceEquals(Host, null);
        internal bool HostIsAvailable => HasRetainedHostReference && Host != null;
        internal bool HasContextualProjection =>
            AssignmentToken.IsValid &&
            AssignmentToken.PlayerSlotId == PlayerSlotId &&
            HostBindingIdentity.IsValid &&
            AssignmentToken.HostBindingIdentity == HostBindingIdentity;
        internal bool HasSessionPhysicalHost =>
            PlayerSlotId.IsValid && HasRetainedHostReference;
        internal bool IsRecorded =>
            HasSessionPhysicalHost;
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
                PlayerSlotAssignmentToken assignmentToken,
                PlayerHostBindingIdentity hostBindingIdentity,
                LocalPlayerHostAuthoring host,
                string source,
                string reason)
            {
                PlayerSlotId = playerSlotId;
                PhysicalProvisioningMode = physicalProvisioningMode;
                AssignmentToken = assignmentToken;
                HostBindingIdentity = hostBindingIdentity;
                Host = host;
                Source = source;
                Reason = reason;
            }

            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerHostProvisioningMode PhysicalProvisioningMode { get; }
            internal PlayerSlotAssignmentToken AssignmentToken { get; set; }
            internal PlayerHostBindingIdentity HostBindingIdentity { get; set; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal string Source { get; set; }
            internal string Reason { get; set; }
        }

        private readonly PlayerParticipationRuntimeContext _participationContext;
        private readonly string _sessionContextId;
        private readonly Dictionary<PlayerSlotId, Record> _records = new();

        internal PlayerHostEvidenceProjection(
            PlayerParticipationRuntimeContext participationContext)
        {
            this._participationContext = participationContext ??
                throw new ArgumentNullException(nameof(participationContext));
            PlayerParticipationSnapshot snapshot =
                participationContext.CreateSnapshot();
            _sessionContextId = snapshot != null
                ? snapshot.ContextId.NormalizeText()
                : string.Empty;
            if (string.IsNullOrEmpty(_sessionContextId))
            {
                throw new InvalidOperationException(
                    "Host evidence projection requires an initialized Player participation context.");
            }
        }

        internal int RetainedEvidenceCount => _records.Count;
        internal string SessionContextId => _sessionContextId;

        private static string EscapeContextDiagnosticValue(string value) =>
            (value ?? "<none>").Replace("\r", " ").Replace("\n", " ").Replace("'", "\"");

        internal void LogContextDiagnostic(
            string phase, string operation, PlayerSlotId playerSlotId,
            Immersive.Framework.RuntimeContent.RuntimeContentOwner activityOwner,
            string source, string reason, string result = "not-evaluated",
            string issue = "", LocalPlayerHostAuthoring expectedHost = null,
            PlayerHostEvidenceSnapshot? previous = null)
        {
            bool assigned = _participationContext.TryGetCurrentAssignment(playerSlotId, out var assignment);
            bool projected = TryGetRetainedEvidence(playerSlotId, out var evidence);
            string host = evidence.Host != null
                ? $"{EscapeContextDiagnosticValue(evidence.Host.name)}#{evidence.Host.GetEntityId()}"
                : ReferenceEquals(evidence.Host, null) ? "null" : "destroyed";
            string expected = expectedHost != null
                ? $"{EscapeContextDiagnosticValue(expectedHost.name)}#{expectedHost.GetEntityId()}"
                : ReferenceEquals(expectedHost, null) ? "not-specified" : "destroyed";
            string previousText = previous.HasValue
                ? $" previousToken='{previous.Value.AssignmentToken.StableText}' " +
                  $"previousBinding='{previous.Value.HostBindingIdentity.StableText}'"
                : string.Empty;
            Debug.Log(
                $"[FRAMEWORK_PLAYER_CONTEXT_DIAG] phase='{phase}' operation='{operation}' " +
                $"slot='{playerSlotId.StableText}' activity='{EscapeContextDiagnosticValue(activityOwner.StableText)}' occurrence='unavailable' " +
                $"projectionInstance='{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this)}' " +
                $"session='{EscapeContextDiagnosticValue(_sessionContextId)}' frame='{Time.frameCount}' " +
                $"canonicalExists='{assigned}' " +
                $"canonicalOwner='{EscapeContextDiagnosticValue(assignment.AssignmentOwner.StableText)}' canonicalToken='{assignment.AssignmentToken.StableText}' " +
                $"canonicalBinding='{assignment.HostBindingIdentity.StableText}' projectedExists='{projected}' " +
                $"hasContextualProjection='{evidence.HasContextualProjection}' " +
                $"projectedToken='{evidence.AssignmentToken.StableText}' projectedBinding='{evidence.HostBindingIdentity.StableText}' " +
                $"host='{host}' expectedHost='{expected}' bindingMatch='{assigned && evidence.HasContextualProjection && assignment.HostBindingIdentity == evidence.HostBindingIdentity}' " +
                $"hostMatch='{(ReferenceEquals(expectedHost, null) ? "not-evaluated" : ReferenceEquals(expectedHost, evidence.Host).ToString())}' " +
                $"result='{result}' issue='{EscapeContextDiagnosticValue(issue)}' source='{EscapeContextDiagnosticValue(source)}' reason='{EscapeContextDiagnosticValue(reason)}'" + previousText);
        }

        internal PlayerHostEvidenceResult RegisterSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            PlayerHostProvisioningMode physicalProvisioningMode,
            LocalPlayerHostAuthoring host,
            string source,
            string reason)
        {
            const string operation = "RegisterSessionPhysicalHost";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerHostEvidenceProjection));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "register-session-physical-host");
            if (!playerSlotId.IsValid ||
                !physicalProvisioningMode.IsDefinedMode())
            {
                return Result(
                    PlayerHostEvidenceStatus.RejectedInvalidRequest,
                    operation,
                    default,
                    default,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Session physical Host registration requires a valid Slot and explicit physical provisioning mode.");
            }

            if (ReferenceEquals(host, null) || host == null ||
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

            if (_records.TryGetValue(playerSlotId, out Record existing))
            {
                PlayerHostEvidenceSnapshot snapshot = Snapshot(existing);
                return ReferenceEquals(existing.Host, host) &&
                    existing.PhysicalProvisioningMode == physicalProvisioningMode
                    ? Result(PlayerHostEvidenceStatus.SucceededAlreadyRegistered, operation, snapshot, snapshot, null, resolvedSource, resolvedReason, "The exact Session physical Host is already registered.")
                    : Result(PlayerHostEvidenceStatus.RejectedHostConflict, operation, snapshot, snapshot, null, resolvedSource, resolvedReason, "Another Session physical Host or provisioning mode is already registered for this Slot.");
            }

            foreach (KeyValuePair<PlayerSlotId, Record> pair in _records)
            {
                if (ReferenceEquals(pair.Value.Host, host))
                {
                    PlayerHostEvidenceSnapshot conflict = Snapshot(pair.Value);
                    return Result(PlayerHostEvidenceStatus.RejectedHostConflict, operation, conflict, conflict, null, resolvedSource, resolvedReason, "The physical Host is already registered for another Slot.");
                }
            }

            var record = new Record(
                playerSlotId,
                physicalProvisioningMode,
                default,
                default,
                host,
                resolvedSource,
                resolvedReason);
            _records.Add(playerSlotId, record);
            return Result(PlayerHostEvidenceStatus.SucceededRegistered, operation, default, Snapshot(record), null, resolvedSource, resolvedReason, "Session physical Host registered without a contextual assignment.");
        }

        internal bool TryGetSessionPhysicalHost(
            PlayerSlotId playerSlotId,
            out LocalPlayerHostAuthoring host,
            out PlayerHostEvidenceResult result)
        {
            host = null;
            if (!playerSlotId.IsValid || !_records.TryGetValue(playerSlotId, out Record record))
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
                !_records.TryGetValue(playerSlotId, out Record existing))
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
                _participationContext.TryConfirmCurrentAssignment(
                    playerSlotId,
                    assignmentToken,
                    resolvedSource,
                    resolvedReason);
            PlayerHostEvidenceResult assignmentFailure = ValidateAssignment(
                operation,
                playerSlotId,
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

            existing.AssignmentToken = assignmentToken;
            existing.HostBindingIdentity = hostBindingIdentity;
            existing.Source = resolvedSource;
            existing.Reason = resolvedReason;
            LogContextDiagnostic("mutation", operation, playerSlotId,
                assignment.CurrentAssignment.AssignmentOwner, resolvedSource, resolvedReason,
                "SucceededReprojected", previous: previous);
            return Result(
                PlayerHostEvidenceStatus.SucceededReprojected,
                operation,
                previous,
                Snapshot(existing),
                assignment,
                resolvedSource,
                resolvedReason,
                "Retained Session physical Host evidence re-correlated with the fresh contextual assignment.");
        }

        internal bool TryGetRetainedEvidence(
            PlayerSlotId playerSlotId,
            out PlayerHostEvidenceSnapshot evidence)
        {
            if (playerSlotId.IsValid &&
                _records.TryGetValue(playerSlotId, out Record record))
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

            if (!_records.TryGetValue(playerSlotId, out Record record))
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
                _participationContext.TryConfirmCurrentAssignment(
                    playerSlotId,
                    record.AssignmentToken,
                    resolvedSource,
                    resolvedReason);
            PlayerHostEvidenceResult assignmentFailure = ValidateAssignment(
                operation,
                playerSlotId,
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
            record.AssignmentToken = default;
            record.HostBindingIdentity = default;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            LogContextDiagnostic("mutation", operation, playerSlotId,
                confirmation.AssignmentResult != null ? confirmation.AssignmentResult.CurrentAssignment.AssignmentOwner : default,
                resolvedSource, resolvedReason,
                "SucceededReleased", expectedHost: expectedHost, previous: previous);
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
            if (!playerSlotId.IsValid || !_records.TryGetValue(playerSlotId, out Record record))
            {
                return Result(PlayerHostEvidenceStatus.RejectedNoEvidence, operation, default, default, null, resolvedSource, resolvedReason, "No Session physical Host evidence exists for terminal release.");
            }

            PlayerHostEvidenceSnapshot previous = Snapshot(record);
            if (!ReferenceEquals(record.Host, expectedHost))
            {
                return Result(PlayerHostEvidenceStatus.RejectedHostConflict, operation, previous, previous, null, resolvedSource, resolvedReason, "Terminal physical Host release requires the exact retained Host reference.");
            }

            _records.Remove(playerSlotId);
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

            _records.Remove(playerSlotId);
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
            _records.Clear();
        }

        private PlayerHostEvidenceResult ValidateRequest(
            string operation,
            PlayerSlotId playerSlotId,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            LocalPlayerHostAuthoring host,
            string source,
            string reason,
            bool requireJoinedHost)
        {
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
                    "Host contextual projection requires a valid Player Slot.");
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
                    _sessionContextId,
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

            if (!_records.TryGetValue(playerSlotId, out record))
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
                    _sessionContextId,
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
