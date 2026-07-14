using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Immutable framework-authorized request to stage one selected Logical Player Actor
    /// under an already joined Local Player Host.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P3J.3 contextual Logical Player Actor materialization request.")]
    public readonly struct PlayerActorMaterializationRequest
    {
        internal PlayerActorMaterializationRequest(
            PlayerActorMaterializationOperationId operationId,
            string sessionContextId,
            RuntimeScopeContext scopeContext,
            PlayerSlotRuntimeSnapshot slot,
            ActorProfile actorProfile,
            LocalPlayerHostAuthoring localPlayerHost,
            ActorId actorId,
            RuntimeContentId runtimeContentId,
            int materializationRevision,
            string source,
            string reason)
        {
            OperationId = operationId;
            SessionContextId = sessionContextId.NormalizeText();
            ScopeContext = scopeContext;
            Slot = slot;
            ActorProfile = actorProfile;
            LocalPlayerHost = localPlayerHost;
            ActorId = actorId;
            RuntimeContentId = runtimeContentId;
            MaterializationRevision = materializationRevision;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public PlayerActorMaterializationOperationId OperationId { get; }

        public string SessionContextId { get; }

        public RuntimeScopeContext ScopeContext { get; }

        public RuntimeContentOwner Owner => ScopeContext.Owner;

        public PlayerSlotRuntimeSnapshot Slot { get; }

        public ActorProfile ActorProfile { get; }

        public ActorProfileId ActorProfileId =>
            ActorProfile != null &&
            ActorProfile.TryGetActorProfileId(out ActorProfileId actorProfileId, out _)
                ? actorProfileId
                : default;

        public LocalPlayerHostAuthoring LocalPlayerHost { get; }

        public ActorId ActorId { get; }

        public RuntimeContentId RuntimeContentId { get; }

        public RuntimeContentIdentity RuntimeContentIdentity =>
            ScopeContext.IsValid && RuntimeContentId.IsValid
                ? ScopeContext.CreateIdentity(RuntimeContentId)
                : default;

        public int MaterializationRevision { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid =>
            OperationId.IsValid &&
            !string.IsNullOrEmpty(SessionContextId) &&
            ScopeContext.IsValid &&
            Slot.IsValid &&
            Slot.IsJoined &&
            ActorProfile != null &&
            ActorProfileId.IsValid &&
            LocalPlayerHost != null &&
            ActorId.IsValid &&
            RuntimeContentId.IsValid &&
            MaterializationRevision > 0 &&
            !string.IsNullOrEmpty(Source) &&
            !string.IsNullOrEmpty(Reason);

        public string ToDiagnosticString()
        {
            return $"operation='{OperationId.StableText}' session='{SessionContextId}' " +
                $"owner='{(ScopeContext.IsValid ? Owner.StableText : string.Empty)}' " +
                $"slot='{(Slot.PlayerSlotId.IsValid ? Slot.PlayerSlotId.StableText : string.Empty)}' " +
                $"actorProfile='{(ActorProfileId.IsValid ? ActorProfileId.StableText : string.Empty)}' " +
                $"actor='{(ActorId.IsValid ? ActorId.StableText : string.Empty)}' " +
                $"runtimeContent='{(RuntimeContentIdentity.IsValid ? RuntimeContentIdentity.StableText : string.Empty)}' " +
                $"revision='{MaterializationRevision}' source='{Source}' reason='{Reason}'";
        }
    }
}
