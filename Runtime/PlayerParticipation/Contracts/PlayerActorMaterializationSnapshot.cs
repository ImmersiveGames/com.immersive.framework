using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable non-physical evidence for one contextual Logical Player Actor materialization.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.3 immutable Player Actor materialization snapshot.")]
    public readonly struct PlayerActorMaterializationSnapshot
    {
        internal PlayerActorMaterializationSnapshot(
            PlayerActorMaterializationOperationId operationId,
            RuntimeContentIdentity runtimeContentIdentity,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            int materializationRevision,
            PlayerActorMaterializationState state,
            string source,
            string reason)
        {
            OperationId = operationId;
            RuntimeContentIdentity = runtimeContentIdentity;
            PlayerSlotId = playerSlotId;
            ActorProfileId = actorProfileId;
            ActorId = actorId;
            MaterializationRevision = materializationRevision;
            State = state;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public PlayerActorMaterializationOperationId OperationId { get; }
        public RuntimeContentIdentity RuntimeContentIdentity { get; }
        public RuntimeContentOwner Owner => RuntimeContentIdentity.Owner;
        public PlayerSlotId PlayerSlotId { get; }
        public ActorProfileId ActorProfileId { get; }
        public ActorId ActorId { get; }
        public int MaterializationRevision { get; }
        public PlayerActorMaterializationState State { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            OperationId.IsValid &&
            RuntimeContentIdentity.IsValid &&
            PlayerSlotId.IsValid &&
            ActorProfileId.IsValid &&
            ActorId.IsValid &&
            MaterializationRevision > 0 &&
            State != PlayerActorMaterializationState.Unknown;

        public bool IsStaged => State == PlayerActorMaterializationState.StagedInactive;
        public bool IsActive => State == PlayerActorMaterializationState.Active;
        public bool IsReleased => State == PlayerActorMaterializationState.Released;

        public string ToDiagnosticString()
        {
            return $"operation='{OperationId.StableText}' runtimeContent='{RuntimeContentIdentity.StableText}' " +
                $"slot='{PlayerSlotId.StableText}' actorProfile='{ActorProfileId.StableText}' " +
                $"actor='{ActorId.StableText}' revision='{MaterializationRevision}' state='{State}' " +
                $"source='{Source}' reason='{Reason}'";
        }
    }
}
