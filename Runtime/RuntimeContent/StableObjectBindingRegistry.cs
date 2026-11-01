using System;
using System.Collections.Generic;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Identity;
using UnityEngine;

namespace Immersive.Framework.RuntimeContent
{
    internal enum StableObjectBindingResolutionStatus
    {
        Resolved = 10,
        NotFound = 20,
        Ambiguous = 30,
        InvalidRequest = 40
    }

    internal sealed class StableObjectBinding
    {
        internal StableObjectBinding(ObjectEntryId objectEntryId, RuntimeContentOwner owner, GameObject physicalObject)
        {
            ObjectEntryId = objectEntryId;
            Owner = owner;
            PhysicalObject = physicalObject;
        }

        internal ObjectEntryId ObjectEntryId { get; }
        internal RuntimeContentOwner Owner { get; }
        internal GameObject PhysicalObject { get; }
    }

    /// <summary>Host-owned runtime projection from stable Object Entry identity to admitted physical occurrences.</summary>
    internal sealed class StableObjectBindingRegistry
    {
        private sealed class OwnerBindings
        {
            internal OwnerBindings(
                RuntimeContentOwner owner,
                List<StableObjectBinding> bindings,
                List<ObjectEntryOccurrenceQueryBinding> queryBindings)
            {
                Owner = owner;
                Bindings = bindings;
                QueryBindings = queryBindings;
            }

            internal RuntimeContentOwner Owner { get; }
            internal List<StableObjectBinding> Bindings { get; }
            internal List<ObjectEntryOccurrenceQueryBinding> QueryBindings { get; }
            internal bool IsCommitted { get; set; }
        }

        private readonly Dictionary<RuntimeContentOwner, OwnerBindings> _owners =
            new Dictionary<RuntimeContentOwner, OwnerBindings>();
        private bool _queryAccessInvalidated;

        internal int BindingCount
        {
            get
            {
                int count = 0;
                foreach (OwnerBindings owner in _owners.Values) count += owner.Bindings.Count;
                return count;
            }
        }

        internal bool TryRegisterOwnerContent(
            RuntimeContentOwner owner,
            IReadOnlyList<GameObject> roots,
            out string diagnostic)
        {
            if (!owner.IsValid || (owner.Scope != RuntimeContentScope.Route && owner.Scope != RuntimeContentScope.Activity))
            {
                diagnostic = "stable-object-binding-invalid-owner: registration requires a valid Route or Activity RuntimeContentOwner.";
                return false;
            }
            if (_owners.ContainsKey(owner))
            {
                diagnostic = $"stable-object-binding-owner-already-registered: owner='{owner.StableText}' must be released before another admission.";
                return false;
            }

            var bindings = new List<StableObjectBinding>();
            var queryBindings = new List<ObjectEntryOccurrenceQueryBinding>();
            var seenRoots = new HashSet<GameObject>();
            var seenDeclarations = new HashSet<ObjectEntryDeclaration>();
            var seenQueryBindings = new HashSet<ObjectEntryOccurrenceQueryBinding>();
            if (roots != null)
            {
                for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
                {
                    GameObject root = roots[rootIndex];
                    if (root == null || !seenRoots.Add(root)) continue;
                    ObjectEntryDeclaration[] declarations = root.GetComponentsInChildren<ObjectEntryDeclaration>(true);
                    for (int declarationIndex = 0; declarationIndex < declarations.Length; declarationIndex++)
                    {
                        ObjectEntryDeclaration declaration = declarations[declarationIndex];
                        if (declaration == null || !seenDeclarations.Add(declaration)) continue;
                        if (!declaration.TryGetObjectEntryId(out ObjectEntryId objectEntryId))
                        {
                            diagnostic = $"stable-object-binding-invalid-declaration: owner='{owner.StableText}' requires a valid ObjectEntryId.";
                            return false;
                        }
                        bindings.Add(new StableObjectBinding(objectEntryId, owner, declaration.gameObject));
                    }

                    ObjectEntryOccurrenceQueryBinding[] ownerQueryBindings =
                        root.GetComponentsInChildren<ObjectEntryOccurrenceQueryBinding>(true);
                    for (int bindingIndex = 0; bindingIndex < ownerQueryBindings.Length; bindingIndex++)
                    {
                        ObjectEntryOccurrenceQueryBinding queryBinding = ownerQueryBindings[bindingIndex];
                        if (queryBinding == null || !seenQueryBindings.Add(queryBinding)) continue;
                        if (_queryAccessInvalidated)
                        {
                            diagnostic = "stable-object-binding-query-access-invalidated: Framework host teardown has invalidated query anchors.";
                            return false;
                        }
                        if (TryGetQueryBindingOwner(queryBinding, out RuntimeContentOwner existingOwner))
                        {
                            diagnostic = $"stable-object-binding-query-anchor-already-owned: anchor='{queryBinding.name}' owner='{existingOwner.StableText}'.";
                            return false;
                        }
                        if (!queryBinding.CanBindRuntime(this, owner, out diagnostic))
                        {
                            diagnostic = "stable-object-binding-query-anchor-invalid: " + diagnostic;
                            return false;
                        }

                        queryBindings.Add(queryBinding);
                    }
                }
            }

            _owners.Add(owner, new OwnerBindings(owner, bindings, queryBindings));

            diagnostic = $"stable-object-bindings-registered: owner='{owner.StableText}' bindings='{bindings.Count}'.";
            return true;
        }

        internal bool TryRollbackOwner(RuntimeContentOwner owner, out string diagnostic) =>
            TryRemoveOwner(owner, "rollback", out diagnostic);

        internal bool TryReleaseOwner(RuntimeContentOwner owner, out string diagnostic) =>
            TryRemoveOwner(owner, "release", out diagnostic);

        internal bool TryCommitOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            if (!owner.IsValid || !_owners.TryGetValue(owner, out OwnerBindings registration))
            {
                diagnostic = $"stable-object-binding-commit-owner-missing: owner='{owner.StableText}' has no prepared binding transaction.";
                return false;
            }

            if (registration.IsCommitted)
            {
                diagnostic = $"stable-object-binding-owner-committed: owner='{owner.StableText}'.";
                return true;
            }
            if (_queryAccessInvalidated && registration.QueryBindings.Count > 0)
            {
                diagnostic = "stable-object-binding-query-access-invalidated: Framework host teardown has invalidated query anchors.";
                return false;
            }
            if (!TryBindQueryBindings(registration, out diagnostic)) return false;

            registration.IsCommitted = true;
            diagnostic = $"stable-object-binding-owner-committed: owner='{owner.StableText}'.";
            return true;
        }

        internal void InvalidateQueryAccess()
        {
            if (_queryAccessInvalidated) return;
            _queryAccessInvalidated = true;

            foreach (OwnerBindings registration in _owners.Values)
            {
                List<ObjectEntryOccurrenceQueryBinding> queryBindings = registration.QueryBindings;
                for (int index = 0; index < queryBindings.Count; index++)
                {
                    ObjectEntryOccurrenceQueryBinding binding = queryBindings[index];
                    if (!ReferenceEquals(binding, null))
                        binding.UnbindRuntime(this, registration.Owner);
                }
                queryBindings.Clear();
            }
        }

        internal bool TryResolve(
            ObjectEntryId objectEntryId,
            FrameworkIdentityKey? ownerSelector,
            RuntimeDefinitionToken? ownerDefinitionSelector,
            out StableObjectBinding binding,
            out StableObjectBindingResolutionStatus status,
            out string diagnostic)
        {
            binding = null;
            if (!objectEntryId.IsValid || (ownerSelector.HasValue && !ownerSelector.Value.IsValid)
                || (ownerDefinitionSelector.HasValue && !ownerDefinitionSelector.Value.IsValid)
                || ownerSelector.HasValue != ownerDefinitionSelector.HasValue)
            {
                status = StableObjectBindingResolutionStatus.InvalidRequest;
                diagnostic = "stable-object-binding-invalid-reference: ObjectEntryId and optional owner selector must be valid.";
                return false;
            }

            var matches = new List<StableObjectBinding>();
            foreach (OwnerBindings registration in _owners.Values)
            {
                if (!registration.IsCommitted) continue;
                if (ownerSelector.HasValue && !registration.Owner.OwnerIdentity.Equals(ownerSelector.Value)) continue;
                if (ownerDefinitionSelector.HasValue && !registration.Owner.DefinitionToken.Equals(ownerDefinitionSelector.Value)) continue;
                for (int index = 0; index < registration.Bindings.Count; index++)
                {
                    StableObjectBinding candidate = registration.Bindings[index];
                    if (candidate.PhysicalObject == null) continue;
                    if (candidate.ObjectEntryId == objectEntryId) matches.Add(candidate);
                }
            }

            if (matches.Count == 0)
            {
                status = StableObjectBindingResolutionStatus.NotFound;
                diagnostic = $"stable-object-binding-not-found: ObjectEntryId='{objectEntryId.StableText}' has no live admitted occurrence.";
                return false;
            }
            if (matches.Count > 1)
            {
                status = StableObjectBindingResolutionStatus.Ambiguous;
                diagnostic = $"stable-object-binding-ambiguous: ObjectEntryId='{objectEntryId.StableText}' matched '{matches.Count}' live occurrences.";
                return false;
            }

            binding = matches[0];
            status = StableObjectBindingResolutionStatus.Resolved;
            diagnostic = $"stable-object-binding-resolved: ObjectEntryId='{objectEntryId.StableText}' owner='{binding.Owner.StableText}'.";
            return true;
        }

        private bool TryRemoveOwner(RuntimeContentOwner owner, string operation, out string diagnostic)
        {
            if (!owner.IsValid)
            {
                diagnostic = $"stable-object-binding-{operation}-invalid-owner: owner is invalid.";
                return false;
            }
            bool applied = _owners.TryGetValue(owner, out OwnerBindings registration);
            int removed = applied ? registration.Bindings.Count : 0;
            if (applied)
            {
                ReleaseQueryBindings(registration);
                _owners.Remove(owner);
            }
            diagnostic = $"stable-object-binding-{operation}-completed: owner='{owner.StableText}' bindings='{removed}' applied='{applied}'.";
            return true;
        }

        private bool TryBindQueryBindings(OwnerBindings registration, out string diagnostic)
        {
            diagnostic = string.Empty;
            List<ObjectEntryOccurrenceQueryBinding> queryBindings = registration.QueryBindings;
            for (int index = 0; index < queryBindings.Count;)
            {
                ObjectEntryOccurrenceQueryBinding binding = queryBindings[index];
                if (binding == null)
                {
                    queryBindings.RemoveAt(index);
                    continue;
                }
                if (!binding.TryBindRuntime(this, registration.Owner, out diagnostic))
                {
                    for (int boundIndex = 0; boundIndex < index; boundIndex++)
                    {
                        ObjectEntryOccurrenceQueryBinding bound = queryBindings[boundIndex];
                        if (!ReferenceEquals(bound, null)) bound.UnbindRuntime(this, registration.Owner);
                    }
                    return false;
                }
                index++;
            }

            return true;
        }

        private void ReleaseQueryBindings(OwnerBindings registration)
        {
            List<ObjectEntryOccurrenceQueryBinding> queryBindings = registration.QueryBindings;
            for (int index = 0; index < queryBindings.Count; index++)
            {
                ObjectEntryOccurrenceQueryBinding binding = queryBindings[index];
                if (!ReferenceEquals(binding, null)) binding.UnbindRuntime(this, registration.Owner);
            }
            queryBindings.Clear();
        }

        private bool TryGetQueryBindingOwner(
            ObjectEntryOccurrenceQueryBinding queryBinding,
            out RuntimeContentOwner owner)
        {
            foreach (OwnerBindings registration in _owners.Values)
            {
                List<ObjectEntryOccurrenceQueryBinding> queryBindings = registration.QueryBindings;
                for (int index = 0; index < queryBindings.Count; index++)
                {
                    if (!ReferenceEquals(queryBindings[index], queryBinding)) continue;
                    owner = registration.Owner;
                    return true;
                }
            }

            owner = default;
            return false;
        }

    }
}
