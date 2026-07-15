using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3K.7D reversible P3J current/candidate preparation handoff lease.")]
    internal sealed class PlayerActorPreparationHandoff
    {
        internal PlayerActorPreparationHandoff(
            PlayerActorPreparationRuntimeContext owner,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationSummary previousPreparation,
            PlayerActorPreparationSummary currentPreparation,
            PlayerActorCandidatePromotionHandle candidate,
            int revision)
        {
            Owner = owner;
            PlayerSlotId = playerSlotId;
            PreviousPreparation = previousPreparation;
            CurrentPreparation = currentPreparation;
            Candidate = candidate;
            Revision = revision;
        }

        internal PlayerActorPreparationRuntimeContext Owner { get; }
        internal PlayerSlotId PlayerSlotId { get; }
        internal PlayerActorPreparationSummary PreviousPreparation { get; }
        internal PlayerActorPreparationSummary CurrentPreparation { get; }
        internal PlayerActorCandidatePromotionHandle Candidate { get; }
        internal int Revision { get; }
        internal bool CandidateOwnershipCompleted { get; set; }
        internal bool PreviousActorReleased { get; set; }
        internal bool IsCompleted => CandidateOwnershipCompleted && PreviousActorReleased;
    }
}
