using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable evidence for one configured Session Player Slot.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F Player Slot runtime snapshot.")]
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
            string reason)
        {
            ConfiguredIndex = configuredIndex;
            Profile = profile;
            PlayerSlotId = playerSlotId;
            AllocationState = allocationState;
            ReservationToken = reservationToken;
            Revision = revision;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public int ConfiguredIndex { get; }

        public PlayerSlotProfile Profile { get; }

        public PlayerSlotId PlayerSlotId { get; }

        public PlayerSlotAllocationState AllocationState { get; }

        public PlayerSlotReservationToken ReservationToken { get; }

        public int Revision { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid =>
            ConfiguredIndex >= 0 &&
            Profile != null &&
            PlayerSlotId.IsValid &&
            Revision >= 0;

        public bool IsReserved => AllocationState == PlayerSlotAllocationState.Reserved;

        public bool IsJoined => AllocationState == PlayerSlotAllocationState.Joined;

        public bool ConsumesCapacity =>
            AllocationState == PlayerSlotAllocationState.Reserved ||
            AllocationState == PlayerSlotAllocationState.Joined ||
            AllocationState == PlayerSlotAllocationState.Leaving;
    }
}
