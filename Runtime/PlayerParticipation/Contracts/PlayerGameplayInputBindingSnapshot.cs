using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable ordered Session snapshot for current gameplay input bindings.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.3 Session gameplay input binding snapshot.")]
    public sealed class PlayerGameplayInputBindingSnapshot
    {
        private readonly PlayerGameplayInputBindingSummary[] slots;

        internal PlayerGameplayInputBindingSnapshot(
            string sessionContextId,
            int revision,
            PlayerGameplayInputBindingSummary[] slots,
            PlayerGameplayInputBindingStatus lastOperationStatus,
            string lastOperationMessage)
        {
            SessionContextId = sessionContextId ?? string.Empty;
            Revision = revision;
            this.slots = slots != null
                ? (PlayerGameplayInputBindingSummary[])slots.Clone()
                : Array.Empty<PlayerGameplayInputBindingSummary>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this.slots.Length; index++)
            {
                PlayerGameplayInputBindingSummary summary = this.slots[index];
                if (summary.IsBound || summary.IsReleaseFailed)
                {
                    BoundCount++;
                    if (summary.IsAllowed) AllowedCount++;
                    if (summary.IsBlockedByGate) BlockedCount++;
                    if (summary.IsReleaseFailed) ReleaseFailedCount++;
                }
                else if (summary.IsUnbound)
                {
                    UnboundCount++;
                }
            }
        }

        public string SessionContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<PlayerGameplayInputBindingSummary> Slots => slots;
        public int ConfiguredSlotCount => slots.Length;
        public int BoundCount { get; }
        public int UnboundCount { get; }
        public int AllowedCount { get; }
        public int BlockedCount { get; }
        public int ReleaseFailedCount { get; }
        public PlayerGameplayInputBindingStatus LastOperationStatus { get; }
        public string LastOperationMessage { get; }
        public bool IsInitialized => !string.IsNullOrEmpty(SessionContextId) && Revision > 0;

        public bool TryGetSummary(
            PlayerSlotId playerSlotId,
            out PlayerGameplayInputBindingSummary summary)
        {
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index].PlayerSlotId == playerSlotId)
                {
                    summary = slots[index];
                    return true;
                }
            }

            summary = default;
            return false;
        }
    }
}
