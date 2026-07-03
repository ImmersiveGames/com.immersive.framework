using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Executes explicit visual materialization for one Pause visual surface contract.
    /// It composes RuntimeContent materialization, ContentAnchor binding and physical placement, but it does not toggle Pause,
    /// change InputMode, alter Time.timeScale or wire into Route/Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Pause visual surface materialization executor; explicit visual proof only.")]
    internal static class PauseVisualSurfaceMaterializationExecutor
    {
        private const string DefaultSource = nameof(PauseVisualSurfaceMaterializationExecutor);
        private const string DefaultReason = "pause.visual.surface.materialization";

        internal static PauseVisualSurfaceMaterializationResult Execute(
            PauseVisualSurfaceContract contract,
            ContentAnchorSet anchorSet,
            Transform anchorTransform,
            RuntimeScopeContext runtimeContext,
            FrameworkRuntimeHost runtimeHost,
            UnityRuntimeMaterializedObjectRegistry physicalRegistry,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(DefaultSource);
            string normalizedReason = reason.NormalizeTextOrFallback(DefaultReason);

            if (runtimeHost == null)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedMissingRuntimeHost,
                    contract,
                    default,
                    default,
                    physicalRegistry,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface materialization failed because the FrameworkRuntimeHost is missing.");
            }

            if (runtimeHost.RuntimeContentRuntime == null)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedMissingRuntimeContentRuntime,
                    contract,
                    default,
                    default,
                    physicalRegistry,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface materialization failed because RuntimeContentRuntime is missing.");
            }

            if (!contract.IsValid)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedInvalidContract,
                    contract,
                    default,
                    default,
                    physicalRegistry,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface materialization rejected an invalid contract.");
            }

            if (!runtimeContext.IsValid || runtimeContext.Owner != contract.RuntimeOwner)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedInvalidRuntimeContext,
                    contract,
                    default,
                    default,
                    physicalRegistry,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface materialization rejected an invalid or mismatched runtime context.");
            }

            if (physicalRegistry == null)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedMissingPhysicalRegistry,
                    contract,
                    default,
                    default,
                    physicalRegistry,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface materialization requires an explicit Unity physical registry.");
            }

            if (anchorTransform == null)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedMissingAnchorTransform,
                    contract,
                    default,
                    default,
                    physicalRegistry,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface materialization requires an explicit anchor Transform.");
            }

            var requestResult = PauseVisualSurfaceBindingRequestFactory.Create(
                contract,
                runtimeContext,
                normalizedSource,
                normalizedReason);
            if (!requestResult.Succeeded)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedBindingRequest,
                    contract,
                    requestResult,
                    default,
                    physicalRegistry,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface materialization failed before visual side effects because the binding request could not be created.");
            }

            var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                contract.VisualPrefab,
                physicalRegistry,
                null,
                normalizedSource);
            var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(
                physicalRegistry,
                normalizedSource);
            var placementAdapter = new UnityContentAnchorPlacementAdapter(normalizedSource);
            var pipeline = new UnityContentAnchorMaterializationPipeline(
                materializationAdapter,
                placementAdapter,
                releaseAdapter,
                normalizedSource);

            var pipelineResult = pipeline.MaterializeBindPlace(
                runtimeHost,
                anchorSet,
                requestResult.Request,
                anchorTransform,
                contract.ResetLocalTransform,
                normalizedReason);
            if (!pipelineResult.Succeeded)
            {
                return Failure(
                    PauseVisualSurfaceMaterializationStatus.FailedMaterializationPipeline,
                    contract,
                    requestResult,
                    pipelineResult,
                    physicalRegistry,
                    true,
                    normalizedSource,
                    normalizedReason,
                    pipelineResult.Message);
            }

            return PauseVisualSurfaceMaterializationResult.Success(
                contract,
                requestResult,
                pipelineResult,
                physicalRegistry.Count,
                physicalRegistry.ActiveCount,
                normalizedSource,
                normalizedReason,
                "Pause visual surface materialization explicitly materialized, bound and physically placed the visual prefab.");
        }

        private static PauseVisualSurfaceMaterializationResult Failure(
            PauseVisualSurfaceMaterializationStatus status,
            PauseVisualSurfaceContract contract,
            PauseVisualSurfaceBindingRequestResult requestResult,
            UnityContentAnchorMaterializationPipelineResult pipelineResult,
            UnityRuntimeMaterializedObjectRegistry physicalRegistry,
            bool materializationAttempted,
            string source,
            string reason,
            string message)
        {
            int entries = physicalRegistry != null ? physicalRegistry.Count : 0;
            int active = physicalRegistry != null ? physicalRegistry.ActiveCount : 0;
            return PauseVisualSurfaceMaterializationResult.Failure(
                status,
                contract,
                requestResult,
                pipelineResult,
                entries,
                active,
                materializationAttempted,
                source,
                reason,
                message);
        }
    }
}
