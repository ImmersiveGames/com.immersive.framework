using System;

namespace Immersive.Framework.PlayerParticipation
{
    [Obsolete(
        "Use IPlayerSessionScopedAccess and request ILocalPlayerJoinAccess only when Manager-Provisioned join is required.")]
    public interface ILocalPlayerProvisioningConsumerAccess :
        IPlayerSessionScopedAccess,
        ILocalPlayerJoinAccess
    {
        /// <summary>
        /// Obsolete Manager-Provisioned observation shape preserved for
        /// existing consumers while they migrate to the provider-neutral P2.
        /// </summary>
        bool TryGetObservation(
            out LocalPlayerProvisioningConsumerObservationSnapshot observation);
    }
}
