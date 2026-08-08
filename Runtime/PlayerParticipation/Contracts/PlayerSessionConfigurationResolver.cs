using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Pure resolution of authored Player Session intent into one immutable
    /// effective configuration. This resolver performs no runtime lookup or
    /// mutation and does not initialize a Player Session.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 pure Player Session initial configuration resolver.")]
    public static class PlayerSessionConfigurationResolver
    {
        public static PlayerSessionInitializationResult Resolve(
            PlayerSessionProfile playerSessionProfile)
        {
            if (playerSessionProfile == null)
            {
                return Failed(
                    PlayerSessionInitializationFailure
                        .MissingRequiredConfiguration,
                    "Player Session initialization requires a Player Session Profile.");
            }

            PlayerProvisioningProfile provisioningProfile =
                playerSessionProfile.PlayerProvisioningProfile;
            if (provisioningProfile == null)
            {
                return Failed(
                    PlayerSessionInitializationFailure
                        .MissingRequiredConfiguration,
                    $"Player Session Profile '{playerSessionProfile.name}' requires a Player Provisioning Profile.");
            }

            if (!provisioningProfile.TryValidate(
                    out string provisioningIssue))
            {
                return Failed(
                    PlayerSessionInitializationFailure
                        .InvalidPlayerProvisioningProfile,
                    $"Player Provisioning Profile '{provisioningProfile.name}' is invalid. {provisioningIssue}");
            }

            if (!playerSessionProfile.TryValidate(
                    out string sessionIssue))
            {
                return Failed(
                    PlayerSessionInitializationFailure
                        .InvalidPlayerSessionProfile,
                    $"Player Session Profile '{playerSessionProfile.name}' is invalid. {sessionIssue}");
            }

            IReadOnlyList<PlayerSlotProfile> supportedSlots =
                playerSessionProfile.SupportedSlots;
            if (!TryCollectSupportedSlotIds(
                    supportedSlots,
                    out HashSet<PlayerSlotId> supportedSlotIds,
                    out string supportedSlotsIssue))
            {
                return Failed(
                    PlayerSessionInitializationFailure
                        .InvalidPlayerSessionProfile,
                    supportedSlotsIssue);
            }

            if (!TryCollectOverrideModes(
                    provisioningProfile.SlotOverrides,
                    supportedSlotIds,
                    out Dictionary<PlayerSlotId, PlayerHostProvisioningMode>
                        overrideModes,
                    out PlayerSessionInitializationFailure overrideFailure,
                    out string overrideIssue))
            {
                return Failed(overrideFailure, overrideIssue);
            }

            var effectiveSlots =
                new EffectivePlayerSlotProvisioning[supportedSlots.Count];
            for (int index = 0; index < supportedSlots.Count; index++)
            {
                PlayerSlotProfile playerSlotProfile = supportedSlots[index];
                PlayerSlotId playerSlotId = playerSlotProfile.PlayerSlotId;
                PlayerHostProvisioningMode hostProvisioningMode =
                    overrideModes.TryGetValue(
                        playerSlotId,
                        out PlayerHostProvisioningMode overrideMode)
                        ? overrideMode
                        : provisioningProfile.DefaultHostProvisioning;

                try
                {
                    effectiveSlots[index] = new EffectivePlayerSlotProvisioning(
                        playerSlotProfile,
                        hostProvisioningMode);
                }
                catch (ArgumentException exception)
                {
                    return Failed(
                        PlayerSessionInitializationFailure
                            .InvalidEffectiveConfiguration,
                        $"Effective Player Slot configuration at index '{index}' is invalid. {exception.Message}");
                }
            }

            try
            {
                var configuration = new EffectivePlayerSessionConfiguration(
                    effectiveSlots,
                    playerSessionProfile.InitialCapacity,
                    playerSessionProfile.InitialJoiningOpen,
                    provisioningProfile.ActorResolutionPolicy);
                return PlayerSessionInitializationResult.SucceededWith(
                    configuration,
                    "Player Session initial configuration resolved.");
            }
            catch (ArgumentException exception)
            {
                return Failed(
                    PlayerSessionInitializationFailure
                        .InvalidEffectiveConfiguration,
                    $"Effective Player Session configuration is invalid. {exception.Message}");
            }
        }

        private static bool TryCollectSupportedSlotIds(
            IReadOnlyList<PlayerSlotProfile> supportedSlots,
            out HashSet<PlayerSlotId> supportedSlotIds,
            out string issue)
        {
            supportedSlotIds = new HashSet<PlayerSlotId>();
            for (int index = 0; index < supportedSlots.Count; index++)
            {
                PlayerSlotProfile playerSlotProfile = supportedSlots[index];
                if (playerSlotProfile == null)
                {
                    issue =
                        $"Player Session Profile has a null Supported Slot at index '{index}'.";
                    return false;
                }

                if (!playerSlotProfile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string playerSlotIssue))
                {
                    issue =
                        $"Player Session Profile has an invalid Supported Slot at index '{index}'. {playerSlotIssue}";
                    return false;
                }

                if (!supportedSlotIds.Add(playerSlotId))
                {
                    issue =
                        $"Player Session Profile supports Player Slot '{playerSlotId.StableText}' more than once.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool TryCollectOverrideModes(
            IReadOnlyList<PlayerSlotProvisioningOverride> slotOverrides,
            HashSet<PlayerSlotId> supportedSlotIds,
            out Dictionary<PlayerSlotId, PlayerHostProvisioningMode>
                overrideModes,
            out PlayerSessionInitializationFailure failure,
            out string issue)
        {
            overrideModes =
                new Dictionary<PlayerSlotId, PlayerHostProvisioningMode>();
            for (int index = 0; index < slotOverrides.Count; index++)
            {
                PlayerSlotProvisioningOverride slotOverride =
                    slotOverrides[index];
                if (slotOverride == null)
                {
                    failure = PlayerSessionInitializationFailure
                        .InvalidPlayerProvisioningProfile;
                    issue =
                        $"Player Provisioning Profile has a null Slot override at index '{index}'.";
                    return false;
                }

                PlayerSlotProfile playerSlotProfile =
                    slotOverride.PlayerSlotProfile;
                if (playerSlotProfile == null)
                {
                    failure = PlayerSessionInitializationFailure
                        .InvalidPlayerProvisioningProfile;
                    issue =
                        $"Player Provisioning Profile requires a Player Slot Profile for override index '{index}'.";
                    return false;
                }

                if (!playerSlotProfile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string playerSlotIssue))
                {
                    failure = PlayerSessionInitializationFailure
                        .InvalidPlayerProvisioningProfile;
                    issue =
                        $"Player Provisioning Profile has invalid Slot override at index '{index}'. {playerSlotIssue}";
                    return false;
                }

                if (!slotOverride.HostProvisioningMode.IsDefinedMode())
                {
                    failure = PlayerSessionInitializationFailure
                        .InvalidPlayerProvisioningProfile;
                    issue =
                        $"Player Provisioning Profile has invalid Host Provisioning '{slotOverride.HostProvisioningMode}' for Slot '{playerSlotId.StableText}'.";
                    return false;
                }

                if (!supportedSlotIds.Contains(playerSlotId))
                {
                    failure = PlayerSessionInitializationFailure
                        .UnsupportedProvisioningOverrideSlot;
                    issue =
                        $"Player Provisioning override for Slot '{playerSlotId.StableText}' is not supported by the resolved Player Session Profile.";
                    return false;
                }

                if (overrideModes.ContainsKey(playerSlotId))
                {
                    failure = PlayerSessionInitializationFailure
                        .InvalidPlayerProvisioningProfile;
                    issue =
                        $"Player Provisioning Profile overrides Player Slot '{playerSlotId.StableText}' more than once.";
                    return false;
                }

                overrideModes.Add(
                    playerSlotId,
                    slotOverride.HostProvisioningMode);
            }

            failure = PlayerSessionInitializationFailure.None;
            issue = string.Empty;
            return true;
        }

        private static PlayerSessionInitializationResult Failed(
            PlayerSessionInitializationFailure failure,
            string message)
        {
            return PlayerSessionInitializationResult.FailedWith(
                failure,
                message);
        }
    }
}
