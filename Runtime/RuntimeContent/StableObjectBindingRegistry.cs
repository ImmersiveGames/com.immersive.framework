using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        internal StableObjectBinding(
            ObjectEntryId objectEntryId,
            RuntimeContentOwner owner,
            GameObject physicalObject,
            ObjectEntryDeclaration declaration,
            ObjectEntryCompositionAuthoring compositionAuthoring = null)
        {
            ObjectEntryId = objectEntryId;
            Owner = owner;
            PhysicalObject = physicalObject;
            Declaration = declaration;
            CompositionAuthoring = compositionAuthoring;
            CompositionProfile = compositionAuthoring != null ? compositionAuthoring.Profile : null;
        }

        internal ObjectEntryId ObjectEntryId { get; }
        internal RuntimeContentOwner Owner { get; }
        internal GameObject PhysicalObject { get; }
        internal ObjectEntryDeclaration Declaration { get; }
        internal ObjectEntryCompositionAuthoring CompositionAuthoring { get; }
        internal ObjectEntryCompositionProfile CompositionProfile { get; }
        internal ObjectEntryCompositionRuntimeBinding CompositionRuntime { get; set; }
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
            internal bool IsReleasing { get; set; }
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

        internal int CompositionBindingCount
        {
            get
            {
                int count = 0;
                foreach (OwnerBindings owner in _owners.Values)
                {
                    for (int index = 0; index < owner.Bindings.Count; index++)
                    {
                        if (owner.Bindings[index].CompositionRuntime != null) count++;
                    }
                }
                return count;
            }
        }

        internal int UnresolvedCompositionCleanupCount
        {
            get
            {
                int count = 0;
                foreach (OwnerBindings owner in _owners.Values)
                {
                    for (int index = 0; index < owner.Bindings.Count; index++)
                    {
                        ObjectEntryCompositionRuntimeBinding composition =
                            owner.Bindings[index].CompositionRuntime;
                        if (composition != null && !composition.CleanupComplete) count++;
                    }
                }
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
                        ObjectEntryCompositionAuthoring compositionAuthoring =
                            declaration.GetComponent<ObjectEntryCompositionAuthoring>();
                        if (compositionAuthoring != null &&
                            !compositionAuthoring.TryValidateConfiguration(out string compositionIssue))
                        {
                            diagnostic =
                                $"stable-object-binding-invalid-composition-authoring: owner='{owner.StableText}' ObjectEntryId='{objectEntryId.StableText}'. {compositionIssue}";
                            return false;
                        }

                        bindings.Add(new StableObjectBinding(
                            objectEntryId,
                            owner,
                            declaration.gameObject,
                            declaration,
                            compositionAuthoring));
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

            for (int index = 0; index < bindings.Count; index++)
            {
                StableObjectBinding candidate = bindings[index];
                if (candidate.CompositionAuthoring != null)
                {
                    int sameIdCount = 0;
                    for (int otherIndex = 0; otherIndex < bindings.Count; otherIndex++)
                    {
                        if (bindings[otherIndex].ObjectEntryId == candidate.ObjectEntryId) sameIdCount++;
                    }

                    if (sameIdCount > 1)
                    {
                        diagnostic =
                            $"stable-object-binding-ambiguous-composition-target: owner='{owner.StableText}' ObjectEntryId='{candidate.ObjectEntryId.StableText}' matched '{sameIdCount}' declarations under one owner.";
                        return false;
                    }
                }

                foreach (OwnerBindings existingOwner in _owners.Values)
                {
                    if (!existingOwner.IsCommitted || existingOwner.IsReleasing) continue;
                    for (int existingIndex = 0; existingIndex < existingOwner.Bindings.Count; existingIndex++)
                    {
                        StableObjectBinding existing = existingOwner.Bindings[existingIndex];
                        if (existing.ObjectEntryId == candidate.ObjectEntryId &&
                            ReferenceEquals(existing.PhysicalObject, candidate.PhysicalObject) &&
                            (candidate.CompositionAuthoring != null || existing.CompositionAuthoring != null))
                        {
                            diagnostic =
                                $"stable-object-binding-ambiguous-composition-target: ObjectEntryId='{candidate.ObjectEntryId.StableText}' and physical GameObject are already admitted by owner='{existing.Owner.StableText}'.";
                            return false;
                        }
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
            if (registration.IsReleasing)
            {
                diagnostic = $"stable-object-binding-commit-owner-releasing: owner='{owner.StableText}' is already releasing.";
                return false;
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
                registration.IsCommitted = false;
                registration.IsReleasing = true;
                for (int bindingIndex = 0; bindingIndex < registration.Bindings.Count; bindingIndex++)
                {
                    registration.Bindings[bindingIndex].CompositionRuntime?.InvalidateWriteAccess();
                }

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

        internal async Task<string> ActivateOwnerCompositionsAsync(RuntimeContentOwner owner)
        {
            if (!owner.IsValid || !_owners.TryGetValue(owner, out OwnerBindings registration))
            {
                return $"stable-object-composition-activation-owner-missing: owner='{owner.StableText}' has no prepared binding transaction.";
            }
            if (!registration.IsCommitted || registration.IsReleasing || _queryAccessInvalidated)
            {
                return $"stable-object-composition-activation-before-commit-or-after-invalidation: owner='{owner.StableText}' is not a committed writable occurrence.";
            }

            var activated = new List<StableObjectBinding>();
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                StableObjectBinding binding = registration.Bindings[index];
                ObjectEntryCompositionAuthoring authoring = binding.CompositionAuthoring;
                if (authoring == null) continue;
                if (!IsCompositionTargetCurrent(registration, binding))
                {
                    string staleDiagnostic =
                        $"stable-object-composition-target-stale: ObjectEntryId='{binding.ObjectEntryId.StableText}' owner='{owner.StableText}' is no longer the exact committed occurrence.";
                    binding.CompositionRuntime?.InvalidateWriteAccess();
                    string bindingCleanupIssue = binding.CompositionRuntime != null
                        ? await binding.CompositionRuntime.CloseAndReconcileAsync()
                        : string.Empty;
                    string otherCleanupIssue = await CloseActivatedCompositionsAsync(activated);
                    return AppendDiagnostic(
                        AppendDiagnostic(staleDiagnostic, bindingCleanupIssue),
                        otherCleanupIssue);
                }

                if (binding.CompositionRuntime?.IsActive == true) continue;

                if (binding.CompositionRuntime != null &&
                    !binding.CompositionRuntime.CleanupComplete)
                {
                    string pendingCleanupIssue =
                        await binding.CompositionRuntime.CloseAndReconcileAsync();
                    if (!string.IsNullOrEmpty(pendingCleanupIssue))
                    {
                        string diagnostic =
                            $"Object Entry composition recovery remains pending. owner='{owner.StableText}' ObjectEntryId='{binding.ObjectEntryId.StableText}'. {pendingCleanupIssue}";
                        return AppendDiagnostic(
                            diagnostic,
                            await CloseActivatedCompositionsAsync(activated));
                    }
                }

                try
                {
                    if (binding.CompositionRuntime == null || binding.CompositionRuntime.CleanupComplete)
                    {
                        binding.CompositionRuntime = new ObjectEntryCompositionRuntimeBinding(
                            binding,
                            binding.CompositionProfile,
                            candidate => IsCompositionTargetCurrent(registration, candidate));
                    }

                    string activationIssue = await binding.CompositionRuntime.ActivateAsync();
                    if (string.IsNullOrEmpty(activationIssue))
                    {
                        activated.Add(binding);
                        continue;
                    }

                    string cleanupIssue = await binding.CompositionRuntime.CloseAndReconcileAsync();
                    string diagnostic =
                        $"Object Entry composition activation failed after owner commit. owner='{owner.StableText}' ObjectEntryId='{binding.ObjectEntryId.StableText}'. {activationIssue}";
                    if (!string.IsNullOrEmpty(cleanupIssue))
                    {
                        diagnostic += " Cleanup remains unresolved. " + cleanupIssue;
                    }

                    for (int activatedIndex = activated.Count - 1; activatedIndex >= 0; activatedIndex--)
                    {
                        StableObjectBinding previouslyActivated = activated[activatedIndex];
                        string previousCleanupIssue =
                            await previouslyActivated.CompositionRuntime.CloseAndReconcileAsync();
                        if (!string.IsNullOrEmpty(previousCleanupIssue))
                        {
                            diagnostic +=
                                $" Cleanup of previously activated ObjectEntryId='{previouslyActivated.ObjectEntryId.StableText}' remains unresolved. {previousCleanupIssue}";
                        }
                    }
                    return diagnostic;
                }
                catch (Exception exception)
                {
                    binding.CompositionRuntime?.InvalidateWriteAccess();
                    string cleanupIssue = binding.CompositionRuntime != null
                        ? await binding.CompositionRuntime.CloseAndReconcileAsync()
                        : string.Empty;
                    string previousCleanupIssue = await CloseActivatedCompositionsAsync(activated);
                    string diagnostic =
                        $"Object Entry composition activation threw after owner commit. owner='{owner.StableText}' ObjectEntryId='{binding.ObjectEntryId.StableText}' exception='{exception.GetType().Name}: {exception.Message}'. {cleanupIssue}";
                    return AppendDiagnostic(diagnostic, previousCleanupIssue);
                }
            }

            return string.Empty;
        }

        internal bool IsCompositionTargetCurrent(StableObjectBinding binding)
        {
            return binding != null && _owners.TryGetValue(binding.Owner, out OwnerBindings registration) &&
                IsCompositionTargetCurrent(registration, binding);
        }

        internal async Task<string> ReleaseOwnerAsync(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                return $"stable-object-binding-release-invalid-owner: owner='{owner.StableText}' is invalid.";
            }
            if (!_owners.TryGetValue(owner, out OwnerBindings registration))
            {
                return string.Empty;
            }

            registration.IsReleasing = true;
            registration.IsCommitted = false;
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                registration.Bindings[index].CompositionRuntime?.InvalidateWriteAccess();
            }
            ReleaseQueryBindings(registration);

            var issues = new List<string>();
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                StableObjectBinding binding = registration.Bindings[index];
                if (binding.CompositionRuntime == null) continue;
                string issue = await binding.CompositionRuntime.CloseAndReconcileAsync();
                if (!string.IsNullOrEmpty(issue))
                {
                    issues.Add($"ObjectEntryId='{binding.ObjectEntryId.StableText}': {issue}");
                }
            }

            if (issues.Count > 0)
            {
                return $"stable-object-composition-release-pending: owner='{owner.StableText}' bindingsRetained='{registration.Bindings.Count}'. {string.Join(" | ", issues)}";
            }

            _owners.Remove(owner);
            return string.Empty;
        }

        internal async Task<string> ReleaseNonCurrentOwnerBindingsAsync(
            RuntimeContentScope scope,
            RuntimeContentOwner? retainedOwner)
        {
            RuntimeContentOwner[] pendingOwners = _owners.Values
                .Where(registration => registration.Owner.Scope == scope &&
                    (!retainedOwner.HasValue || !registration.Owner.Equals(retainedOwner.Value)) &&
                    registration.Bindings.Any(binding =>
                        binding.CompositionAuthoring != null ||
                        binding.CompositionRuntime != null))
                .Select(registration => registration.Owner)
                .ToArray();
            var issues = new List<string>();
            for (int index = 0; index < pendingOwners.Length; index++)
            {
                string issue = await ReleaseOwnerAsync(pendingOwners[index]);
                if (!string.IsNullOrEmpty(issue))
                {
                    issues.Add(issue);
                }
            }

            return issues.Count == 0
                ? string.Empty
                : $"stable-object-binding-pending-owner-release: scope='{scope}' owners='{pendingOwners.Length}'. {string.Join(" | ", issues)}";
        }

        private static async Task<string> CloseActivatedCompositionsAsync(
            IReadOnlyList<StableObjectBinding> activated)
        {
            var issues = new List<string>();
            for (int index = activated.Count - 1; index >= 0; index--)
            {
                StableObjectBinding binding = activated[index];
                try
                {
                    string issue = await binding.CompositionRuntime.CloseAndReconcileAsync();
                    if (!string.IsNullOrEmpty(issue))
                    {
                        issues.Add(
                            $"ObjectEntryId='{binding.ObjectEntryId.StableText}' cleanup remains unresolved. {issue}");
                    }
                }
                catch (Exception exception)
                {
                    issues.Add(
                        $"ObjectEntryId='{binding.ObjectEntryId.StableText}' cleanup threw '{exception.GetType().Name}: {exception.Message}'.");
                }
            }

            return issues.Count == 0
                ? string.Empty
                : "Previously activated Object Entry compositions could not all be rolled back. " + string.Join(" | ", issues);
        }

        private static string AppendDiagnostic(string current, string next)
        {
            return string.IsNullOrEmpty(next)
                ? current
                : string.IsNullOrEmpty(current) ? next : current + " " + next;
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

        internal bool TryRecognizeOccurrence(
            ObjectEntryId objectEntryId,
            GameObject candidateRoot,
            out StableObjectBinding binding,
            out StableObjectBindingResolutionStatus status,
            out string diagnostic)
        {
            binding = null;
            if (!objectEntryId.IsValid || candidateRoot == null)
            {
                status = StableObjectBindingResolutionStatus.InvalidRequest;
                diagnostic = "stable-object-binding-invalid-recognition: a valid ObjectEntryId and physical root are required.";
                return false;
            }

            int matchingOwnerCount = 0;
            bool duplicateRecordsUnderMatchingOwner = false;
            StableObjectBinding selected = null;

            foreach (OwnerBindings registration in _owners.Values)
            {
                if (!registration.IsCommitted) continue;

                int liveRecordsForId = 0;
                int candidateRootMatches = 0;
                StableObjectBinding ownerMatch = null;

                for (int index = 0; index < registration.Bindings.Count; index++)
                {
                    StableObjectBinding candidate = registration.Bindings[index];
                    if (candidate.ObjectEntryId != objectEntryId) continue;

                    GameObject physicalObject = candidate.PhysicalObject;
                    if (physicalObject == null) continue;
                    liveRecordsForId++;

                    if (physicalObject != candidateRoot) continue;
                    candidateRootMatches++;
                    ownerMatch = candidate;
                }

                if (candidateRootMatches == 0) continue;

                matchingOwnerCount++;
                selected = ownerMatch;
                if (liveRecordsForId > 1 || candidateRootMatches > 1)
                    duplicateRecordsUnderMatchingOwner = true;
            }

            if (matchingOwnerCount == 0)
            {
                status = StableObjectBindingResolutionStatus.NotFound;
                diagnostic = $"stable-object-binding-recognition-unavailable: no committed binding for ObjectEntryId='{objectEntryId.StableText}' matches the exact declaration GameObject.";
                return false;
            }

            if (duplicateRecordsUnderMatchingOwner || matchingOwnerCount > 1)
            {
                status = StableObjectBindingResolutionStatus.Ambiguous;
                diagnostic = $"stable-object-binding-recognition-ambiguous: ObjectEntryId='{objectEntryId.StableText}' has duplicate records under an exact owner or matches multiple owners for the supplied GameObject.";
                return false;
            }

            binding = selected;
            status = StableObjectBindingResolutionStatus.Resolved;
            diagnostic = $"stable-object-binding-recognized: ObjectEntryId='{objectEntryId.StableText}' matches the exact committed declaration GameObject.";
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
            if (applied)
            {
                for (int index = 0; index < registration.Bindings.Count; index++)
                {
                    ObjectEntryCompositionRuntimeBinding composition =
                        registration.Bindings[index].CompositionRuntime;
                    if (composition != null && !composition.CleanupComplete)
                    {
                        diagnostic =
                            $"stable-object-binding-{operation}-requires-async-composition-release: owner='{owner.StableText}' must complete its owner-scoped composition barrier before binding removal.";
                        return false;
                    }
                }
            }
            int removed = applied ? registration.Bindings.Count : 0;
            if (applied)
            {
                ReleaseQueryBindings(registration);
                _owners.Remove(owner);
            }
            diagnostic = $"stable-object-binding-{operation}-completed: owner='{owner.StableText}' bindings='{removed}' applied='{applied}'.";
            return true;
        }

        private bool IsCompositionTargetCurrent(
            OwnerBindings registration,
            StableObjectBinding binding)
        {
            if (registration == null || binding == null || _queryAccessInvalidated ||
                !registration.IsCommitted || registration.IsReleasing ||
                !registration.Owner.Equals(binding.Owner) ||
                !_owners.TryGetValue(registration.Owner, out OwnerBindings currentRegistration) ||
                !ReferenceEquals(currentRegistration, registration) ||
                binding.PhysicalObject == null)
            {
                return false;
            }

            int exactRecordCount = 0;
            int sameIdCount = 0;
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                StableObjectBinding candidate = registration.Bindings[index];
                if (ReferenceEquals(candidate, binding)) exactRecordCount++;
                if (candidate.ObjectEntryId == binding.ObjectEntryId) sameIdCount++;
            }

            if (exactRecordCount != 1 || sameIdCount != 1 || binding.CompositionAuthoring == null)
            {
                return false;
            }

            int matchingPhysicalBindingCount = 0;
            foreach (OwnerBindings candidateOwner in _owners.Values)
            {
                if (!candidateOwner.IsCommitted || candidateOwner.IsReleasing) continue;
                for (int index = 0; index < candidateOwner.Bindings.Count; index++)
                {
                    StableObjectBinding candidate = candidateOwner.Bindings[index];
                    if (candidate.ObjectEntryId == binding.ObjectEntryId &&
                        ReferenceEquals(candidate.PhysicalObject, binding.PhysicalObject))
                    {
                        matchingPhysicalBindingCount++;
                    }
                }
            }
            if (matchingPhysicalBindingCount != 1)
            {
                return false;
            }

            ObjectEntryDeclaration declaration = binding.PhysicalObject.GetComponent<ObjectEntryDeclaration>();
            ObjectEntryCompositionAuthoring authoring =
                binding.PhysicalObject.GetComponent<ObjectEntryCompositionAuthoring>();
            return declaration != null && ReferenceEquals(declaration, binding.Declaration) &&
                ReferenceEquals(authoring, binding.CompositionAuthoring) &&
                authoring.Profile == binding.CompositionProfile &&
                declaration.TryGetObjectEntryId(out ObjectEntryId currentId) &&
                currentId == binding.ObjectEntryId;
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
