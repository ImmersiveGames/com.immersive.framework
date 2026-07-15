using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Compact functional identity for one current gameplay admission transaction.
    /// Exact prerequisite tokens remain in the admission summary and internal record;
    /// they are not recursively embedded in this token.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3K.5 compact current gameplay admission token.")]
    public readonly struct PlayerGameplayAdmissionToken :
        IEquatable<PlayerGameplayAdmissionToken>
    {
        internal PlayerGameplayAdmissionToken(
            string sessionContextId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentIdentity runtimeContentIdentity,
            int materializationRevision,
            int occupancyRevision,
            int inputBindingRevision,
            int cameraEligibilityRevision,
            int admissionRevision)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Owner = owner;
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            RuntimeContentIdentity = runtimeContentIdentity;
            MaterializationRevision = materializationRevision;
            OccupancyRevision = occupancyRevision;
            InputBindingRevision = inputBindingRevision;
            CameraEligibilityRevision = cameraEligibilityRevision;
            AdmissionRevision = admissionRevision;
        }

        public string SessionContextId { get; }
        public RuntimeContentOwner Owner { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int MaterializationRevision { get; }
        public int OccupancyRevision { get; }
        public int InputBindingRevision { get; }
        public int CameraEligibilityRevision { get; }
        public int AdmissionRevision { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Owner.IsValid &&
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            ActorId.IsValid &&
            RuntimeContentIdentity.IsValid &&
            RuntimeContentIdentity.Owner == Owner &&
            MaterializationRevision > 0 &&
            OccupancyRevision > 0 &&
            InputBindingRevision > 0 &&
            CameraEligibilityRevision > 0 &&
            AdmissionRevision > 0;

        public string StableText => IsValid
            ? $"player-gameplay-admission:{SessionContextId}:" +
              $"{Owner.Scope}:{Owner.OwnerIdentity.Value.Value}:" +
              $"{PlayerSlotId.Value.Value}:{ActorId.Value.Value}:" +
              $"{MaterializationRevision}:{OccupancyRevision}:" +
              $"{InputBindingRevision}:{CameraEligibilityRevision}:" +
              $"{AdmissionRevision}"
            : string.Empty;

        public bool Equals(PlayerGameplayAdmissionToken other)
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
                MaterializationRevision == other.MaterializationRevision &&
                OccupancyRevision == other.OccupancyRevision &&
                InputBindingRevision == other.InputBindingRevision &&
                CameraEligibilityRevision == other.CameraEligibilityRevision &&
                AdmissionRevision == other.AdmissionRevision;
        }

        public override bool Equals(object obj) =>
            obj is PlayerGameplayAdmissionToken other && Equals(other);

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
                hash = hash * 397 ^ MaterializationRevision;
                hash = hash * 397 ^ OccupancyRevision;
                hash = hash * 397 ^ InputBindingRevision;
                hash = hash * 397 ^ CameraEligibilityRevision;
                hash = hash * 397 ^ AdmissionRevision;
                return hash;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            PlayerGameplayAdmissionToken left,
            PlayerGameplayAdmissionToken right) => left.Equals(right);

        public static bool operator !=(
            PlayerGameplayAdmissionToken left,
            PlayerGameplayAdmissionToken right) => !left.Equals(right);
    }
}
