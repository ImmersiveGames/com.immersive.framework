using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable, ordered initial Player Session configuration. It captures
    /// resolution evidence only and grants no runtime mutation authority.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 resolved immutable Player Session initial configuration.")]
    public sealed class EffectivePlayerSessionConfiguration
    {
        private readonly IReadOnlyList<EffectivePlayerSlotProvisioning> slots;

        public EffectivePlayerSessionConfiguration(
            IReadOnlyList<EffectivePlayerSlotProvisioning> orderedSlots,
            int initialCapacity,
            bool initialJoiningOpen,
            PlayerActorResolutionPolicy actorResolutionPolicy)
        {
            actorResolutionPolicy.ThrowIfInvalid(
                nameof(actorResolutionPolicy));

            EffectivePlayerSlotProvisioning[] copiedSlots =
                FrameworkCollectionCopy.ToArrayOrEmpty(orderedSlots);
            ValidateSlots(copiedSlots);

            if (initialCapacity < 0 || initialCapacity > copiedSlots.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialCapacity),
                    initialCapacity,
                    $"Initial capacity must be between 0 and configured Slot count '{copiedSlots.Length}'.");
            }

            slots = Array.AsReadOnly(copiedSlots);
            InitialCapacity = initialCapacity;
            InitialJoiningOpen = initialJoiningOpen;
            ActorResolutionPolicy = actorResolutionPolicy;
        }

        /// <summary>
        /// Resolved Slot provisioning in canonical allocation order.
        /// </summary>
        public IReadOnlyList<EffectivePlayerSlotProvisioning> Slots => slots;

        public int InitialCapacity { get; }

        public bool InitialJoiningOpen { get; }

        public PlayerActorResolutionPolicy ActorResolutionPolicy { get; }

        public int SupportedSlotCount => slots.Count;

        private static void ValidateSlots(
            IReadOnlyList<EffectivePlayerSlotProvisioning> orderedSlots)
        {
            var configuredSlotIds = new HashSet<PlayerSlotId>();
            for (int index = 0; index < orderedSlots.Count; index++)
            {
                EffectivePlayerSlotProvisioning slot = orderedSlots[index];
                if (!slot.IsValid)
                {
                    throw new ArgumentException(
                        $"Effective Player Slot provisioning at index '{index}' is invalid.",
                        nameof(orderedSlots));
                }

                if (!configuredSlotIds.Add(slot.PlayerSlotId))
                {
                    throw new ArgumentException(
                        $"Player Slot '{slot.PlayerSlotId.StableText}' is configured more than once.",
                        nameof(orderedSlots));
                }
            }
        }
    }
}
