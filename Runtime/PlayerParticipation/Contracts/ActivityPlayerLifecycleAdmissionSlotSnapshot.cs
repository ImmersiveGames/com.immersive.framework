using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7H immutable per-Slot Activity lifecycle admission evidence.")]
    public readonly struct ActivityPlayerLifecycleAdmissionSlotSnapshot
    {
        internal ActivityPlayerLifecycleAdmissionSlotSnapshot(
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken previousAdmissionToken,
            PlayerActorCandidateStageToken candidateToken,
            PlayerActorPreparationToken targetPreparationToken,
            PlayerGameplayAdmissionToken targetAdmissionToken,
            bool staged,
            bool groupBegan,
            bool committed,
            bool adopted,
            bool released,
            string message)
        {
            PlayerSlotId = playerSlotId;
            PreviousAdmissionToken = previousAdmissionToken;
            CandidateToken = candidateToken;
            TargetPreparationToken = targetPreparationToken;
            TargetAdmissionToken = targetAdmissionToken;
            Staged = staged;
            GroupBegan = groupBegan;
            Committed = committed;
            Adopted = adopted;
            Released = released;
            Message = message.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }
        public PlayerGameplayAdmissionToken PreviousAdmissionToken { get; }
        public PlayerActorCandidateStageToken CandidateToken { get; }
        public PlayerActorPreparationToken TargetPreparationToken { get; }
        public PlayerGameplayAdmissionToken TargetAdmissionToken { get; }
        public bool Staged { get; }
        public bool GroupBegan { get; }
        public bool Committed { get; }
        public bool Adopted { get; }
        public bool Released { get; }
        public string Message { get; }

        public bool IsValid =>
            PlayerSlotId.IsValid &&
            PreviousAdmissionToken.IsValid &&
            CandidateToken.IsValid;
    }
}
