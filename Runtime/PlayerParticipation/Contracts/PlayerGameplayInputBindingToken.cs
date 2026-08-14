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
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
            PlayerActorPreparationToken preparationToken,
            int bindingRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Owner = owner;
            PlayerSlotId = playerSlotId;
            AssignmentToken = assignmentToken;
            HostBindingIdentity = hostBindingIdentity;
            PreparationToken = preparationToken;
            BindingRevision = bindingRevision;
        }

        public string SessionContextId { get; }
        /// <summary>Activity occurrence that owns this contextual input binding.</summary>
        public RuntimeContentOwner Owner { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerSlotAssignmentToken AssignmentToken { get; }
        public PlayerHostBindingIdentity HostBindingIdentity { get; }
        public PlayerActorPreparationToken PreparationToken { get; }
        public int BindingRevision { get; }
        public ActorProfileId ActorProfileId => PreparationToken.ActorProfileId;
        public ActorId ActorId => PreparationToken.ActorId;
        public RuntimeContentIdentity RuntimeContentIdentity =>
            PreparationToken.RuntimeContentIdentity;
        public int MaterializationRevision =>
            PreparationToken.MaterializationRevision;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Owner.IsValid &&
            PlayerSlotId.IsValid &&
            AssignmentToken.IsValid &&
            HostBindingIdentity.IsValid &&
            PreparationToken.IsValid &&
            BindingRevision > 0 &&
            string.Equals(
                AssignmentToken.SessionContextId,
                SessionContextId,
                StringComparison.Ordinal) &&
            AssignmentToken.PlayerSlotId == PlayerSlotId &&
            AssignmentToken.HostBindingIdentity == HostBindingIdentity &&
            string.Equals(
                PreparationToken.SessionContextId,
                SessionContextId,
                StringComparison.Ordinal) &&
            PreparationToken.PlayerSlotId == PlayerSlotId;

        public string StableText => IsValid
            ? $"player-gameplay-input:{SessionContextId}:" +
              $"{Owner.Scope}:{Owner.OwnerIdentity.Value.Value}:" +
              $"{PlayerSlotId.Value.Value}:" +
              $"{AssignmentToken.AssignmentSequence}:" +
              $"{AssignmentToken.AssignmentRevision}:" +
              $"{HostBindingIdentity.StableText}:" +
              $"{PreparationToken.CorrelationRevision}:" +
              $"{BindingRevision}"
            : string.Empty;

        public bool Equals(PlayerGameplayInputBindingToken other)
        {
            return string.Equals(SessionContextId, other.SessionContextId, StringComparison.Ordinal) &&
                Owner == other.Owner &&
                PlayerSlotId == other.PlayerSlotId &&
                AssignmentToken == other.AssignmentToken &&
                HostBindingIdentity == other.HostBindingIdentity &&
                PreparationToken == other.PreparationToken &&
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
                hash = hash * 397 ^ AssignmentToken.GetHashCode();
                hash = hash * 397 ^ HostBindingIdentity.GetHashCode();
                hash = hash * 397 ^ PreparationToken.GetHashCode();
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
