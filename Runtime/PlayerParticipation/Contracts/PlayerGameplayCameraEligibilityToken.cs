using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Functional token for one current prepared-Player camera eligibility decision.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.4 current prepared Player camera eligibility token.")]
    public readonly struct PlayerGameplayCameraEligibilityToken :
        IEquatable<PlayerGameplayCameraEligibilityToken>
    {
        internal PlayerGameplayCameraEligibilityToken(
            string sessionContextId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            PlayerActorPreparationToken preparationToken,
            PlayerGameplayOccupancyToken occupancyToken,
            PlayerGameplayInputBindingToken inputBindingToken,
            RuntimeContentIdentity runtimeContentIdentity,
            int materializationRevision,
            int occupancyRevision,
            int inputBindingRevision,
            int eligibilityRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Owner = owner;
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            PreparationToken = preparationToken;
            OccupancyToken = occupancyToken;
            InputBindingToken = inputBindingToken;
            RuntimeContentIdentity = runtimeContentIdentity;
            MaterializationRevision = materializationRevision;
            OccupancyRevision = occupancyRevision;
            InputBindingRevision = inputBindingRevision;
            EligibilityRevision = eligibilityRevision;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner Owner { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public PlayerGameplayOccupancyToken OccupancyToken { get; }
        public PlayerGameplayInputBindingToken InputBindingToken { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int MaterializationRevision { get; }
        public int OccupancyRevision { get; }
        public int InputBindingRevision { get; }
        public int EligibilityRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Owner.IsValid &&
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            ActorId.IsValid &&
            PreparationToken.IsValid &&
            OccupancyToken.IsValid &&
            InputBindingToken.IsValid &&
            RuntimeContentIdentity.IsValid &&
            RuntimeContentIdentity.Owner == Owner &&
            MaterializationRevision > 0 &&
            OccupancyRevision > 0 &&
            InputBindingRevision > 0 &&
            EligibilityRevision > 0 &&
            string.Equals(
                PreparationToken.SessionContextId,
                SessionContextId,
                StringComparison.Ordinal) &&
            PreparationToken.PlayerSlotId == PlayerSlotId &&
            PreparationToken.ActorId == ActorId &&
            PreparationToken.RuntimeContentIdentity == RuntimeContentIdentity &&
            PreparationToken.MaterializationRevision == MaterializationRevision &&
            OccupancyToken.SessionContextId == SessionContextId &&
            OccupancyToken.Owner == Owner &&
            OccupancyToken.PlayerSlotId == PlayerSlotId &&
            OccupancyToken.ActorProfileId == ActorProfileId &&
            OccupancyToken.ActorId == ActorId &&
            OccupancyToken.PreparationToken == PreparationToken &&
            OccupancyToken.RuntimeContentIdentity == RuntimeContentIdentity &&
            OccupancyToken.MaterializationRevision == MaterializationRevision &&
            OccupancyToken.OccupancyRevision == OccupancyRevision &&
            InputBindingToken.SessionContextId == SessionContextId &&
            InputBindingToken.Owner == Owner &&
            InputBindingToken.PlayerSlotId == PlayerSlotId &&
            InputBindingToken.ActorProfileId == ActorProfileId &&
            InputBindingToken.ActorId == ActorId &&
            InputBindingToken.PreparationToken == PreparationToken &&
            InputBindingToken.OccupancyToken == OccupancyToken &&
            InputBindingToken.RuntimeContentIdentity == RuntimeContentIdentity &&
            InputBindingToken.MaterializationRevision == MaterializationRevision &&
            InputBindingToken.OccupancyRevision == OccupancyRevision &&
            InputBindingToken.BindingRevision == InputBindingRevision;

        public string StableText => IsValid
            ? $"player-gameplay-camera:{SessionContextId}:" +
              $"{Owner.Scope}:{Owner.OwnerIdentity.Value.Value}:" +
              $"{PlayerSlotId.Value.Value}:{ActorId.Value.Value}:" +
              $"{MaterializationRevision}:{OccupancyRevision}:" +
              $"{InputBindingRevision}:{EligibilityRevision}"
            : string.Empty;

        public bool Equals(PlayerGameplayCameraEligibilityToken other)
        {
            return string.Equals(
                    SessionContextId,
                    other.SessionContextId,
                    StringComparison.Ordinal) &&
                Owner == other.Owner &&
                PlayerSlotId == other.PlayerSlotId &&
                ActorProfileId == other.ActorProfileId &&
                ActorId == other.ActorId &&
                PreparationToken == other.PreparationToken &&
                OccupancyToken == other.OccupancyToken &&
                InputBindingToken == other.InputBindingToken &&
                RuntimeContentIdentity == other.RuntimeContentIdentity &&
                MaterializationRevision == other.MaterializationRevision &&
                OccupancyRevision == other.OccupancyRevision &&
                InputBindingRevision == other.InputBindingRevision &&
                EligibilityRevision == other.EligibilityRevision;
        }

        public override bool Equals(object obj) =>
            obj is PlayerGameplayCameraEligibilityToken other &&
            Equals(other);

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
                hash = hash * 397 ^ PreparationToken.GetHashCode();
                hash = hash * 397 ^ OccupancyToken.GetHashCode();
                hash = hash * 397 ^ InputBindingToken.GetHashCode();
                hash = hash * 397 ^ RuntimeContentIdentity.GetHashCode();
                hash = hash * 397 ^ MaterializationRevision;
                hash = hash * 397 ^ OccupancyRevision;
                hash = hash * 397 ^ InputBindingRevision;
                hash = hash * 397 ^ EligibilityRevision;
                return hash;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            PlayerGameplayCameraEligibilityToken left,
            PlayerGameplayCameraEligibilityToken right) =>
            left.Equals(right);

        public static bool operator !=(
            PlayerGameplayCameraEligibilityToken left,
            PlayerGameplayCameraEligibilityToken right) =>
            !left.Equals(right);
    }
}
