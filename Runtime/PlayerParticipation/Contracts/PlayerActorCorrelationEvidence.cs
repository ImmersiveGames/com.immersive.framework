using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable Session-physical evidence for one prepared Logical Player Actor.
    /// Activity assignment, admission and binding identities remain contextual evidence elsewhere.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-3 current Logical Player Actor correlation evidence.")]
    public readonly struct PlayerActorCorrelationEvidence
    {
        internal PlayerActorCorrelationEvidence(
            string sessionContextId,
            PlayerSlotId playerSlotId,
            PlayerHostProvisioningMode provisioningOrigin,
            ActorProfileId actorProfileId,
            int selectionRevision,
            ActorId actorId,
            RuntimeContentIdentity runtimeContentIdentity,
            int materializationRevision,
            PlayerActorPhysicalOwnership physicalOwnership,
            int correlationRevision,
            string source,
            string reason)
        {
            SessionContextId = sessionContextId.NormalizeText();
            PlayerSlotId = playerSlotId;
            ProvisioningOrigin = provisioningOrigin;
            ActorProfileId = actorProfileId;
            SelectionRevision = selectionRevision;
            ActorId = actorId;
            RuntimeContentIdentity = runtimeContentIdentity;
            MaterializationRevision = materializationRevision;
            PhysicalOwnership = physicalOwnership;
            CorrelationRevision = correlationRevision;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public string SessionContextId { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public PlayerHostProvisioningMode ProvisioningOrigin { get; }
        public ActorProfileId ActorProfileId { get; }
        public int SelectionRevision { get; }
        public ActorId ActorId { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public int MaterializationRevision { get; }
        public RuntimeContentOwner Owner => RuntimeContentIdentity.Owner;
        public PlayerActorPhysicalOwnership PhysicalOwnership { get; }
        public int CorrelationRevision { get; }
        public string Source { get; }
        public string Reason { get; }

        public PlayerActorPreparationToken PreparationToken =>
            IsValid
                ? new PlayerActorPreparationToken(
                    SessionContextId,
                    PlayerSlotId,
                    ActorProfileId,
                    SelectionRevision,
                    ActorId,
                    RuntimeContentIdentity,
                    MaterializationRevision,
                    CorrelationRevision)
                : default;

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            PlayerSlotId.IsValid &&
            ProvisioningOrigin.IsDefinedMode() &&
            ActorProfileId.IsValid &&
            SelectionRevision > 0 &&
            ActorId.IsValid &&
            RuntimeContentIdentity.IsValid &&
            MaterializationRevision > 0 &&
            (PhysicalOwnership is
                PlayerActorPhysicalOwnership.FrameworkOwned or
                PlayerActorPhysicalOwnership.ExternalSceneOwned) &&
            CorrelationRevision > 0 &&
            !string.IsNullOrEmpty(Source) &&
            !string.IsNullOrEmpty(Reason);

        public string ToDiagnosticString()
        {
            return
                $"session='{SessionContextId}' slot='{PlayerSlotId.StableText}' provisioning='{ProvisioningOrigin}' " +
                $"actorProfile='{ActorProfileId.StableText}' selectionRevision='{SelectionRevision}' " +
                $"actor='{ActorId.StableText}' runtimeContent='{RuntimeContentIdentity.StableText}' " +
                $"materializationRevision='{MaterializationRevision}' " +
                $"owner='{Owner.StableText}' physicalOwnership='{PhysicalOwnership}' " +
                $"correlationRevision='{CorrelationRevision}' preparation='{PreparationToken.StableText}' " +
                $"source='{Source}' reason='{Reason}'";
        }
    }
}
