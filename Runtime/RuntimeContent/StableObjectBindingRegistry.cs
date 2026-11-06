using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Identity;
using UnityEngine;

namespace Immersive.Framework.RuntimeContent
{
    internal sealed class ObjectEntryCompositionRuntimeSlot
    {
        private ObjectEntryCompositionRuntimeBinding _runtime;
        private bool _invalidated;

        internal ObjectEntryCompositionRuntimeBinding Runtime => _runtime;
        internal bool IsInvalidated => _invalidated;

        internal bool TryBind(ObjectEntryCompositionRuntimeBinding runtime)
        {
            if (_invalidated || runtime == null) return false;
            if (_runtime == null)
            {
                _runtime = runtime;
                return true;
            }

            return ReferenceEquals(_runtime, runtime);
        }

        internal bool IsBoundTo(ObjectEntryCompositionRuntimeBinding runtime) =>
            !_invalidated && ReferenceEquals(_runtime, runtime);

        internal void Invalidate() => _invalidated = true;
    }

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
            HasAuthoredProfile = !ReferenceEquals(CompositionProfile, null);
            AllowsDynamicComposition = compositionAuthoring != null && compositionAuthoring.AllowsDynamicContributions;
            CompositionRuntimeSlot = compositionAuthoring != null ? new ObjectEntryCompositionRuntimeSlot() : null;
        }

        internal ObjectEntryId ObjectEntryId { get; }
        internal RuntimeContentOwner Owner { get; }
        internal GameObject PhysicalObject { get; }
        internal ObjectEntryDeclaration Declaration { get; }
        internal ObjectEntryCompositionAuthoring CompositionAuthoring { get; }
        internal ObjectEntryCompositionProfile CompositionProfile { get; }
        internal bool HasAuthoredProfile { get; }
        internal bool AllowsDynamicComposition { get; }
        internal ObjectEntryCompositionRuntimeSlot CompositionRuntimeSlot { get; set; }
        internal ObjectEntryCompositionAccessGrant CompositionAccessGrant { get; set; }
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
            internal TaskCompletionSource<string> ReleaseFlight { get; set; }
        }

        private readonly Dictionary<RuntimeContentOwner, OwnerBindings> _owners =
            new Dictionary<RuntimeContentOwner, OwnerBindings>();
        private bool _queryAccessInvalidated;
        private bool _dynamicCompositionWritesInvalidated;

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
                InvalidateCompositionAccessGrants(registration);
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

        internal void InvalidateDynamicCompositionWrites()
        {
            if (_dynamicCompositionWritesInvalidated) return;
            _dynamicCompositionWritesInvalidated = true;
            OwnerBindings[] registrations = _owners.Values.ToArray();

            foreach (OwnerBindings registration in registrations)
            {
                InvalidateCompositionAccessGrants(registration);
            }

            foreach (OwnerBindings registration in registrations)
            {
                for (int index = 0; index < registration.Bindings.Count; index++)
                {
                    registration.Bindings[index].CompositionRuntime?.BlockDynamicOperations();
                }
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

            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                StableObjectBinding binding = registration.Bindings[index];
                if (!binding.HasAuthoredProfile && binding.AllowsDynamicComposition &&
                    binding.CompositionRuntime != null && binding.CompositionRuntime.CleanupComplete &&
                    IsCompositionTargetCurrent(registration, binding))
                {
                    ResetCompositionContext(binding);
                    binding.CompositionRuntime = null;
                }
            }

            string profilelessGrantIssue = _dynamicCompositionWritesInvalidated
                ? string.Empty
                : PublishProfilelessCompositionAccessGrants(registration);
            if (!string.IsNullOrEmpty(profilelessGrantIssue))
            {
                return profilelessGrantIssue;
            }

            var activated = new List<StableObjectBinding>();
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                StableObjectBinding binding = registration.Bindings[index];
                if (binding.CompositionAuthoring == null) continue;
                if (binding.HasAuthoredProfile && binding.CompositionProfile == null)
                {
                    InvalidateCompositionAccessForBinding(binding);
                    string destroyedProfileIssue =
                        $"Object Entry Base Profile was destroyed after owner admission. owner='{owner.StableText}' ObjectEntryId='{binding.ObjectEntryId.StableText}'.";
                    return AppendDiagnostic(destroyedProfileIssue, await CloseActivatedCompositionsAsync(activated));
                }

                if (!IsCompositionTargetCurrent(registration, binding))
                {
                    string staleDiagnostic =
                        $"stable-object-composition-target-stale: ObjectEntryId='{binding.ObjectEntryId.StableText}' owner='{owner.StableText}' is no longer the exact committed occurrence.";
                    InvalidateCompositionAccessForBinding(binding);
                    binding.CompositionRuntime?.InvalidateWriteAccess();
                    string bindingCleanupIssue = binding.CompositionRuntime != null
                        ? await binding.CompositionRuntime.CloseAndReconcileAsync()
                        : string.Empty;
                    string otherCleanupIssue = await CloseActivatedCompositionsAsync(activated);
                    return AppendDiagnostic(
                        AppendDiagnostic(staleDiagnostic, bindingCleanupIssue),
                        otherCleanupIssue);
                }

                if (!binding.HasAuthoredProfile && binding.AllowsDynamicComposition)
                {
                    continue;
                }
                if (binding.CompositionRuntime?.IsActive == true) continue;

                if (binding.CompositionRuntime != null &&
                    !binding.CompositionRuntime.CleanupComplete)
                {
                    InvalidateCompositionAccessForBinding(binding);
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
                    if (binding.CompositionRuntime != null && binding.CompositionRuntime.CleanupComplete)
                    {
                        ResetCompositionContext(binding);
                        binding.CompositionRuntime = null;
                    }
                    if (binding.CompositionRuntime == null)
                    {
                        EnsureCompositionContextSlot(binding);
                        binding.CompositionRuntime = new ObjectEntryCompositionRuntimeBinding(
                            binding,
                            binding.CompositionProfile,
                            candidate => IsCompositionTargetCurrent(registration, candidate),
                            allowDynamicContributions: binding.AllowsDynamicComposition);
                    }

                    string activationIssue = await binding.CompositionRuntime.ActivateAsync();
                    if (string.IsNullOrEmpty(activationIssue))
                    {
                        activated.Add(binding);
                        continue;
                    }

                    InvalidateCompositionAccessForBinding(binding);
                    string cleanupIssue = await binding.CompositionRuntime.CloseAndReconcileAsync();
                    string diagnostic =
                        $"Object Entry composition activation failed after owner commit. owner='{owner.StableText}' ObjectEntryId='{binding.ObjectEntryId.StableText}'. {activationIssue}";
                    if (!string.IsNullOrEmpty(cleanupIssue))
                    {
                        diagnostic += " Cleanup remains unresolved. " + cleanupIssue;
                    }

                    string previousCleanupIssue = await CloseActivatedCompositionsAsync(activated);
                    return AppendDiagnostic(diagnostic, previousCleanupIssue);
                }
                catch (Exception exception)
                {
                    InvalidateCompositionAccessForBinding(binding);
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

            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                StableObjectBinding binding = registration.Bindings[index];
                if (!binding.HasAuthoredProfile || !binding.AllowsDynamicComposition) continue;
                if (!binding.CompositionAuthoring.AllowsDynamicContributions)
                {
                    InvalidateCompositionAccessForBinding(binding);
                    continue;
                }
                if (_dynamicCompositionWritesInvalidated)
                {
                    InvalidateCompositionAccessForBinding(binding);
                    continue;
                }
                if (!TryPublishCompositionAccessGrant(registration, binding, out string grantIssue))
                {
                    string cleanupIssue = await CloseActivatedCompositionsAsync(activated);
                    return AppendDiagnostic(grantIssue, cleanupIssue);
                }
            }

            return string.Empty;
        }
        internal bool IsCompositionTargetCurrent(StableObjectBinding binding)
        {
            return binding != null && _owners.TryGetValue(binding.Owner, out OwnerBindings registration) &&
                IsCompositionTargetCurrent(registration, binding);
        }

        internal bool TryOpenDynamicContribution(
            ObjectEntryCompositionAccessGrant grant,
            int priority,
            out ObjectEntryCompositionContribution contribution,
            out string diagnostic)
        {
            contribution = null;
            if (priority == int.MinValue)
            {
                diagnostic = "Dynamic Object Entry composition priority cannot use the reserved B2 Base priority.";
                return false;
            }
            if (!TryGetCurrentCompositionAccessGrant(
                    grant,
                    out OwnerBindings registration,
                    out StableObjectBinding binding,
                    out diagnostic))
            {
                return false;
            }
            if (!grant.TryGetBinding(out _, out _, out ObjectEntryCompositionRuntimeSlot contextSlot))
            {
                diagnostic = "Object Entry composition access grant has no exact context slot.";
                return false;
            }

            ObjectEntryCompositionRuntimeBinding runtime = binding.CompositionRuntime;
            bool createdRuntime = false;
            if (runtime == null)
            {
                if (binding.HasAuthoredProfile)
                {
                    diagnostic = "Dynamic Object Entry composition access is unavailable until the Base Profile activates.";
                    return false;
                }

                runtime = new ObjectEntryCompositionRuntimeBinding(
                    binding,
                    null,
                    candidate => IsCompositionTargetCurrent(registration, candidate),
                    allowDynamicContributions: true);
                createdRuntime = true;
            }

            if (contextSlot.Runtime != null && !contextSlot.IsBoundTo(runtime))
            {
                InvalidateCompositionAccessForBinding(binding);
                diagnostic = "Dynamic Object Entry composition context changed after this grant was issued.";
                return false;
            }

            if (!runtime.TryCreateDynamicContribution(
                    priority,
                    out Immersive.Composition.CompositionContribution composerContribution,
                    out diagnostic))
            {
                return false;
            }

            if (contextSlot.Runtime == null && !contextSlot.TryBind(runtime))
            {
                diagnostic = "Dynamic Object Entry composition context was invalidated while opening a contribution.";
                return false;
            }

            if (createdRuntime)
            {
                binding.CompositionRuntime = runtime;
            }

            contribution = new ObjectEntryCompositionContribution(
                grant,
                runtime,
                composerContribution,
                priority);
            diagnostic = string.Empty;
            return true;
        }

        internal async Task<ObjectEntryCompositionOperationResult> SetDynamicContributionPartAsync(
            ObjectEntryCompositionAccessGrant grant,
            ObjectEntryCompositionContribution contribution,
            ObjectEntryCompositionPartDefinition part,
            CancellationToken cancellationToken)
        {
            if (!TryGetDynamicContributionRuntime(
                    grant, contribution, out StableObjectBinding binding,
                    out ObjectEntryCompositionRuntimeBinding runtime, out bool staleHandle,
                    out string diagnostic))
            {
                return CreateOperationResult(
                    staleHandle ? ObjectEntryCompositionOperationStatus.Stale : ObjectEntryCompositionOperationStatus.Rejected,
                    runtime?.Revision ?? 0, diagnostic, false);
            }

            if (!runtime.TryCreateDynamicPartDefinition(part, out Immersive.Composition.PartDefinition definition, out diagnostic))
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Rejected, runtime.Revision, diagnostic, false);
            }

            Func<bool> isCurrent = () => IsDynamicContributionCurrent(grant, contribution, binding, runtime);
            if (!isCurrent())
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Stale, runtime.Revision,
                    "The exact Object Entry contribution grant or context became stale before mutation.", false);
            }

            try
            {
                Immersive.Composition.CompositionResult result = await runtime.SetDynamicPartAsync(
                    contribution.ComposerContribution, definition, isCurrent, cancellationToken);
                return ConvertOperationResult(result, runtime.Revision);
            }
            catch (OperationCanceledException)
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Cancelled, runtime.Revision,
                    "Object Entry composition mutation was cancelled before the kernel returned a result; effects may have applied.", true);
            }
            catch (Exception exception)
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Faulted, runtime.Revision,
                    $"Object Entry composition mutation failed. {exception.GetType().Name}: {exception.Message}", true);
            }
        }

        internal async Task<ObjectEntryCompositionOperationResult> RemoveDynamicContributionPartAsync(
            ObjectEntryCompositionAccessGrant grant,
            ObjectEntryCompositionContribution contribution,
            string keyText,
            CancellationToken cancellationToken)
        {
            if (!TryGetDynamicContributionRuntime(
                    grant, contribution, out StableObjectBinding binding,
                    out ObjectEntryCompositionRuntimeBinding runtime, out bool staleHandle,
                    out string diagnostic))
            {
                return CreateOperationResult(
                    staleHandle ? ObjectEntryCompositionOperationStatus.Stale : ObjectEntryCompositionOperationStatus.Rejected,
                    runtime?.Revision ?? 0, diagnostic, false);
            }

            if (!Immersive.Composition.ContentKey.TryCreate(keyText, out Immersive.Composition.ContentKey key))
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Rejected, runtime.Revision,
                    $"Dynamic Object Entry composition removal has invalid key '{keyText}'.", false);
            }

            Func<bool> isCurrent = () => IsDynamicContributionCurrent(grant, contribution, binding, runtime);
            if (!isCurrent())
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Stale, runtime.Revision,
                    "The exact Object Entry contribution grant or context became stale before removal.", false);
            }

            try
            {
                Immersive.Composition.CompositionResult result = await runtime.RemoveDynamicPartAsync(
                    contribution.ComposerContribution, key, isCurrent, cancellationToken);
                return ConvertOperationResult(result, runtime.Revision);
            }
            catch (OperationCanceledException)
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Cancelled, runtime.Revision,
                    "Object Entry composition removal was cancelled before the kernel returned a result; effects may have applied.", true);
            }
            catch (Exception exception)
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Faulted, runtime.Revision,
                    $"Object Entry composition removal failed. {exception.GetType().Name}: {exception.Message}", true);
            }
        }

        internal async Task<ObjectEntryCompositionOperationResult> CloseDynamicContributionAsync(
            ObjectEntryCompositionAccessGrant grant,
            ObjectEntryCompositionContribution contribution,
            CancellationToken cancellationToken)
        {
            if (!TryGetDynamicContributionRuntime(
                    grant, contribution, out StableObjectBinding binding,
                    out ObjectEntryCompositionRuntimeBinding runtime, out bool staleHandle,
                    out string diagnostic))
            {
                return CreateOperationResult(
                    staleHandle ? ObjectEntryCompositionOperationStatus.Stale : ObjectEntryCompositionOperationStatus.Rejected,
                    runtime?.Revision ?? 0, diagnostic, false);
            }

            Func<bool> isCurrent = () => IsDynamicContributionCurrent(grant, contribution, binding, runtime);
            if (!isCurrent())
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Stale, runtime.Revision,
                    "The exact Object Entry contribution grant or context became stale before close.", false);
            }

            try
            {
                Immersive.Composition.CompositionResult result = await runtime.CloseDynamicContributionAsync(
                    contribution.ComposerContribution, isCurrent, cancellationToken);
                return ConvertOperationResult(result, runtime.Revision);
            }
            catch (OperationCanceledException)
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Cancelled, runtime.Revision,
                    "Object Entry composition contribution close was cancelled before the kernel returned a result; effects may have applied.", true);
            }
            catch (Exception exception)
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Faulted, runtime.Revision,
                    $"Object Entry composition contribution close failed. {exception.GetType().Name}: {exception.Message}", true);
            }
        }

        private bool TryGetDynamicContributionRuntime(
            ObjectEntryCompositionAccessGrant grant,
            ObjectEntryCompositionContribution contribution,
            out StableObjectBinding binding,
            out ObjectEntryCompositionRuntimeBinding runtime,
            out bool staleHandle,
            out string diagnostic)
        {
            binding = null;
            runtime = contribution?.Runtime;
            staleHandle = true;
            if (!TryGetCurrentCompositionAccessGrant(grant, out _, out binding, out diagnostic))
                return false;

            staleHandle = false;
            if (contribution == null || !contribution.IsFrom(grant) || contribution.ComposerContribution == null)
            {
                diagnostic = "The contribution handle does not belong to this exact Object Entry access grant.";
                return false;
            }

            if (!grant.TryGetBinding(out _, out _, out ObjectEntryCompositionRuntimeSlot contextSlot) ||
                binding.CompositionRuntime == null ||
                !contextSlot.IsBoundTo(binding.CompositionRuntime) ||
                (grant.ContextAtIssue != null && !ReferenceEquals(grant.ContextAtIssue, binding.CompositionRuntime)) ||
                !contribution.IsFromRuntime(binding.CompositionRuntime))
            {
                staleHandle = true;
                diagnostic = "The contribution handle belongs to a previous Object Entry composition context.";
                return false;
            }

            runtime = binding.CompositionRuntime;
            diagnostic = string.Empty;
            return true;
        }

        private bool IsDynamicContributionCurrent(
            ObjectEntryCompositionAccessGrant grant,
            ObjectEntryCompositionContribution contribution,
            StableObjectBinding expectedBinding,
            ObjectEntryCompositionRuntimeBinding expectedRuntime)
        {
            return TryGetDynamicContributionRuntime(
                    grant, contribution, out StableObjectBinding currentBinding,
                    out ObjectEntryCompositionRuntimeBinding currentRuntime, out _, out _) &&
                ReferenceEquals(currentBinding, expectedBinding) &&
                ReferenceEquals(currentRuntime, expectedRuntime);
        }

        private static ObjectEntryCompositionOperationResult ConvertOperationResult(
            Immersive.Composition.CompositionResult result,
            long currentRevision)
        {
            if (result == null)
            {
                return CreateOperationResult(ObjectEntryCompositionOperationStatus.Stale, currentRevision,
                    "The exact Object Entry contribution became stale before the Composition kernel returned a result.", false);
            }

            ObjectEntryCompositionOperationStatus status = result.Status switch
            {
                Immersive.Composition.CompositionResultStatus.Committed => ObjectEntryCompositionOperationStatus.Committed,
                Immersive.Composition.CompositionResultStatus.NoChange => ObjectEntryCompositionOperationStatus.NoChange,
                Immersive.Composition.CompositionResultStatus.Rejected => ObjectEntryCompositionOperationStatus.Rejected,
                Immersive.Composition.CompositionResultStatus.Stale => ObjectEntryCompositionOperationStatus.Stale,
                Immersive.Composition.CompositionResultStatus.Cancelled => ObjectEntryCompositionOperationStatus.Cancelled,
                Immersive.Composition.CompositionResultStatus.Degraded => ObjectEntryCompositionOperationStatus.Degraded,
                _ => ObjectEntryCompositionOperationStatus.Faulted
            };
            string issue = result.Issues.Count == 0
                ? string.Empty
                : string.Join(" | ", result.Issues.Select(value => value.ToString()));
            return new ObjectEntryCompositionOperationResult(
                status,
                result.Revision,
                result.ChangedKeys.Select(key => key.Value),
                issue,
                result.EffectsMayHaveApplied);
        }

        private static ObjectEntryCompositionOperationResult CreateOperationResult(
            ObjectEntryCompositionOperationStatus status,
            long revision,
            string diagnostic,
            bool effectsMayHaveApplied) =>
            new ObjectEntryCompositionOperationResult(status, revision, null, diagnostic, effectsMayHaveApplied);
        internal Task<string> ReleaseOwnerAsync(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                return Task.FromResult(
                    $"stable-object-binding-release-invalid-owner: owner='{owner.StableText}' is invalid.");
            }
            if (!_owners.TryGetValue(owner, out OwnerBindings registration))
            {
                return Task.FromResult(string.Empty);
            }

            if (registration.ReleaseFlight != null)
            {
                return registration.ReleaseFlight.Task;
            }

            var completion = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            registration.ReleaseFlight = completion;
            registration.IsReleasing = true;
            registration.IsCommitted = false;
            _ = CompleteOwnerReleaseAsync(owner, registration, completion);
            return completion.Task;
        }

        private async Task CompleteOwnerReleaseAsync(
            RuntimeContentOwner owner,
            OwnerBindings registration,
            TaskCompletionSource<string> completion)
        {
            string result;
            try
            {
                InvalidateCompositionAccessGrants(registration);
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
                    result = $"stable-object-composition-release-pending: owner='{owner.StableText}' bindingsRetained='{registration.Bindings.Count}'. {string.Join(" | ", issues)}";
                }
                else
                {
                    if (_owners.TryGetValue(owner, out OwnerBindings currentRegistration) &&
                        ReferenceEquals(currentRegistration, registration))
                    {
                        _owners.Remove(owner);
                    }
                    result = string.Empty;
                }
            }
            catch (Exception exception)
            {
                result = $"stable-object-composition-release-pending: owner='{owner.StableText}' bindingsRetained='{registration.Bindings.Count}'. Release threw '{exception.GetType().Name}: {exception.Message}'.";
            }

            if (!string.IsNullOrEmpty(result) &&
                ReferenceEquals(registration.ReleaseFlight, completion))
            {
                registration.ReleaseFlight = null;
            }
            completion.TrySetResult(result);
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

        private async Task<string> CloseActivatedCompositionsAsync(
            IReadOnlyList<StableObjectBinding> activated)
        {
            var issues = new List<string>();
            for (int index = activated.Count - 1; index >= 0; index--)
            {
                StableObjectBinding binding = activated[index];
                try
                {
                    InvalidateCompositionAccessForBinding(binding);
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

                registration.IsReleasing = true;
                registration.IsCommitted = false;
                InvalidateCompositionAccessGrants(registration);
                for (int index = 0; index < registration.Bindings.Count; index++)
                {
                    registration.Bindings[index].CompositionRuntime?.InvalidateWriteAccess();
                }
                ReleaseQueryBindings(registration);
            }

            int removed = applied ? registration.Bindings.Count : 0;
            if (applied)
            {
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

            if (exactRecordCount != 1 || sameIdCount != 1 || binding.CompositionAuthoring == null ||
                binding.CompositionRuntimeSlot == null)
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
                ReferenceEquals(authoring.Profile, binding.CompositionProfile) &&
                declaration.TryGetObjectEntryId(out ObjectEntryId currentId) &&
                currentId == binding.ObjectEntryId;
        }

        private string PublishProfilelessCompositionAccessGrants(OwnerBindings registration)
        {
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                StableObjectBinding binding = registration.Bindings[index];
                if (binding.HasAuthoredProfile || !binding.AllowsDynamicComposition) continue;
                if (!IsCompositionTargetCurrent(registration, binding)) continue;
                if (!binding.CompositionAuthoring.AllowsDynamicContributions)
                {
                    InvalidateCompositionAccessForBinding(binding);
                    continue;
                }
                if (!TryPublishCompositionAccessGrant(registration, binding, out string issue))
                {
                    return issue;
                }
            }

            return string.Empty;
        }

        private bool TryPublishCompositionAccessGrant(
            OwnerBindings registration,
            StableObjectBinding binding,
            out string diagnostic)
        {
            if (_dynamicCompositionWritesInvalidated)
            {
                diagnostic = "Object Entry dynamic composition access cannot be published after Framework Host stop invalidated dynamic writes.";
                return false;
            }
            if (!binding.AllowsDynamicComposition || binding.CompositionAuthoring == null)
            {
                diagnostic = "Object Entry composition access requires explicit dynamic-contribution opt-in.";
                return false;
            }
            if (!binding.CompositionAuthoring.AllowsDynamicContributions)
            {
                InvalidateCompositionAccessForBinding(binding);
                diagnostic = "Object Entry composition access was revoked because dynamic contributions were disabled after admission.";
                return false;
            }
            if (binding.HasAuthoredProfile && binding.CompositionProfile == null)
            {
                InvalidateCompositionAccessForBinding(binding);
                diagnostic = "Object Entry composition access cannot be published because its authored Base Profile was destroyed.";
                return false;
            }
            if (binding.HasAuthoredProfile && binding.CompositionRuntime?.IsActive != true)
            {
                diagnostic = "Object Entry composition access cannot be published before the Base Profile activates.";
                return false;
            }
            if (!IsCompositionTargetCurrent(registration, binding))
            {
                InvalidateCompositionAccessForBinding(binding);
                diagnostic = $"Object Entry composition access target is stale for ObjectEntryId='{binding.ObjectEntryId.StableText}'.";
                return false;
            }

            EnsureCompositionContextSlot(binding);
            ObjectEntryCompositionRuntimeSlot contextSlot = binding.CompositionRuntimeSlot;
            if (binding.CompositionRuntime != null)
            {
                if (binding.CompositionRuntime.CleanupComplete)
                {
                    diagnostic = "Object Entry composition access cannot be published for a closed context.";
                    return false;
                }
                if (!contextSlot.TryBind(binding.CompositionRuntime))
                {
                    diagnostic = "Object Entry composition access context slot is already bound to another runtime context.";
                    return false;
                }
            }

            ObjectEntryCompositionAccessGrant grant = binding.CompositionAccessGrant;
            if (grant != null &&
                grant.IsBoundTo(this, binding, contextSlot) &&
                (grant.ContextAtIssue == null || ReferenceEquals(grant.ContextAtIssue, contextSlot.Runtime)) &&
                binding.CompositionAuthoring.IsBoundToCompositionAccessGrant(grant))
            {
                diagnostic = string.Empty;
                return true;
            }

            InvalidateCompositionAccessForBinding(binding);
            EnsureCompositionContextSlot(binding);
            contextSlot = binding.CompositionRuntimeSlot;
            if (binding.CompositionRuntime != null &&
                !contextSlot.TryBind(binding.CompositionRuntime))
            {
                diagnostic = "Object Entry composition access context slot could not bind to the current runtime context.";
                return false;
            }

            grant = new ObjectEntryCompositionAccessGrant(
                this,
                binding,
                contextSlot,
                contextSlot.Runtime);
            if (!binding.CompositionAuthoring.TryBindCompositionAccessGrant(grant, out diagnostic))
            {
                grant.Invalidate();
                diagnostic = "Object Entry composition access handoff failed. " + diagnostic;
                return false;
            }

            binding.CompositionAccessGrant = grant;
            diagnostic = string.Empty;
            return true;
        }

        internal bool ValidateCompositionAccessGrant(
            ObjectEntryCompositionAccessGrant grant,
            out string diagnostic)
        {
            return TryGetCurrentCompositionAccessGrant(
                grant,
                out _,
                out _,
                out diagnostic);
        }

        private bool TryGetCurrentCompositionAccessGrant(
            ObjectEntryCompositionAccessGrant grant,
            out OwnerBindings registration,
            out StableObjectBinding binding,
            out string diagnostic)
        {
            registration = null;
            binding = null;
            if (grant == null || !grant.TryGetBinding(
                    out StableObjectBindingRegistry grantRegistry,
                    out StableObjectBinding grantBinding,
                    out ObjectEntryCompositionRuntimeSlot contextSlot) ||
                !ReferenceEquals(grantRegistry, this))
            {
                diagnostic = "Object Entry composition access grant is invalid or belongs to another Runtime Content registry.";
                return false;
            }

            if (!_owners.TryGetValue(grantBinding.Owner, out registration))
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = "Object Entry composition access grant is stale because its exact owner binding record is no longer registered.";
                return false;
            }
            if (!registration.IsCommitted || registration.IsReleasing)
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = "Object Entry composition access grant was invalidated when its owner began release or rollback.";
                registration = null;
                return false;
            }
            if (_queryAccessInvalidated)
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = "Object Entry composition access grant was invalidated by Framework host teardown.";
                registration = null;
                return false;
            }
            if (_dynamicCompositionWritesInvalidated)
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = "Object Entry composition access grant was invalidated when Framework Host stop blocked dynamic writes.";
                registration = null;
                return false;
            }

            bool bindingCurrent = IsCompositionTargetCurrent(registration, grantBinding);
            if (!bindingCurrent)
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = "Object Entry composition access grant is stale because its exact committed occurrence is no longer current.";
                registration = null;
                return false;
            }

            bool optInCurrent = grantBinding.CompositionAuthoring != null &&
                grantBinding.AllowsDynamicComposition &&
                grantBinding.CompositionAuthoring.AllowsDynamicContributions;
            bool contextCurrent =
                grant.IsBoundTo(this, grantBinding, contextSlot) &&
                ReferenceEquals(grantBinding.CompositionRuntimeSlot, contextSlot) &&
                !contextSlot.IsInvalidated &&
                (grant.ContextAtIssue == null || ReferenceEquals(grant.ContextAtIssue, contextSlot.Runtime)) &&
                (contextSlot.Runtime == null
                    ? grantBinding.CompositionRuntime == null
                    : contextSlot.IsBoundTo(grantBinding.CompositionRuntime));
            bool authoringHandoffCurrent =
                grantBinding.CompositionAuthoring != null &&
                ReferenceEquals(grantBinding.CompositionAccessGrant, grant) &&
                grantBinding.CompositionAuthoring.IsBoundToCompositionAccessGrant(grant);

            if (!optInCurrent || !contextCurrent || !authoringHandoffCurrent)
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = !optInCurrent
                    ? "Object Entry composition access grant was revoked because dynamic contributions are no longer enabled."
                    : "Object Entry composition access grant is stale because its exact committed occurrence or context slot is no longer current.";
                registration = null;
                return false;
            }

            if (grantBinding.HasAuthoredProfile && grantBinding.CompositionProfile == null)
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = "Object Entry composition access is unavailable because its authored Base Profile was destroyed.";
                registration = null;
                return false;
            }
            if (grantBinding.HasAuthoredProfile && grantBinding.CompositionRuntime?.IsActive != true)
            {
                InvalidateCompositionAccessGrant(grantBinding, grant);
                diagnostic = "Object Entry composition access is unavailable until the Base Profile activates.";
                registration = null;
                return false;
            }

            binding = grantBinding;
            diagnostic = string.Empty;
            return true;
        }

        private static void InvalidateCompositionAccessGrant(
            StableObjectBinding binding,
            ObjectEntryCompositionAccessGrant grant)
        {
            if (grant == null) return;
            grant.Invalidate();
            if (binding == null || !ReferenceEquals(binding.CompositionAccessGrant, grant)) return;

            binding.CompositionRuntimeSlot?.Invalidate();
            binding.CompositionAuthoring?.UnbindCompositionAccessGrant(grant);
            binding.CompositionAccessGrant = null;
        }
        private void InvalidateCompositionAccessGrants(OwnerBindings registration)
        {
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                InvalidateCompositionAccessForBinding(registration.Bindings[index]);
            }
        }

        private static void InvalidateCompositionAccessForBinding(StableObjectBinding binding)
        {
            if (binding == null) return;
            ObjectEntryCompositionAccessGrant grant = binding.CompositionAccessGrant;
            grant?.Invalidate();
            binding.CompositionRuntimeSlot?.Invalidate();
            if (grant != null)
            {
                binding.CompositionAuthoring?.UnbindCompositionAccessGrant(grant);
                binding.CompositionAccessGrant = null;
            }
        }

        private static void ResetCompositionContext(StableObjectBinding binding)
        {
            InvalidateCompositionAccessForBinding(binding);
            binding.CompositionRuntimeSlot = new ObjectEntryCompositionRuntimeSlot();
        }

        private static void EnsureCompositionContextSlot(StableObjectBinding binding)
        {
            if (binding.CompositionRuntimeSlot == null || binding.CompositionRuntimeSlot.IsInvalidated)
            {
                binding.CompositionRuntimeSlot = new ObjectEntryCompositionRuntimeSlot();
            }
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
