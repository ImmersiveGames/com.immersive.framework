using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable ordered Session snapshot for prepared Player camera eligibility.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 Session prepared Player camera eligibility snapshot.")]
    public sealed class PlayerGameplayCameraEligibilitySnapshot
    {
        private readonly PlayerGameplayCameraEligibilitySummary[] slots;

        internal PlayerGameplayCameraEligibilitySnapshot(
            string sessionContextId,
            int revision,
            PlayerGameplayCameraEligibilitySummary[] slots,
            PlayerGameplayCameraEligibilityStatus lastOperationStatus,
            string lastOperationMessage)
        {
            SessionContextId = sessionContextId ?? string.Empty;
            Revision = revision;
            this.slots = slots != null
                ? (PlayerGameplayCameraEligibilitySummary[])slots.Clone()
                : Array.Empty<PlayerGameplayCameraEligibilitySummary>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this.slots.Length; index++)
            {
                PlayerGameplayCameraEligibilitySummary summary =
                    this.slots[index];
                if (summary.IsEligible)
                {
                    EligibleCount++;
                    if (summary.IsRequired) RequiredEligibleCount++;
                    else OptionalEligibleCount++;
                }
                else if (summary.IsSkippedOptional)
                {
                    SkippedOptionalCount++;
                }
                else if (summary.IsNotEvaluated)
                {
                    NotEvaluatedCount++;
                }
            }
        }

        public string SessionContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<PlayerGameplayCameraEligibilitySummary> Slots =>
            slots;
        public int ConfiguredSlotCount => slots.Length;
        public int EligibleCount { get; }
        public int RequiredEligibleCount { get; }
        public int OptionalEligibleCount { get; }
        public int SkippedOptionalCount { get; }
        public int NotEvaluatedCount { get; }
        public PlayerGameplayCameraEligibilityStatus LastOperationStatus { get; }
        public string LastOperationMessage { get; }

        public bool IsInitialized =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Revision > 0;

        public bool TryGetSummary(
            PlayerSlotId playerSlotId,
            out PlayerGameplayCameraEligibilitySummary summary)
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
