using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Host-local Route occurrence gate for first physical activation.</summary>
    [DisallowMultipleComponent]
    internal sealed class RoutePlayerSpatialEntryRuntimeBinding : MonoBehaviour
    {
        private RoutePlayerSpatialEntryContext _context;
        private int _lastOccurrenceSequence;
        private string _lastRepresentationIdentity;

        internal void Configure(RoutePlayerSpatialEntryContext value)
        {
            if (_context.Matches(value)) return;
            _context = value;
            _lastOccurrenceSequence = 0;
            _lastRepresentationIdentity = string.Empty;
        }

        internal bool TryApplyBeforeActivation(PlayerActorMaterializationHandle handle, out string issue)
        {
            issue = string.Empty;
            if (handle == null || handle.PlayerActorDeclaration == null || handle.PlayerActorRuntimeHost == null)
            {
                issue = "Route Player spatial entry requires a complete materialization handle.";
                return false;
            }
            if (!_context.IsValid)
            {
                issue = "Session Player cannot activate without current Route spatial-entry occurrence evidence.";
                return false;
            }

            string representation = handle.Request.RuntimeContentIdentity.StableText;
            if (_lastOccurrenceSequence == _context.OccurrenceSequence &&
                string.Equals(_lastRepresentationIdentity, representation, System.StringComparison.Ordinal))
                return true;

            Transform declaration = handle.PlayerActorDeclaration.transform;
            Transform root = handle.PlayerActorRuntimeHost.transform;
            Transform target = ReferenceEquals(declaration, root) || declaration.IsChildOf(root)
                ? root : declaration;
            if (!RoutePlayerSpatialEntryRuntime.TryApply(
                    _context, handle.Request.Slot.PlayerSlotId, handle.Request.ActorId,
                    representation, target, out issue)) return false;

            _lastOccurrenceSequence = _context.OccurrenceSequence;
            _lastRepresentationIdentity = representation;
            return true;
        }
    }
}
