using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Passive description of an authored PlayerSlot to Actor relation.
    /// It does not own possession, actor replacement, input routing, camera routing or runtime registration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot occupancy passive descriptor.")]
    public readonly struct PlayerSlotOccupancyDescriptor : IEquatable<PlayerSlotOccupancyDescriptor>
    {
        public PlayerSlotOccupancyDescriptor(
            PlayerSlotId playerSlotId,
            ActorId occupiedActorId,
            bool hasActorDeclarationEvidence,
            string displayName,
            string sceneName,
            string objectName,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerSlot occupancy descriptor requires a valid player slot id.", nameof(playerSlotId));
            }

            if (!occupiedActorId.IsValid)
            {
                throw new ArgumentException("PlayerSlot occupancy descriptor requires a valid occupied actor id.", nameof(occupiedActorId));
            }

            PlayerSlotId = playerSlotId;
            OccupiedActorId = occupiedActorId;
            HasActorDeclarationEvidence = hasActorDeclarationEvidence;
            DisplayName = displayName.NormalizeTextOrFallback($"{playerSlotId.StableText}->{occupiedActorId.StableText}");
            SceneName = sceneName.NormalizeText();
            ObjectName = objectName.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerSlotOccupancyDescriptor));
            Reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public ActorId OccupiedActorId { get; }

        public bool HasActorDeclarationEvidence { get; }

        public string DisplayName { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool ChangesOccupancy => false;

        public bool SpawnsActor => false;

        public bool ResolvesCapability => false;

        public bool Equals(PlayerSlotOccupancyDescriptor other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && OccupiedActorId.Equals(other.OccupiedActorId)
                && HasActorDeclarationEvidence == other.HasActorDeclarationEvidence
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
                && string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSlotOccupancyDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ OccupiedActorId.GetHashCode();
                hash = (hash * 397) ^ HasActorDeclarationEvidence.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SceneName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ObjectName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{PlayerSlotId.StableText}->{OccupiedActorId.StableText}";
        }
    }
}
