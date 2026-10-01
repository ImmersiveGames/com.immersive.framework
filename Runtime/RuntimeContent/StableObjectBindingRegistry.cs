using System;
using System.Collections.Generic;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
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
        private readonly Dictionary<RuntimeContentOwner, List<StableObjectBinding>> _bindingsByOwner =
            new Dictionary<RuntimeContentOwner, List<StableObjectBinding>>();
        private readonly HashSet<RuntimeContentOwner> _committedOwners = new HashSet<RuntimeContentOwner>();

        internal int BindingCount
        {
            get
            {
                int count = 0;
                foreach (List<StableObjectBinding> bindings in _bindingsByOwner.Values) count += bindings.Count;
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
            if (_bindingsByOwner.ContainsKey(owner))
            {
                diagnostic = $"stable-object-binding-owner-already-registered: owner='{owner.StableText}' must be released before another admission.";
                return false;
            }

            var bindings = new List<StableObjectBinding>();
            var seenRoots = new HashSet<GameObject>();
            var seenDeclarations = new HashSet<ObjectEntryDeclaration>();
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
                }
            }

            _bindingsByOwner.Add(owner, bindings);
            diagnostic = $"stable-object-bindings-registered: owner='{owner.StableText}' bindings='{bindings.Count}'.";
            return true;
        }

        internal bool TryRollbackOwner(RuntimeContentOwner owner, out string diagnostic) =>
            TryRemoveOwner(owner, "rollback", out diagnostic);

        internal bool TryReleaseOwner(RuntimeContentOwner owner, out string diagnostic) =>
            TryRemoveOwner(owner, "release", out diagnostic);

        internal bool TryCommitOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            if (!owner.IsValid || !_bindingsByOwner.ContainsKey(owner))
            {
                diagnostic = $"stable-object-binding-commit-owner-missing: owner='{owner.StableText}' has no prepared binding transaction.";
                return false;
            }

            _committedOwners.Add(owner);
            diagnostic = $"stable-object-binding-owner-committed: owner='{owner.StableText}'.";
            return true;
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
            var ownersToPrune = new List<RuntimeContentOwner>();
            foreach (KeyValuePair<RuntimeContentOwner, List<StableObjectBinding>> pair in _bindingsByOwner)
            {
                if (!_committedOwners.Contains(pair.Key)) continue;
                if (ownerSelector.HasValue && !pair.Key.OwnerIdentity.Equals(ownerSelector.Value)) continue;
                if (ownerDefinitionSelector.HasValue && !pair.Key.DefinitionToken.Equals(ownerDefinitionSelector.Value)) continue;
                List<StableObjectBinding> ownerBindings = pair.Value;
                for (int index = ownerBindings.Count - 1; index >= 0; index--)
                {
                    StableObjectBinding candidate = ownerBindings[index];
                    if (candidate.PhysicalObject == null)
                    {
                        ownerBindings.RemoveAt(index);
                        continue;
                    }
                    if (candidate.ObjectEntryId == objectEntryId) matches.Add(candidate);
                }
                if (ownerBindings.Count == 0) ownersToPrune.Add(pair.Key);
            }
            for (int index = 0; index < ownersToPrune.Count; index++)
            {
                _bindingsByOwner.Remove(ownersToPrune[index]);
                _committedOwners.Remove(ownersToPrune[index]);
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
            int removed = _bindingsByOwner.TryGetValue(owner, out List<StableObjectBinding> bindings) ? bindings.Count : 0;
            bool applied = _bindingsByOwner.Remove(owner);
            _committedOwners.Remove(owner);
            diagnostic = $"stable-object-binding-{operation}-completed: owner='{owner.StableText}' bindings='{removed}' applied='{applied}'.";
            return true;
        }

    }
}
