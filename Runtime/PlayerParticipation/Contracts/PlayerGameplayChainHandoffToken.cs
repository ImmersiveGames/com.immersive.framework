using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7D exact current/candidate gameplay handoff token.")]
    public readonly struct PlayerGameplayChainHandoffToken :
        IEquatable<PlayerGameplayChainHandoffToken>
    {
        internal PlayerGameplayChainHandoffToken(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerActorCandidateStageToken candidateToken,
            PlayerActorPreparationToken previousPreparationToken,
            PlayerGameplayAdmissionToken previousAdmissionToken,
            int handoffRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            CandidateToken = candidateToken;
            PreviousPreparationToken = previousPreparationToken;
            PreviousAdmissionToken = previousAdmissionToken;
            HandoffRevision = handoffRevision;
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerActorCandidateStageToken CandidateToken { get; }
        public PlayerActorPreparationToken PreviousPreparationToken { get; }
        public PlayerGameplayAdmissionToken PreviousAdmissionToken { get; }
        public int HandoffRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            CandidateToken.IsValid &&
            CandidateToken.SessionContextId == SessionContextId &&
            CandidateToken.PlayerSlotId == PlayerSlotId &&
            PreviousPreparationToken.IsValid &&
            PreviousPreparationToken.SessionContextId == SessionContextId &&
            PreviousPreparationToken.PlayerSlotId == PlayerSlotId &&
            PreviousAdmissionToken.IsValid &&
            PreviousAdmissionToken.SessionContextId == SessionContextId &&
            PreviousAdmissionToken.PlayerSlotId == PlayerSlotId &&
            HandoffRevision > 0;

        public string StableText => IsValid
            ? $"player-gameplay-handoff:{SessionContextId}:{PlayerSlotId.StableText}:" +
              $"{HandoffRevision}:{CandidateToken.CandidateRevision}"
            : string.Empty;

        public bool Equals(PlayerGameplayChainHandoffToken other) =>
            string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
            PlayerSlotId == other.PlayerSlotId &&
            CandidateToken == other.CandidateToken &&
            PreviousPreparationToken == other.PreviousPreparationToken &&
            PreviousAdmissionToken == other.PreviousAdmissionToken &&
            HandoffRevision == other.HandoffRevision;

        public override bool Equals(object obj) =>
            obj is PlayerGameplayChainHandoffToken other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(SessionContextId ?? string.Empty);
                hash = hash * 397 ^ PlayerSlotId.GetHashCode();
                hash = hash * 397 ^ CandidateToken.GetHashCode();
                hash = hash * 397 ^ PreviousPreparationToken.GetHashCode();
                hash = hash * 397 ^ PreviousAdmissionToken.GetHashCode();
                hash = hash * 397 ^ HandoffRevision;
                return hash;
            }
        }

        public override string ToString() => StableText;
        public static bool operator ==(PlayerGameplayChainHandoffToken left, PlayerGameplayChainHandoffToken right) => left.Equals(right);
        public static bool operator !=(PlayerGameplayChainHandoffToken left, PlayerGameplayChainHandoffToken right) => !left.Equals(right);
    }
}
