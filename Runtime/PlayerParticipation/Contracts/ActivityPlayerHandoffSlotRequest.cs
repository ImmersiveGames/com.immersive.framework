using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "P3K.7E exact per-Slot request for an Activity Player handoff group.")]
    public readonly struct ActivityPlayerHandoffSlotRequest
    {
        public ActivityPlayerHandoffSlotRequest(
            PlayerActorCandidateStageToken candidateToken,
            PlayerGameplayAdmissionToken currentAdmissionToken)
        {
            CandidateToken = candidateToken;
            CurrentAdmissionToken = currentAdmissionToken;
        }

        public PlayerActorCandidateStageToken CandidateToken { get; }
        public PlayerGameplayAdmissionToken CurrentAdmissionToken { get; }
        public PlayerSlotId PlayerSlotId => CandidateToken.PlayerSlotId;
        public bool IsValid => CandidateToken.IsValid &&
            CurrentAdmissionToken.IsValid &&
            CandidateToken.SessionContextId == CurrentAdmissionToken.SessionContextId &&
            CandidateToken.PlayerSlotId == CurrentAdmissionToken.PlayerSlotId;
    }
}
