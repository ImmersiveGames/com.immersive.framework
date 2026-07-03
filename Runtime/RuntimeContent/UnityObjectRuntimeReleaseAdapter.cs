using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Explicit Unity physical release proof for objects registered by UnityRuntimeMaterializedObjectRegistry.
    /// It requests Object.Destroy for adapter-created objects only; it does not perform logical RuntimeContent release by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8R-E first explicit Unity physical release adapter proof; no pooling, Addressables, scene unload or ContentAnchor placement.")]
    public sealed class UnityObjectRuntimeReleaseAdapter : IRuntimeReleaseAdapter
    {
        private readonly UnityRuntimeMaterializedObjectRegistry _registry;
        private readonly string _source;

        public UnityObjectRuntimeReleaseAdapter(UnityRuntimeMaterializedObjectRegistry registry)
            : this(registry, nameof(UnityObjectRuntimeReleaseAdapter))
        {
        }

        public UnityObjectRuntimeReleaseAdapter(
            UnityRuntimeMaterializedObjectRegistry registry,
            string source)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _source = Normalize(source, nameof(UnityObjectRuntimeReleaseAdapter));
        }

        public UnityRuntimeMaterializedObjectRegistry Registry => _registry;

        public RuntimeReleaseResult Release(RuntimeReleaseRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Unity object runtime release requires a valid release request.", nameof(request));
            }

            if (!_registry.TryMarkPhysicalReleaseRequested(
                    request,
                    _source,
                    request.Reason,
                    out var evidence,
                    out string message))
            {
                return RuntimeReleaseResult.Failure(
                    request,
                    RuntimeReleaseStatus.FailedReleaseAdapter,
                    evidence?.Handle,
                    evidence?.Handle?.State ?? RuntimeContentState.Unknown,
                    evidence?.Handle?.State ?? RuntimeContentState.Unknown,
                    _source,
                    request.Reason,
                    message);
            }

            try
            {
                Object.Destroy(evidence.Instance);
                return RuntimeReleaseResult.Success(
                    request,
                    evidence.Handle,
                    evidence.Handle.State,
                    evidence.Handle.State,
                    false,
                    _source,
                    request.Reason,
                    "Unity object runtime release requested Object.Destroy for adapter-created physical evidence.");
            }
            catch (Exception exception)
            {
                return RuntimeReleaseResult.Failure(
                    request,
                    RuntimeReleaseStatus.FailedReleaseAdapter,
                    evidence.Handle,
                    evidence.Handle.State,
                    evidence.Handle.State,
                    _source,
                    request.Reason,
                    $"Unity object runtime release adapter failed. exception='{exception.GetType().Name.ToDiagnosticText()}' message='{exception.Message.ToDiagnosticText()}'.");
            }
        }

        private static string Normalize(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }
    }
}
