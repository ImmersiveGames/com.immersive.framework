using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit Unity composition proof for prefab materialization, logical ContentAnchor binding and physical placement.
    /// This is not ContentAnchor core behavior, not automatic lifecycle wiring and not a consumer adapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-C Unity RuntimeContent/ContentAnchor composition proof; no automatic consumer ownership.")]
    internal sealed class UnityContentAnchorMaterializationPipeline
    {
        private readonly UnityPrefabRuntimeMaterializationAdapter _materializationAdapter;
        private readonly UnityContentAnchorPlacementAdapter _placementAdapter;
        private readonly UnityObjectRuntimeReleaseAdapter _releaseAdapter;
        private readonly string _source;

        internal UnityContentAnchorMaterializationPipeline(
            UnityPrefabRuntimeMaterializationAdapter materializationAdapter,
            UnityContentAnchorPlacementAdapter placementAdapter,
            UnityObjectRuntimeReleaseAdapter releaseAdapter,
            string source)
        {
            _materializationAdapter = materializationAdapter ?? throw new ArgumentNullException(nameof(materializationAdapter));
            _placementAdapter = placementAdapter ?? throw new ArgumentNullException(nameof(placementAdapter));
            _releaseAdapter = releaseAdapter ?? throw new ArgumentNullException(nameof(releaseAdapter));
            if (!ReferenceEquals(_materializationAdapter.Registry, _releaseAdapter.Registry))
            {
                throw new ArgumentException("Content Anchor materialization pipeline requires materialization and release adapters to share the same explicit Unity materialized object registry.", nameof(releaseAdapter));
            }

            _source = source.NormalizeTextOrFallback(nameof(UnityContentAnchorMaterializationPipeline));
        }

        internal UnityContentAnchorMaterializationPipelineResult MaterializeBindPlace(
            FrameworkRuntimeHost runtimeHost,
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest bindingRequest,
            Transform anchorTransform,
            bool resetLocalTransform,
            string reason)
        {
            var service = new ContentAnchorMaterializationService(
                _materializationAdapter,
                _placementAdapter,
                _releaseAdapter,
                _source);
            var result = service.MaterializeBindPlace(
                runtimeHost,
                anchorSet,
                bindingRequest,
                anchorTransform,
                resetLocalTransform,
                reason);
            return ToPipelineResult(result);
        }

        private UnityContentAnchorMaterializationPipelineResult ToPipelineResult(ContentAnchorMaterializationResult result)
        {
            if (result.Succeeded)
            {
                return UnityContentAnchorMaterializationPipelineResult.Success(
                    result.Request,
                    result.MaterializationResult,
                    result.AppliedMaterializationResult,
                    result.BindingResult,
                    result.PlacementResult,
                    result.Source,
                    result.Reason,
                    "Content Anchor materialization pipeline materialized, logically bound and physically placed runtime content.");
            }

            return UnityContentAnchorMaterializationPipelineResult.Failure(
                result.Request,
                MapStatus(result.FailedStage),
                result.MaterializationResult,
                result.AppliedMaterializationResult,
                result.BindingResult,
                result.PlacementResult,
                result.RollbackResult.PhysicalReleaseResult,
                result.RollbackResult.LogicalReleaseResult,
                result.RollbackResult.Attempted,
                result.RollbackResult.Succeeded,
                result.Source,
                result.Reason,
                result.Message);
        }

        internal static UnityContentAnchorMaterializationPipelineStatus MapStatus(ContentAnchorMaterializationStage failedStage)
        {
            switch (failedStage)
            {
                case ContentAnchorMaterializationStage.None:
                    return UnityContentAnchorMaterializationPipelineStatus.Succeeded;
                case ContentAnchorMaterializationStage.RuntimeHost:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedMissingRuntimeHost;
                case ContentAnchorMaterializationStage.RuntimeContentRuntime:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedMissingRuntimeContentRuntime;
                case ContentAnchorMaterializationStage.AnchorTransform:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedMissingAnchorTransform;
                case ContentAnchorMaterializationStage.MaterializationRequest:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedMaterializationRequest;
                case ContentAnchorMaterializationStage.PhysicalMaterialization:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedMaterialization;
                case ContentAnchorMaterializationStage.RuntimeContentApply:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedMaterializationApply;
                case ContentAnchorMaterializationStage.MaterializedEvidence:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedMissingPhysicalEvidence;
                case ContentAnchorMaterializationStage.LogicalBinding:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedLogicalBinding;
                case ContentAnchorMaterializationStage.PhysicalPlacement:
                    return UnityContentAnchorMaterializationPipelineStatus.FailedPhysicalPlacement;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failedStage), failedStage, "Content Anchor materialization failed stage must be explicit.");
            }
        }
    }
}
