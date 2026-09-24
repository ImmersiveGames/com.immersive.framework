using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Functional token for one current effective Player Slot to prepared Actor occupancy.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.2 current effective Player gameplay occupancy token.")]
    public readonly struct PlayerGameplayOccupancyToken :
        IEquatable<PlayerGameplayOccupancyToken>
    {
        internal PlayerGameplayOccupancyToken(
            string sessionContextId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            PlayerActorPreparationToken preparationToken,
            RuntimeContentIdentity runtimeContentIdentity,
            int materializationRevision,
            int occupancyRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Owner = owner;
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            PreparationToken = preparationToken;
            RuntimeContentIdentity = runtimeContentIdentity;
            MaterializationRevision = materializationRevision;
            OccupancyRevision = occupancyRevision;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner Owner { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int MaterializationRevision { get; }
        public int OccupancyRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Owner.IsValid &&
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            ActorId.IsValid &&
            PreparationToken.IsValid &&
            RuntimeContentIdentity.IsValid &&
            RuntimeContentIdentity.Owner == Owner &&
            MaterializationRevision > 0 &&
            OccupancyRevision > 0 &&
            string.Equals(
                PreparationToken.SessionContextId,
                SessionContextId,
                StringComparison.Ordinal) &&
            PreparationToken.PlayerSlotId == PlayerSlotId &&
            PreparationToken.ActorId == ActorId &&
            PreparationToken.RuntimeContentIdentity == RuntimeContentIdentity &&
            PreparationToken.MaterializationRevision == MaterializationRevision;

        public string StableText => IsValid
            ? $"player-gameplay-occupancy:{SessionContextId}:" +
              $"{Owner.Scope}:{Owner.OwnerIdentity.Value.Value}:" +
              $"{PlayerSlotId.Value.Value}:{ActorId.Value.Value}:" +
              $"{MaterializationRevision}:{OccupancyRevision}"
            : string.Empty;

        public bool Equals(PlayerGameplayOccupancyToken other)
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
                RuntimeContentIdentity == other.RuntimeContentIdentity &&
                MaterializationRevision == other.MaterializationRevision &&
                OccupancyRevision == other.OccupancyRevision;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerGameplayOccupancyToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode =
                    StringComparer.Ordinal.GetHashCode(SessionContextId ?? string.Empty);
                hashCode = hashCode * 397 ^ Owner.GetHashCode();
                hashCode = hashCode * 397 ^ PlayerSlotId.GetHashCode();
                hashCode = hashCode * 397 ^ ActorProfileId.GetHashCode();
                hashCode = hashCode * 397 ^ ActorId.GetHashCode();
                hashCode = hashCode * 397 ^ PreparationToken.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeContentIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ MaterializationRevision;
                hashCode = hashCode * 397 ^ OccupancyRevision;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static bool operator ==(
            PlayerGameplayOccupancyToken left,
            PlayerGameplayOccupancyToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            PlayerGameplayOccupancyToken left,
            PlayerGameplayOccupancyToken right)
        {
            return !left.Equals(right);
        }
    }
}
