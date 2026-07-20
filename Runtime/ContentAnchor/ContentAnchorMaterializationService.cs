using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Reusable non-MonoBehaviour entry point for RuntimeContent materialization, ContentAnchor binding and physical placement.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "H2.2.11 ContentAnchor materialization service uses an explicit scoped runtime port.")]
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
                throw new ArgumentException(
                    "Content Anchor materialization service requires materialization and release adapters to share the same explicit Unity materialized object registry.",
                    nameof(releaseAdapter));
            }

            _source = source.NormalizeTextOrFallback(nameof(ContentAnchorMaterializationService));
        }

        internal ContentAnchorMaterializationResult MaterializeBindPlace(
            IContentAnchorMaterializationRuntimePort runtimePort,
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest bindingRequest,
            Transform anchorTransform,
            bool resetLocalTransform,
            string reason)
        {
            ValidateBindingRequest(bindingRequest);
            string resolvedReason = reason.NormalizeText();

            if (runtimePort == null)
            {
                return Failure(
                    bindingRequest,
                    default,
                    ContentAnchorMaterializationStage.RuntimeHost,
                    default,
                    default,
                    default,
                    default,
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires an explicit materialization runtime port.");
            }

            RuntimeContentRuntime runtimeContentRuntime = runtimePort.ContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return Failure(
                    bindingRequest,
                    default,
                    ContentAnchorMaterializationStage.RuntimeContentRuntime,
                    default,
                    default,
                    default,
                    default,
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires RuntimeContentRuntime.");
            }

            if (anchorTransform == null)
            {
                return Failure(
                    bindingRequest,
                    default,
                    ContentAnchorMaterializationStage.AnchorTransform,
                    default,
                    default,
                    default,
                    default,
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
                    default,
                    ContentAnchorMaterializationStage.MaterializationRequest,
                    default,
                    default,
                    default,
                    default,
                    NoRollback(resolvedReason),
                    resolvedReason,
                    guardResult.Message);
            }

            return MaterializeBindPlace(
                runtimePort,
                anchorSet,
                bindingRequest,
                materializationRequest,
                anchorTransform,
                resetLocalTransform,
                resolvedReason);
        }

        internal ContentAnchorMaterializationResult MaterializeBindPlace(
            IContentAnchorMaterializationRuntimePort runtimePort,
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

            if (runtimePort == null)
            {
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.RuntimeHost,
                    default,
                    default,
                    default,
                    default,
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires an explicit materialization runtime port.");
            }

            RuntimeContentRuntime runtimeContentRuntime = runtimePort.ContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.RuntimeContentRuntime,
                    default,
                    default,
                    default,
                    default,
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
                    default,
                    default,
                    default,
                    default,
                    NoRollback(resolvedReason),
                    resolvedReason,
                    "Content Anchor materialization pipeline requires an explicit anchor Transform before materialization side effects.");
            }

            RuntimeMaterializationResult materializationResult = _materializationAdapter.Materialize(materializationRequest);
            if (!materializationResult.Succeeded)
            {
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.PhysicalMaterialization,
                    materializationResult,
                    default,
                    default,
                    default,
                    NoRollback(resolvedReason),
                    resolvedReason,
                    materializationResult.Message);
            }

            RuntimeMaterializationResult appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                _source,
                resolvedReason);
            if (!appliedMaterializationResult.Succeeded)
            {
                ContentAnchorMaterializationRollbackResult rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    false,
                    runtimePort,
                    default,
                    resolvedReason);
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.RuntimeContentApply,
                    materializationResult,
                    appliedMaterializationResult,
                    default,
                    default,
                    rollback,
                    resolvedReason,
                    appliedMaterializationResult.Message);
            }

            if (!_materializationAdapter.Registry.TryGet(materializationRequest.Identity, out var evidence)
                || evidence == null
                || !evidence.HasLiveInstance)
            {
                ContentAnchorReleaseExecutionResult releaseExecution = ExecuteRollbackRelease(
                    runtimeContentRuntime,
                    materializationRequest,
                    resolvedReason);
                ContentAnchorMaterializationRollbackResult rollback = ContentAnchorMaterializationRollbackResultFrom(
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
                    default,
                    default,
                    rollback,
                    resolvedReason,
                    "Content Anchor materialization pipeline failed because physical materialization evidence was not available after successful materialization.");
            }

            ContentAnchorBindingResult bindingResult = runtimePort.BindContentAnchor(
                anchorSet,
                bindingRequest,
                _source,
                resolvedReason);
            if (!bindingResult.Succeeded)
            {
                ContentAnchorMaterializationRollbackResult rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    false,
                    runtimePort,
                    bindingResult,
                    resolvedReason);
                return Failure(
                    bindingRequest,
                    materializationRequest,
                    ContentAnchorMaterializationStage.LogicalBinding,
                    materializationResult,
                    appliedMaterializationResult,
                    bindingResult,
                    default,
                    rollback,
                    resolvedReason,
                    bindingResult.Message);
            }

            UnityContentAnchorPlacementResult placementResult = _placementAdapter.Place(
                bindingResult,
                evidence,
                anchorTransform,
                resetLocalTransform,
                resolvedReason);
            if (!placementResult.Succeeded)
            {
                ContentAnchorMaterializationRollbackResult rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    true,
                    runtimePort,
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
            IContentAnchorMaterializationRuntimePort runtimePort,
            ContentAnchorBindingResult bindingResult,
            string reason)
        {
            bool bindingUnbound = shouldUnbind
                && bindingResult.HasHandle
                && runtimePort.UnbindContentAnchor(bindingResult.Handle);

            ContentAnchorReleaseExecutionResult releaseExecution = ExecuteRollbackRelease(
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
            ContentAnchorReleaseExecutionResult releaseExecution = ContentAnchorReleaseExecution.Execute(
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

            RuntimeReleaseResult logicalReleaseResult = runtimeContentRuntime.ReleaseHandleLogically(
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
                throw new ArgumentException(
                    "Content Anchor materialization service requires a valid binding request.",
                    nameof(bindingRequest));
            }
        }

        private static void ValidateMaterializationRequest(
            ContentAnchorBindingRequest bindingRequest,
            RuntimeMaterializationRequest materializationRequest)
        {
            if (!materializationRequest.IsValid)
            {
                throw new ArgumentException(
                    "Content Anchor materialization service requires a valid materialization request.",
                    nameof(materializationRequest));
            }

            if (!materializationRequest.Context.Equals(bindingRequest.RuntimeContext)
                || !materializationRequest.ContentId.Equals(bindingRequest.RuntimeContentId)
                || !materializationRequest.Resource.Equals(bindingRequest.Resource))
            {
                throw new ArgumentException(
                    "Content Anchor materialization service requires the supplied materialization request to match the binding request context, content id and resource.",
                    nameof(materializationRequest));
            }
        }
    }
}
