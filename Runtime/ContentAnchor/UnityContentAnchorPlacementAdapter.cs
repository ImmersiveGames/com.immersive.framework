using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit Unity adapter proof for physically parenting materialized runtime content under an authored anchor Transform.
    /// It requires an already successful logical ContentAnchor binding and existing Unity materialization evidence; it does not instantiate, destroy, pool, load Addressables or infer identity from hierarchy names.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-B first explicit Unity Content Anchor physical placement adapter proof; no materialization, release, pooling, Addressables or consumers.")]
    public sealed class UnityContentAnchorPlacementAdapter
    {
        private readonly string _source;

        public UnityContentAnchorPlacementAdapter()
            : this(nameof(UnityContentAnchorPlacementAdapter))
        {
        }

        public UnityContentAnchorPlacementAdapter(string source)
        {
            _source = Normalize(source, nameof(UnityContentAnchorPlacementAdapter));
        }

        public string Source => _source;

        public UnityContentAnchorPlacementResult Place(
            ContentAnchorBindingResult bindingResult,
            UnityRuntimeMaterializedObjectEvidence evidence,
            Transform anchorTransform,
            bool resetLocalTransform,
            string reason)
        {
            if (!bindingResult.Succeeded || !bindingResult.HasHandle)
            {
                return UnityContentAnchorPlacementResult.Failure(
                    bindingResult,
                    UnityContentAnchorPlacementStatus.FailedInvalidBinding,
                    evidence,
                    anchorTransform,
                    _source,
                    reason,
                    "Unity Content Anchor placement requires a successful logical Content Anchor binding result before physical placement.");
            }

            if (evidence == null)
            {
                return UnityContentAnchorPlacementResult.Failure(
                    bindingResult,
                    UnityContentAnchorPlacementStatus.FailedMissingPhysicalEvidence,
                    evidence,
                    anchorTransform,
                    _source,
                    reason,
                    "Unity Content Anchor placement requires explicit physical materialization evidence.");
            }

            if (evidence.Identity != bindingResult.RuntimeIdentity)
            {
                return UnityContentAnchorPlacementResult.Failure(
                    bindingResult,
                    UnityContentAnchorPlacementStatus.RejectedMismatchedRuntimeIdentity,
                    evidence,
                    anchorTransform,
                    _source,
                    reason,
                    "Unity Content Anchor placement rejected physical evidence with mismatched runtime content identity.");
            }

            if (evidence.PhysicalReleaseRequested)
            {
                return UnityContentAnchorPlacementResult.Failure(
                    bindingResult,
                    UnityContentAnchorPlacementStatus.RejectedReleasedPhysicalEvidence,
                    evidence,
                    anchorTransform,
                    _source,
                    reason,
                    "Unity Content Anchor placement rejected physical evidence that has already requested release.");
            }

            if (!evidence.HasLiveInstance || evidence.Instance == null)
            {
                return UnityContentAnchorPlacementResult.Failure(
                    bindingResult,
                    UnityContentAnchorPlacementStatus.FailedMissingInstance,
                    evidence,
                    anchorTransform,
                    _source,
                    reason,
                    "Unity Content Anchor placement requires a live materialized GameObject instance.");
            }

            if (anchorTransform == null)
            {
                return UnityContentAnchorPlacementResult.Failure(
                    bindingResult,
                    UnityContentAnchorPlacementStatus.FailedMissingAnchorTransform,
                    evidence,
                    anchorTransform,
                    _source,
                    reason,
                    "Unity Content Anchor placement requires an explicit authored anchor Transform.");
            }

            try
            {
                var instanceTransform = evidence.Instance.transform;
                if (instanceTransform.parent == anchorTransform)
                {
                    return UnityContentAnchorPlacementResult.AlreadyPlaced(
                        bindingResult,
                        evidence,
                        anchorTransform,
                        _source,
                        reason,
                        "Unity Content Anchor physical placement already has the expected parent Transform.");
                }

                instanceTransform.SetParent(anchorTransform, false);
                if (resetLocalTransform)
                {
                    instanceTransform.localPosition = Vector3.zero;
                    instanceTransform.localRotation = Quaternion.identity;
                    instanceTransform.localScale = Vector3.one;
                }

                return UnityContentAnchorPlacementResult.Success(
                    bindingResult,
                    evidence,
                    anchorTransform,
                    true,
                    _source,
                    reason,
                    "Unity Content Anchor physical placement parented the materialized instance under the explicit anchor Transform.");
            }
            catch (Exception exception)
            {
                return UnityContentAnchorPlacementResult.Failure(
                    bindingResult,
                    UnityContentAnchorPlacementStatus.FailedPlacementAdapter,
                    evidence,
                    anchorTransform,
                    _source,
                    reason,
                    $"Unity Content Anchor placement adapter failed. exception='{exception.GetType().Name.ToDiagnosticText()}' message='{exception.Message.ToDiagnosticText()}'.");
            }
        }

        private static string Normalize(string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }
    }
}
