using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Immersive.Composition;
using Immersive.Framework.ObjectEntry;
using UnityEngine;

namespace Immersive.Framework.RuntimeContent
{
    internal sealed class ObjectEntryCompositionPartMaterializer : IPartMaterializer
    {
        internal sealed class PartBinding
        {
            internal PartBinding(
                StableObjectBinding target,
                GameObject prefab,
                GameObject adoptedInstance = null)
            {
                Target = target ?? throw new ArgumentNullException(nameof(target));
                Prefab = prefab;
                AdoptedInstance = adoptedInstance;
            }

            internal StableObjectBinding Target { get; }
            internal GameObject Prefab { get; }
            internal GameObject AdoptedInstance { get; }
            internal bool IsAdopted => !object.ReferenceEquals(AdoptedInstance, null);
        }

        private sealed class AcquiredInstance
        {
            internal AcquiredInstance(GameObject instance, GameObject protectedRoot)
            {
                Instance = instance;
                ProtectedRoot = protectedRoot;
            }

            internal GameObject Instance { get; }
            internal GameObject ProtectedRoot { get; }
        }

        private sealed class ProtectedRootCloseFailureEvidence
        {
            internal ProtectedRootCloseFailureEvidence(
                CompositionContextId contextId,
                CompositionOperationId operationId,
                ContentKey key)
            {
                ContextId = contextId;
                OperationId = operationId;
                Key = key;
            }

            internal CompositionContextId ContextId { get; }
            internal CompositionOperationId OperationId { get; }
            internal ContentKey Key { get; }
        }

        private sealed class PartProvenance
        {
            internal PartProvenance(
                PartBinding binding,
                GameObject root,
                GameObject instance,
                Transform originalParent,
                bool originalActive)
            {
                Binding = binding;
                Root = root;
                Instance = instance;
                OriginalParent = originalParent;
                OriginalActive = originalActive;
            }

            internal PartBinding Binding { get; }
            internal GameObject Root { get; }
            internal GameObject Instance { get; }
            internal Transform OriginalParent { get; }
            internal bool OriginalActive { get; }
        }

        private sealed class ConsumerOperationScope
        {
            private int _isActive = 1;

            internal ConsumerOperationScope(StableObjectBinding target, Func<bool> isCurrent)
            {
                Target = target;
                IsCurrent = isCurrent;
            }

            internal StableObjectBinding Target { get; }
            internal Func<bool> IsCurrent { get; }
            internal bool IsActive => Volatile.Read(ref _isActive) != 0;
            internal void Complete() => Interlocked.Exchange(ref _isActive, 0);
        }

        private readonly Func<StableObjectBinding, bool> _isTargetCurrent;
        private readonly Func<GameObject, Task<bool>> _destroyAndConfirmAsync;
        private readonly List<AcquiredInstance> _acquiredInstances = new List<AcquiredInstance>();
        private readonly AsyncLocal<ConsumerOperationScope> _consumerOperation = new AsyncLocal<ConsumerOperationScope>();
        private ProtectedRootCloseFailureEvidence _protectedRootCloseFailure;

        internal ObjectEntryCompositionPartMaterializer(
            Func<StableObjectBinding, bool> isTargetCurrent,
            Func<GameObject, Task<bool>> destroyAndConfirmAsync = null)
        {
            _isTargetCurrent = isTargetCurrent ?? throw new ArgumentNullException(nameof(isTargetCurrent));
            _destroyAndConfirmAsync = destroyAndConfirmAsync ?? DestroyAndConfirmWithUnityAsync;
        }

        internal async Task<CompositionResult> ExecuteConsumerOperationAsync(
            StableObjectBinding target,
            Func<bool> isCurrent,
            Func<Task<CompositionResult>> operation)
        {
            if (target == null || isCurrent == null || operation == null)
                return null;

            ConsumerOperationScope previous = _consumerOperation.Value;
            var scope = new ConsumerOperationScope(target, isCurrent);
            if (!IsConsumerScopeCurrent(scope))
                return null;

            _consumerOperation.Value = scope;
            try
            {
                return await operation();
            }
            finally
            {
                scope.Complete();
                _consumerOperation.Value = previous;
            }
        }

        internal bool HasOutstandingAcquiredInstances
        {
            get
            {
                RemoveDestroyedInstances();
                return _acquiredInstances.Count > 0;
            }
        }

        public async Task<PartPreparationResult> PrepareAsync(
            PartDefinition definition,
            PartMaterializationContext context,
            CancellationToken cancellationToken)
        {
            if (definition?.Descriptor is not PartBinding binding ||
                binding.Target.PhysicalObject == null ||
                !IsTargetCurrent(binding.Target, context))
            {
                return PartPreparationResult.Failed(
                    "Object Entry composition preparation requires the exact current admitted binding and its declared part.");
            }

            GameObject root = binding.Target.PhysicalObject;
            if (binding.IsAdopted)
            {
                GameObject adopted = binding.AdoptedInstance;
                if (adopted == null || adopted == root || !adopted.transform.IsChildOf(root.transform))
                {
                    return PartPreparationResult.Failed(
                        $"Adopted Object Entry part '{definition.Key.Value}' must be a live subordinate of its exact declaration root.");
                }

                var adoptedProvenance = new PartProvenance(
                    binding,
                    root,
                    adopted,
                    adopted.transform.parent,
                    adopted.activeSelf);
                return PartPreparationResult.Prepared(
                    new PartMaterialization(adopted, PartOwnership.Adopted, adoptedProvenance));
            }

            if (binding.Prefab == null)
            {
                return PartPreparationResult.Failed(
                    $"Object Entry composition prefab for key '{definition.Key.Value}' is missing.");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            GameObject stagingRoot = null;
            GameObject instance = null;
            bool authoredActiveSelf = binding.Prefab.activeSelf;
            try
            {
                stagingRoot = new GameObject("Object Entry Composition Preparation");
                TrackAcquired(stagingRoot, root);
                stagingRoot.SetActive(false);
                if (!IsTargetCurrent(binding.Target, context))
                {
                    await CleanupPreparationAsync(stagingRoot, instance);
                    return PartPreparationResult.Failed("Object Entry binding became stale during staging-root preparation.");
                }
                stagingRoot.transform.SetParent(root.transform, false);
                if (!IsTargetCurrent(binding.Target, context) ||
                    !IsSafePreparationHierarchy(root, stagingRoot, null, null))
                {
                    await CleanupPreparationAsync(stagingRoot, instance);
                    return PartPreparationResult.Failed("Object Entry binding or protected-root hierarchy changed while parenting the staging root.");
                }
                instance = UnityEngine.Object.Instantiate(
                    binding.Prefab,
                    stagingRoot.transform,
                    false);
                if (instance == null)
                {
                    await ReleaseAcquiredAsync(stagingRoot);
                    stagingRoot = null;
                    return PartPreparationResult.Failed(
                        $"Object Entry composition prefab for key '{definition.Key.Value}' returned null.");
                }

                TrackAcquired(instance, root);
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                if (!IsTargetCurrent(binding.Target, context) ||
                    !IsSafePreparationHierarchy(root, stagingRoot, instance, stagingRoot.transform))
                {
                    await CleanupPreparationAsync(stagingRoot, instance);
                    return PartPreparationResult.Failed($"Object Entry binding or protected-root hierarchy changed while instantiating part '{definition.Key.Value}'.");
                }
                instance.SetActive(false);
                if (!IsTargetCurrent(binding.Target, context) ||
                    !IsSafePreparationHierarchy(root, stagingRoot, instance, stagingRoot.transform))
                {
                    await CleanupPreparationAsync(stagingRoot, instance);
                    return PartPreparationResult.Failed($"Object Entry binding or protected-root hierarchy changed while disabling part '{definition.Key.Value}'.");
                }
                instance.transform.SetParent(root.transform, false);
                if (!IsTargetCurrent(binding.Target, context) ||
                    !IsSafePreparationHierarchy(root, stagingRoot, instance, root.transform))
                {
                    await CleanupPreparationAsync(stagingRoot, instance);
                    return PartPreparationResult.Failed($"Object Entry binding or protected-root hierarchy changed while parenting part '{definition.Key.Value}'.");
                }
                instance.name = binding.Prefab.name;

                if (!IsSafePreparationHierarchy(root, stagingRoot, instance, root.transform))
                {
                    await CleanupPreparationAsync(stagingRoot, instance);
                    return PartPreparationResult.Failed($"The protected-root hierarchy changed before staging cleanup for part '{definition.Key.Value}'.");
                }
                if (!await DestroyAndConfirmAsync(stagingRoot, root))
                {
                    await CleanupPreparationAsync(stagingRoot, instance);
                    return PartPreparationResult.Failed(
                        $"Temporary staging root for Object Entry part '{definition.Key.Value}' could not be confirmed destroyed.");
                }

                RemoveAcquired(stagingRoot);
                stagingRoot = null;
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                if (!IsTargetCurrent(binding.Target, context) ||
                    !IsExactSubordinate(root, instance) || ContainsProtectedRoot(instance, root))
                {
                    await ReleaseAcquiredAsync(instance);
                    return PartPreparationResult.Failed(
                        $"Object Entry binding became stale while preparing part '{definition.Key.Value}'.");
                }

                var provenance = new PartProvenance(binding, root, instance, null, authoredActiveSelf);
                return PartPreparationResult.Prepared(
                    new PartMaterialization(instance, PartOwnership.Acquired, provenance));
            }
            catch (OperationCanceledException)
            {
                if (instance != null)
                    await ReleaseAcquiredAsync(instance);
                if (stagingRoot != null)
                {
                    try
                    {
                        await ReleaseAcquiredAsync(stagingRoot);
                    }
                    catch
                    {
                        // Keep the cancellation outcome; Framework owner cleanup retains unresolved acquisitions.
                    }
                }

                throw;
            }
            catch (Exception exception)
            {
                if (instance != null)
                {
                    await ReleaseAcquiredAsync(instance);
                }
                if (stagingRoot != null)
                {
                    try
                    {
                        await ReleaseAcquiredAsync(stagingRoot);
                    }
                    catch
                    {
                        // Preserve the materialization diagnostic; the staging root is empty or scene-owned.
                    }
                }

                return PartPreparationResult.Failed(
                    $"Object Entry composition materialization failed. {exception.GetType().Name}: {exception.Message}");
            }
        }

        public PartEffectResult Apply(
            PartMaterialization candidate,
            PartMaterialization previous,
            PartMaterializationContext context)
        {
            if (!TryGetProvenance(candidate, out PartProvenance provenance) ||
                provenance.Root == null ||
                !IsTargetCurrent(provenance.Binding.Target, context) ||
                !IsExactSubordinate(provenance.Root, provenance.Instance))
            {
                return PartEffectResult.Failed(
                    "Object Entry composition Apply requires the exact prepared subordinate while its binding is current.",
                    false);
            }

            try
            {
                if (candidate.Ownership == PartOwnership.Acquired)
                {
                    provenance.Instance.SetActive(provenance.OriginalActive);
                }

                if (provenance.Root == null ||
                    !IsTargetCurrent(provenance.Binding.Target, context) ||
                    !IsExactSubordinate(provenance.Root, provenance.Instance))
                {
                    return PartEffectResult.Failed(
                        "Object Entry composition Apply changed or lost the exact subordinate after activation callbacks.",
                        true);
                }
                return PartEffectResult.Applied();
            }
            catch (Exception exception)
            {
                return PartEffectResult.Failed(
                    $"Object Entry composition Apply failed. {exception.GetType().Name}: {exception.Message}",
                    true);
            }
        }

        public PartEffectResult Detach(
            PartMaterialization current,
            PartMaterializationContext context)
        {
            if (!TryGetProvenance(current, out PartProvenance provenance) ||
                object.ReferenceEquals(provenance.Instance, provenance.Root))
            {
                return PartEffectResult.Failed(
                    "Object Entry composition Detach requires the exact subordinate materialization.",
                    false);
            }

            if (ContainsProtectedRoot(provenance.Instance, provenance.Root))
            {
                if (context.IsClosingContext && current.Ownership == PartOwnership.Acquired)
                {
                    _protectedRootCloseFailure = new ProtectedRootCloseFailureEvidence(
                        context.ContextId,
                        context.OperationId,
                        context.Key);
                }

                return PartEffectResult.Failed(
                    "Object Entry composition Detach refused because the acquired part contains its protected declaration root.",
                    false);
            }

            if (!IsTargetCurrent(provenance.Binding.Target, context))
                return PartEffectResult.Failed("Object Entry composition Detach requires the exact current consumer grant.", false);

            if (current.Ownership == PartOwnership.Adopted && context.IsClosingContext)
            {
                return PartEffectResult.Applied();
            }

            try
            {
                if (current.Ownership == PartOwnership.Adopted)
                {
                    provenance.Instance.transform.SetParent(provenance.OriginalParent, false);
                    if (!IsTargetCurrent(provenance.Binding.Target, context))
                        return PartEffectResult.Failed("Object Entry composition Detach was invalidated by a parent-change callback.", true);
                    provenance.Instance.SetActive(provenance.OriginalActive);
                    if (!IsTargetCurrent(provenance.Binding.Target, context))
                        return PartEffectResult.Failed("Object Entry composition Detach was invalidated by an activation callback.", true);
                }
                else
                {
                    provenance.Instance.SetActive(false);
                    if (!IsTargetCurrent(provenance.Binding.Target, context))
                        return PartEffectResult.Failed("Object Entry composition Detach was invalidated by an activation callback.", true);
                    if (ContainsProtectedRoot(provenance.Instance, provenance.Root))
                    {
                        return PartEffectResult.Failed(
                            "Object Entry composition Detach activation callbacks placed the declaration root under its acquired part.",
                            true);
                    }

                    if (provenance.Instance.transform.parent != null)
                    {
                        provenance.Instance.transform.SetParent(null, true);
                        if (!IsTargetCurrent(provenance.Binding.Target, context))
                            return PartEffectResult.Failed("Object Entry composition Detach was invalidated by a parent-change callback.", true);
                        if (ContainsProtectedRoot(provenance.Instance, provenance.Root))
                        {
                            return PartEffectResult.Failed(
                                "Object Entry composition Detach callbacks placed the declaration root under its acquired part.",
                                true);
                        }
                    }
                }

                return PartEffectResult.Applied();
            }
            catch (Exception exception)
            {
                return PartEffectResult.Failed(
                    $"Object Entry composition Detach failed. {exception.GetType().Name}: {exception.Message}",
                    true);
            }
        }

        public async Task<PartReleaseResult> ReleaseAsync(
            PartMaterialization materialization,
            PartMaterializationContext context,
            CancellationToken cancellationToken)
        {
            if (!TryGetProvenance(materialization, out PartProvenance provenance) ||
                object.ReferenceEquals(provenance.Instance, provenance.Root))
            {
                return PartReleaseResult.Failed(
                    "Object Entry composition Release requires the exact subordinate materialization.");
            }

            if (ContainsProtectedRoot(provenance.Instance, provenance.Root))
            {
                return PartReleaseResult.Failed(
                    "Object Entry composition Release preserved an acquired part that contains its declaration root.");
            }

            if (materialization.Ownership == PartOwnership.Adopted)
            {
                return PartReleaseResult.Retained();
            }

            try
            {
                return await ReleaseAcquiredAsync(provenance.Instance)
                    ? PartReleaseResult.Released()
                    : PartReleaseResult.Failed(
                        $"Unity destruction of acquired Object Entry part '{context.Key.Value}' has not been confirmed.");
            }
            catch (Exception exception)
            {
                return PartReleaseResult.Failed(
                    $"Object Entry composition release failed. {exception.GetType().Name}: {exception.Message}");
            }
        }

        internal bool ConfirmsProtectedRootCloseFailure(CompositionResult result)
        {
            ProtectedRootCloseFailureEvidence evidence = _protectedRootCloseFailure;
            return evidence != null &&
                ObjectEntryCompositionRuntimeBinding.IsRecoverableCloseFailure(
                    result,
                    evidence.ContextId,
                    true) &&
                result.OperationId == evidence.OperationId &&
                result.Issues[0].Key == evidence.Key;
        }

        internal async Task<string> ReconcileOutstandingAcquiredInstancesAsync()
        {
            RemoveDestroyedInstances();
            AcquiredInstance[] pending = _acquiredInstances.ToArray();
            for (int index = 0; index < pending.Length; index++)
            {
                AcquiredInstance acquired = pending[index];
                GameObject instance = acquired.Instance;
                if (instance != null && ContainsProtectedRoot(instance, acquired.ProtectedRoot))
                {
                    return $"Acquired part '{instance.name}' contains its Object Entry root; cleanup was retained without destroying any acquired part.";
                }
            }

            string issue = string.Empty;
            for (int index = pending.Length - 1; index >= 0; index--)
            {
                AcquiredInstance acquired = pending[index];
                GameObject instance = acquired.Instance;
                if (instance == null)
                {
                    RemoveAcquired(instance);
                    continue;
                }

                try
                {
                    if (!await ReleaseAcquiredAsync(instance))
                    {
                        issue = AppendIssue(
                            issue,
                            $"Acquired part '{instance.name}' could not be safely released or its Unity destruction was not confirmed.");
                        break;
                    }
                }
                catch (Exception exception)
                {
                    issue = AppendIssue(
                        issue,
                        $"Acquired part cleanup threw '{exception.GetType().Name}': {exception.Message}");
                    break;
                }
            }

            RemoveDestroyedInstances();
            return _acquiredInstances.Count == 0 ? string.Empty :
                AppendIssue(issue, $"'{_acquiredInstances.Count}' acquired Object Entry composition part(s) remain unresolved.");
        }

        private async Task<bool> ReleaseAcquiredAsync(GameObject instance)
        {
            if (instance == null)
            {
                RemoveAcquired(instance);
                return true;
            }

            AcquiredInstance acquired = FindAcquired(instance);
            if (acquired == null)
            {
                return false;
            }

            GameObject protectedRoot = acquired?.ProtectedRoot;
            if (ContainsProtectedRoot(instance, protectedRoot))
            {
                return false;
            }

            if (instance.activeSelf)
            {
                instance.SetActive(false);
                if (ContainsProtectedRoot(instance, protectedRoot))
                {
                    return false;
                }
            }

            if (instance.transform.parent != null)
            {
                instance.transform.SetParent(null, true);
                if (ContainsProtectedRoot(instance, protectedRoot))
                {
                    return false;
                }
            }

            if (!await DestroyAndConfirmAsync(instance, protectedRoot))
            {
                return false;
            }

            RemoveAcquired(instance);
            return true;
        }

        private async Task CleanupPreparationAsync(GameObject stagingRoot, GameObject instance)
        {
            if (instance != null)
                await ReleaseAcquiredAsync(instance);
            if (stagingRoot != null)
                await ReleaseAcquiredAsync(stagingRoot);
        }

        private bool IsTargetCurrent(StableObjectBinding target, PartMaterializationContext context)
        {
            if (context != null && context.IsClosingContext)
                return true;
            if (!_isTargetCurrent(target))
                return false;

            ConsumerOperationScope scope = _consumerOperation.Value;
            return scope == null || !scope.IsActive ||
                (ReferenceEquals(scope.Target, target) && IsConsumerScopeCurrent(scope));
        }

        private static bool IsConsumerScopeCurrent(ConsumerOperationScope scope)
        {
            try
            {
                return scope != null && scope.IsActive && scope.IsCurrent != null && scope.IsCurrent();
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetProvenance(
            PartMaterialization materialization,
            out PartProvenance provenance)
        {
            provenance = materialization?.Provenance as PartProvenance;
            return provenance != null && materialization.Value is GameObject instance &&
                ReferenceEquals(instance, provenance.Instance);
        }

        private static bool IsExactSubordinate(GameObject root, GameObject instance)
        {
            return root != null && instance != null && !ReferenceEquals(root, instance) &&
                instance.transform.IsChildOf(root.transform);
        }

        private static bool IsSafePreparationHierarchy(
            GameObject root,
            GameObject stagingRoot,
            GameObject instance,
            Transform expectedInstanceParent)
        {
            return root != null && stagingRoot != null && !stagingRoot.activeSelf &&
                ReferenceEquals(stagingRoot.transform.parent, root.transform) &&
                !ContainsProtectedRoot(stagingRoot, root) &&
                (instance == null ||
                 (!ReferenceEquals(instance, root) &&
                  !ContainsProtectedRoot(instance, root) &&
                  ReferenceEquals(instance.transform.parent, expectedInstanceParent)));
        }

        private void TrackAcquired(GameObject instance, GameObject protectedRoot)
        {
            if (!ContainsAcquired(instance))
            {
                _acquiredInstances.Add(new AcquiredInstance(instance, protectedRoot));
            }
        }

        private bool ContainsAcquired(GameObject instance)
        {
            for (int index = 0; index < _acquiredInstances.Count; index++)
            {
                if (ReferenceEquals(_acquiredInstances[index].Instance, instance)) return true;
            }
            return false;
        }

        private AcquiredInstance FindAcquired(GameObject instance)
        {
            for (int index = 0; index < _acquiredInstances.Count; index++)
            {
                AcquiredInstance acquired = _acquiredInstances[index];
                if (ReferenceEquals(acquired.Instance, instance)) return acquired;
            }

            return null;
        }

        private void RemoveAcquired(GameObject instance)
        {
            for (int index = _acquiredInstances.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(_acquiredInstances[index].Instance, instance))
                {
                    _acquiredInstances.RemoveAt(index);
                    return;
                }
            }
        }

        private void RemoveDestroyedInstances()
        {
            for (int index = _acquiredInstances.Count - 1; index >= 0; index--)
            {
                if (_acquiredInstances[index].Instance == null)
                {
                    _acquiredInstances.RemoveAt(index);
                }
            }
        }

        private Task<bool> DestroyAndConfirmAsync(GameObject instance, GameObject protectedRoot)
        {
            if (ContainsProtectedRoot(instance, protectedRoot))
            {
                return Task.FromResult(false);
            }

            return _destroyAndConfirmAsync(instance);
        }

        private static bool ContainsProtectedRoot(GameObject instance, GameObject protectedRoot)
        {
            return instance != null && protectedRoot != null &&
                (object.ReferenceEquals(instance, protectedRoot) ||
                 protectedRoot.transform.IsChildOf(instance.transform));
        }

        private static async Task<bool> DestroyAndConfirmWithUnityAsync(GameObject instance)
        {
            if (instance == null)
            {
                return true;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(instance);
                await Awaitable.NextFrameAsync();
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            return instance == null;
        }

        private static string AppendIssue(string current, string next)
        {
            return string.IsNullOrEmpty(current) ? next : current + " " + next;
        }
    }
}
