using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable ordered Session participation snapshot.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3F/P3H Session Player participation snapshot with Actor selection evidence.")]
    public sealed class PlayerParticipationSnapshot
    {
        private readonly PlayerSlotRuntimeSnapshot[] _slots;

        internal PlayerParticipationSnapshot(
            string contextId,
            int revision,
            bool initialized,
            bool joiningOpen,
            PlayerActorSelectionDuplicatePolicy actorSelectionDuplicatePolicy,
            PlayerSlotRuntimeSnapshot[] slots,
            PlayerParticipationOperationStatus lastOperationStatus,
            string lastOperationMessage)
        {
            ContextId = contextId ?? string.Empty;
            Revision = revision;
            IsInitialized = initialized;
            JoiningOpen = joiningOpen;
            ActorSelectionDuplicatePolicy = actorSelectionDuplicatePolicy;
            this._slots = slots != null
                ? (PlayerSlotRuntimeSnapshot[])slots.Clone()
                : Array.Empty<PlayerSlotRuntimeSnapshot>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this._slots.Length; index++)
            {
                PlayerSlotRuntimeSnapshot slot = this._slots[index];
                switch (slot.AllocationState)
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
                        if (!slot.HasSelectedActor)
                        {
                            JoinedWithoutSelectedActorCount++;
                        }
                        break;
                    case PlayerSlotAllocationState.Leaving:
                        LeavingCount++;
                        break;
                }

                if (slot.HasSelectedActor)
                {
                    SelectedActorCount++;
                }
            }
        }

        public string ContextId { get; }

        public int Revision { get; }

        public bool IsInitialized { get; }

        public bool JoiningOpen { get; }

        public bool HasActorSelectionPolicy =>
            ActorSelectionDuplicatePolicy.IsDefinedPolicy();

        public PlayerActorSelectionDuplicatePolicy ActorSelectionDuplicatePolicy { get; }

        public IReadOnlyList<PlayerSlotRuntimeSnapshot> Slots => _slots;

        public int ConfiguredSlotCount => _slots.Length;

        public int UnavailableCount { get; }

        public int AvailableCount { get; }

        public int ReservedCount { get; }

        public int JoinedCount { get; }

        public int LeavingCount { get; }

        public int SelectedActorCount { get; }

        public int JoinedWithoutSelectedActorCount { get; }

        public bool AllJoinedSlotsHaveSelectedActors =>
            JoinedCount > 0 && JoinedWithoutSelectedActorCount == 0;

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
                false,
                PlayerActorSelectionDuplicatePolicy.Unspecified,
                Array.Empty<PlayerSlotRuntimeSnapshot>(),
                status,
                message);
        }
    }
}
