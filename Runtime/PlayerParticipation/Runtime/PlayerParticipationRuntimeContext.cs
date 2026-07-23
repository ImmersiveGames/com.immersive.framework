using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
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
    internal sealed partial class PlayerParticipationRuntimeContext
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
                SelectedActorProfile = null;
                SelectionRevision = 0;
                SelectionSource = string.Empty;
                SelectionReason = string.Empty;
            }

            internal int ConfiguredIndex { get; }
            internal PlayerSlotProfile Profile { get; }
            internal PlayerSlotId PlayerSlotId { get; }
            internal PlayerSlotAllocationState AllocationState { get; set; }
            internal PlayerSlotReservationToken ReservationToken { get; set; }
            internal int Revision { get; set; }
            internal string Source { get; set; }
            internal string Reason { get; set; }
            internal ActorProfile SelectedActorProfile { get; set; }
            internal int SelectionRevision { get; set; }
            internal string SelectionSource { get; set; }
            internal string SelectionReason { get; set; }
        }

        private readonly string contextId;
        private readonly List<SlotRecord> slots;
        private readonly PlayerActorSelectionDuplicatePolicy actorSelectionDuplicatePolicy;
        private int revision;
        private int reservationSequence;
        private int dynamicCapacity;
        private bool joiningOpen;
        private PlayerParticipationOperationStatus lastOperationStatus;
        private string lastOperationMessage;

        private PlayerParticipationRuntimeContext(
            List<SlotRecord> slots,
            int initialDynamicCapacity,
            bool initialJoiningOpen,
            PlayerActorSelectionDuplicatePolicy actorSelectionDuplicatePolicy)
        {
            contextId = Guid.NewGuid().ToString("N");
            this.slots = slots ?? throw new ArgumentNullException(nameof(slots));
            dynamicCapacity = initialDynamicCapacity;
            joiningOpen = initialJoiningOpen;
            this.actorSelectionDuplicatePolicy = actorSelectionDuplicatePolicy;
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
            return TryCreateCore(
                orderedProfiles, initialDynamicCapacity, initialJoiningOpen,
                PlayerActorSelectionDuplicatePolicy.Unspecified,
                "Initialize", "Player participation runtime context initialized.",
                source, reason, out context);
        }

        internal static PlayerParticipationOperationResult TryCreateWithActorSelectionPolicy(
            IReadOnlyList<PlayerSlotProfile> orderedProfiles,
            int initialDynamicCapacity,
            bool initialJoiningOpen,
            PlayerActorSelectionDuplicatePolicy actorSelectionDuplicatePolicy,
            string source,
            string reason,
            out PlayerParticipationRuntimeContext context)
        {
            string resolvedSource = source.NormalizeTextOrFallback("PlayerParticipationRuntimeContext");
            string resolvedReason = reason.NormalizeTextOrFallback("initialization");

            if (actorSelectionDuplicatePolicy == PlayerActorSelectionDuplicatePolicy.Unspecified)
            {
                context = null;
                return CreateInitializationFailure(
                    resolvedSource,
                    resolvedReason,
                    "Player Actor selection duplicate policy is required for a selection-capable Session context.");
            }

            if (!actorSelectionDuplicatePolicy.IsDefinedPolicy())
            {
                context = null;
                return CreateInitializationFailure(
                    resolvedSource,
                    resolvedReason,
                    $"Player Actor selection duplicate policy '{actorSelectionDuplicatePolicy}' is invalid for a selection-capable Session context.");
            }

            return TryCreateCore(
                orderedProfiles, initialDynamicCapacity, initialJoiningOpen,
                actorSelectionDuplicatePolicy, "InitializeWithActorSelectionPolicy",
                $"Player participation runtime context initialized with Actor selection policy '{actorSelectionDuplicatePolicy}'.",
                resolvedSource, resolvedReason, out context);
        }

        private static PlayerParticipationOperationResult TryCreateCore(
            IReadOnlyList<PlayerSlotProfile> orderedProfiles,
            int initialDynamicCapacity,
            bool initialJoiningOpen,
            PlayerActorSelectionDuplicatePolicy actorSelectionDuplicatePolicy,
            string operation,
            string successMessage,
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
                return CreateInitializationFailure(resolvedSource, resolvedReason,
                    $"Initial dynamic capacity '{initialDynamicCapacity}' must be between 0 and configured Slot count '{records.Count}'.");
            }

            context = new PlayerParticipationRuntimeContext(
                records, initialDynamicCapacity, initialJoiningOpen, actorSelectionDuplicatePolicy);
            return context.CreateResult(
                PlayerParticipationOperationStatus.Succeeded, operation,
                resolvedSource, resolvedReason, successMessage, 0, default, default);
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

        internal PlayerActorSelectionResult TrySelectActorProfile(
            PlayerActorSelectionRequest request)
        {
            return ApplyActorSelection(
                request,
                PlayerActorSelectionOperation.Select,
                "SelectActorProfile");
        }

        internal PlayerActorSelectionResult TryReplaceActorSelection(
            PlayerActorSelectionRequest request)
        {
            return ApplyActorSelection(
                request,
                PlayerActorSelectionOperation.Replace,
                "ReplaceActorSelection");
        }

        internal PlayerActorSelectionResult TryClearActorSelection(
            PlayerActorSelectionRequest request)
        {
            return ApplyActorSelection(
                request,
                PlayerActorSelectionOperation.Clear,
                "ClearActorSelection");
        }

        internal PlayerActorSelectionResult TrySelectDefaultActor(
            PlayerSlotId playerSlotId,
            int expectedSelectionRevision,
            string source,
            string reason)
        {
            SlotRecord record = FindSlot(playerSlotId);
            ActorProfile defaultActorProfile = record != null && record.Profile != null
                ? record.Profile.DefaultActorProfile
                : null;
            var request = new PlayerActorSelectionRequest(
                playerSlotId,
                defaultActorProfile,
                source,
                reason,
                expectedSelectionRevision);
            return ApplyActorSelection(
                request,
                PlayerActorSelectionOperation.Select,
                "SelectDefaultActor");
        }

        internal bool TryGetActorSelection(
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot snapshot)
        {
            return TryGetSlotSnapshot(playerSlotId, out snapshot);
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
                actorSelectionDuplicatePolicy,
                snapshots,
                lastOperationStatus,
                lastOperationMessage);
        }

        private enum PlayerActorSelectionOperation
        {
            Select = 10,
            Replace = 20,
            Clear = 30
        }

        private PlayerActorSelectionResult ApplyActorSelection(
            PlayerActorSelectionRequest request,
            PlayerActorSelectionOperation operation,
            string operationName)
        {
            string source = request.Source.NormalizeText();
            string reason = request.Reason.NormalizeText();

            if (!request.PlayerSlotId.IsValid || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(reason) ||
                request.ExpectedSelectionRevision < PlayerActorSelectionRequest.NoExpectedRevision)
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedInvalidRequest,
                    operationName,
                    request.PlayerSlotId,
                    null,
                    null,
                    0,
                    0,
                    source,
                    reason,
                    default,
                    "Actor selection request is invalid. Slot, source and reason are required and expected revision cannot be below -1.");
            }

            if (actorSelectionDuplicatePolicy == PlayerActorSelectionDuplicatePolicy.Unspecified)
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedPolicyMissing,
                    operationName,
                    request.PlayerSlotId,
                    null,
                    null,
                    0,
                    0,
                    source,
                    reason,
                    default,
                    "Actor selection policy is not configured for this Session participation context.");
            }

            if (!actorSelectionDuplicatePolicy.IsDefinedPolicy())
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedPolicyInvalid,
                    operationName,
                    request.PlayerSlotId,
                    null,
                    null,
                    0,
                    0,
                    source,
                    reason,
                    default,
                    $"Actor selection duplicate policy '{actorSelectionDuplicatePolicy}' is invalid for this Session participation context.");
            }

            SlotRecord record = FindSlot(request.PlayerSlotId);
            if (record == null)
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedSlotNotConfigured,
                    operationName,
                    request.PlayerSlotId,
                    null,
                    null,
                    0,
                    0,
                    source,
                    reason,
                    default,
                    $"Player Slot '{request.PlayerSlotId.StableText}' is not configured in this Session context.");
            }

            ActorProfile previous = record.SelectedActorProfile;
            int previousSelectionRevision = record.SelectionRevision;

            if (record.AllocationState != PlayerSlotAllocationState.Joined)
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedSlotNotJoined,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    previous,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    default,
                    $"Player Slot '{record.PlayerSlotId.StableText}' must be Joined before Actor selection can change.");
            }

            if (request.HasExpectedSelectionRevision &&
                request.ExpectedSelectionRevision != record.SelectionRevision)
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedStaleSelectionRevision,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    previous,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    default,
                    $"Expected selection revision '{request.ExpectedSelectionRevision}' does not match current revision '{record.SelectionRevision}'.");
            }

            if (operation == PlayerActorSelectionOperation.Clear)
            {
                if (request.ActorProfile != null)
                {
                    return CreateActorSelectionResult(
                        PlayerActorSelectionStatus.RejectedInvalidRequest,
                        operationName,
                        record.PlayerSlotId,
                        previous,
                        previous,
                        previousSelectionRevision,
                        previousSelectionRevision,
                        source,
                        reason,
                        default,
                        "Clear Actor selection request must not carry an ActorProfile.");
                }

                if (previous == null)
                {
                    return CreateActorSelectionResult(
                        PlayerActorSelectionStatus.SucceededCleared,
                        operationName,
                        record.PlayerSlotId,
                        null,
                        null,
                        previousSelectionRevision,
                        previousSelectionRevision,
                        source,
                        reason,
                        default,
                        "Actor selection is already clear; no runtime state changed.");
                }

                CommitActorSelection(record, null, source, reason);
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.SucceededCleared,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    null,
                    previousSelectionRevision,
                    record.SelectionRevision,
                    source,
                    reason,
                    default,
                    "Actor selection cleared without changing Slot allocation.");
            }

            if (request.ActorProfile == null)
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedActorProfileMissing,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    previous,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    default,
                    operationName == "SelectDefaultActor"
                        ? $"Player Slot '{record.PlayerSlotId.StableText}' has no Default Actor Profile."
                        : "Actor selection request requires an ActorProfile.");
            }

            if (!TryValidateActorProfile(request.ActorProfile, out ActorProfileId requestedActorProfileId, out string profileIssue))
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedActorProfileInvalid,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    previous,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    default,
                    profileIssue);
            }

            if (operation == PlayerActorSelectionOperation.Select && previous != null)
            {
                if (TryGetActorProfileId(previous, out ActorProfileId previousId) && previousId == requestedActorProfileId)
                {
                    return CreateActorSelectionResult(
                        PlayerActorSelectionStatus.SucceededSelected,
                        operationName,
                        record.PlayerSlotId,
                        previous,
                        previous,
                        previousSelectionRevision,
                        previousSelectionRevision,
                        source,
                        reason,
                        default,
                        "Requested ActorProfile is already selected; no runtime state changed.");
                }

                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedInvalidRequest,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    previous,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    default,
                    "Player Slot already has an Actor selection. Use ReplaceActorSelection for an explicit replacement.");
            }

            if (operation == PlayerActorSelectionOperation.Replace && previous == null)
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedInvalidRequest,
                    operationName,
                    record.PlayerSlotId,
                    null,
                    null,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    default,
                    "Player Slot has no Actor selection to replace. Use SelectActorProfile first.");
            }

            if (previous != null && TryGetActorProfileId(previous, out ActorProfileId currentId) &&
                currentId == requestedActorProfileId)
            {
                return CreateActorSelectionResult(
                    operation == PlayerActorSelectionOperation.Replace
                        ? PlayerActorSelectionStatus.SucceededReplaced
                        : PlayerActorSelectionStatus.SucceededSelected,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    previous,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    default,
                    "Requested ActorProfile is already selected; no runtime state changed.");
            }

            if (actorSelectionDuplicatePolicy.RequiresUniqueActors() &&
                TryFindDuplicateActorSelection(record, requestedActorProfileId, out SlotRecord conflictingRecord))
            {
                return CreateActorSelectionResult(
                    PlayerActorSelectionStatus.RejectedDuplicateActorSelection,
                    operationName,
                    record.PlayerSlotId,
                    previous,
                    previous,
                    previousSelectionRevision,
                    previousSelectionRevision,
                    source,
                    reason,
                    conflictingRecord.PlayerSlotId,
                    $"ActorProfile '{requestedActorProfileId.StableText}' is already selected by Joined Slot '{conflictingRecord.PlayerSlotId.StableText}'.");
            }

            CommitActorSelection(record, request.ActorProfile, source, reason);
            return CreateActorSelectionResult(
                previous == null
                    ? PlayerActorSelectionStatus.SucceededSelected
                    : PlayerActorSelectionStatus.SucceededReplaced,
                operationName,
                record.PlayerSlotId,
                previous,
                request.ActorProfile,
                previousSelectionRevision,
                record.SelectionRevision,
                source,
                reason,
                default,
                previous == null
                    ? "ActorProfile selected for Joined Player Slot."
                    : "ActorProfile selection replaced for Joined Player Slot.");
        }

        private void CommitActorSelection(
            SlotRecord record,
            ActorProfile actorProfile,
            string source,
            string reason)
        {
            record.SelectedActorProfile = actorProfile;
            record.SelectionRevision++;
            record.SelectionSource = source;
            record.SelectionReason = reason;
            record.Revision++;
            revision++;
        }

        private PlayerActorSelectionResult CreateActorSelectionResult(
            PlayerActorSelectionStatus status,
            string operation,
            PlayerSlotId playerSlotId,
            ActorProfile previousActorProfile,
            ActorProfile selectedActorProfile,
            int previousSelectionRevision,
            int currentSelectionRevision,
            string source,
            string reason,
            PlayerSlotId conflictingPlayerSlotId,
            string message)
        {
            SlotRecord record = playerSlotId.IsValid ? FindSlot(playerSlotId) : null;
            PlayerSlotRuntimeSnapshot slotSnapshot = record != null
                ? CreateSlotSnapshot(record)
                : default;
            return new PlayerActorSelectionResult(
                status,
                operation,
                playerSlotId,
                record != null ? record.Profile : null,
                previousActorProfile,
                selectedActorProfile,
                previousSelectionRevision,
                currentSelectionRevision,
                actorSelectionDuplicatePolicy,
                conflictingPlayerSlotId,
                source,
                reason,
                message,
                slotSnapshot,
                CreateSnapshot());
        }

        private bool TryFindDuplicateActorSelection(
            SlotRecord targetRecord,
            ActorProfileId requestedActorProfileId,
            out SlotRecord conflictingRecord)
        {
            for (int index = 0; index < slots.Count; index++)
            {
                SlotRecord candidate = slots[index];
                if (ReferenceEquals(candidate, targetRecord) ||
                    candidate.AllocationState != PlayerSlotAllocationState.Joined ||
                    candidate.SelectedActorProfile == null)
                {
                    continue;
                }

                if (TryGetActorProfileId(candidate.SelectedActorProfile, out ActorProfileId candidateId) &&
                    candidateId == requestedActorProfileId)
                {
                    conflictingRecord = candidate;
                    return true;
                }
            }

            conflictingRecord = null;
            return false;
        }

        private static bool TryValidateActorProfile(
            ActorProfile actorProfile,
            out ActorProfileId actorProfileId,
            out string issue)
        {
            actorProfileId = default;
            if (actorProfile == null)
            {
                issue = "ActorProfile is missing.";
                return false;
            }

            if (!actorProfile.TryGetActorProfileId(out actorProfileId, out issue))
            {
                return false;
            }

            if (!actorProfile.HasDefinedActorKind)
            {
                issue = $"ActorProfile '{actorProfile.name}' has an invalid Actor Kind '{actorProfile.ActorKind}'.";
                return false;
            }

            if (!actorProfile.HasDefinedActorRole)
            {
                issue = $"ActorProfile '{actorProfile.name}' has an invalid Actor Role '{actorProfile.ActorRole}'.";
                return false;
            }

            if (!actorProfile.HasLogicalActorHostPrefab)
            {
                issue = $"ActorProfile '{actorProfile.name}' requires a Logical Actor Host prefab.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool TryGetActorProfileId(
            ActorProfile actorProfile,
            out ActorProfileId actorProfileId)
        {
            if (actorProfile == null)
            {
                actorProfileId = default;
                return false;
            }

            return actorProfile.TryGetActorProfileId(out actorProfileId, out _);
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
                record.Reason,
                record.SelectedActorProfile,
                record.SelectionRevision,
                record.SelectionSource,
                record.SelectionReason);
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
