using Immersive.Framework.ActivityFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Local Player Host-scoped transient bridge between ActivityFlow target occurrence authority
    /// and staged Manager-Provisioned Actor activation. It never discovers an Activity globally.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class ActivityPlayerInitialPlacementRuntimeBinding : MonoBehaviour
    {
        private ActivityTransitionPreparationContext context;
        private RoutePlayerSpatialEntryContext routeContext;
        private ActivityPlayerInitialPlacementEvidence lastEvidence;
        private int lastRouteOccurrenceSequence;
        private string lastRouteRepresentationIdentity;

        internal void Configure(ActivityTransitionPreparationContext value)
        {
            context = value;
            lastEvidence = default;
        }

        internal void ConfigureRouteSpatialEntry(RoutePlayerSpatialEntryContext value)
        {
            if (routeContext.Matches(value))
            {
                return;
            }

            routeContext = value;
            lastRouteOccurrenceSequence = 0;
            lastRouteRepresentationIdentity = string.Empty;
        }

        internal bool MatchesOwner(RuntimeContentOwner owner) =>
            context.IsValid && context.Owner == owner;

        internal bool TryApplyBeforeActivation(
            PlayerActorMaterializationHandle handle,
            out string issue)
        {
            issue = string.Empty;
            if (handle == null ||
                handle.LogicalActorHost == null ||
                handle.PlayerActorDeclaration == null)
            {
                issue = "Player Actor initial placement activation gate requires a complete materialization handle.";
                return false;
            }

            if (!TryApplyRouteSpatialEntry(handle, out issue))
            {
                return false;
            }

            // Activity relocation is optional and strictly subsequent to Route entry.
            // A Route without a current Activity is therefore a complete supported state.
            if (!context.IsValid)
            {
                return true;
            }

            Transform declarationTransform = handle.PlayerActorDeclaration.transform;
            Transform logicalRoot = handle.LogicalActorHost.transform;
            bool frameworkOwnedPhysicalActor = ReferenceEquals(declarationTransform, logicalRoot) ||
                declarationTransform.IsChildOf(logicalRoot);
            if (!frameworkOwnedPhysicalActor)
            {
                return true;
            }

            if (!context.IsValid)
            {
                issue =
                    "Session-owned Manager-Provisioned Actor cannot activate without current Activity initial-placement occurrence evidence.";
                return false;
            }

            string representationIdentity =
                handle.Request.RuntimeContentIdentity.StableText;
            if (lastEvidence.IsSuccessful &&
                lastEvidence.Owner == context.Owner &&
                lastEvidence.Occurrence.Matches(
                    context.Activity,
                    context.Occurrence.TransitionSequence) &&
                lastEvidence.PlayerSlotId ==
                    handle.Request.Slot.PlayerSlotId &&
                lastEvidence.ActorId == handle.Request.ActorId &&
                string.Equals(
                    lastEvidence.RepresentationIdentity,
                    representationIdentity,
                    System.StringComparison.Ordinal))
            {
                return true;
            }

            return ActivityPlayerInitialPlacementRuntime
                .TryApplyRequiredPlacement(
                    context,
                    handle.Request.Slot.PlayerSlotId,
                    handle.Request.ActorId,
                    representationIdentity,
                    logicalRoot,
                    out lastEvidence,
                out issue);
        }

        internal bool TryApplyRouteSpatialEntry(
            PlayerActorMaterializationHandle handle,
            out string issue)
        {
            issue = string.Empty;
            if (handle == null || handle.PlayerActorDeclaration == null ||
                handle.LogicalActorHost == null)
            {
                issue = "Route Player spatial entry requires a complete materialization handle.";
                return false;
            }

            if (!routeContext.IsValid)
            {
                issue =
                    "Session Player cannot activate without current Route spatial-entry occurrence evidence.";
                return false;
            }

            string representationIdentity = handle.Request.RuntimeContentIdentity.StableText;
            if (lastRouteOccurrenceSequence == routeContext.OccurrenceSequence &&
                string.Equals(lastRouteRepresentationIdentity, representationIdentity,
                    System.StringComparison.Ordinal))
            {
                return true;
            }

            Transform declarationTransform = handle.PlayerActorDeclaration.transform;
            Transform logicalRoot = handle.LogicalActorHost.transform;
            bool frameworkOwnedPhysicalActor = ReferenceEquals(declarationTransform, logicalRoot) ||
                declarationTransform.IsChildOf(logicalRoot);
            Transform target = frameworkOwnedPhysicalActor
                ? logicalRoot
                : declarationTransform;
            if (!RoutePlayerSpatialEntryRuntime.TryApply(
                    routeContext,
                    handle.Request.Slot.PlayerSlotId,
                    handle.Request.ActorId,
                    representationIdentity,
                    target,
                    out issue))
            {
                return false;
            }

            lastRouteOccurrenceSequence = routeContext.OccurrenceSequence;
            lastRouteRepresentationIdentity = representationIdentity;
            return true;
        }

        internal ActivityPlayerInitialPlacementEvidence LastEvidence =>
            lastEvidence;
    }
}
