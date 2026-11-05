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
                ObjectEntryCompositionPart definition,
                GameObject adoptedInstance = null)
            {
                Target = target ?? throw new ArgumentNullException(nameof(target));
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                AdoptedInstance = adoptedInstance;
            }

            internal StableObjectBinding Target { get; }
            internal ObjectEntryCompositionPart Definition { get; }
            internal GameObject AdoptedInstance { get; }
            internal bool IsAdopted => AdoptedInstance != null;
        }

        private sealed class PartProvenance
        {
            internal PartProvenance(
                PartBinding binding,
                GameObject instance,
                Transform originalParent,
                bool originalActive)
            {
                Binding = binding;
                Instance = instance;
                OriginalParent = originalParent;
                OriginalActive = originalActive;
            }

            internal PartBinding Binding { get; }
            internal GameObject Instance { get; }
            internal Transform OriginalParent { get; }
            internal bool OriginalActive { get; }
        }

        private readonly Func<StableObjectBinding, bool> _isTargetCurrent;
        private readonly Func<GameObject, Task<bool>> _destroyAndConfirmAsync;
        private readonly List<GameObject> _acquiredInstances = new List<GameObject>();

        internal ObjectEntryCompositionPartMaterializer(
            Func<StableObjectBinding, bool> isTargetCurrent,
            Func<GameObject, Task<bool>> destroyAndConfirmAsync = null)
        {
            _isTargetCurrent = isTargetCurrent ?? throw new ArgumentNullException(nameof(isTargetCurrent));
            _destroyAndConfirmAsync = destroyAndConfirmAsync ?? DestroyAndConfirmWithUnityAsync;
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
                !_isTargetCurrent(binding.Target))
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
                    adopted,
                    adopted.transform.parent,
                    adopted.activeSelf);
                return PartPreparationResult.Prepared(
                    new PartMaterialization(adopted, PartOwnership.Adopted, adoptedProvenance));
            }

            if (binding.Definition.Prefab == null)
            {
                return PartPreparationResult.Failed(
                    $"Object Entry composition prefab for key '{definition.Key.Value}' is missing.");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return PartPreparationResult.Failed(
                    $"Object Entry composition preparation for key '{definition.Key.Value}' was cancelled before materialization.");
            }

            GameObject stagingRoot = null;
            GameObject instance = null;
            bool authoredActiveSelf = binding.Definition.Prefab.activeSelf;
            try
            {
                stagingRoot = new GameObject("Object Entry Composition Preparation");
                stagingRoot.SetActive(false);
                stagingRoot.transform.SetParent(root.transform, false);
                TrackAcquired(stagingRoot);
                instance = UnityEngine.Object.Instantiate(
                    binding.Definition.Prefab,
                    stagingRoot.transform,
                    false);
                if (instance == null)
                {
                    await ReleaseAcquiredAsync(stagingRoot);
                    stagingRoot = null;
                    return PartPreparationResult.Failed(
                        $"Object Entry composition prefab for key '{definition.Key.Value}' returned null.");
                }

                TrackAcquired(instance);
                instance.SetActive(false);
                instance.transform.SetParent(root.transform, false);
                instance.name = binding.Definition.Prefab.name;

                if (!await DestroyAndConfirmAsync(stagingRoot))
                {
                    return PartPreparationResult.Failed(
                        $"Temporary staging root for Object Entry part '{definition.Key.Value}' could not be confirmed destroyed.");
                }

                RemoveAcquired(stagingRoot);
                stagingRoot = null;
                if (cancellationToken.IsCancellationRequested ||
                    !_isTargetCurrent(binding.Target) ||
                    !IsExactSubordinate(root, instance))
                {
                    await ReleaseAcquiredAsync(instance);
                    return PartPreparationResult.Failed(
                        $"Object Entry binding became stale while preparing part '{definition.Key.Value}'.");
                }

                var provenance = new PartProvenance(binding, instance, null, authoredActiveSelf);
                return PartPreparationResult.Prepared(
                    new PartMaterialization(instance, PartOwnership.Acquired, provenance));
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
                provenance.Binding.Target.PhysicalObject == null ||
                !_isTargetCurrent(provenance.Binding.Target) ||
                !IsExactSubordinate(provenance.Binding.Target.PhysicalObject, provenance.Instance))
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

                if (provenance.Binding.Target.PhysicalObject == null ||
                    !_isTargetCurrent(provenance.Binding.Target) ||
                    !IsExactSubordinate(provenance.Binding.Target.PhysicalObject, provenance.Instance))
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
                provenance.Instance == provenance.Binding.Target.PhysicalObject)
            {
                return PartEffectResult.Failed(
                    "Object Entry composition Detach requires the exact subordinate materialization.",
                    false);
            }

            if (current.Ownership == PartOwnership.Adopted && context.IsClosingContext)
            {
                return PartEffectResult.Applied();
            }

            try
            {
                if (current.Ownership == PartOwnership.Adopted)
                {
                    provenance.Instance.transform.SetParent(provenance.OriginalParent, false);
                    provenance.Instance.SetActive(provenance.OriginalActive);
                }
                else
                {
                    provenance.Instance.SetActive(false);
                    if (provenance.Instance.transform.parent != null)
                    {
                        provenance.Instance.transform.SetParent(null, true);
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
                provenance.Instance == provenance.Binding.Target.PhysicalObject)
            {
                return PartReleaseResult.Failed(
                    "Object Entry composition Release requires the exact subordinate materialization.");
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

        internal async Task<string> ReconcileOutstandingAcquiredInstancesAsync()
        {
            RemoveDestroyedInstances();
            string issue = string.Empty;
            GameObject[] pending = _acquiredInstances.ToArray();
            for (int index = 0; index < pending.Length; index++)
            {
                GameObject instance = pending[index];
                if (instance == null)
                {
                    RemoveAcquired(instance);
                    continue;
                }

                try
                {
                    if (!await DestroyAndConfirmAsync(instance))
                    {
                        issue = AppendIssue(
                            issue,
                            $"Acquired part '{instance.name}' remains live after Unity destruction was requested.");
                    }
                    else
                    {
                        RemoveAcquired(instance);
                    }
                }
                catch (Exception exception)
                {
                    issue = AppendIssue(
                        issue,
                        $"Acquired part cleanup threw '{exception.GetType().Name}': {exception.Message}");
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

            if (!ReferenceEquals(instance, null) &&
                _acquiredInstances.Count > 0 &&
                !ContainsAcquired(instance))
            {
                return false;
            }

            if (instance.transform.parent != null)
            {
                instance.transform.SetParent(null, true);
            }

            if (!await DestroyAndConfirmAsync(instance))
            {
                return false;
            }

            RemoveAcquired(instance);
            return true;
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

        private void TrackAcquired(GameObject instance)
        {
            if (!ContainsAcquired(instance))
            {
                _acquiredInstances.Add(instance);
            }
        }

        private bool ContainsAcquired(GameObject instance)
        {
            for (int index = 0; index < _acquiredInstances.Count; index++)
            {
                if (ReferenceEquals(_acquiredInstances[index], instance)) return true;
            }
            return false;
        }

        private void RemoveAcquired(GameObject instance)
        {
            for (int index = _acquiredInstances.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(_acquiredInstances[index], instance))
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
                if (_acquiredInstances[index] == null)
                {
                    _acquiredInstances.RemoveAt(index);
                }
            }
        }

        private Task<bool> DestroyAndConfirmAsync(GameObject instance)
        {
            return _destroyAndConfirmAsync(instance);
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
