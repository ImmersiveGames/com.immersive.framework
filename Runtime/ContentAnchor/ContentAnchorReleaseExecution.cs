using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Mechanical release sequence shared by ContentAnchor materialization and lifecycle release proofs.
    /// It only centralizes the explicit physical release -> logical release application order; it does not own Route or Activity semantics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R helper for explicit ContentAnchor release sequencing; no lifecycle ownership.")]
    internal static class ContentAnchorReleaseExecution
    {
        internal static ContentAnchorReleaseExecutionResult Execute(
            RuntimeContentRuntime runtimeContentRuntime,
            UnityObjectRuntimeReleaseAdapter releaseAdapter,
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            RuntimeReleasePolicy releasePolicy,
            string source,
            string reason)
        {
            if (runtimeContentRuntime == null)
            {
                throw new ArgumentNullException(nameof(runtimeContentRuntime));
            }

            if (releaseAdapter == null)
            {
                throw new ArgumentNullException(nameof(releaseAdapter));
            }

            if (!context.IsValid)
            {
                throw new ArgumentException("Runtime scope context must be valid.", nameof(context));
            }

            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), releasePolicy)
                || releasePolicy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(releasePolicy), releasePolicy, "Runtime release policy must be explicit.");
            }

            var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                context,
                identity,
                releasePolicy,
                source,
                reason);
            return Execute(runtimeContentRuntime, releaseAdapter, releaseRequest, source, reason);
        }

        internal static ContentAnchorReleaseExecutionResult Execute(
            RuntimeContentRuntime runtimeContentRuntime,
            UnityObjectRuntimeReleaseAdapter releaseAdapter,
            RuntimeReleaseRequest releaseRequest,
            string logicalSource,
            string logicalReason)
        {
            if (runtimeContentRuntime == null)
            {
                throw new ArgumentNullException(nameof(runtimeContentRuntime));
            }

            if (releaseAdapter == null)
            {
                throw new ArgumentNullException(nameof(releaseAdapter));
            }

            if (!releaseRequest.IsValid)
            {
                throw new ArgumentException("Runtime release request must be valid.", nameof(releaseRequest));
            }

            var physicalReleaseResult = releaseAdapter.Release(releaseRequest);
            if (!physicalReleaseResult.Succeeded)
            {
                return new ContentAnchorReleaseExecutionResult(
                    physicalReleaseResult,
                    default(RuntimeReleaseResult));
            }

            var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                physicalReleaseResult,
                logicalSource,
                logicalReason);
            return new ContentAnchorReleaseExecutionResult(
                physicalReleaseResult,
                logicalReleaseResult);
        }

        internal static ContentAnchorReleaseExecutionResult Execute(
            RuntimeContentRuntime runtimeContentRuntime,
            UnityObjectRuntimeReleaseAdapter releaseAdapter,
            RuntimeMaterializationRequest materializationRequest,
            RuntimeReleasePolicy releasePolicy,
            string source,
            string reason)
        {
            if (runtimeContentRuntime == null)
            {
                throw new ArgumentNullException(nameof(runtimeContentRuntime));
            }

            if (releaseAdapter == null)
            {
                throw new ArgumentNullException(nameof(releaseAdapter));
            }

            if (!materializationRequest.IsValid)
            {
                throw new ArgumentException("Runtime materialization request must be valid.", nameof(materializationRequest));
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), releasePolicy)
                || releasePolicy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(releasePolicy), releasePolicy, "Runtime release policy must be explicit.");
            }

            var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                materializationRequest.Context,
                materializationRequest.Identity,
                releasePolicy,
                source,
                reason);
            return Execute(runtimeContentRuntime, releaseAdapter, releaseRequest, source, reason);
        }
    }

    /// <summary>
    /// API status: Experimental. Immutable result for the mechanical ContentAnchor release sequence helper.
    /// It records the physical release and the subsequent logical RuntimeContent release result only.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R helper result for explicit ContentAnchor release sequencing; no lifecycle ownership.")]
    internal readonly struct ContentAnchorReleaseExecutionResult
    {
        internal ContentAnchorReleaseExecutionResult(
            RuntimeReleaseResult physicalReleaseResult,
            RuntimeReleaseResult logicalReleaseResult)
        {
            PhysicalReleaseResult = physicalReleaseResult;
            LogicalReleaseResult = logicalReleaseResult;
        }

        public RuntimeReleaseResult PhysicalReleaseResult { get; }

        public RuntimeReleaseResult LogicalReleaseResult { get; }

        public bool PhysicalReleaseSucceeded => PhysicalReleaseResult.Succeeded;

        public bool LogicalReleaseSucceeded => LogicalReleaseResult.Succeeded;

        public bool Succeeded => PhysicalReleaseSucceeded && LogicalReleaseSucceeded;
    }
}
