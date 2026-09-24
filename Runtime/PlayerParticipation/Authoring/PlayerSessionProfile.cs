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
        [Tooltip("Whether Session Joining begins open. Runtime Joining changes remain outside this Profile.")]
        private bool initialJoiningOpen;

        [Header("Host Provisioning")]
        [SerializeField]
        [Tooltip("Host provisioning applied to every Supported Slot when the Session is created.")]
        private PlayerHostProvisioningMode hostProvisioning =
            PlayerHostProvisioningMode.ManagerProvisioned;

        [Header("Actor Resolution")]
        [SerializeField]
        [Tooltip("Initial Actor resolution intent. Actor lifecycle remains outside this Profile.")]
        private PlayerActorResolutionPolicy actorResolutionPolicy =
            PlayerActorResolutionPolicy.ResolveConfiguredDefault;

        /// <summary>
        /// Supported Slots in canonical allocation order. Presentation order
        /// on PlayerSlotProfile does not change this sequence.
        /// </summary>
        public IReadOnlyList<PlayerSlotProfile> SupportedSlots =>
            Array.AsReadOnly(
                supportedSlots ?? Array.Empty<PlayerSlotProfile>());

        public int SupportedSlotCount =>
            supportedSlots != null ? supportedSlots.Length : 0;

        public bool InitialJoiningOpen => initialJoiningOpen;

        public PlayerHostProvisioningMode HostProvisioning => hostProvisioning;

        public PlayerActorResolutionPolicy ActorResolutionPolicy =>
            actorResolutionPolicy;

        public bool IsValid => TryValidate(out _);

        /// <summary>
        /// Validates only authored structural contradictions.
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

            if (!hostProvisioning.IsDefinedMode())
            {
                issue =
                    $"Player Session Profile '{name}' has invalid Host Provisioning '{hostProvisioning}'.";
                return false;
            }

            if (!actorResolutionPolicy.IsDefinedPolicy())
            {
                issue =
                    $"Player Session Profile '{name}' has invalid Actor Resolution '{actorResolutionPolicy}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
