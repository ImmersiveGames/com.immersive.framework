using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Unity adapter-side evidence for one physically instantiated runtime content object.
    /// This evidence is intentionally outside the pure RuntimeContent identity/handle contract and must not be used as functional identity.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8R-E Unity adapter-side physical object evidence; GameObject references remain outside pure RuntimeContent core contracts.")]
    public sealed class UnityRuntimeMaterializedObjectEvidence
    {
        internal UnityRuntimeMaterializedObjectEvidence(
            RuntimeContentIdentity identity,
            RuntimeContentHandle handle,
            GameObject prefab,
            GameObject instance,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }

            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (handle.Identity != identity)
            {
                throw new ArgumentException("Runtime content handle identity must match physical evidence identity.", nameof(handle));
            }

            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Identity = identity;
            Handle = handle;
            Prefab = prefab;
            Instance = instance;
            Source = Normalize(source);
            Reason = Normalize(reason);
            PhysicalReleaseRequested = false;
            PhysicalReleaseSource = string.Empty;
            PhysicalReleaseReason = string.Empty;
        }

        public RuntimeContentIdentity Identity { get; }

        public RuntimeContentOwner Owner => Identity.Owner;

        public RuntimeContentScope Scope => Identity.Scope;

        public RuntimeContentId ContentId => Identity.ContentId;

        public RuntimeContentHandle Handle { get; }

        public GameObject Prefab { get; }

        public GameObject Instance { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool PhysicalReleaseRequested { get; private set; }

        public string PhysicalReleaseSource { get; private set; }

        public string PhysicalReleaseReason { get; private set; }

        public bool HasLiveInstance => Instance != null;

        internal void MarkPhysicalReleaseRequested(string source, string reason)
        {
            PhysicalReleaseRequested = true;
            PhysicalReleaseSource = Normalize(source);
            PhysicalReleaseReason = Normalize(reason);
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string releaseSourceText = PhysicalReleaseSource.ToDiagnosticText();
            string releaseReasonText = PhysicalReleaseReason.ToDiagnosticText();
            string prefabNameText = Prefab.ToDiagnosticText(value => value.name);
            string instanceNameText = Instance.ToDiagnosticText(value => value.name);

            return $"identity='{Identity.StableText}' owner='{Owner.StableText}' scope='{Scope}' contentId='{ContentId.StableText}' hasLiveInstance='{HasLiveInstance}' physicalReleaseRequested='{PhysicalReleaseRequested}' prefabName='{prefabNameText}' instanceName='{instanceNameText}' source='{sourceText}' reason='{reasonText}' releaseSource='{releaseSourceText}' releaseReason='{releaseReasonText}'";
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
