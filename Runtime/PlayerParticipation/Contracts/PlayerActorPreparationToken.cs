using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable functional token for one current prepared Logical Player Actor.
    /// It is returned by Session preparation summaries and used to reject foreign or stale operations.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.4 typed current Logical Player Actor preparation token.")]
    public readonly struct PlayerActorPreparationToken : IEquatable<PlayerActorPreparationToken>
    {
        internal PlayerActorPreparationToken(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            RuntimeContentIdentity runtimeContentIdentity,
            int materializationRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            ActorId = actorId;
            RuntimeContentIdentity = runtimeContentIdentity;
            MaterializationRevision = materializationRevision;
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int MaterializationRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            ActorId.IsValid &&
            RuntimeContentIdentity.IsValid &&
            MaterializationRevision > 0;

        public string StableText => IsValid
            ? $"player-actor-preparation:{SessionContextId}:{PlayerSlotId.Value.Value}:" +
              $"{ActorId.Value.Value}:{MaterializationRevision}"
            : string.Empty;

        public bool Equals(PlayerActorPreparationToken other)
        {
            return string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
                PlayerSlotId == other.PlayerSlotId &&
                ActorId == other.ActorId &&
                RuntimeContentIdentity == other.RuntimeContentIdentity &&
                MaterializationRevision == other.MaterializationRevision;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerActorPreparationToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(SessionContextId ?? string.Empty);
                hashCode = hashCode * 397 ^ PlayerSlotId.GetHashCode();
                hashCode = hashCode * 397 ^ ActorId.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeContentIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ MaterializationRevision;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static bool operator ==(
            PlayerActorPreparationToken left,
            PlayerActorPreparationToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            PlayerActorPreparationToken left,
            PlayerActorPreparationToken right)
        {
            return !left.Equals(right);
        }
    }
}
