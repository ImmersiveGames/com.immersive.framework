using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Reusable non-MonoBehaviour entry point for RuntimeContent materialization, ContentAnchor binding and physical placement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "MAT-4 reusable ContentAnchor materialization service; no lifecycle, pooling, Addressables, actor or consumer ownership.")]
    internal sealed class ContentAnchorMaterializationService
    {
        private readonly UnityPrefabRuntimeMaterializationAdapter _materializationAdapter;
        private readonly UnityContentAnchorPlacementAdapter _placementAdapter;
        private readonly UnityObjectRuntimeReleaseAdapter _releaseAdapter;
        private readonly string _source;

        internal ContentAnchorMaterializationService(
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
                throw new ArgumentException("Content Anchor materialization service requires materialization and release adapters to share the same explicit Unity materialized object registry.", nameof(releaseAdapter));
            }

            _source = source.NormalizeTextOrFallback(nameof(ContentAnchorMaterializationService));
        }

        internal ContentAnchorMaterializationResult MaterializeBindPlace(
            FrameworkRuntimeHost runtimeHost,
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest bindingRequest,
            Transform anchorTransform,
            bool resetLocalTransform,
            string reason)
        {
            ValidateBindingRequest(bindingRequest);
            string resolvedReason = reason.NormalizeText();

            if (runtimeHost == null)
            {
                return Failure(
                    bindingRequest,
                    default(RuntimeMaterializationRequest),
                    ContentAnchorMaterializationStage.RuntimeHost,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires an explicit FrameworkRuntimeHost.");
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return Failure(
                    bindingRequest,
                    default(RuntimeMaterializationRequest),
                    ContentAnchorMaterializationStage.RuntimeContentRuntime,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires RuntimeContentRuntime.");
            }

            if (anchorTransform == null)
            {
                return Failure(
                    bindingRequest,
                    default(RuntimeMaterializationRequest),
                    ContentAnchorMaterializationStage.AnchorTransform,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires an explicit anchor Transform before materialization side effects.");
            }

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    bindingRequest.RuntimeContext,
                    bindingRequest.RuntimeContentId,
                    bindingRequest.Resource,
                    _source,
                    resolvedReason,
                    out var materializationRequest,
                    out var guardResult))
            {
                return Failure(
                    bindingRequest,
                    default(RuntimeMaterializationRequest),
                    ContentAnchorMaterializationStage.MaterializationRequest,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    guardResult.Message);
            }

            return MaterializeBindPlace(
                runtimeHost,
                anchorSet,
                bindingRequest,
                materializationRequest,
                anchorTransform,
                resetLocalTransform,
                resolvedReason);
        }

        internal ContentAnchorMaterializationResult MaterializeBindPlace(
            FrameworkRuntimeHost runtimeHost,
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest bindingRequest,
            RuntimeMaterializationRequest materializationRequest,
            Transform anchorTransform,
            bool resetLocalTransform,
            string reason)
        {
            ValidateBindingRequest(bindingRequest);
            ValidateMaterializationRequest(bindingRequest, materializationRequest);
            string resolvedReason = reason.NormalizeText();

            if (runtimeHost == null)
            {
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.RuntimeHost,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires an explicit FrameworkRuntimeHost.");
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.RuntimeContentRuntime,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires RuntimeContentRuntime.");
            }

            if (anchorTransform == null)
            {
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.AnchorTransform,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires an explicit anchor Transform before materialization side effects.");
            }

            var materializationResult = _materializationAdapter.Materialize(materializationRequest);
            if (!materializationResult.Succeeded)
            {
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.PhysicalMaterialization,
                    materializationResult,
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    NoRollback(resolvedReason),
                    resolvedReason,
                    materializationResult.Message);
            }

            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                _source,
                resolvedReason);
            if (!appliedMaterializationResult.Succeeded)
            {
                var rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    false,
                    runtimeHost,
                    default(ContentAnchorBindingResult),
                    resolvedReason);
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.RuntimeContentApply,
                    materializationResult,
                    appliedMaterializationResult,
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    rollback,
                    resolvedReason,
                    appliedMaterializationResult.Message);
            }

            if (!_materializationAdapter.Registry.TryGet(materializationRequest.Identity, out var evidence)
                || evidence == null
                || !evidence.HasLiveInstance)
            {
                var releaseExecution = ExecuteRollbackRelease(
                    runtimeContentRuntime,
                    materializationRequest,
                    resolvedReason);
                var rollback = ContentAnchorMaterializationRollbackResultFrom(
                    false,
                    releaseExecution,
                    resolvedReason,
                    "Content Anchor materialization service rolled back missing physical evidence.");
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.MaterializedEvidence,
                    materializationResult,
                    appliedMaterializationResult,
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    rollback,
                    resolvedReason,
                    "Content Anchor materialization pipeline failed because physical materialization evidence was not available after successful materialization.");
            }

            var bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                _source,
                resolvedReason);
            if (!bindingResult.Succeeded)
            {
                var rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    false,
                    runtimeHost,
                    bindingResult,
                    resolvedReason);
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.LogicalBinding,
                    materializationResult,
                    appliedMaterializationResult,
                    bindingResult,
                    default(UnityContentAnchorPlacementResult),
                    rollback,
                    resolvedReason,
                    bindingResult.Message);
            }

            var placementResult = _placementAdapter.Place(
                bindingResult,
                evidence,
                anchorTransform,
                resetLocalTransform,
                resolvedReason);
            if (!placementResult.Succeeded)
            {
                var rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    true,
                    runtimeHost,
                    bindingResult,
                    resolvedReason);
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.PhysicalPlacement,
                    materializationResult,
                    appliedMaterializationResult,
                    bindingResult,
                    placementResult,
                    rollback,
                    resolvedReason,
                    placementResult.Message);
            }

            return ContentAnchorMaterializationResult.Success(
                bindingRequest,
                materializationRequest,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                _source,
                resolvedReason,
                "Content Anchor materialization service materialized, logically bound and physically placed runtime content.");
        }

        private ContentAnchorMaterializationRollbackResult RollbackPhysicalAndLogical(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeMaterializationRequest materializationRequest,
            bool shouldUnbind,
            FrameworkRuntimeHost runtimeHost,
            ContentAnchorBindingResult bindingResult,
            string reason)
        {
            bool bindingUnbound = false;
            if (shouldUnbind && bindingResult.HasHandle)
            {
                bindingUnbound = runtimeHost.UnbindContentAnchor(bindingResult.Handle);
            }

            var releaseExecution = ExecuteRollbackRelease(
                runtimeContentRuntime,
                materializationRequest,
                reason);
            return ContentAnchorMaterializationRollbackResultFrom(
                bindingUnbound,
                releaseExecution,
                reason,
                "Content Anchor materialization service rolled back physical and logical materialization state.");
        }

        private ContentAnchorReleaseExecutionResult ExecuteRollbackRelease(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeMaterializationRequest materializationRequest,
            string reason)
        {
            var releaseExecution = ContentAnchorReleaseExecution.Execute(
                runtimeContentRuntime,
                _releaseAdapter,
                materializationRequest,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                _source,
                reason);

            if (releaseExecution.LogicalReleaseResult.Request.IsValid)
            {
                return releaseExecution;
            }

            var logicalReleaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                materializationRequest.Context,
                materializationRequest.Identity,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                _source,
                reason);
            return new ContentAnchorReleaseExecutionResult(
                releaseExecution.PhysicalReleaseResult,
                logicalReleaseResult);
        }

        private ContentAnchorMaterializationRollbackResult ContentAnchorMaterializationRollbackResultFrom(
            bool bindingUnbound,
            ContentAnchorReleaseExecutionResult releaseExecution,
            string reason,
            string message)
        {
            return new ContentAnchorMaterializationRollbackResult(
                true,
                bindingUnbound,
                releaseExecution.PhysicalReleaseResult,
                releaseExecution.LogicalReleaseResult,
                _source,
                reason,
                message);
        }

        private ContentAnchorMaterializationRollbackResult NoRollback(string reason)
        {
            return ContentAnchorMaterializationRollbackResult.NotAttempted(
                _source,
                reason,
                "Content Anchor materialization service did not reach a stage requiring rollback.");
        }

        private ContentAnchorMaterializationResult Failure(
            ContentAnchorBindingRequest request,
            RuntimeMaterializationRequest materializationRequest,
            ContentAnchorMaterializationStage failedStage,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            ContentAnchorMaterializationRollbackResult rollbackResult,
            string reason,
            string message)
        {
            return ContentAnchorMaterializationResult.Failure(
                request,
                materializationRequest,
                failedStage,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                rollbackResult,
                _source,
                reason,
                message);
        }

        private static void ValidateBindingRequest(ContentAnchorBindingRequest bindingRequest)
        {
            if (!bindingRequest.IsValid)
            {
                throw new ArgumentException("Content Anchor materialization service requires a valid binding request.", nameof(bindingRequest));
            }
        }

        private static void ValidateMaterializationRequest(
            ContentAnchorBindingRequest bindingRequest,
            RuntimeMaterializationRequest materializationRequest)
        {
            if (!materializationRequest.IsValid)
            {
                throw new ArgumentException("Content Anchor materialization service requires a valid materialization request.", nameof(materializationRequest));
            }

            if (!materializationRequest.Context.Equals(bindingRequest.RuntimeContext)
                || !materializationRequest.ContentId.Equals(bindingRequest.RuntimeContentId)
                || !materializationRequest.Resource.Equals(bindingRequest.Resource))
            {
                throw new ArgumentException("Content Anchor materialization service requires the supplied materialization request to match the binding request context, content id and resource.", nameof(materializationRequest));
            }
        }
    }
}
