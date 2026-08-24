using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Host-local Route occurrence gate for first physical activation.</summary>
    [DisallowMultipleComponent]
    internal sealed class RoutePlayerSpatialEntryRuntimeBinding : MonoBehaviour
    {
        private RoutePlayerSpatialEntryContext context;
        private int lastOccurrenceSequence;
        private string lastRepresentationIdentity;

        internal void Configure(RoutePlayerSpatialEntryContext value)
        {
            if (context.Matches(value)) return;
            context = value;
            lastOccurrenceSequence = 0;
            lastRepresentationIdentity = string.Empty;
        }

        internal bool TryApplyBeforeActivation(PlayerActorMaterializationHandle handle, out string issue)
        {
            issue = string.Empty;
            if (handle == null || handle.PlayerActorDeclaration == null || handle.LogicalActorHost == null)
            {
                issue = "Route Player spatial entry requires a complete materialization handle.";
                return false;
            }
            if (!context.IsValid)
            {
                issue = "Session Player cannot activate without current Route spatial-entry occurrence evidence.";
                return false;
            }

            string representation = handle.Request.RuntimeContentIdentity.StableText;
            if (lastOccurrenceSequence == context.OccurrenceSequence &&
                string.Equals(lastRepresentationIdentity, representation, System.StringComparison.Ordinal))
                return true;

            Transform declaration = handle.PlayerActorDeclaration.transform;
            Transform root = handle.LogicalActorHost.transform;
            Transform target = ReferenceEquals(declaration, root) || declaration.IsChildOf(root)
                ? root : declaration;
            if (!RoutePlayerSpatialEntryRuntime.TryApply(
                    context, handle.Request.Slot.PlayerSlotId, handle.Request.ActorId,
                    representation, target, out issue)) return false;

            lastOccurrenceSequence = context.OccurrenceSequence;
            lastRepresentationIdentity = representation;
            return true;
        }
    }
}
