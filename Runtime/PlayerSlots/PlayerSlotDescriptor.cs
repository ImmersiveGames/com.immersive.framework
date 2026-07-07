using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Passive description of a PlayerSlot declaration known to validation.
    /// It does not own input behavior, join policy, view channels, actor lifetime or occupancy changes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot passive descriptor.")]
    public readonly struct PlayerSlotDescriptor : IEquatable<PlayerSlotDescriptor>
    {
        public PlayerSlotDescriptor(
            PlayerSlotId playerSlotId,
            bool hasPlayerInputEvidence,
            string displayName,
            string sceneName,
            string objectName,
            string source,
            string reason)
        {
            if (!playerSlotId.IsValid)
            {
                throw new ArgumentException("PlayerSlot descriptor requires a valid player slot id.", nameof(playerSlotId));
            }

            PlayerSlotId = playerSlotId;
            HasPlayerInputEvidence = hasPlayerInputEvidence;
            DisplayName = displayName.NormalizeTextOrFallback(playerSlotId.StableText);
            SceneName = sceneName.NormalizeText();
            ObjectName = objectName.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerSlotDescriptor));
            Reason = reason.NormalizeText();
        }

        public PlayerSlotId PlayerSlotId { get; }

        public bool HasPlayerInputEvidence { get; }

        public string DisplayName { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool RequiresPlayerInput => false;

        public bool OwnsInputBehavior => false;

        public bool SpawnsActor => false;

        public bool ChangesOccupancy => false;

        public bool Equals(PlayerSlotDescriptor other)
        {
            return PlayerSlotId.Equals(other.PlayerSlotId)
                && HasPlayerInputEvidence == other.HasPlayerInputEvidence
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
                && string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSlotDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerSlotId.GetHashCode();
                hash = (hash * 397) ^ HasPlayerInputEvidence.GetHashCode();
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
            return PlayerSlotId.StableText;
        }
    }
}
