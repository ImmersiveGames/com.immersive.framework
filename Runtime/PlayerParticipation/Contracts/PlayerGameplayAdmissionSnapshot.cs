using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable ordered Session snapshot for gameplay admission/readiness.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.5 Session gameplay admission snapshot.")]
    public sealed class PlayerGameplayAdmissionSnapshot
    {
        private readonly PlayerGameplayAdmissionSummary[] slots;

        internal PlayerGameplayAdmissionSnapshot(
            string sessionContextId,
            int revision,
            PlayerGameplayAdmissionSummary[] slots,
            PlayerGameplayAdmissionStatus lastOperationStatus,
            string lastOperationMessage)
        {
            SessionContextId = sessionContextId ?? string.Empty;
            Revision = revision;
            this.slots = slots != null
                ? (PlayerGameplayAdmissionSummary[])slots.Clone()
                : Array.Empty<PlayerGameplayAdmissionSummary>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this.slots.Length; index++)
            {
                PlayerGameplayAdmissionSummary summary = this.slots[index];
                if (summary.IsReady) ReadyCount++;
                else if (summary.IsBlockedByInputGate) BlockedByInputGateCount++;
                else if (summary.IsReleaseFailed) ReleaseFailedCount++;
                else if (summary.IsNotAdmitted) NotAdmittedCount++;
                if (summary.CameraRequestPublished) PublishedCameraCount++;
            }
        }

        public string SessionContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<PlayerGameplayAdmissionSummary> Slots => slots;
        public int ConfiguredSlotCount => slots.Length;
        public int ReadyCount { get; }
        public int BlockedByInputGateCount { get; }
        public int ReleaseFailedCount { get; }
        public int NotAdmittedCount { get; }
        public int PublishedCameraCount { get; }
        public PlayerGameplayAdmissionStatus LastOperationStatus { get; }
        public string LastOperationMessage { get; }

        public bool IsInitialized =>
            !string.IsNullOrEmpty(SessionContextId) && Revision > 0;

        public bool TryGetSummary(
            PlayerSlotId playerSlotId,
            out PlayerGameplayAdmissionSummary summary)
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
