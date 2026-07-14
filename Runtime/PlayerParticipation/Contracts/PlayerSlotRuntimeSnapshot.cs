using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable evidence for one configured Session Player Slot.
    /// Allocation and Actor selection are separate state dimensions.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F/P3H Player Slot runtime snapshot with Actor selection evidence.")]
    public readonly struct PlayerSlotRuntimeSnapshot
    {
        internal PlayerSlotRuntimeSnapshot(
            int configuredIndex,
            PlayerSlotProfile profile,
            PlayerSlotId playerSlotId,
            PlayerSlotAllocationState allocationState,
            PlayerSlotReservationToken reservationToken,
            int revision,
            string source,
            string reason,
            ActorProfile selectedActorProfile,
            int selectionRevision,
            string selectionSource,
            string selectionReason)
        {
            ConfiguredIndex = configuredIndex;
            Profile = profile;
            PlayerSlotId = playerSlotId;
            AllocationState = allocationState;
            ReservationToken = reservationToken;
            Revision = revision;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            SelectedActorProfile = selectedActorProfile;
            SelectionRevision = selectionRevision;
            SelectionSource = selectionSource ?? string.Empty;
            SelectionReason = selectionReason ?? string.Empty;
        }

        public int ConfiguredIndex { get; }

        public PlayerSlotProfile Profile { get; }

        public PlayerSlotId PlayerSlotId { get; }

        public PlayerSlotAllocationState AllocationState { get; }

        public PlayerSlotReservationToken ReservationToken { get; }

        /// <summary>
        /// General Slot state revision. Actor selection also increments this revision.
        /// </summary>
        public int Revision { get; }

        public string Source { get; }

        public string Reason { get; }

        public ActorProfile SelectedActorProfile { get; }

        public ActorProfileId SelectedActorProfileId =>
            SelectedActorProfile != null &&
            SelectedActorProfile.TryGetActorProfileId(out ActorProfileId actorProfileId, out _)
                ? actorProfileId
                : default;

        public int SelectionRevision { get; }

        public string SelectionSource { get; }

        public string SelectionReason { get; }

        public bool HasSelectedActor =>
            SelectedActorProfile != null && SelectedActorProfileId.IsValid;

        public bool IsValid =>
            ConfiguredIndex >= 0 &&
            Profile != null &&
            PlayerSlotId.IsValid &&
            Revision >= 0 &&
            SelectionRevision >= 0;

        public bool IsReserved => AllocationState == PlayerSlotAllocationState.Reserved;

        public bool IsJoined => AllocationState == PlayerSlotAllocationState.Joined;

        public bool ConsumesCapacity =>
            AllocationState == PlayerSlotAllocationState.Reserved ||
            AllocationState == PlayerSlotAllocationState.Joined ||
            AllocationState == PlayerSlotAllocationState.Leaving;
    }
}
