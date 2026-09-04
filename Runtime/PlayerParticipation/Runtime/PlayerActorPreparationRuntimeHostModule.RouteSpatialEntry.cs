using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.PlayerParticipation
{
    internal sealed partial class PlayerActorPreparationRuntimeHostModule
    {
        private RoutePlayerSpatialEntryContext _currentRouteSpatialEntryContext;

        internal void PublishCurrentRouteSpatialEntryGate(LocalPlayerHostAuthoring host)
        {
            if (host == null || !_currentRouteSpatialEntryContext.IsValid) return;
            RoutePlayerSpatialEntryRuntimeBinding binding = host.GetComponent<RoutePlayerSpatialEntryRuntimeBinding>();
            if (binding == null) binding = host.gameObject.AddComponent<RoutePlayerSpatialEntryRuntimeBinding>();
            binding.Configure(_currentRouteSpatialEntryContext);
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
            _currentRouteSpatialEntryContext = context;
            PlayerParticipationSnapshot snapshot = _participationContext.CreateSnapshot();
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
            if (_currentRouteSpatialEntryContext.Matches(context)) _currentRouteSpatialEntryContext = default;
        }

        internal bool TryApplySceneProvidedRouteSpatialEntry(SceneProvidedLocalPlayerAuthoring authoring, out string issue)
        {
            issue = string.Empty;
            if (!_currentRouteSpatialEntryContext.IsValid || authoring == null ||
                !SceneProvidedLocalPlayerCompositionResolver.TryResolve(
                    authoring,
                    out SceneProvidedLocalPlayerComposition composition,
                    out issue) ||
                !authoring.TryGetPlayerSlotId(out PlayerSlotId slot, out issue))
            {
                if (string.IsNullOrEmpty(issue)) issue = "Scene-Provided spatial entry requires current Route occurrence context and complete authoring.";
                return false;
            }
            return RoutePlayerSpatialEntryRuntime.TryApply(
                _currentRouteSpatialEntryContext,
                slot,
                composition.Presentation.transform,
                out issue);
        }

        internal bool ShouldRetainPhysicalActorPresentationForIncomingActivity(
            RuntimeContentOwner exitingOwner, PlayerSlotId playerSlotId) =>
            _currentActivityRelocationContext.IsValid && exitingOwner.IsValid &&
            _currentActivityRelocationContext.Owner != exitingOwner && playerSlotId.IsValid &&
            _participationContext != null &&
            ActivityPlayerParticipationProjectionResolver.TryResolve(
                _currentActivityRelocationContext.Activity, _participationContext, out _,
                out var slots, out _) && slots.Exists(slot => slot.PlayerSlotId == playerSlotId);
    }
}
