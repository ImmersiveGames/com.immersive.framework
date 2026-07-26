using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical correlation for one prepared Logical Player Actor.
    /// Assignment and Host identities are evidence only; their authorities remain external.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-3 current Logical Player Actor correlation evidence.")]
    public readonly struct PlayerActorCorrelationEvidence
    {
        internal PlayerActorCorrelationEvidence(
            PlayerSlotAssignmentOrigin assignmentOrigin,
            PlayerSlotAssignmentToken assignmentToken,
            PlayerHostBindingIdentity hostBindingIdentity,
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
            AssignmentOrigin = assignmentOrigin;
            AssignmentToken = assignmentToken;
            HostBindingIdentity = hostBindingIdentity;
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

        public PlayerSlotId PlayerSlotId => AssignmentToken.PlayerSlotId;
        public PlayerSlotAssignmentOrigin AssignmentOrigin { get; }
        public PlayerSlotAssignmentToken AssignmentToken { get; }
        public PlayerHostBindingIdentity HostBindingIdentity { get; }
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
                    AssignmentToken.SessionContextId,
                    PlayerSlotId,
                    AssignmentToken,
                    HostBindingIdentity,
                    ActorProfileId,
                    SelectionRevision,
                    ActorId,
                    RuntimeContentIdentity,
                    MaterializationRevision,
                    CorrelationRevision)
                : default;

        public bool IsValid =>
            (AssignmentOrigin is
                PlayerSlotAssignmentOrigin.ManagerProvisioned or
                PlayerSlotAssignmentOrigin.SceneProvided) &&
            AssignmentToken.IsValid &&
            HostBindingIdentity.IsValid &&
            AssignmentToken.HostBindingIdentity == HostBindingIdentity &&
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
                $"slot='{PlayerSlotId.StableText}' origin='{AssignmentOrigin}' " +
                $"assignment='{AssignmentToken.StableText}' binding='{HostBindingIdentity.StableText}' " +
                $"actorProfile='{ActorProfileId.StableText}' selectionRevision='{SelectionRevision}' " +
                $"actor='{ActorId.StableText}' runtimeContent='{RuntimeContentIdentity.StableText}' " +
                $"materializationRevision='{MaterializationRevision}' " +
                $"owner='{Owner.StableText}' physicalOwnership='{PhysicalOwnership}' " +
                $"correlationRevision='{CorrelationRevision}' preparation='{PreparationToken.StableText}' " +
                $"source='{Source}' reason='{Reason}'";
        }
    }
}
