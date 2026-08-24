using Immersive.Framework.ActivityFlow;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private ActivityTransitionPreparationContext
            currentActivityInitialPlacementContext;
        private ActivityPlayerInitialPlacementEvidence
            lastActivityInitialPlacementEvidence;
        private RoutePlayerSpatialEntryContext currentRouteSpatialEntryContext;

        internal bool TryConfigureActivityInitialPlacementContext(
            ActivityTransitionPreparationContext context,
            out string issue)
        {
            issue = string.Empty;
            if (!IsReady || !context.IsValid)
            {
                issue =
                    "Activity initial placement requires a ready Player Actor preparation module and valid target occurrence context.";
                return false;
            }

            currentActivityInitialPlacementContext = context;
            PlayerParticipationSnapshot snapshot =
                participationContext.CreateSnapshot();
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined ||
                    !TryGetRegisteredHost(
                        slot.PlayerSlotId,
                        out LocalPlayerHostAuthoring host,
                        out _ ) ||
                    host == null)
                {
                    continue;
                }

                PublishCurrentInitialPlacementGate(host);
            }

            return true;
        }

        internal void PublishCurrentInitialPlacementGate(LocalPlayerHostAuthoring host)
        {
            if (host == null)
            {
                return;
            }

            ActivityPlayerInitialPlacementRuntimeBinding binding =
                host.GetComponent<ActivityPlayerInitialPlacementRuntimeBinding>();
            if (binding == null)
            {
                binding = host.gameObject
                    .AddComponent<ActivityPlayerInitialPlacementRuntimeBinding>();
            }

            if (currentRouteSpatialEntryContext.IsValid)
            {
                binding.ConfigureRouteSpatialEntry(currentRouteSpatialEntryContext);
            }

            if (currentActivityInitialPlacementContext.IsValid)
            {
                binding.Configure(currentActivityInitialPlacementContext);
            }
        }

        bool IRoutePlayerSpatialEntryLifecycleParticipant.TryEnterRouteSpatialEntry(
            RoutePlayerSpatialEntryContext context,
            out string issue)
        {
            issue = string.Empty;
            if (!IsReady || !context.IsValid)
            {
                issue = "Route spatial entry requires a ready Player preparation module and valid Route occurrence context.";
                return false;
            }

            // A Route occurrence supersedes any prior Activity relocation context.
            // The destination Activity, if one exists, publishes a new context later.
            currentActivityInitialPlacementContext = default;
            currentRouteSpatialEntryContext = context;
            PlayerParticipationSnapshot snapshot = participationContext.CreateSnapshot();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined || !TryGetRegisteredHost(slot.PlayerSlotId, out LocalPlayerHostAuthoring host, out _))
                {
                    continue;
                }

                PublishCurrentInitialPlacementGate(host);
                if (!TryGetCurrentPreparation(slot.PlayerSlotId, out PlayerActorPreparationSummary preparation, out _) ||
                    !TryGetPreparedPhysicalEvidence(
                        slot.PlayerSlotId,
                        preparation.Token,
                        out _, out _, out _, out PlayerActorMaterializationHandle handle, out _))
                {
                    continue;
                }

                ActivityPlayerInitialPlacementRuntimeBinding binding = host.GetComponent<ActivityPlayerInitialPlacementRuntimeBinding>();
                if (binding == null || !binding.TryApplyRouteSpatialEntry(handle, out issue))
                {
                    return false;
                }
            }

            return true;
        }

        void IRoutePlayerSpatialEntryLifecycleParticipant.ExitRouteSpatialEntry(
            RoutePlayerSpatialEntryContext context)
        {
            if (currentRouteSpatialEntryContext.Matches(context))
            {
                currentRouteSpatialEntryContext = default;
            }
        }

        internal bool TryApplySceneProvidedInitialPlacement(
            SceneLocalPlayerAdmissionAuthoring authoring,
            out string issue)
        {
            issue = string.Empty;
            if (!currentRouteSpatialEntryContext.IsValid ||
                authoring == null ||
                authoring.SceneLogicalPlayerActor == null ||
                !authoring.TryGetPlayerSlotId(
                    out PlayerSlotId playerSlotId,
                    out issue))
            {
                if (string.IsNullOrEmpty(issue))
                {
                    issue =
                    "Scene-Provided spatial entry requires current Route occurrence context and complete authoring.";
                }
                return false;
            }

            bool applied = RoutePlayerSpatialEntryRuntime
                .TryApply(
                    currentRouteSpatialEntryContext,
                    playerSlotId,
                    authoring.SceneLogicalPlayerActor.ActorId,
                    $"scene-provided:{playerSlotId.StableText}:{authoring.SceneLogicalPlayerActor.ActorId.StableText}",
                    authoring.SceneLogicalPlayerActor.transform,
                    out issue);
            return applied;
        }

        internal ActivityPlayerInitialPlacementEvidence
            LastActivityInitialPlacementEvidence =>
                lastActivityInitialPlacementEvidence;

        internal bool ShouldRetainPhysicalActorPresentationForIncomingActivity(
            RuntimeContentOwner exitingOwner,
            PlayerSlotId playerSlotId)
        {
            if (!currentActivityInitialPlacementContext.IsValid ||
                !exitingOwner.IsValid ||
                currentActivityInitialPlacementContext.Owner == exitingOwner ||
                !playerSlotId.IsValid ||
                participationContext == null)
            {
                return false;
            }

            return ActivityPlayerParticipationProjectionResolver.TryResolve(
                       currentActivityInitialPlacementContext.Activity,
                       participationContext,
                       out _,
                       out var projectedSlots,
                       out _) &&
                   projectedSlots.Exists(slot => slot.PlayerSlotId == playerSlotId);
        }
    }
}
