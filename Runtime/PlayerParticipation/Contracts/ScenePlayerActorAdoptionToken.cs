using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable authority token for one externally owned Scene Logical Player Actor adoption.
    /// It identifies runtime preparation only and never transfers physical ownership to the framework.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3M4B2B foreign/stale-safe Scene Logical Player Actor adoption token.")]
    public readonly struct ScenePlayerActorAdoptionToken : IEquatable<ScenePlayerActorAdoptionToken>
    {
        internal ScenePlayerActorAdoptionToken(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            RuntimeContentIdentity runtimeContentIdentity,
            PlayerActorPreparationToken preparationToken,
            int adoptionRevision)
        {
            SessionContextId = sessionContextId ?? string.Empty;
            PlayerSlotId = playerSlotId;
            ActorId = actorId;
            RuntimeContentIdentity = runtimeContentIdentity;
            PreparationToken = preparationToken;
            AdoptionRevision = adoptionRevision;
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public int AdoptionRevision { get; }
        public PlayerActorPhysicalOwnership PhysicalOwnership =>
            PlayerActorPhysicalOwnership.ExternalSceneOwned;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            ActorId.IsValid &&
            RuntimeContentIdentity.IsValid &&
            PreparationToken.IsValid &&
            AdoptionRevision > 0;

        public string StableText => IsValid
            ? $"scene-player-actor-adoption:{SessionContextId}:{PlayerSlotId.StableText}:{ActorId.StableText}:{AdoptionRevision}"
            : string.Empty;

        public bool Equals(ScenePlayerActorAdoptionToken other)
        {
            return string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
                PlayerSlotId == other.PlayerSlotId &&
                ActorId == other.ActorId &&
                RuntimeContentIdentity == other.RuntimeContentIdentity &&
                PreparationToken == other.PreparationToken &&
                AdoptionRevision == other.AdoptionRevision;
        }

        public override bool Equals(object obj)
        {
            return obj is ScenePlayerActorAdoptionToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(SessionContextId ?? string.Empty);
                hashCode = hashCode * 397 ^ PlayerSlotId.GetHashCode();
                hashCode = hashCode * 397 ^ ActorId.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeContentIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ PreparationToken.GetHashCode();
                hashCode = hashCode * 397 ^ AdoptionRevision;
                return hashCode;
            }
        }

        public static bool operator ==(
            ScenePlayerActorAdoptionToken left,
            ScenePlayerActorAdoptionToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            ScenePlayerActorAdoptionToken left,
            ScenePlayerActorAdoptionToken right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return StableText;
        }
    }
}
