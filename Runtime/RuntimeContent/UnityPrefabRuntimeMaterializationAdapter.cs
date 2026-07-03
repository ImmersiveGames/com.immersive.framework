using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Explicit Unity adapter proof that materializes one configured prefab/template GameObject.
    /// It is not RuntimeContent core, not a ContentAnchor placement adapter, not Addressables and not pooling.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8R-E first explicit Unity prefab materialization adapter proof; no ContentAnchor placement, Addressables, pooling or actor spawn.")]
    public sealed class UnityPrefabRuntimeMaterializationAdapter : IRuntimeMaterializationAdapter
    {
        public const string ResourceType = "UnityPrefab";

        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly UnityRuntimeMaterializedObjectRegistry _registry;
        private readonly string _source;

        public UnityPrefabRuntimeMaterializationAdapter(
            GameObject prefab,
            UnityRuntimeMaterializedObjectRegistry registry)
            : this(prefab, registry, null, nameof(UnityPrefabRuntimeMaterializationAdapter))
        {
        }

        public UnityPrefabRuntimeMaterializationAdapter(
            GameObject prefab,
            UnityRuntimeMaterializedObjectRegistry registry,
            Transform parent,
            string source)
        {
            _prefab = prefab;
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _parent = parent;
            _source = Normalize(source, nameof(UnityPrefabRuntimeMaterializationAdapter));
        }

        public GameObject Prefab => _prefab;

        public Transform Parent => _parent;

        public UnityRuntimeMaterializedObjectRegistry Registry => _registry;

        public RuntimeMaterializationResult Materialize(RuntimeMaterializationRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Unity prefab runtime materialization requires a valid request.", nameof(request));
            }

            if (!request.CancellationToken.AllowsMaterialization)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.RejectedScopeCancellation,
                    _source,
                    request.Reason,
                    "Unity prefab runtime materialization rejected a cancelled scope token before physical instantiation.");
            }

            if (!string.Equals(request.Resource.ResourceType, ResourceType, StringComparison.Ordinal))
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedMaterializer,
                    _source,
                    request.Reason,
                    $"Unity prefab runtime materialization only supports resourceType '{ResourceType}'. Requested resourceType '{request.Resource.ResourceType.ToDiagnosticText()}'.");
            }

            if (_prefab == null)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedMaterializer,
                    _source,
                    request.Reason,
                    "Unity prefab runtime materialization requires an explicit prefab/template GameObject.");
            }

            if (_registry.Contains(request.Identity))
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedMaterializer,
                    _source,
                    request.Reason,
                    "Unity prefab runtime materialization rejected duplicate physical materialization for the same runtime content identity.");
            }

            GameObject instance = null;
            try
            {
                instance = _parent == null
                    ? Object.Instantiate(_prefab)
                    : Object.Instantiate(_prefab, _parent, false);

                if (instance == null)
                {
                    return RuntimeMaterializationResult.Failure(
                        request,
                        RuntimeMaterializationStatus.FailedMaterializer,
                        _source,
                        request.Reason,
                        "Unity prefab runtime materialization produced no GameObject instance.");
                }

                instance.name = $"RuntimeContent_{request.ContentId.StableText}";

                var handle = RuntimeContentHandle.Declared(
                    request.Identity,
                    _source,
                    request.Reason);

                if (!_registry.TryRegister(
                        request,
                        handle,
                        _prefab,
                        instance,
                        _source,
                        request.Reason,
                        out _,
                        out string registryMessage))
                {
                    Object.Destroy(instance);
                    return RuntimeMaterializationResult.Failure(
                        request,
                        RuntimeMaterializationStatus.FailedRegistration,
                        _source,
                        request.Reason,
                        registryMessage);
                }

                return RuntimeMaterializationResult.Success(
                    request,
                    handle,
                    _source,
                    request.Reason,
                    "Unity prefab runtime materialization created physical adapter-side evidence.");
            }
            catch (Exception exception)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }

                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedMaterializer,
                    _source,
                    request.Reason,
                    $"Unity prefab runtime materialization adapter failed. exception='{exception.GetType().Name.ToDiagnosticText()}' message='{exception.Message.ToDiagnosticText()}'.");
            }
        }

        private static string Normalize(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }
    }
}
