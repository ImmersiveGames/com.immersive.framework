using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private RoutePlayerSpatialEntryContext currentRouteSpatialEntryContext;

        internal void PublishCurrentRouteSpatialEntryGate(LocalPlayerHostAuthoring host)
        {
            if (host == null || !currentRouteSpatialEntryContext.IsValid) return;
            RoutePlayerSpatialEntryRuntimeBinding binding = host.GetComponent<RoutePlayerSpatialEntryRuntimeBinding>();
            if (binding == null) binding = host.gameObject.AddComponent<RoutePlayerSpatialEntryRuntimeBinding>();
            binding.Configure(currentRouteSpatialEntryContext);
        }

        bool IRoutePlayerSpatialEntryLifecycleParticipant.TryEnterRouteSpatialEntry(
            RoutePlayerSpatialEntryContext context, out string issue)
        {
            issue = string.Empty;
            if (!IsReady || !context.IsValid)
            {
                issue = "Route spatial entry requires a ready Player preparation module and valid Route occurrence context.";
                return false;
            }
            currentRouteSpatialEntryContext = context;
            PlayerParticipationSnapshot snapshot = participationContext.CreateSnapshot();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined || !TryGetRegisteredHost(slot.PlayerSlotId, out LocalPlayerHostAuthoring host, out _)) continue;
                PublishCurrentRouteSpatialEntryGate(host);
                if (!TryGetCurrentPreparation(slot.PlayerSlotId, out PlayerActorPreparationSummary summary, out _) ||
                    !TryGetPreparedPhysicalEvidence(slot.PlayerSlotId, summary.Token, out _, out _, out _, out PlayerActorMaterializationHandle handle, out _)) continue;
                RoutePlayerSpatialEntryRuntimeBinding binding = host.GetComponent<RoutePlayerSpatialEntryRuntimeBinding>();
                if (binding == null || !binding.TryApplyBeforeActivation(handle, out issue)) return false;
            }
            return true;
        }

        void IRoutePlayerSpatialEntryLifecycleParticipant.ExitRouteSpatialEntry(RoutePlayerSpatialEntryContext context)
        {
            if (currentRouteSpatialEntryContext.Matches(context)) currentRouteSpatialEntryContext = default;
        }

        internal bool TryApplySceneProvidedRouteSpatialEntry(SceneLocalPlayerAdmissionAuthoring authoring, out string issue)
        {
            issue = string.Empty;
            if (!currentRouteSpatialEntryContext.IsValid || authoring == null ||
                authoring.SceneLogicalPlayerActor == null ||
                !authoring.TryGetPlayerSlotId(out PlayerSlotId slot, out issue))
            {
                if (string.IsNullOrEmpty(issue)) issue = "Scene-Provided spatial entry requires current Route occurrence context and complete authoring.";
                return false;
            }
            return RoutePlayerSpatialEntryRuntime.TryApply(
                currentRouteSpatialEntryContext, slot, authoring.SceneLogicalPlayerActor.ActorId,
                $"scene-provided:{slot.StableText}:{authoring.SceneLogicalPlayerActor.ActorId.StableText}",
                authoring.SceneLogicalPlayerActor.transform, out issue);
        }

        internal bool ShouldRetainPhysicalActorPresentationForIncomingActivity(
            RuntimeContentOwner exitingOwner, PlayerSlotId playerSlotId) =>
            currentActivityRelocationContext.IsValid && exitingOwner.IsValid &&
            currentActivityRelocationContext.Owner != exitingOwner && playerSlotId.IsValid &&
            participationContext != null &&
            ActivityPlayerParticipationProjectionResolver.TryResolve(
                currentActivityRelocationContext.Activity, participationContext, out _,
                out var slots, out _) && slots.Exists(slot => slot.PlayerSlotId == playerSlotId);
    }
}
