using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// Owner-bound anchor for on-demand Actor occurrence queries. The Framework binds it when the Session,
    /// Route or Activity content owner whose roots contain it commits, and unbinds it on owner rollback,
    /// owner release or host teardown. The owner defines lifetime only; it does not restrict which
    /// committed occurrences can be resolved. Hand the <see cref="IActorOccurrenceQuery"/> to plain C#
    /// consumers explicitly.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Occurrence Query")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ADR-049 Amendment 001 owner-bound Actor occurrence query anchor.")]
    public sealed class ActorOccurrenceQueryBinding : MonoBehaviour, IActorOccurrenceQuery
    {
        private const string UnboundDiagnostic =
            "Actor occurrence query is not bound to a committed Framework owner.";

        private ActorOccurrenceReferenceRuntime _runtime;
        private RuntimeContentOwner _owner;

        /// <inheritdoc />
        public ActorOccurrenceResolution Resolve(ActorOccurrenceReference reference)
        {
            ActorOccurrenceReferenceRuntime runtime = _runtime;
            return runtime == null
                ? new ActorOccurrenceResolution(ActorOccurrenceResolutionStatus.Unavailable, default, UnboundDiagnostic)
                : runtime.Resolve(reference);
        }

        internal RuntimeContentOwner BoundOwner => _owner;

        internal bool CanBindRuntime(
            ActorOccurrenceReferenceRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (runtime == null || !owner.IsValid)
            {
                diagnostic = "Actor occurrence query binding requires an exact runtime and valid lifecycle owner.";
                return false;
            }

            if (_runtime != null)
            {
                diagnostic = _runtime == runtime && _owner == owner
                    ? "Actor occurrence query binding is already bound to this owner."
                    : $"Actor occurrence query binding is already bound to owner '{_owner.StableText}'.";
                return false;
            }

            return true;
        }

        internal bool TryBindRuntime(
            ActorOccurrenceReferenceRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            if (!CanBindRuntime(runtime, owner, out diagnostic)) return false;
            _runtime = runtime;
            _owner = owner;
            return true;
        }

        internal void UnbindRuntime(ActorOccurrenceReferenceRuntime runtime, RuntimeContentOwner owner)
        {
            if (_runtime != runtime || _owner != owner) return;
            _runtime = null;
            _owner = default;
        }
    }
}
