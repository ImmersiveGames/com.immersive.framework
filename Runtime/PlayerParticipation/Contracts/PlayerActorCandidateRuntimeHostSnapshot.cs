using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7C runtime-host contextual Player Actor candidate diagnostics.")]
    public sealed class PlayerActorCandidateRuntimeHostSnapshot
    {
        private readonly PlayerActorCandidateStageSnapshot[] candidates;

        internal PlayerActorCandidateRuntimeHostSnapshot(
            bool initialized,
            string sessionContextId,
            int revision,
            PlayerActorCandidateStageSnapshot[] candidates,
            PlayerActorCandidateStageStatus lastOperationStatus,
            string lastOperationMessage)
        {
            IsInitialized = initialized;
            SessionContextId = sessionContextId ?? string.Empty;
            Revision = revision;
            this.candidates = candidates != null
                ? (PlayerActorCandidateStageSnapshot[])candidates.Clone()
                : Array.Empty<PlayerActorCandidateStageSnapshot>();
            LastOperationStatus = lastOperationStatus;
            LastOperationMessage = lastOperationMessage ?? string.Empty;

            int stagedInactiveCount = 0;
            int rollbackFailedCount = 0;
            for (int index = 0; index < this.candidates.Length; index++)
            {
                if (this.candidates[index]?.IsStagedInactive == true)
                {
                    stagedInactiveCount++;
                }
                else if (this.candidates[index]?.IsRollbackFailed == true)
                {
                    rollbackFailedCount++;
                }
            }

            StagedInactiveCount = stagedInactiveCount;
            RollbackFailedCount = rollbackFailedCount;
        }

        public bool IsInitialized { get; }
        public string SessionContextId { get; }
        public int Revision { get; }
        public IReadOnlyList<PlayerActorCandidateStageSnapshot> Candidates => candidates;
        public int CandidateCount => candidates.Length;
        public int StagedInactiveCount { get; }
        public int RollbackFailedCount { get; }
        public PlayerActorCandidateStageStatus LastOperationStatus { get; }
        public string LastOperationMessage { get; }

        internal static PlayerActorCandidateRuntimeHostSnapshot Unavailable(
            string message)
        {
            return new PlayerActorCandidateRuntimeHostSnapshot(
                false,
                string.Empty,
                0,
                Array.Empty<PlayerActorCandidateStageSnapshot>(),
                PlayerActorCandidateStageStatus.RejectedRuntimeUnavailable,
                message);
        }
    }
}
