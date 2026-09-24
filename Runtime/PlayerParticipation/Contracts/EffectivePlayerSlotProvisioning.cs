using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable resolved provisioning for one configured Player Slot. Slot
    /// identity and default Actor intent are captured at resolution time, so
    /// later edits to the authored Slot Profile cannot rewrite this value.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR-016 effective per-Slot Player provisioning contract.")]
    public readonly struct EffectivePlayerSlotProvisioning
    {
        public EffectivePlayerSlotProvisioning(
            PlayerSlotProfile playerSlotProfile,
            PlayerHostProvisioningMode hostProvisioningMode)
        {
            if (playerSlotProfile == null)
            {
                throw new ArgumentNullException(nameof(playerSlotProfile));
            }

            if (!playerSlotProfile.TryGetPlayerSlotId(
                    out PlayerSlotId playerSlotId,
                    out string issue))
            {
                throw new ArgumentException(
                    issue,
                    nameof(playerSlotProfile));
            }

            hostProvisioningMode.ThrowIfInvalid(
                nameof(hostProvisioningMode));

            PlayerSlotProfile = playerSlotProfile;
            PlayerSlotId = playerSlotId;
            DefaultActorProfile = playerSlotProfile.DefaultActorProfile;
            HostProvisioningMode = hostProvisioningMode;
        }

        /// <summary>
        /// Reusable authored Slot definition required by existing Session
        /// participation runtime initialization. It is not mutable runtime
        /// authority.
        /// </summary>
        public PlayerSlotProfile PlayerSlotProfile { get; }

        /// <summary>
        /// Canonical Slot identity captured from PlayerSlotProfile.
        /// </summary>
        public PlayerSlotId PlayerSlotId { get; }

        /// <summary>
        /// Default Actor intent captured from PlayerSlotProfile. Null remains
        /// an explicit unresolved default and is not replaced by a fallback.
        /// </summary>
        public ActorProfile DefaultActorProfile { get; }

        public PlayerHostProvisioningMode HostProvisioningMode { get; }

        public bool HasDefaultActorProfile => DefaultActorProfile != null;

        public bool IsValid =>
            PlayerSlotProfile != null &&
            PlayerSlotId.IsValid &&
            HostProvisioningMode.IsDefinedMode();
    }
}
