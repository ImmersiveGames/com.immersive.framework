using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Reusable authored initial configuration for one Player Session. This
    /// asset declares structural Slot support and initial intent only; it
    /// never holds or mutates live Session state.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerSessionProfile",
        menuName = "Immersive Framework/Player/Player Session Profile",
        order = 30)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 reusable authored Player Session initial configuration.")]
    public sealed class PlayerSessionProfile : ScriptableObject
    {
        [Header("Supported Slots")]
        [SerializeField]
        [Tooltip("Supported Player Slots in canonical allocation and Join order.")]
        private PlayerSlotProfile[] supportedSlots =
            Array.Empty<PlayerSlotProfile>();

        [Header("Initial Session State")]
        [SerializeField]
        [Tooltip("Initial Session capacity. Runtime Capacity changes remain outside this Profile.")]
        private int initialCapacity;

        [SerializeField]
        [Tooltip("Whether Session Joining begins open. Runtime Joining changes remain outside this Profile.")]
        private bool initialJoiningOpen;

        [Header("Provisioning")]
        [SerializeField]
        [Tooltip("Reusable initial Player Host provisioning and Actor resolution intent.")]
        private PlayerProvisioningProfile playerProvisioningProfile;

        /// <summary>
        /// Supported Slots in canonical allocation order. Presentation order
        /// on PlayerSlotProfile does not change this sequence.
        /// </summary>
        public IReadOnlyList<PlayerSlotProfile> SupportedSlots =>
            Array.AsReadOnly(
                supportedSlots ?? Array.Empty<PlayerSlotProfile>());

        public int SupportedSlotCount =>
            supportedSlots != null ? supportedSlots.Length : 0;

        public int InitialCapacity => initialCapacity;

        public bool InitialJoiningOpen => initialJoiningOpen;

        public PlayerProvisioningProfile PlayerProvisioningProfile =>
            playerProvisioningProfile;

        public bool IsValid => TryValidate(out _);

        /// <summary>
        /// Validates only authored structural contradictions. Resolving Slot
        /// provisioning overrides against this Session's Supported Slots is
        /// intentionally deferred to the effective configuration resolver.
        /// </summary>
        public bool TryValidate(out string issue)
        {
            PlayerSlotProfile[] slots =
                supportedSlots ?? Array.Empty<PlayerSlotProfile>();
            var supportedSlotIds = new HashSet<PlayerSlotId>();

            for (int index = 0; index < slots.Length; index++)
            {
                PlayerSlotProfile playerSlotProfile = slots[index];
                if (playerSlotProfile == null)
                {
                    issue =
                        $"Player Session Profile '{name}' has a null Supported Slot at index '{index}'.";
                    return false;
                }

                if (!playerSlotProfile.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string playerSlotIssue))
                {
                    issue =
                        $"Player Session Profile '{name}' has an invalid Supported Slot at index '{index}'. {playerSlotIssue}";
                    return false;
                }

                if (!supportedSlotIds.Add(playerSlotId))
                {
                    issue =
                        $"Player Session Profile '{name}' supports Player Slot '{playerSlotId.StableText}' more than once.";
                    return false;
                }
            }

            if (initialCapacity < 0 || initialCapacity > slots.Length)
            {
                issue =
                    $"Player Session Profile '{name}' Initial Capacity '{initialCapacity}' must be between 0 and Supported Slot count '{slots.Length}'.";
                return false;
            }

            if (playerProvisioningProfile == null)
            {
                issue =
                    $"Player Session Profile '{name}' requires a Player Provisioning Profile.";
                return false;
            }

            if (!playerProvisioningProfile.TryValidate(
                    out string provisioningIssue))
            {
                issue =
                    $"Player Session Profile '{name}' references an invalid Player Provisioning Profile. {provisioningIssue}";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
