using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Local adapter-side registry for physical Unity objects created by runtime materialization adapters.
    /// This is instantiable and explicit; it is not a singleton, service locator, RuntimeContent core registry replacement or Content Anchor placement system.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8R-E local Unity materialized object registry for adapter proof; no singleton, ContentAnchor placement, pooling or Addressables.")]
    public sealed class UnityRuntimeMaterializedObjectRegistry
    {
        private readonly Dictionary<RuntimeContentIdentity, UnityRuntimeMaterializedObjectEvidence> _entries;

        public UnityRuntimeMaterializedObjectRegistry()
        {
            _entries = new Dictionary<RuntimeContentIdentity, UnityRuntimeMaterializedObjectEvidence>();
        }

        public int Count => _entries.Count;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var entry in _entries.Values)
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int PhysicalReleaseRequestedCount
        {
            get
            {
                int count = 0;
                foreach (var entry in _entries.Values)
                {
                    if (entry != null && entry.PhysicalReleaseRequested)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool TryGet(
            RuntimeContentIdentity identity,
            out UnityRuntimeMaterializedObjectEvidence evidence)
        {
            ValidateIdentity(identity);
            return _entries.TryGetValue(identity, out evidence);
        }

        public bool Contains(RuntimeContentIdentity identity)
        {
            ValidateIdentity(identity);
            return _entries.ContainsKey(identity);
        }

        public UnityRuntimeMaterializedObjectEvidence[] Snapshot()
        {
            var values = new UnityRuntimeMaterializedObjectEvidence[_entries.Count];
            _entries.Values.CopyTo(values, 0);
            return values;
        }

        internal bool TryRegister(
            RuntimeMaterializationRequest request,
            RuntimeContentHandle handle,
            GameObject prefab,
            GameObject instance,
            string source,
            string reason,
            out UnityRuntimeMaterializedObjectEvidence evidence,
            out string message)
        {
            if (!request.IsValid)
            {
                evidence = null;
                message = "Unity runtime materialized object registration requires a valid materialization request.";
                return false;
            }

            if (handle == null)
            {
                evidence = null;
                message = "Unity runtime materialized object registration requires a runtime content handle.";
                return false;
            }

            if (handle.Identity != request.Identity)
            {
                evidence = null;
                message = "Unity runtime materialized object registration rejected a handle with mismatched identity.";
                return false;
            }

            if (prefab == null)
            {
                evidence = null;
                message = "Unity runtime materialized object registration requires an explicit prefab/template object.";
                return false;
            }

            if (instance == null)
            {
                evidence = null;
                message = "Unity runtime materialized object registration requires a created instance.";
                return false;
            }

            if (_entries.ContainsKey(request.Identity))
            {
                evidence = null;
                message = "Unity runtime materialized object registration rejected duplicate materialization for the same runtime content identity.";
                return false;
            }

            evidence = new UnityRuntimeMaterializedObjectEvidence(
                request.Identity,
                handle,
                prefab,
                instance,
                source,
                reason);
            _entries.Add(request.Identity, evidence);
            message = "Unity runtime materialized object registered as adapter-side physical evidence.";
            return true;
        }

        internal bool TryMarkPhysicalReleaseRequested(
            RuntimeReleaseRequest request,
            string source,
            string reason,
            out UnityRuntimeMaterializedObjectEvidence evidence,
            out string message)
        {
            if (!request.IsValid)
            {
                evidence = null;
                message = "Unity runtime physical release requires a valid release request.";
                return false;
            }

            if (!_entries.TryGetValue(request.Identity, out evidence) || evidence == null)
            {
                message = "Unity runtime physical release rejected a missing physical object evidence entry.";
                return false;
            }

            if (evidence.PhysicalReleaseRequested)
            {
                message = "Unity runtime physical release rejected a duplicate release for the same runtime content identity.";
                return false;
            }

            if (!evidence.HasLiveInstance)
            {
                message = "Unity runtime physical release rejected a missing Unity instance reference.";
                return false;
            }

            evidence.MarkPhysicalReleaseRequested(source, reason);
            message = "Unity runtime physical release requested for the materialized object.";
            return true;
        }

        public string ToDiagnosticString()
        {
            return $"entries='{Count}' active='{ActiveCount}' physicalReleaseRequested='{PhysicalReleaseRequestedCount}'";
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }
    }
}
