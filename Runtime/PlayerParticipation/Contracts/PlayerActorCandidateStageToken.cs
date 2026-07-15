using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.7C exact contextual Logical Player Actor candidate token.")]
    public readonly struct PlayerActorCandidateStageToken :
        IEquatable<PlayerActorCandidateStageToken>
    {
        internal PlayerActorCandidateStageToken(
            string sessionContextId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentIdentity runtimeContentIdentity,
            int candidateRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Owner = owner;
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            RuntimeContentIdentity = runtimeContentIdentity;
            CandidateRevision = candidateRevision;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner Owner { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int CandidateRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Owner.IsValid &&
            Owner.Scope == RuntimeContentScope.Activity &&
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            ActorId.IsValid &&
            RuntimeContentIdentity.IsValid &&
            RuntimeContentIdentity.Owner == Owner &&
            CandidateRevision > 0;

        public string StableText => IsValid
            ? $"player-actor-candidate:{SessionContextId}:{PlayerSlotId.StableText}:" +
              $"{Owner.OwnerIdentity.Value.Value}:{CandidateRevision}:{ActorId.StableText}"
            : string.Empty;

        public bool Equals(PlayerActorCandidateStageToken other)
        {
            return string.Equals(
                    SessionContextId,
                    other.SessionContextId,
                    StringComparison.Ordinal) &&
                Owner == other.Owner &&
                PlayerSlotId == other.PlayerSlotId &&
                ActorProfileId == other.ActorProfileId &&
                ActorId == other.ActorId &&
                RuntimeContentIdentity == other.RuntimeContentIdentity &&
                CandidateRevision == other.CandidateRevision;
        }

        public override bool Equals(object obj) =>
            obj is PlayerActorCandidateStageToken other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(
                    SessionContextId ?? string.Empty);
                hash = hash * 397 ^ Owner.GetHashCode();
                hash = hash * 397 ^ PlayerSlotId.GetHashCode();
                hash = hash * 397 ^ ActorProfileId.GetHashCode();
                hash = hash * 397 ^ ActorId.GetHashCode();
                hash = hash * 397 ^ RuntimeContentIdentity.GetHashCode();
                hash = hash * 397 ^ CandidateRevision;
                return hash;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            PlayerActorCandidateStageToken left,
            PlayerActorCandidateStageToken right) => left.Equals(right);

        public static bool operator !=(
            PlayerActorCandidateStageToken left,
            PlayerActorCandidateStageToken right) => !left.Equals(right);
    }
}
