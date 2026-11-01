using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Owner-bound access point for read-only Object Entry occurrence queries. Framework Runtime Content
    /// binds it when the containing Route or Activity commits and unbinds it on rollback, release or host
    /// teardown. The owner defines access lifetime only; the supplied reference selects the occurrence.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Object Entry/Occurrence Query")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Owner-bound Object Entry occurrence query anchor.")]
    public sealed class ObjectEntryOccurrenceQueryBinding : MonoBehaviour, IObjectEntryOccurrenceQuery
    {
        private const string UnboundDiagnostic =
            "Object Entry occurrence query is not bound to a committed Route or Activity owner.";

        private StableObjectBindingRegistry _registry;
        private RuntimeContentOwner _owner;

        /// <inheritdoc />
        public ObjectEntryOccurrenceResolution Resolve(StableObjectReference reference)
        {
            if (!reference.TryGetObjectEntryId(out ObjectEntryId objectEntryId)
                || !reference.TryGetOwnerSelector(
                    out FrameworkIdentityKey? ownerSelector,
                    out RuntimeDefinitionToken? ownerDefinitionSelector))
            {
                return new ObjectEntryOccurrenceResolution(
                    ObjectEntryOccurrenceResolutionStatus.InvalidReference,
                    null,
                    "Object Entry occurrence query requires a valid ObjectEntryId and typed optional owner selector.");
            }

            StableObjectBindingRegistry registry = _registry;
            if (registry == null)
            {
                return new ObjectEntryOccurrenceResolution(
                    ObjectEntryOccurrenceResolutionStatus.Unavailable,
                    null,
                    UnboundDiagnostic);
            }

            if (!registry.TryResolve(
                    objectEntryId,
                    ownerSelector,
                    ownerDefinitionSelector,
                    out StableObjectBinding binding,
                    out StableObjectBindingResolutionStatus status,
                    out string diagnostic))
            {
                ObjectEntryOccurrenceResolutionStatus publicStatus = status switch
                {
                    StableObjectBindingResolutionStatus.Ambiguous => ObjectEntryOccurrenceResolutionStatus.Ambiguous,
                    StableObjectBindingResolutionStatus.InvalidRequest => ObjectEntryOccurrenceResolutionStatus.InvalidReference,
                    _ => ObjectEntryOccurrenceResolutionStatus.Unavailable
                };
                return new ObjectEntryOccurrenceResolution(publicStatus, null, diagnostic);
            }

            GameObject occurrence = binding?.PhysicalObject;
            return occurrence == null
                ? new ObjectEntryOccurrenceResolution(
                    ObjectEntryOccurrenceResolutionStatus.Unavailable,
                    null,
                    $"Object Entry '{objectEntryId.StableText}' is no longer physically available.")
                : new ObjectEntryOccurrenceResolution(
                    ObjectEntryOccurrenceResolutionStatus.Resolved,
                    occurrence,
                    diagnostic);
        }

        internal bool CanBindRuntime(
            StableObjectBindingRegistry registry,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (registry == null || !owner.IsValid
                || (owner.Scope != RuntimeContentScope.Route && owner.Scope != RuntimeContentScope.Activity))
            {
                diagnostic = "Object Entry query binding requires a Runtime Content registry and an exact Route or Activity owner.";
                return false;
            }

            if (_registry != null)
            {
                if (_registry == registry && _owner == owner)
                {
                    diagnostic = "Object Entry query binding is already bound to this owner.";
                    return true;
                }

                diagnostic = $"Object Entry query binding is already bound to owner '{_owner.StableText}'.";
                return false;
            }

            return true;
        }

        internal bool TryBindRuntime(
            StableObjectBindingRegistry registry,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            if (!CanBindRuntime(registry, owner, out diagnostic)) return false;
            _registry = registry;
            _owner = owner;
            return true;
        }

        internal void UnbindRuntime(StableObjectBindingRegistry registry, RuntimeContentOwner owner)
        {
            if (_registry != registry || _owner != owner) return;
            _registry = null;
            _owner = default;
        }
    }
}
