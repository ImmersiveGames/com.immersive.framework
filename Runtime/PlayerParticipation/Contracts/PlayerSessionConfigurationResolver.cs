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

            var effectiveSlots =
                new EffectivePlayerSlotProvisioning[supportedSlots.Count];
            for (int index = 0; index < supportedSlots.Count; index++)
            {
                PlayerSlotProfile playerSlotProfile = supportedSlots[index];
                PlayerSlotId playerSlotId = playerSlotProfile.PlayerSlotId;
                PlayerHostProvisioningMode hostProvisioningMode =
                    playerSessionProfile.HostProvisioning;

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
                    playerSessionProfile.InitialJoiningOpen,
                    playerSessionProfile.HostProvisioning,
                    playerSessionProfile.ActorResolutionPolicy);
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
