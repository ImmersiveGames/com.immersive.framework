using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerParticipationRuntimeContext
    {
        private sealed class SessionPlayerLeaveRecord
        {
            internal SessionPlayerLeaveRecord(
                SessionPlayerLeaveToken token,
                int currentSlotRevision)
            {
                Token = token;
                CurrentSlotRevision = currentSlotRevision;
            }

            internal SessionPlayerLeaveToken Token { get; }
            internal int CurrentSlotRevision { get; set; }
        }

        private readonly Dictionary<PlayerSlotId, SessionPlayerLeaveRecord>
            _activeSessionPlayerLeaves = new();
        private int _sessionPlayerLeaveSequence;

        /// <summary>
        /// Stages Leave for the exact currently Joined Slot occurrence. This transition only
        /// changes Session allocation state to Leaving. Contextual and physical resource release
        /// is orchestrated by later ADR-020 cuts before terminal commit.
        /// </summary>
        internal SessionPlayerLeaveRuntimeResult TryBeginSessionPlayerLeave(
            PlayerSlotId playerSlotId,
            int expectedOccurrenceRevision,
            string source,
            string reason)
        {
            const string operation = "BeginSessionPlayerLeave";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "session-player-leave");
            int previousContextRevision = _revision;

            if (!playerSlotId.IsValid || expectedOccurrenceRevision < 0)
            {
                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.RejectedInvalidRequest,
                    operation,
                    default,
                    default,
                    default,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    "Session Player Leave requires a valid Player Slot and a non-negative expected occurrence revision.");
            }

            SlotRecord record = FindSlot(playerSlotId);
            if (record == null)
            {
                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.RejectedSlotNotConfigured,
                    operation,
                    default,
                    default,
                    default,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    $"Player Slot '{playerSlotId.StableText}' is not configured in this Session context.");
            }

            PlayerSlotRuntimeSnapshot previousSlot = CreateSlotSnapshot(record);

            if (record.AllocationState == PlayerSlotAllocationState.Leaving)
            {
                if (!_activeSessionPlayerLeaves.TryGetValue(
                        playerSlotId,
                        out SessionPlayerLeaveRecord activeLeave))
                {
                    return CreateSessionPlayerLeaveResult(
                        SessionPlayerLeaveRuntimeStatus.FailedInvariant,
                        operation,
                        default,
                        previousSlot,
                        previousSlot,
                        previousContextRevision,
                        resolvedSource,
                        resolvedReason,
                        "Player Slot is Leaving without active Session Player Leave correlation evidence.");
                }

                if (record.Revision != activeLeave.CurrentSlotRevision)
                {
                    return CreateSessionPlayerLeaveResult(
                        SessionPlayerLeaveRuntimeStatus.FailedInvariant,
                        operation,
                        activeLeave.Token,
                        previousSlot,
                        previousSlot,
                        previousContextRevision,
                        resolvedSource,
                        resolvedReason,
                        $"Active Session Player Leave expected Slot revision '{activeLeave.CurrentSlotRevision}' but current revision is '{record.Revision}'.");
                }

                if (activeLeave.Token.IsValid &&
                    activeLeave.Token.ExpectedOccurrenceRevision == expectedOccurrenceRevision)
                {
                    return CreateSessionPlayerLeaveResult(
                        SessionPlayerLeaveRuntimeStatus.SucceededAlreadyLeaving,
                        operation,
                        activeLeave.Token,
                        previousSlot,
                        previousSlot,
                        previousContextRevision,
                        resolvedSource,
                        resolvedReason,
                        "The exact Session Player occurrence is already staged Leaving; the active Leave token was preserved.");
                }

                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.RejectedForeignOrStaleOccurrence,
                    operation,
                    default,
                    previousSlot,
                    previousSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    "Session Player Leave rejected a request that does not correlate to the active Leaving occurrence.");
            }

            if (record.AllocationState != PlayerSlotAllocationState.Joined)
            {
                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.RejectedSlotNotJoined,
                    operation,
                    default,
                    previousSlot,
                    previousSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    $"Player Slot '{record.PlayerSlotId.StableText}' must be Joined before Session Player Leave can begin.");
            }

            if (record.Revision != expectedOccurrenceRevision)
            {
                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.RejectedForeignOrStaleOccurrence,
                    operation,
                    default,
                    previousSlot,
                    previousSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    $"Expected Session Player occurrence revision '{expectedOccurrenceRevision}' does not match current Slot revision '{record.Revision}'.");
            }

            if (_activeSessionPlayerLeaves.ContainsKey(playerSlotId))
            {
                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.FailedInvariant,
                    operation,
                    default,
                    previousSlot,
                    previousSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    "A Joined Slot unexpectedly retains active Session Player Leave correlation evidence.");
            }

            record.AllocationState = PlayerSlotAllocationState.Leaving;
            record.ReservationToken = default;
            record.Revision++;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            _revision++;

            _sessionPlayerLeaveSequence++;
            var token = new SessionPlayerLeaveToken(
                _contextId,
                _sessionPlayerLeaveSequence,
                record.PlayerSlotId,
                expectedOccurrenceRevision,
                record.Revision);
            _activeSessionPlayerLeaves.Add(
                record.PlayerSlotId,
                new SessionPlayerLeaveRecord(token, record.Revision));

            PlayerSlotRuntimeSnapshot currentSlot = CreateSlotSnapshot(record);
            PublishSlotAllocationChange(previousSlot, currentSlot);
            return CreateSessionPlayerLeaveResult(
                SessionPlayerLeaveRuntimeStatus.SucceededLeaving,
                operation,
                token,
                previousSlot,
                currentSlot,
                previousContextRevision,
                resolvedSource,
                resolvedReason,
                "Exact Joined Session Player occurrence staged Leaving. Slot vacancy has not been committed.");
        }

        /// <summary>
        /// Confirms that a Leave token still owns the exact active Leaving occurrence before a
        /// caller performs an irreversible contextual or provisioning-specific release step.
        /// </summary>
        internal SessionPlayerLeaveRuntimeResult TryConfirmSessionPlayerLeave(
            SessionPlayerLeaveToken token,
            string source,
            string reason)
        {
            const string operation = "ConfirmSessionPlayerLeave";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "confirm-session-player-leave");
            int previousContextRevision = _revision;

            if (!TryResolveActiveSessionPlayerLeave(
                    token,
                    out SlotRecord record,
                    out _,
                    out SessionPlayerLeaveRuntimeStatus rejectionStatus,
                    out string issue))
            {
                PlayerSlotRuntimeSnapshot rejectedSlot = record != null
                    ? CreateSlotSnapshot(record)
                    : default;
                return CreateSessionPlayerLeaveResult(
                    rejectionStatus,
                    operation,
                    token,
                    rejectedSlot,
                    rejectedSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            PlayerSlotRuntimeSnapshot currentSlot = CreateSlotSnapshot(record);
            return CreateSessionPlayerLeaveResult(
                SessionPlayerLeaveRuntimeStatus.SucceededConfirmed,
                operation,
                token,
                currentSlot,
                currentSlot,
                previousContextRevision,
                resolvedSource,
                resolvedReason,
                "Session Player Leave token still owns the exact active Leaving occurrence.");
        }

        /// <summary>
        /// Clears Session-scoped Actor selection only for the exact active Leave occurrence.
        /// Ordinary Actor selection remains Joined-only; this is the privileged terminal cleanup
        /// primitive used after required resource release has succeeded.
        /// </summary>
        internal SessionPlayerLeaveRuntimeResult TryClearActorSelectionForSessionPlayerLeave(
            SessionPlayerLeaveToken token,
            string source,
            string reason)
        {
            const string operation = "ClearActorSelectionForSessionPlayerLeave";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "session-player-leave-clear-actor-selection");
            int previousContextRevision = _revision;

            if (!TryResolveActiveSessionPlayerLeave(
                    token,
                    out SlotRecord record,
                    out SessionPlayerLeaveRecord activeLeave,
                    out SessionPlayerLeaveRuntimeStatus rejectionStatus,
                    out string issue))
            {
                PlayerSlotRuntimeSnapshot rejectedSlot = record != null
                    ? CreateSlotSnapshot(record)
                    : default;
                return CreateSessionPlayerLeaveResult(
                    rejectionStatus,
                    operation,
                    token,
                    rejectedSlot,
                    rejectedSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            PlayerSlotRuntimeSnapshot previousSlot = CreateSlotSnapshot(record);
            if (record.SelectedActorProfile == null)
            {
                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.SucceededActorSelectionAlreadyClear,
                    operation,
                    token,
                    previousSlot,
                    previousSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    "Session-scoped Actor selection is already clear for the active Leaving occurrence.");
            }

            CommitActorSelection(record, null, resolvedSource, resolvedReason);
            activeLeave.CurrentSlotRevision = record.Revision;

            PlayerSlotRuntimeSnapshot currentSlot = CreateSlotSnapshot(record);
            return CreateSessionPlayerLeaveResult(
                SessionPlayerLeaveRuntimeStatus.SucceededActorSelectionCleared,
                operation,
                token,
                previousSlot,
                currentSlot,
                previousContextRevision,
                resolvedSource,
                resolvedReason,
                "Session-scoped Actor selection cleared for the exact active Leaving occurrence.");
        }

        /// <summary>
        /// Terminal logical Leave commit. The caller may invoke this only after all required
        /// contextual and provisioning-specific release steps have succeeded and Actor selection
        /// for this occurrence has been cleared.
        /// </summary>
        internal SessionPlayerLeaveRuntimeResult TryCommitSessionPlayerLeave(
            SessionPlayerLeaveToken token,
            string source,
            string reason)
        {
            const string operation = "CommitSessionPlayerLeave";
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "commit-session-player-leave");
            int previousContextRevision = _revision;

            if (!TryResolveActiveSessionPlayerLeave(
                    token,
                    out SlotRecord record,
                    out _,
                    out SessionPlayerLeaveRuntimeStatus rejectionStatus,
                    out string issue))
            {
                PlayerSlotRuntimeSnapshot rejectedSlot = record != null
                    ? CreateSlotSnapshot(record)
                    : default;
                return CreateSessionPlayerLeaveResult(
                    rejectionStatus,
                    operation,
                    token,
                    rejectedSlot,
                    rejectedSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    issue);
            }

            PlayerSlotRuntimeSnapshot previousSlot = CreateSlotSnapshot(record);
            if (record.SelectedActorProfile != null)
            {
                return CreateSessionPlayerLeaveResult(
                    SessionPlayerLeaveRuntimeStatus.RejectedDependentState,
                    operation,
                    token,
                    previousSlot,
                    previousSlot,
                    previousContextRevision,
                    resolvedSource,
                    resolvedReason,
                    "Terminal Session Player Leave commit requires Session-scoped Actor selection to be cleared first.");
            }

            record.AllocationState = PlayerSlotAllocationState.Available;
            record.ReservationToken = default;
            record.Revision++;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            _revision++;
            _activeSessionPlayerLeaves.Remove(record.PlayerSlotId);

            PlayerSlotRuntimeSnapshot currentSlot = CreateSlotSnapshot(record);
            PublishSlotAllocationChange(previousSlot, currentSlot);
            return CreateSessionPlayerLeaveResult(
                SessionPlayerLeaveRuntimeStatus.SucceededCommitted,
                operation,
                token,
                previousSlot,
                currentSlot,
                previousContextRevision,
                resolvedSource,
                resolvedReason,
                "Session Player Leave committed. The departed occurrence ended and the Slot is Available for future allocation policy.");
        }

        private bool TryResolveActiveSessionPlayerLeave(
            SessionPlayerLeaveToken token,
            out SlotRecord record,
            out SessionPlayerLeaveRecord activeLeave,
            out SessionPlayerLeaveRuntimeStatus rejectionStatus,
            out string issue)
        {
            record = null;
            activeLeave = null;
            rejectionStatus = SessionPlayerLeaveRuntimeStatus.RejectedForeignOrStaleOccurrence;

            if (!token.IsValid ||
                !string.Equals(token.ContextId, _contextId, StringComparison.Ordinal))
            {
                issue = "Session Player Leave token is invalid or belongs to another Session participation context.";
                return false;
            }

            record = FindSlot(token.PlayerSlotId);
            if (record == null)
            {
                issue = "Session Player Leave token targets a Slot that is not configured in this Session context.";
                return false;
            }

            if (!_activeSessionPlayerLeaves.TryGetValue(token.PlayerSlotId, out activeLeave) ||
                activeLeave.Token != token)
            {
                issue = "Session Player Leave token is foreign, stale or no longer owns the active Slot occurrence.";
                return false;
            }

            if (record.AllocationState != PlayerSlotAllocationState.Leaving)
            {
                rejectionStatus = SessionPlayerLeaveRuntimeStatus.FailedInvariant;
                issue = $"Active Session Player Leave correlation exists while Slot '{record.PlayerSlotId.StableText}' is '{record.AllocationState}' instead of Leaving.";
                return false;
            }

            if (record.Revision != activeLeave.CurrentSlotRevision)
            {
                rejectionStatus = SessionPlayerLeaveRuntimeStatus.FailedInvariant;
                issue = $"Active Session Player Leave expected Slot revision '{activeLeave.CurrentSlotRevision}' but current revision is '{record.Revision}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private SessionPlayerLeaveRuntimeResult CreateSessionPlayerLeaveResult(
            SessionPlayerLeaveRuntimeStatus status,
            string operation,
            SessionPlayerLeaveToken token,
            PlayerSlotRuntimeSnapshot previousSlot,
            PlayerSlotRuntimeSnapshot currentSlot,
            int previousContextRevision,
            string source,
            string reason,
            string message)
        {
            _lastOperationStatus = MapSessionPlayerLeaveStatus(status);
            _lastOperationMessage = message ?? string.Empty;
            return new SessionPlayerLeaveRuntimeResult(
                status,
                operation,
                token,
                previousSlot,
                currentSlot,
                previousContextRevision,
                _revision,
                source,
                reason,
                message);
        }

        private static PlayerParticipationOperationStatus MapSessionPlayerLeaveStatus(
            SessionPlayerLeaveRuntimeStatus status)
        {
            if (status is
                SessionPlayerLeaveRuntimeStatus.SucceededLeaving or
                SessionPlayerLeaveRuntimeStatus.SucceededActorSelectionCleared or
                SessionPlayerLeaveRuntimeStatus.SucceededCommitted)
            {
                return PlayerParticipationOperationStatus.Succeeded;
            }

            if (status is
                SessionPlayerLeaveRuntimeStatus.SucceededAlreadyLeaving or
                SessionPlayerLeaveRuntimeStatus.SucceededConfirmed or
                SessionPlayerLeaveRuntimeStatus.SucceededActorSelectionAlreadyClear)
            {
                return PlayerParticipationOperationStatus.IgnoredNoChange;
            }

            if (status == SessionPlayerLeaveRuntimeStatus.RejectedInvalidRequest)
            {
                return PlayerParticipationOperationStatus.RejectedInvalidRequest;
            }

            if (status == SessionPlayerLeaveRuntimeStatus.FailedInvariant)
            {
                return PlayerParticipationOperationStatus.FailedInvalidConfiguration;
            }

            return PlayerParticipationOperationStatus.RejectedInvalidState;
        }
    }
}
