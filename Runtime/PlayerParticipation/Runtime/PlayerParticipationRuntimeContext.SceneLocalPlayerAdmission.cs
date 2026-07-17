using System;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerParticipationRuntimeContext
    {
        internal PlayerParticipationOperationResult TryReserveSceneLocalPlayerSlot(
            PlayerSlotId requestedPlayerSlotId,
            string source,
            string reason,
            out bool orderedSlotMismatch)
        {
            orderedSlotMismatch = false;
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "reserve-scene-local-player-slot");
            int previousRevision = revision;

            if (!requestedPlayerSlotId.IsValid)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidRequest,
                    "ReserveSceneLocalPlayerSlot",
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player reservation requires a valid explicit Player Slot identity.",
                    previousRevision,
                    default,
                    default);
            }

            SlotRecord requested = FindSlot(requestedPlayerSlotId);
            if (requested == null)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidRequest,
                    "ReserveSceneLocalPlayerSlot",
                    resolvedSource,
                    resolvedReason,
                    $"Scene Local Player requested Slot '{requestedPlayerSlotId.StableText}' is not configured in this Session.",
                    previousRevision,
                    default,
                    default);
            }

            if (requested.AllocationState != PlayerSlotAllocationState.Available)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedNoAvailableSlot,
                    "ReserveSceneLocalPlayerSlot",
                    resolvedSource,
                    resolvedReason,
                    $"Scene Local Player requested Slot '{requestedPlayerSlotId.StableText}' is '{requested.AllocationState}', not Available.",
                    previousRevision,
                    CreateSlotSnapshot(requested),
                    default);
            }

            if (CountConsumedCapacity() >= dynamicCapacity)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedCapacityReached,
                    "ReserveSceneLocalPlayerSlot",
                    resolvedSource,
                    resolvedReason,
                    $"Scene Local Player reservation rejected because dynamic capacity '{dynamicCapacity}' is reached.",
                    previousRevision,
                    default,
                    default);
            }

            SlotRecord firstAvailable = null;
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].AllocationState == PlayerSlotAllocationState.Available)
                {
                    firstAvailable = slots[index];
                    break;
                }
            }

            if (firstAvailable == null)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedNoAvailableSlot,
                    "ReserveSceneLocalPlayerSlot",
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player reservation rejected because no configured Slot is Available.",
                    previousRevision,
                    default,
                    default);
            }

            if (!ReferenceEquals(firstAvailable, requested))
            {
                orderedSlotMismatch = true;
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidRequest,
                    "ReserveSceneLocalPlayerSlot",
                    resolvedSource,
                    resolvedReason,
                    $"Scene Local Player requested Slot '{requestedPlayerSlotId.StableText}', but ordered allocation requires current first Available Slot '{firstAvailable.PlayerSlotId.StableText}'. No fallback was applied.",
                    previousRevision,
                    CreateSlotSnapshot(requested),
                    default);
            }

            requested.AllocationState = PlayerSlotAllocationState.Reserved;
            requested.Revision++;
            reservationSequence++;
            requested.ReservationToken = new PlayerSlotReservationToken(
                contextId,
                reservationSequence,
                requested.PlayerSlotId,
                requested.Revision);
            requested.Source = resolvedSource;
            requested.Reason = resolvedReason;
            revision++;

            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "ReserveSceneLocalPlayerSlot",
                resolvedSource,
                resolvedReason,
                "Explicit Scene Local Player Slot reserved by configured order.",
                previousRevision,
                CreateSlotSnapshot(requested),
                requested.ReservationToken);
        }

        internal PlayerParticipationOperationResult TryBeginSceneLocalPlayerRelease(
            SceneLocalPlayerAdmissionToken admissionToken,
            string source,
            string reason,
            out SceneLocalPlayerAdmissionReleaseToken releaseToken)
        {
            releaseToken = default;
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "begin-scene-local-player-release");
            int previousRevision = revision;

            if (!TryResolveCurrentSceneAdmissionRecord(
                    admissionToken,
                    PlayerSlotAllocationState.Joined,
                    requireJoinedRevisionMatch: false,
                    out SlotRecord record,
                    out string issue))
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    "BeginSceneLocalPlayerRelease",
                    resolvedSource,
                    resolvedReason,
                    issue,
                    previousRevision,
                    record != null ? CreateSlotSnapshot(record) : default,
                    default);
            }

            if (record.SelectedActorProfile != null)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    "BeginSceneLocalPlayerRelease",
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player Slot cannot begin host release while an Actor selection remains committed.",
                    previousRevision,
                    CreateSlotSnapshot(record),
                    default);
            }

            int joinedRevision = record.Revision;
            record.AllocationState = PlayerSlotAllocationState.Leaving;
            record.Revision++;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            revision++;
            releaseToken = new SceneLocalPlayerAdmissionReleaseToken(
                admissionToken,
                joinedRevision,
                record.Revision);

            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "BeginSceneLocalPlayerRelease",
                resolvedSource,
                resolvedReason,
                "Scene Local Player Slot entered Leaving state.",
                previousRevision,
                CreateSlotSnapshot(record),
                default);
        }

        internal PlayerParticipationOperationResult TryCommitSceneLocalPlayerRelease(
            SceneLocalPlayerAdmissionReleaseToken releaseToken,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "commit-scene-local-player-release");
            int previousRevision = revision;

            if (!TryResolveReleaseRecord(releaseToken, out SlotRecord record, out string issue))
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    "CommitSceneLocalPlayerRelease",
                    resolvedSource,
                    resolvedReason,
                    issue,
                    previousRevision,
                    record != null ? CreateSlotSnapshot(record) : default,
                    default);
            }

            record.AllocationState = PlayerSlotAllocationState.Available;
            record.ReservationToken = default;
            record.Revision++;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            revision++;

            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "CommitSceneLocalPlayerRelease",
                resolvedSource,
                resolvedReason,
                "Scene Local Player Slot release committed and Slot returned to Available.",
                previousRevision,
                CreateSlotSnapshot(record),
                default);
        }

        internal PlayerParticipationOperationResult TryRollbackSceneLocalPlayerRelease(
            SceneLocalPlayerAdmissionReleaseToken releaseToken,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "rollback-scene-local-player-release");
            int previousRevision = revision;

            if (!TryResolveReleaseRecord(releaseToken, out SlotRecord record, out string issue))
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    "RollbackSceneLocalPlayerRelease",
                    resolvedSource,
                    resolvedReason,
                    issue,
                    previousRevision,
                    record != null ? CreateSlotSnapshot(record) : default,
                    default);
            }

            record.AllocationState = PlayerSlotAllocationState.Joined;
            record.Revision++;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            revision++;

            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "RollbackSceneLocalPlayerRelease",
                resolvedSource,
                resolvedReason,
                "Scene Local Player Slot release rolled back to Joined.",
                previousRevision,
                CreateSlotSnapshot(record),
                default);
        }

        internal PlayerParticipationOperationResult TryAbandonCommittedSceneAdmission(
            SceneLocalPlayerAdmissionToken admissionToken,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback(
                nameof(PlayerParticipationRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback(
                "abandon-committed-scene-admission");
            int previousRevision = revision;

            if (!TryResolveCurrentSceneAdmissionRecord(
                    admissionToken,
                    PlayerSlotAllocationState.Joined,
                    requireJoinedRevisionMatch: true,
                    out SlotRecord record,
                    out string issue))
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    "AbandonCommittedSceneAdmission",
                    resolvedSource,
                    resolvedReason,
                    issue,
                    previousRevision,
                    record != null ? CreateSlotSnapshot(record) : default,
                    default);
            }

            if (record.SelectedActorProfile != null)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidState,
                    "AbandonCommittedSceneAdmission",
                    resolvedSource,
                    resolvedReason,
                    "Committed Scene admission cannot be abandoned after Actor selection.",
                    previousRevision,
                    CreateSlotSnapshot(record),
                    default);
            }

            record.AllocationState = PlayerSlotAllocationState.Available;
            record.ReservationToken = default;
            record.Revision++;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            revision++;

            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "AbandonCommittedSceneAdmission",
                resolvedSource,
                resolvedReason,
                "Committed Scene Local Player admission was compensated and Slot returned to Available.",
                previousRevision,
                CreateSlotSnapshot(record),
                default);
        }

        private bool TryResolveCurrentSceneAdmissionRecord(
            SceneLocalPlayerAdmissionToken admissionToken,
            PlayerSlotAllocationState expectedState,
            bool requireJoinedRevisionMatch,
            out SlotRecord record,
            out string issue)
        {
            record = null;
            if (!admissionToken.IsValid ||
                !string.Equals(admissionToken.ContextId, contextId, StringComparison.Ordinal))
            {
                issue = "Scene Local Player admission token is invalid or belongs to another Session context.";
                return false;
            }

            record = FindSlot(admissionToken.PlayerSlotId);
            if (record == null ||
                record.AllocationState != expectedState ||
                (requireJoinedRevisionMatch &&
                    record.Revision != admissionToken.JoinedSlotRevision))
            {
                issue = "Scene Local Player admission token is foreign or stale for the current Slot state.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryResolveReleaseRecord(
            SceneLocalPlayerAdmissionReleaseToken releaseToken,
            out SlotRecord record,
            out string issue)
        {
            record = null;
            if (!releaseToken.IsValid ||
                !string.Equals(releaseToken.AdmissionToken.ContextId, contextId, StringComparison.Ordinal))
            {
                issue = "Scene Local Player release token is invalid or belongs to another Session context.";
                return false;
            }

            record = FindSlot(releaseToken.AdmissionToken.PlayerSlotId);
            if (record == null ||
                record.AllocationState != PlayerSlotAllocationState.Leaving ||
                record.Revision != releaseToken.LeavingSlotRevision)
            {
                issue = "Scene Local Player release token is foreign or stale for the current Leaving Slot state.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }

    internal readonly struct SceneLocalPlayerAdmissionReleaseToken
    {
        internal SceneLocalPlayerAdmissionReleaseToken(
            SceneLocalPlayerAdmissionToken admissionToken,
            int joinedSlotRevision,
            int leavingSlotRevision)
        {
            AdmissionToken = admissionToken;
            JoinedSlotRevision = joinedSlotRevision;
            LeavingSlotRevision = leavingSlotRevision;
        }

        internal SceneLocalPlayerAdmissionToken AdmissionToken { get; }
        internal int JoinedSlotRevision { get; }
        internal int LeavingSlotRevision { get; }

        internal bool IsValid =>
            AdmissionToken.IsValid &&
            JoinedSlotRevision >= AdmissionToken.JoinedSlotRevision &&
            LeavingSlotRevision > JoinedSlotRevision;
    }
}
