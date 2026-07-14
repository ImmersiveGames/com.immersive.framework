using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Functional token for one current prepared-Actor gameplay input binding.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.3 current typed gameplay input binding token.")]
    public readonly struct PlayerGameplayInputBindingToken :
        IEquatable<PlayerGameplayInputBindingToken>
    {
        internal PlayerGameplayInputBindingToken(
            string sessionContextId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            PlayerActorPreparationToken preparationToken,
            PlayerGameplayOccupancyToken occupancyToken,
            RuntimeContentIdentity runtimeContentIdentity,
            int materializationRevision,
            int occupancyRevision,
            int bindingRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Owner = owner;
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            PreparationToken = preparationToken;
            OccupancyToken = occupancyToken;
            RuntimeContentIdentity = runtimeContentIdentity;
            MaterializationRevision = materializationRevision;
            OccupancyRevision = occupancyRevision;
            BindingRevision = bindingRevision;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner Owner { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public PlayerGameplayOccupancyToken OccupancyToken { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int MaterializationRevision { get; }
        public int OccupancyRevision { get; }
        public int BindingRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Owner.IsValid &&
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            ActorId.IsValid &&
            PreparationToken.IsValid &&
            OccupancyToken.IsValid &&
            RuntimeContentIdentity.IsValid &&
            RuntimeContentIdentity.Owner == Owner &&
            MaterializationRevision > 0 &&
            OccupancyRevision > 0 &&
            BindingRevision > 0 &&
            string.Equals(PreparationToken.SessionContextId, SessionContextId, StringComparison.Ordinal) &&
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
            OccupancyToken.OccupancyRevision == OccupancyRevision;

        public string StableText => IsValid
            ? $"player-gameplay-input:{SessionContextId}:" +
              $"{Owner.Scope}:{Owner.OwnerIdentity.Value.Value}:" +
              $"{PlayerSlotId.Value.Value}:{ActorId.Value.Value}:" +
              $"{MaterializationRevision}:{OccupancyRevision}:{BindingRevision}"
            : string.Empty;

        public bool Equals(PlayerGameplayInputBindingToken other)
        {
            return string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
                Owner == other.Owner &&
                PlayerSlotId == other.PlayerSlotId &&
                ActorProfileId == other.ActorProfileId &&
                ActorId == other.ActorId &&
                PreparationToken == other.PreparationToken &&
                OccupancyToken == other.OccupancyToken &&
                RuntimeContentIdentity == other.RuntimeContentIdentity &&
                MaterializationRevision == other.MaterializationRevision &&
                OccupancyRevision == other.OccupancyRevision &&
                BindingRevision == other.BindingRevision;
        }

        public override bool Equals(object obj) =>
            obj is PlayerGameplayInputBindingToken other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(SessionContextId ?? string.Empty);
                hash = hash * 397 ^ Owner.GetHashCode();
                hash = hash * 397 ^ PlayerSlotId.GetHashCode();
                hash = hash * 397 ^ ActorProfileId.GetHashCode();
                hash = hash * 397 ^ ActorId.GetHashCode();
                hash = hash * 397 ^ PreparationToken.GetHashCode();
                hash = hash * 397 ^ OccupancyToken.GetHashCode();
                hash = hash * 397 ^ RuntimeContentIdentity.GetHashCode();
                hash = hash * 397 ^ MaterializationRevision;
                hash = hash * 397 ^ OccupancyRevision;
                hash = hash * 397 ^ BindingRevision;
                return hash;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            PlayerGameplayInputBindingToken left,
            PlayerGameplayInputBindingToken right) => left.Equals(right);

        public static bool operator !=(
            PlayerGameplayInputBindingToken left,
            PlayerGameplayInputBindingToken right) => !left.Equals(right);
    }
}
