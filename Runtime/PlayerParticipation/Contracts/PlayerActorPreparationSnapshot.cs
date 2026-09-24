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
        private readonly PlayerActorPreparationSummary[] _slots;
        private readonly PlayerActorMaterializationSnapshot[] _retainedReleaseFailures;

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
            this._slots = slots != null
                ? (PlayerActorPreparationSummary[])slots.Clone()
                : Array.Empty<PlayerActorPreparationSummary>();
            this._retainedReleaseFailures = retainedReleaseFailures != null
                ? (PlayerActorMaterializationSnapshot[])retainedReleaseFailures.Clone()
                : Array.Empty<PlayerActorMaterializationSnapshot>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            for (int index = 0; index < this._slots.Length; index++)
            {
                if (this._slots[index].IsPrepared)
                {
                    PreparedCount++;
                }
                else if (this._slots[index].IsReleaseFailed)
                {
                    ReleaseFailedCount++;
                }
                else if (this._slots[index].IsUnprepared)
                {
                    UnpreparedCount++;
                }
            }
        }

        public string SessionContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<PlayerActorPreparationSummary> Slots => _slots;
        public IReadOnlyList<PlayerActorMaterializationSnapshot> RetainedReleaseFailures => _retainedReleaseFailures;
        public int ConfiguredSlotCount => _slots.Length;
        public int PreparedCount { get; }
        public int UnpreparedCount { get; }
        public int ReleaseFailedCount { get; }
        public int RetainedReleaseFailureCount => _retainedReleaseFailures.Length;
        public PlayerActorPreparationStatus LastOperationStatus { get; }
        public string LastOperationMessage { get; }

        public bool IsInitialized =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Revision > 0;
    }
}
