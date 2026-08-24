using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable ordered Session snapshot for effective Player gameplay occupancy.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.2 Session effective Player gameplay occupancy snapshot.")]
    public sealed class PlayerGameplayOccupancySnapshot
    {
        private readonly PlayerGameplayOccupancySummary[] _slots;

        internal PlayerGameplayOccupancySnapshot(
            string sessionContextId,
            int revision,
            PlayerGameplayOccupancySummary[] slots,
            PlayerGameplayOccupancyStatus lastOperationStatus,
            string lastOperationMessage)
        {
            SessionContextId = sessionContextId ?? string.Empty;
            Revision = revision;
            this._slots = slots != null
                ? (PlayerGameplayOccupancySummary[])slots.Clone()
                : Array.Empty<PlayerGameplayOccupancySummary>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this._slots.Length; index++)
            {
                if (this._slots[index].IsOccupied)
                {
                    OccupiedCount++;
                }
                else if (this._slots[index].IsVacant)
                {
                    VacantCount++;
                }
            }
        }

        public string SessionContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<PlayerGameplayOccupancySummary> Slots => _slots;
        public int ConfiguredSlotCount => _slots.Length;
        public int OccupiedCount { get; }
        public int VacantCount { get; }
        public PlayerGameplayOccupancyStatus LastOperationStatus { get; }
        public string LastOperationMessage { get; }

        public bool IsInitialized =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Revision > 0;

        public bool TryGetSummary(
            PlayerSlotId playerSlotId,
            out PlayerGameplayOccupancySummary summary)
        {
            for (int index = 0; index < _slots.Length; index++)
            {
                if (_slots[index].PlayerSlotId == playerSlotId)
                {
                    summary = _slots[index];
                    return true;
                }
            }

            summary = default;
            return false;
        }
    }
}
