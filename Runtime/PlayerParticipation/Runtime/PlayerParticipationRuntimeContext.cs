using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Session-scoped mutable authority for ordered Player Slot allocation.
    /// This plain C# context is composed by Framework Core and is not a singleton or service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "P3F Session Player participation runtime authority.")]
    internal sealed class PlayerParticipationRuntimeContext
    {
        private sealed class SlotRecord
        {
            internal SlotRecord(int configuredIndex, PlayerSlotProfile profile, PlayerSlotId playerSlotId)
            {
                ConfiguredIndex = configuredIndex;
                Profile = profile;
                PlayerSlotId = playerSlotId;
                AllocationState = PlayerSlotAllocationState.Available;
                Revision = 0;
                Source = "PlayerParticipationRuntimeContext";
                Reason = "initialization";
            }

            internal int ConfiguredIndex { get; }
            internal PlayerSlotProfile Profile { get; }
            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerSlotAllocationState AllocationState { get; set; }
            internal PlayerSlotReservationToken ReservationToken { get; set; }
            internal int Revision { get; set; }
            internal string Source { get; set; }
            internal string Reason { get; set; }
        }

        private readonly string contextId;
        private readonly List<SlotRecord> slots;
        private int revision;
        private int reservationSequence;
        private int dynamicCapacity;
        private bool joiningOpen;
        private PlayerParticipationOperationStatus lastOperationStatus;
        private string lastOperationMessage;

        private PlayerParticipationRuntimeContext(
            List<SlotRecord> slots,
            int initialDynamicCapacity,
            bool initialJoiningOpen)
        {
            contextId = Guid.NewGuid().ToString("N");
            this.slots = slots ?? throw new ArgumentNullException(nameof(slots));
            dynamicCapacity = initialDynamicCapacity;
            joiningOpen = initialJoiningOpen;
            revision = 1;
            lastOperationStatus = PlayerParticipationOperationStatus.Succeeded;
            lastOperationMessage = "Player participation runtime context initialized.";
        }

        internal static PlayerParticipationOperationResult TryCreate(
            IReadOnlyList<PlayerSlotProfile> orderedProfiles,
            int initialDynamicCapacity,
            bool initialJoiningOpen,
            string source,
            string reason,
            out PlayerParticipationRuntimeContext context)
        {
            string resolvedSource = source.NormalizeTextOrFallback("PlayerParticipationRuntimeContext");
            string resolvedReason = reason.NormalizeTextOrFallback("initialization");

            if (!TryCreateSlotRecords(orderedProfiles, out List<SlotRecord> records, out string issue))
            {
                context = null;
                return CreateInitializationFailure(resolvedSource, resolvedReason, issue);
            }

            if (initialDynamicCapacity < 0 || initialDynamicCapacity > records.Count)
            {
                context = null;
                return CreateInitializationFailure(
                    resolvedSource,
                    resolvedReason,
                    $"Initial dynamic capacity '{initialDynamicCapacity}' must be between 0 and configured Slot count '{records.Count}'.");
            }

            context = new PlayerParticipationRuntimeContext(
                records,
                initialDynamicCapacity,
                initialJoiningOpen);
            return context.CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "Initialize",
                resolvedSource,
                resolvedReason,
                "Player participation runtime context initialized.",
                0,
                default,
                default);
        }

        internal PlayerParticipationOperationResult TrySetDynamicCapacity(
            int requestedCapacity,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback("capacity-change");
            int previousRevision = revision;

            if (requestedCapacity < 0 || requestedCapacity > slots.Count)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedInvalidRequest,
                    "SetDynamicCapacity",
                    resolvedSource,
                    resolvedReason,
                    $"Dynamic capacity '{requestedCapacity}' must be between 0 and configured Slot count '{slots.Count}'.",
                    previousRevision,
                    default,
                    default);
            }

            if (requestedCapacity == dynamicCapacity)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.IgnoredNoChange,
                    "SetDynamicCapacity",
                    resolvedSource,
                    resolvedReason,
                    "Dynamic capacity already matches the requested value.",
                    previousRevision,
                    default,
                    default);
            }

            dynamicCapacity = requestedCapacity;
            revision++;
            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "SetDynamicCapacity",
                resolvedSource,
                resolvedReason,
                "Dynamic capacity changed without evicting existing participation.",
                previousRevision,
                default,
                default);
        }

        internal PlayerParticipationOperationResult TryOpenJoining(string source, string reason)
        {
            return TrySetJoiningOpen(true, source, reason);
        }

        internal PlayerParticipationOperationResult TryCloseJoining(string source, string reason)
        {
            return TrySetJoiningOpen(false, source, reason);
        }

        internal PlayerParticipationOperationResult TryReserveNextAvailableSlot(
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback("reserve-next-slot");
            int previousRevision = revision;

            if (!joiningOpen)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedJoiningClosed,
                    "ReserveNextAvailableSlot",
                    resolvedSource,
                    resolvedReason,
                    "Slot reservation rejected because joining is closed.",
                    previousRevision,
                    default,
                    default);
            }

            if (CountConsumedCapacity() >= dynamicCapacity)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedCapacityReached,
                    "ReserveNextAvailableSlot",
                    resolvedSource,
                    resolvedReason,
                    $"Slot reservation rejected because dynamic capacity '{dynamicCapacity}' is reached.",
                    previousRevision,
                    default,
                    default);
            }

            SlotRecord selected = null;
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].AllocationState == PlayerSlotAllocationState.Available)
                {
                    selected = slots[index];
                    break;
                }
            }

            if (selected == null)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedNoAvailableSlot,
                    "ReserveNextAvailableSlot",
                    resolvedSource,
                    resolvedReason,
                    "Slot reservation rejected because no configured Slot is Available.",
                    previousRevision,
                    default,
                    default);
            }

            selected.AllocationState = PlayerSlotAllocationState.Reserved;
            selected.Revision++;
            reservationSequence++;
            selected.ReservationToken = new PlayerSlotReservationToken(
                contextId,
                reservationSequence,
                selected.PlayerSlotId,
                selected.Revision);
            selected.Source = resolvedSource;
            selected.Reason = resolvedReason;
            revision++;

            PlayerSlotRuntimeSnapshot slotSnapshot = CreateSlotSnapshot(selected);
            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                "ReserveNextAvailableSlot",
                resolvedSource,
                resolvedReason,
                "First configured Available Slot reserved.",
                previousRevision,
                slotSnapshot,
                selected.ReservationToken);
        }

        internal PlayerParticipationOperationResult TryReleaseReservation(
            PlayerSlotReservationToken reservationToken,
            string source,
            string reason)
        {
            return ApplyReservationTransition(
                reservationToken,
                PlayerSlotAllocationState.Available,
                "ReleaseReservation",
                source,
                reason,
                "Reservation released and Slot returned to Available.");
        }

        internal PlayerParticipationOperationResult TryMarkJoined(
            PlayerSlotReservationToken reservationToken,
            string source,
            string reason)
        {
            return ApplyReservationTransition(
                reservationToken,
                PlayerSlotAllocationState.Joined,
                "MarkJoined",
                source,
                reason,
                "Reserved Slot marked Joined.");
        }

        internal bool TryGetSlotSnapshot(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            if (!playerSlotId.IsValid)
            {
                snapshot = default;
                return false;
            }

            SlotRecord record = FindSlot(playerSlotId);
            if (record == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = CreateSlotSnapshot(record);
            return true;
        }

        internal PlayerParticipationSnapshot CreateSnapshot()
        {
            var snapshots = new PlayerSlotRuntimeSnapshot[slots.Count];
            for (int index = 0; index < slots.Count; index++)
            {
                snapshots[index] = CreateSlotSnapshot(slots[index]);
            }

            return new PlayerParticipationSnapshot(
                contextId,
                revision,
                true,
                dynamicCapacity,
                joiningOpen,
                snapshots,
                lastOperationStatus,
                lastOperationMessage);
        }

        private PlayerParticipationOperationResult TrySetJoiningOpen(
            bool requestedOpen,
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback(requestedOpen ? "open-joining" : "close-joining");
            int previousRevision = revision;

            if (joiningOpen == requestedOpen)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.IgnoredNoChange,
                    requestedOpen ? "OpenJoining" : "CloseJoining",
                    resolvedSource,
                    resolvedReason,
                    requestedOpen ? "Joining is already open." : "Joining is already closed.",
                    previousRevision,
                    default,
                    default);
            }

            joiningOpen = requestedOpen;
            revision++;
            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                requestedOpen ? "OpenJoining" : "CloseJoining",
                resolvedSource,
                resolvedReason,
                requestedOpen ? "Joining opened." : "Joining closed.",
                previousRevision,
                default,
                default);
        }

        private PlayerParticipationOperationResult ApplyReservationTransition(
            PlayerSlotReservationToken reservationToken,
            PlayerSlotAllocationState targetState,
            string operation,
            string source,
            string reason,
            string successMessage)
        {
            string resolvedSource = source.NormalizeTextOrFallback("Unknown");
            string resolvedReason = reason.NormalizeTextOrFallback(operation);
            int previousRevision = revision;

            if (!reservationToken.IsValid ||
                !string.Equals(reservationToken.ContextId, contextId, StringComparison.Ordinal))
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    operation,
                    resolvedSource,
                    resolvedReason,
                    "Reservation token is invalid or belongs to another Session participation context.",
                    previousRevision,
                    default,
                    reservationToken);
            }

            SlotRecord record = FindSlot(reservationToken.PlayerSlotId);
            if (record == null ||
                record.AllocationState != PlayerSlotAllocationState.Reserved ||
                record.ReservationToken != reservationToken ||
                record.Revision != reservationToken.SlotRevision)
            {
                return CreateResult(
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    operation,
                    resolvedSource,
                    resolvedReason,
                    "Reservation token is stale or no longer owns the reserved Slot.",
                    previousRevision,
                    record != null ? CreateSlotSnapshot(record) : default,
                    reservationToken);
            }

            record.AllocationState = targetState;
            record.ReservationToken = default;
            record.Revision++;
            record.Source = resolvedSource;
            record.Reason = resolvedReason;
            revision++;

            return CreateResult(
                PlayerParticipationOperationStatus.Succeeded,
                operation,
                resolvedSource,
                resolvedReason,
                successMessage,
                previousRevision,
                CreateSlotSnapshot(record),
                reservationToken);
        }

        private PlayerParticipationOperationResult CreateResult(
            PlayerParticipationOperationStatus status,
            string operation,
            string source,
            string reason,
            string message,
            int previousRevision,
            PlayerSlotRuntimeSnapshot slot,
            PlayerSlotReservationToken reservationToken)
        {
            lastOperationStatus = status;
            lastOperationMessage = message ?? string.Empty;
            PlayerParticipationSnapshot snapshot = CreateSnapshot();
            return new PlayerParticipationOperationResult(
                status,
                operation,
                source,
                reason,
                message,
                previousRevision,
                revision,
                slot,
                reservationToken,
                snapshot);
        }

        private static PlayerParticipationOperationResult CreateInitializationFailure(
            string source,
            string reason,
            string issue)
        {
            PlayerParticipationSnapshot snapshot = PlayerParticipationSnapshot.Empty(
                PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                issue);
            return new PlayerParticipationOperationResult(
                PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                "Initialize",
                source,
                reason,
                issue,
                0,
                0,
                default,
                default,
                snapshot);
        }

        private int CountConsumedCapacity()
        {
            int count = 0;
            for (int index = 0; index < slots.Count; index++)
            {
                PlayerSlotAllocationState state = slots[index].AllocationState;
                if (state == PlayerSlotAllocationState.Reserved ||
                    state == PlayerSlotAllocationState.Joined ||
                    state == PlayerSlotAllocationState.Leaving)
                {
                    count++;
                }
            }

            return count;
        }

        private SlotRecord FindSlot(PlayerSlotId playerSlotId)
        {
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index].PlayerSlotId == playerSlotId)
                {
                    return slots[index];
                }
            }

            return null;
        }

        private static PlayerSlotRuntimeSnapshot CreateSlotSnapshot(SlotRecord record)
        {
            return new PlayerSlotRuntimeSnapshot(
                record.ConfiguredIndex,
                record.Profile,
                record.PlayerSlotId,
                record.AllocationState,
                record.ReservationToken,
                record.Revision,
                record.Source,
                record.Reason);
        }

        private static bool TryCreateSlotRecords(
            IReadOnlyList<PlayerSlotProfile> orderedProfiles,
            out List<SlotRecord> records,
            out string issue)
        {
            records = new List<SlotRecord>();
            issue = string.Empty;

            if (orderedProfiles == null || orderedProfiles.Count == 0)
            {
                issue = "At least one configured PlayerSlotProfile is required.";
                return false;
            }

            var profileReferences = new HashSet<PlayerSlotProfile>();
            var identities = new HashSet<PlayerSlotId>();
            for (int index = 0; index < orderedProfiles.Count; index++)
            {
                PlayerSlotProfile profile = orderedProfiles[index];
                if (profile == null)
                {
                    issue = $"Configured Player Slot at index '{index}' is missing its PlayerSlotProfile reference.";
                    return false;
                }

                if (!profileReferences.Add(profile))
                {
                    issue = $"Configured Player Slot at index '{index}' repeats PlayerSlotProfile '{profile.name}'.";
                    return false;
                }

                if (!profile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out string identityIssue))
                {
                    issue = identityIssue;
                    return false;
                }

                if (!identities.Add(playerSlotId))
                {
                    issue = $"Configured Player Slot at index '{index}' repeats PlayerSlotId '{playerSlotId.StableText}'.";
                    return false;
                }

                records.Add(new SlotRecord(index, profile, playerSlotId));
            }

            return true;
        }
    }
}
