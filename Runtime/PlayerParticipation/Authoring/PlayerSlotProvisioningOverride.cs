using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit authored Host provisioning override for one Player Slot. It
    /// does not imply a fallback and does not execute provisioning.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 authored Player Slot Host provisioning override.")]
    public sealed class PlayerSlotProvisioningOverride
    {
        [SerializeField]
        [Tooltip("Exact Player Slot Profile whose default Host provisioning is overridden.")]
        private PlayerSlotProfile playerSlotProfile;

        [SerializeField]
        [Tooltip("Explicit Host provisioning for this Slot. Unspecified is invalid.")]
        private PlayerHostProvisioningMode hostProvisioningMode;

        public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;

        public PlayerHostProvisioningMode HostProvisioningMode =>
            hostProvisioningMode;
    }
}
