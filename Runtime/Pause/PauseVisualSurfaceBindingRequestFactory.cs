using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Converts a passive Pause visual surface contract into an explicit ContentAnchor binding request.
    /// This factory is request-only: it does not create RuntimeContent roots or handles, bind anchors, instantiate prefabs,
    /// move transforms, change Pause/Input state, release content or wire into Route/Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Pause visual surface binding request factory; no binding execution or materialization.")]
    public static class PauseVisualSurfaceBindingRequestFactory
    {
        private const string DefaultSource = nameof(PauseVisualSurfaceBindingRequestFactory);
        private const string DefaultReason = "pause.visual.surface.binding.request";

        public static PauseVisualSurfaceBindingRequestResult Create(
            PauseVisualSurfaceContract contract,
            string source,
            string reason)
        {
            if (!contract.IsValid)
            {
                return PauseVisualSurfaceBindingRequestResult.Failure(
                    PauseVisualSurfaceBindingRequestStatus.RejectedInvalidContract,
                    contract,
                    NormalizeSource(source),
                    NormalizeReason(reason),
                    "Pause visual surface binding request rejected because the contract is invalid.");
            }

            var context = new RuntimeScopeContext(
                contract.RuntimeOwner,
                NormalizeSource(source),
                NormalizeReason(reason));

            return Create(contract, context, source, reason);
        }

        public static PauseVisualSurfaceBindingRequestResult Create(
            PauseVisualSurfaceContract contract,
            RuntimeScopeContext runtimeContext,
            string source,
            string reason)
        {
            string normalizedSource = NormalizeSource(source);
            string normalizedReason = NormalizeReason(reason);

            if (!contract.IsValid)
            {
                return PauseVisualSurfaceBindingRequestResult.Failure(
                    PauseVisualSurfaceBindingRequestStatus.RejectedInvalidContract,
                    contract,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding request rejected because the contract is invalid.");
            }

            if (!runtimeContext.IsValid)
            {
                return PauseVisualSurfaceBindingRequestResult.Failure(
                    PauseVisualSurfaceBindingRequestStatus.RejectedInvalidRuntimeContext,
                    contract,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding request rejected because the runtime context is invalid.");
            }

            if (runtimeContext.Owner != contract.RuntimeOwner)
            {
                return PauseVisualSurfaceBindingRequestResult.Failure(
                    PauseVisualSurfaceBindingRequestStatus.RejectedMismatchedRuntimeOwner,
                    contract,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding request rejected because the runtime context owner does not match the Pause visual surface owner.");
            }

            var request = new ContentAnchorBindingRequest(
                runtimeContext,
                contract.AnchorScope,
                contract.AnchorOwner,
                contract.AnchorKind,
                contract.AnchorId,
                contract.RuntimeContentId,
                contract.Resource,
                normalizedSource,
                normalizedReason);

            return PauseVisualSurfaceBindingRequestResult.Success(
                contract,
                request,
                normalizedSource,
                normalizedReason,
                "Pause visual surface binding request created without executing binding or materialization.");
        }

        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback(DefaultSource);
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback(DefaultReason);
        }
    }
}
