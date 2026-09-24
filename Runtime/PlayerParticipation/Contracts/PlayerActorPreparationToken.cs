using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable Session-physical token for one prepared Logical Player Actor.
    /// Contextual assignment and binding identities deliberately do not participate in this token.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.4 typed current Logical Player Actor preparation token.")]
    public readonly struct PlayerActorPreparationToken : IEquatable<PlayerActorPreparationToken>
    {
        internal PlayerActorPreparationToken(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            int selectionRevision,
            ActorId actorId,
            RuntimeContentIdentity runtimeContentIdentity,
            int materializationRevision,
            int correlationRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            SelectionRevision = selectionRevision;
            ActorId = actorId;
            RuntimeContentIdentity = runtimeContentIdentity;
            MaterializationRevision = materializationRevision;
            CorrelationRevision = correlationRevision;
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfileId ActorProfileId { get; }
        public int SelectionRevision { get; }
        public ActorId ActorId { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int MaterializationRevision { get; }
        public int CorrelationRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            SelectionRevision > 0 &&
            ActorId.IsValid &&
            RuntimeContentIdentity.IsValid &&
            MaterializationRevision > 0 &&
            CorrelationRevision > 0;

        public string StableText => IsValid
            ? $"player-actor-preparation:{SessionContextId}:{PlayerSlotId.Value.Value}:" +
              $"{ActorId.Value.Value}:{CorrelationRevision}"
            : string.Empty;

        public bool Equals(PlayerActorPreparationToken other)
        {
            return string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
                PlayerSlotId == other.PlayerSlotId &&
                ActorProfileId == other.ActorProfileId &&
                SelectionRevision == other.SelectionRevision &&
                ActorId == other.ActorId &&
                RuntimeContentIdentity == other.RuntimeContentIdentity &&
                MaterializationRevision == other.MaterializationRevision &&
                CorrelationRevision == other.CorrelationRevision;
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
                hashCode = hashCode * 397 ^ ActorProfileId.GetHashCode();
                hashCode = hashCode * 397 ^ SelectionRevision;
                hashCode = hashCode * 397 ^ ActorId.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeContentIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ MaterializationRevision;
                hashCode = hashCode * 397 ^ CorrelationRevision;
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
