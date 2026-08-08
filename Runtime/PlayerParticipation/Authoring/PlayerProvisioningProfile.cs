using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Reusable authored initial provisioning intent for a Player Session.
    /// This asset does not create Hosts, join Players, select Actors or hold
    /// mutable Session runtime state.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerProvisioningProfile",
        menuName = "Immersive Framework/Player/Player Provisioning Profile",
        order = 20)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 reusable authored Player Session provisioning intent.")]
    public sealed class PlayerProvisioningProfile : ScriptableObject
    {
        [Header("Host Provisioning")]
        [SerializeField]
        [Tooltip("Host provisioning used by every supported Slot without an explicit Slot override.")]
        private PlayerHostProvisioningMode defaultHostProvisioning =
            PlayerHostProvisioningMode.ManagerProvisioned;

        [SerializeField]
        [Tooltip("Explicit Host provisioning overrides by Player Slot Profile. An override never acts as a fallback.")]
        private PlayerSlotProvisioningOverride[] slotOverrides =
            Array.Empty<PlayerSlotProvisioningOverride>();

        [Header("Actor Resolution")]
        [SerializeField]
        [Tooltip("Initial Actor resolution intent. Actor lifecycle remains outside this Profile.")]
        private PlayerActorResolutionPolicy actorResolutionPolicy =
            PlayerActorResolutionPolicy.ResolveConfiguredDefault;

        public PlayerHostProvisioningMode DefaultHostProvisioning =>
            defaultHostProvisioning;

        /// <summary>
        /// Explicit Slot overrides in authored order. The Profile does not
        /// resolve these against a Session Slot universe.
        /// </summary>
        public IReadOnlyList<PlayerSlotProvisioningOverride> SlotOverrides =>
            Array.AsReadOnly(
                slotOverrides ?? Array.Empty<PlayerSlotProvisioningOverride>());

        public int SlotOverrideCount =>
            slotOverrides != null ? slotOverrides.Length : 0;

        public PlayerActorResolutionPolicy ActorResolutionPolicy =>
            actorResolutionPolicy;

        public bool IsValid => TryValidate(out _);

        /// <summary>
        /// Validates only contradictions visible inside this authored asset.
        /// Membership in a Session's Supported Slots is resolved later.
        /// </summary>
        public bool TryValidate(out string issue)
        {
            if (!defaultHostProvisioning.IsDefinedMode())
            {
                issue =
                    $"Player Provisioning Profile '{name}' has invalid Default Host Provisioning '{defaultHostProvisioning}'.";
                return false;
            }

            if (!actorResolutionPolicy.IsDefinedPolicy())
            {
                issue =
                    $"Player Provisioning Profile '{name}' has invalid Actor Resolution Policy '{actorResolutionPolicy}'.";
                return false;
            }

            PlayerSlotProvisioningOverride[] overrides =
                slotOverrides ?? Array.Empty<PlayerSlotProvisioningOverride>();
            var overriddenSlotIds = new HashSet<PlayerSlotId>();

            for (int index = 0; index < overrides.Length; index++)
            {
                PlayerSlotProvisioningOverride slotOverride = overrides[index];
                if (slotOverride == null)
                {
                    issue =
                        $"Player Provisioning Profile '{name}' has a null Slot override at index '{index}'.";
                    return false;
                }

                PlayerSlotProfile playerSlotProfile =
                    slotOverride.PlayerSlotProfile;
                if (playerSlotProfile == null)
                {
                    issue =
                        $"Player Provisioning Profile '{name}' requires a Player Slot Profile for override index '{index}'.";
                    return false;
                }

                if (!playerSlotProfile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string playerSlotIssue))
                {
                    issue =
                        $"Player Provisioning Profile '{name}' has an invalid Slot override at index '{index}'. {playerSlotIssue}";
                    return false;
                }

                if (!slotOverride.HostProvisioningMode.IsDefinedMode())
                {
                    issue =
                        $"Player Provisioning Profile '{name}' has invalid Host Provisioning '{slotOverride.HostProvisioningMode}' for Slot '{playerSlotId.StableText}'.";
                    return false;
                }

                if (!overriddenSlotIds.Add(playerSlotId))
                {
                    issue =
                        $"Player Provisioning Profile '{name}' overrides Player Slot '{playerSlotId.StableText}' more than once.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }
    }
}
