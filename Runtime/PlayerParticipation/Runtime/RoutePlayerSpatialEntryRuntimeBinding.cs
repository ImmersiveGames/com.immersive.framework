using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Host-local Route occurrence gate for first physical activation.</summary>
    [DisallowMultipleComponent]
    internal sealed class RoutePlayerSpatialEntryRuntimeBinding : MonoBehaviour
    {
        private RoutePlayerSpatialEntryContext _context;
        private bool _hasAppliedForCurrentOccurrence;
        private int _lastOccurrenceSequence;

        internal static Transform ResolveActorRoot(PlayerActorRuntimeHost actorRuntimeHost) =>
            actorRuntimeHost != null ? actorRuntimeHost.transform : null;

        internal void Configure(RoutePlayerSpatialEntryContext value)
        {
            if (_context.Matches(value)) return;
            _context = value;
            _hasAppliedForCurrentOccurrence = false;
            _lastOccurrenceSequence = 0;
        }

        internal bool TryApplyBeforeActivation(PlayerActorMaterializationHandle handle, out string issue)
        {
            issue = string.Empty;
            if (handle == null || ResolveActorRoot(handle.PlayerActorRuntimeHost) == null)
            {
                issue = "Route Player spatial entry requires a materialized Actor occurrence root.";
                return false;
            }
            if (!_context.IsValid)
            {
                issue = "Session Player cannot activate without current Route spatial-entry occurrence evidence.";
                return false;
            }

            if (_hasAppliedForCurrentOccurrence &&
                _lastOccurrenceSequence == _context.OccurrenceSequence)
                return true;

            if (!RoutePlayerSpatialEntryRuntime.TryApply(
                    _context,
                    handle.Request.Slot.PlayerSlotId,
                    ResolveActorRoot(handle.PlayerActorRuntimeHost),
                    out issue)) return false;

            _lastOccurrenceSequence = _context.OccurrenceSequence;
            _hasAppliedForCurrentOccurrence = true;
            return true;
        }
    }
}
