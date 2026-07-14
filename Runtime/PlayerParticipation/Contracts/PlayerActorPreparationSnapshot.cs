using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable ordered Session snapshot for Logical Player Actor preparation evidence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.4 Session Logical Player Actor preparation snapshot.")]
    public sealed class PlayerActorPreparationSnapshot
    {
        private readonly PlayerActorPreparationSummary[] slots;
        private readonly PlayerActorMaterializationSnapshot[] retainedReleaseFailures;

        internal PlayerActorPreparationSnapshot(
            string sessionContextId,
            int revision,
            PlayerActorPreparationSummary[] slots,
            PlayerActorMaterializationSnapshot[] retainedReleaseFailures,
            PlayerActorPreparationStatus lastOperationStatus,
            string lastOperationMessage)
        {
            SessionContextId = sessionContextId ?? string.Empty;
            Revision = revision;
            this.slots = slots != null
                ? (PlayerActorPreparationSummary[])slots.Clone()
                : Array.Empty<PlayerActorPreparationSummary>();
            this.retainedReleaseFailures = retainedReleaseFailures != null
                ? (PlayerActorMaterializationSnapshot[])retainedReleaseFailures.Clone()
                : Array.Empty<PlayerActorMaterializationSnapshot>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this.slots.Length; index++)
            {
                if (this.slots[index].IsPrepared)
                {
                    PreparedCount++;
                }
                else if (this.slots[index].IsReleaseFailed)
                {
                    ReleaseFailedCount++;
                }
                else if (this.slots[index].IsUnprepared)
                {
                    UnpreparedCount++;
                }
            }
        }

        public string SessionContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<PlayerActorPreparationSummary> Slots => slots;
        public IReadOnlyList<PlayerActorMaterializationSnapshot> RetainedReleaseFailures => retainedReleaseFailures;
        public int ConfiguredSlotCount => slots.Length;
        public int PreparedCount { get; }
        public int UnpreparedCount { get; }
        public int ReleaseFailedCount { get; }
        public int RetainedReleaseFailureCount => retainedReleaseFailures.Length;
        public PlayerActorPreparationStatus LastOperationStatus { get; }
        public string LastOperationMessage { get; }

        public bool IsInitialized =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Revision > 0;
    }
}
