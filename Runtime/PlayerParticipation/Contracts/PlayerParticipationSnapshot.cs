using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable ordered Session participation snapshot.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F Session Player participation snapshot.")]
    public sealed class PlayerParticipationSnapshot
    {
        private readonly PlayerSlotRuntimeSnapshot[] slots;

        internal PlayerParticipationSnapshot(
            string contextId,
            int revision,
            bool initialized,
            int dynamicCapacity,
            bool joiningOpen,
            PlayerSlotRuntimeSnapshot[] slots,
            PlayerParticipationOperationStatus lastOperationStatus,
            string lastOperationMessage)
        {
            ContextId = contextId ?? string.Empty;
            Revision = revision;
            IsInitialized = initialized;
            DynamicCapacity = dynamicCapacity;
            JoiningOpen = joiningOpen;
            this.slots = slots != null
                ? (PlayerSlotRuntimeSnapshot[])slots.Clone()
                : Array.Empty<PlayerSlotRuntimeSnapshot>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this.slots.Length; index++)
            {
                switch (this.slots[index].AllocationState)
                {
                    case PlayerSlotAllocationState.Unavailable:
                        UnavailableCount++;
                        break;
                    case PlayerSlotAllocationState.Available:
                        AvailableCount++;
                        break;
                    case PlayerSlotAllocationState.Reserved:
                        ReservedCount++;
                        break;
                    case PlayerSlotAllocationState.Joined:
                        JoinedCount++;
                        break;
                    case PlayerSlotAllocationState.Leaving:
                        LeavingCount++;
                        break;
                }
            }
        }

        public string ContextId { get; }

        public int Revision { get; }

        public bool IsInitialized { get; }

        public int DynamicCapacity { get; }

        public bool JoiningOpen { get; }

        public IReadOnlyList<PlayerSlotRuntimeSnapshot> Slots => slots;

        public int ConfiguredSlotCount => slots.Length;

        public int UnavailableCount { get; }

        public int AvailableCount { get; }

        public int ReservedCount { get; }

        public int JoinedCount { get; }

        public int LeavingCount { get; }

        public int ConsumedCapacityCount => ReservedCount + JoinedCount + LeavingCount;

        public bool IsOverCapacity => ConsumedCapacityCount > DynamicCapacity;

        public PlayerParticipationOperationStatus LastOperationStatus { get; }

        public string LastOperationMessage { get; }

        internal static PlayerParticipationSnapshot Empty(
            PlayerParticipationOperationStatus status,
            string message)
        {
            return new PlayerParticipationSnapshot(
                string.Empty,
                0,
                false,
                0,
                false,
                Array.Empty<PlayerSlotRuntimeSnapshot>(),
                status,
                message);
        }
    }
}
