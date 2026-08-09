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
            bool initialJoiningOpen,
            PlayerHostProvisioningMode hostProvisioning,
            PlayerActorResolutionPolicy actorResolutionPolicy)
        {
            hostProvisioning.ThrowIfInvalid(nameof(hostProvisioning));
            actorResolutionPolicy.ThrowIfInvalid(
                nameof(actorResolutionPolicy));

            EffectivePlayerSlotProvisioning[] copiedSlots =
                FrameworkCollectionCopy.ToArrayOrEmpty(orderedSlots);
            ValidateSlots(copiedSlots, hostProvisioning);

            slots = Array.AsReadOnly(copiedSlots);
            InitialJoiningOpen = initialJoiningOpen;
            HostProvisioning = hostProvisioning;
            ActorResolutionPolicy = actorResolutionPolicy;
        }

        /// <summary>
        /// Resolved Slot provisioning in canonical allocation order.
        /// </summary>
        public IReadOnlyList<EffectivePlayerSlotProvisioning> Slots => slots;

        public bool InitialJoiningOpen { get; }

        /// <summary>
        /// Host provisioning selected for the whole initial Session. Each
        /// resolved Slot carries the same value as execution evidence; it is
        /// not an independent per-Slot authoring choice.
        /// </summary>
        public PlayerHostProvisioningMode HostProvisioning { get; }

        public PlayerActorResolutionPolicy ActorResolutionPolicy { get; }

        public int SupportedSlotCount => slots.Count;

        private static void ValidateSlots(
            IReadOnlyList<EffectivePlayerSlotProvisioning> orderedSlots,
            PlayerHostProvisioningMode hostProvisioning)
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

                if (slot.HostProvisioningMode != hostProvisioning)
                {
                    throw new ArgumentException(
                        $"Player Slot '{slot.PlayerSlotId.StableText}' does not match the Session Host Provisioning '{hostProvisioning}'.",
                        nameof(orderedSlots));
                }
            }
        }
    }
}
