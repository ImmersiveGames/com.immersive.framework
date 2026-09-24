using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal owner for runtime-created content state in the current framework runtime.
    /// It coordinates explicit scope roots, scope contexts, transition guards, scoped cancellation, passive handles and materialization request creation without becoming a service locator or materializer.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8J internal RuntimeContentRuntime owner; adds logical release request/result execution without physical cleanup.")]
    internal sealed class RuntimeContentRuntime
    {
        private readonly RuntimeRootRegistry _registry;
        private readonly RuntimeScopeTransitionGuard _transitionGuard;

        public RuntimeContentRuntime()
        {
            _registry = new RuntimeRootRegistry();
            _transitionGuard = new RuntimeScopeTransitionGuard();
        }

        public int RootCount => _registry.RootCount;

        public bool HasRoots => _registry.HasRoots;

        public RuntimeRootRegistryOperationResult CreateScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            var result = _registry.CreateRoot(owner, source, reason);

            if (result.Applied || result.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists)
            {
                _transitionGuard.OpenScope(owner, source, reason);
            }

            return result;
        }

        public RuntimeRootRegistryOperationResult RemoveScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            _transitionGuard.RequestCancellation(owner, source, reason);
            var result = _registry.RemoveRoot(owner, source, reason);

            if (result.Applied || result.Status == RuntimeRootRegistryOperationStatus.RootMissing)
            {
                _transitionGuard.MarkRemoved(owner, source, reason);
            }

            return result;
        }

        public bool TryCreateScopeContext(
            RuntimeContentOwner owner,
            string source,
            string reason,
            out RuntimeScopeContext context)
        {
            ValidateOwner(owner);

            if (!_registry.TryGetRoot(owner, out _))
            {
                context = default(RuntimeScopeContext);
                return false;
            }

            context = new RuntimeScopeContext(owner, source, reason);
            return true;
        }

        public RuntimeScopeRoot[] SnapshotRoots()
        {
            return _registry.SnapshotRoots();
        }

        public RuntimeScopeRoot[] SnapshotRoots(RuntimeContentScope scope)
        {
            return _registry.SnapshotRoots(scope);
        }

        public RuntimeContentHandle[] SnapshotHandles(RuntimeScopeContext context)
        {
            ValidateContext(context);
            return _registry.SnapshotHandles(context.Owner);
        }

        public RuntimeRootRegistryOperationResult DeclareHandle(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            string source,
            string reason)
        {
            ValidateContext(context);

            var identity = context.CreateIdentity(contentId);
            var handle = new RuntimeContentHandle(
                identity,
                RuntimeContentState.Declared,
                source,
                reason,
                "Runtime content handle declared by RuntimeContentRuntime.");

            return RegisterHandle(context, handle, source, reason);
        }

        public RuntimeRootRegistryOperationResult DeclareHandle(
            RuntimeScopeContext context,
            string contentId,
            string source,
            string reason)
        {
            return DeclareHandle(context, RuntimeContentId.From(contentId), source, reason);
        }

        public RuntimeRootRegistryOperationResult RegisterHandle(
            RuntimeScopeContext context,
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            ValidateContext(context);

            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (handle.Owner != context.Owner)
            {
                if (_registry.TryGetRoot(context.Owner, out var contextRoot))
                {
                    return RuntimeRootRegistryOperationResult.RejectedMismatchedOwner(
                        contextRoot,
                        handle.Identity,
                        source,
                        reason);
                }

                return RuntimeRootRegistryOperationResult.RejectedMissingRoot(
                    context.Owner,
                    handle.Identity,
                    true,
                    source,
                    reason);
            }

            return _registry.RegisterHandle(handle, source, reason);
        }

        public RuntimeRootRegistryOperationResult UnregisterHandle(
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            ValidateContext(context);
            ValidateIdentity(identity);

            if (identity.Owner != context.Owner)
            {
                if (_registry.TryGetRoot(context.Owner, out var contextRoot))
                {
                    return RuntimeRootRegistryOperationResult.RejectedMismatchedOwner(
                        contextRoot,
                        identity,
                        source,
                        reason);
                }

                return RuntimeRootRegistryOperationResult.RejectedMissingRoot(
                    context.Owner,
                    identity,
                    true,
                    source,
                    reason);
            }

            return _registry.UnregisterHandle(identity, source, reason);
        }

        public bool TryGetHandle(
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            out RuntimeContentHandle handle)
        {
            ValidateContext(context);
            ValidateIdentity(identity);

            if (identity.Owner != context.Owner)
            {
                handle = null;
                return false;
            }

            return _registry.TryGetHandle(identity, out handle);
        }

        public RuntimeMaterializationRequest CreateMaterializationRequest(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            if (!TryCreateMaterializationRequest(context, contentId, resource, source, reason, out var request, out var guardResult))
            {
                throw new InvalidOperationException(guardResult.ToDiagnosticString());
            }

            return request;
        }

        public RuntimeMaterializationRequest CreateMaterializationRequest(
            RuntimeScopeContext context,
            string contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            return CreateMaterializationRequest(
                context,
                RuntimeContentId.From(contentId),
                resource,
                source,
                reason);
        }

        public bool TryCreateMaterializationRequest(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason,
            out RuntimeMaterializationRequest request,
            out RuntimeScopeTransitionGuardResult guardResult)
        {
            ValidateContext(context);

            if (!contentId.IsValid)
            {
                throw new ArgumentException("Runtime content id must be valid.", nameof(contentId));
            }

            if (!resource.IsValid)
            {
                throw new ArgumentException("Runtime materialization resource must be valid.", nameof(resource));
            }

            guardResult = _transitionGuard.AllowMaterialization(context, source, reason);
            if (!guardResult.Allowed)
            {
                request = default(RuntimeMaterializationRequest);
                return false;
            }

            request = new RuntimeMaterializationRequest(
                context,
                contentId,
                resource,
                guardResult.Token,
                source,
                reason);

            return true;
        }

        public bool TryCreateMaterializationRequest(
            RuntimeScopeContext context,
            string contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason,
            out RuntimeMaterializationRequest request,
            out RuntimeScopeTransitionGuardResult guardResult)
        {
            return TryCreateMaterializationRequest(
                context,
                RuntimeContentId.From(contentId),
                resource,
                source,
                reason,
                out request,
                out guardResult);
        }

        public RuntimeMaterializationResult RejectMaterializationFromGuard(
            RuntimeMaterializationRequest request,
            string source,
            string reason)
        {
            ValidateMaterializationRequest(request);

            var guardResult = _transitionGuard.ValidateMaterializationRequest(request, source, reason);
            if (guardResult.Allowed)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedInvalidRequest,
                    source,
                    reason,
                    "Runtime materialization request is still allowed; no guard rejection was produced.");
            }

            return RuntimeMaterializationResult.Failure(
                request,
                guardResult.ToMaterializationStatus(),
                source,
                reason,
                guardResult.Message);
        }


        public RuntimeMaterializationResult ApplyMaterializationResult(
            RuntimeMaterializationResult materializationResult,
            string source,
            string reason)
        {
            ValidateMaterializationResult(materializationResult);

            if (materializationResult.Failed)
            {
                return materializationResult;
            }

            var request = materializationResult.Request;
            var guardResult = _transitionGuard.ValidateMaterializationRequest(request, source, reason);
            if (!guardResult.Allowed)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    guardResult.ToMaterializationStatus(),
                    source,
                    reason,
                    guardResult.Message);
            }

            var handle = materializationResult.Handle;
            if (handle == null)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedInvalidHandle,
                    source,
                    reason,
                    "Runtime materialization result cannot be applied because it has no handle.");
            }

            if (handle.Identity != request.Identity)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.RejectedMismatchedIdentity,
                    source,
                    reason,
                    "Runtime materialization result handle identity does not match the request identity.");
            }

            if (handle.IsDeclared)
            {
                var materializedResult = handle.MarkMaterialized(source, reason);
                if (materializedResult.Rejected)
                {
                    return RuntimeMaterializationResult.Failure(
                        request,
                        RuntimeMaterializationStatus.FailedInvalidHandle,
                        source,
                        reason,
                        materializedResult.Message);
                }
            }

            if (!handle.IsMaterialized)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedInvalidHandle,
                    source,
                    reason,
                    $"Runtime materialization result handle must be materialized before registration. Current state: '{handle.State}'.");
            }

            var registryResult = RegisterHandle(request.Context, handle, source, reason);
            return RuntimeMaterializationResult.FromRegistryResult(
                request,
                handle,
                registryResult,
                source,
                reason);
        }

        public bool IsMaterializationRequestCurrent(
            RuntimeMaterializationRequest request,
            string source,
            string reason,
            out RuntimeScopeTransitionGuardResult guardResult)
        {
            ValidateMaterializationRequest(request);
            guardResult = _transitionGuard.ValidateMaterializationRequest(request, source, reason);
            return guardResult.Allowed;
        }

        public RuntimeReleaseRequest CreateReleaseRequest(
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            ValidateContext(context);
            ValidateIdentity(identity);
            ValidateReleasePolicy(policy);

            if (identity.Owner != context.Owner)
            {
                throw new ArgumentException("Runtime release request identity owner must match the request context owner.", nameof(identity));
            }

            return new RuntimeReleaseRequest(context, identity, policy, source, reason);
        }

        public RuntimeReleaseRequest CreateReleaseRequest(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            if (!contentId.IsValid)
            {
                throw new ArgumentException("Runtime content id must be valid.", nameof(contentId));
            }

            return CreateReleaseRequest(
                context,
                context.CreateIdentity(contentId),
                policy,
                source,
                reason);
        }

        public RuntimeReleaseRequest CreateReleaseRequest(
            RuntimeScopeContext context,
            string contentId,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            return CreateReleaseRequest(
                context,
                RuntimeContentId.From(contentId),
                policy,
                source,
                reason);
        }

        public RuntimeReleaseResult ReleaseHandleLogically(
            RuntimeReleaseRequest request,
            string source,
            string reason)
        {
            ValidateReleaseRequest(request);

            if (!_registry.TryGetRoot(request.Owner, out _))
            {
                return RuntimeReleaseResult.Failure(
                    request,
                    RuntimeReleaseStatus.FailedMissingRoot,
                    null,
                    RuntimeContentState.Unknown,
                    RuntimeContentState.Unknown,
                    source,
                    reason,
                    "Runtime release failed because the owner root is missing.");
            }

            if (!_registry.TryGetHandle(request.Identity, out var handle))
            {
                return RuntimeReleaseResult.Failure(
                    request,
                    RuntimeReleaseStatus.FailedMissingHandle,
                    null,
                    RuntimeContentState.Unknown,
                    RuntimeContentState.Unknown,
                    source,
                    reason,
                    "Runtime release failed because the handle is not registered in the owner root.");
            }

            return ApplyLogicalRelease(request, handle, source, reason);
        }

        public RuntimeReleaseResult ReleaseHandleLogically(
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            var request = CreateReleaseRequest(context, identity, policy, source, reason);
            return ReleaseHandleLogically(request, source, reason);
        }

        public RuntimeReleaseResult ReleaseHandleLogically(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            var request = CreateReleaseRequest(context, contentId, policy, source, reason);
            return ReleaseHandleLogically(request, source, reason);
        }

        public RuntimeReleaseResult[] ReleaseScopeLogically(
            RuntimeScopeContext context,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            ValidateContext(context);
            ValidateReleasePolicy(policy);

            if (!_registry.TryGetRoot(context.Owner, out var root))
            {
                throw new InvalidOperationException("Runtime scope root must exist before scope release can execute.");
            }

            RuntimeContentHandle[] handles = root.SnapshotHandles();
            var results = new RuntimeReleaseResult[handles.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                var request = CreateReleaseRequest(context, handles[i].Identity, policy, source, reason);
                results[i] = ReleaseHandleLogically(request, source, reason);
            }

            return results;
        }

        public RuntimeReleaseResult ApplyReleaseResult(
            RuntimeReleaseResult releaseResult,
            string source,
            string reason)
        {
            ValidateReleaseResult(releaseResult);

            if (releaseResult.Failed)
            {
                return releaseResult;
            }

            return ReleaseHandleLogically(releaseResult.Request, source, reason);
        }

        public string ToDiagnosticString()
        {
            return _registry.ToDiagnosticString();
        }

        private RuntimeReleaseResult ApplyLogicalRelease(
            RuntimeReleaseRequest request,
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            var previousState = handle.State;
            if (handle.IsReleased)
            {
                bool alreadyReleasedUnregistered = UnregisterHandleIfRequired(request, source, reason, out RuntimeReleaseResult? alreadyReleasedUnregisterFailure);
                if (alreadyReleasedUnregisterFailure.HasValue)
                {
                    return alreadyReleasedUnregisterFailure.Value;
                }

                return RuntimeReleaseResult.AlreadyReleased(
                    request,
                    handle,
                    alreadyReleasedUnregistered,
                    source,
                    reason,
                    alreadyReleasedUnregistered
                        ? "Runtime content was already released and was unregistered from the logical scope root."
                        : "Runtime content was already released.");
            }

            var releaseRequestResult = handle.RequestRelease(source, reason);
            if (releaseRequestResult.Rejected)
            {
                return RuntimeReleaseResult.Failure(
                    request,
                    RuntimeReleaseStatus.RejectedInvalidTransition,
                    handle,
                    previousState,
                    handle.State,
                    source,
                    reason,
                    releaseRequestResult.Message);
            }

            var markReleasedResult = handle.MarkReleased(source, reason);
            if (markReleasedResult.Rejected)
            {
                return RuntimeReleaseResult.Failure(
                    request,
                    RuntimeReleaseStatus.RejectedInvalidTransition,
                    handle,
                    previousState,
                    handle.State,
                    source,
                    reason,
                    markReleasedResult.Message);
            }

            bool unregistered = UnregisterHandleIfRequired(request, source, reason, out RuntimeReleaseResult? unregisterFailure);
            if (unregisterFailure.HasValue)
            {
                return unregisterFailure.Value;
            }

            return RuntimeReleaseResult.Success(
                request,
                handle,
                previousState,
                handle.State,
                unregistered,
                source,
                reason,
                unregistered
                    ? "Runtime content released logically and unregistered from the logical scope root."
                    : "Runtime content released logically and kept registered in the logical scope root.");
        }

        private bool UnregisterHandleIfRequired(
            RuntimeReleaseRequest request,
            string source,
            string reason,
            out RuntimeReleaseResult? failure)
        {
            failure = null;
            if (!request.ShouldUnregister)
            {
                return false;
            }

            var unregisterResult = _registry.UnregisterHandle(request.Identity, source, reason);
            if (unregisterResult.Applied || unregisterResult.Status == RuntimeRootRegistryOperationStatus.HandleMissing)
            {
                return true;
            }

            RuntimeContentHandle handle = null;
            _registry.TryGetHandle(request.Identity, out handle);
            failure = RuntimeReleaseResult.Failure(
                request,
                unregisterResult.Status == RuntimeRootRegistryOperationStatus.RejectedMissingRoot
                    ? RuntimeReleaseStatus.FailedMissingRoot
                    : RuntimeReleaseStatus.FailedUnregister,
                handle,
                handle?.State ?? RuntimeContentState.Unknown,
                handle?.State ?? RuntimeContentState.Unknown,
                source,
                reason,
                unregisterResult.Message);
            return false;
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }

        private static void ValidateContext(RuntimeScopeContext context)
        {
            if (!context.IsValid)
            {
                throw new ArgumentException("Runtime scope context must be valid.", nameof(context));
            }
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }

        private static void ValidateMaterializationRequest(RuntimeMaterializationRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Runtime materialization request must be valid.", nameof(request));
            }
        }


        private static void ValidateMaterializationResult(RuntimeMaterializationResult result)
        {
            if (!result.Request.IsValid || result.Status == RuntimeMaterializationStatus.Unknown)
            {
                throw new ArgumentException("Runtime materialization result must be valid.", nameof(result));
            }
        }

        private static void ValidateReleasePolicy(RuntimeReleasePolicy policy)
        {
            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), policy) || policy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(policy), policy, "Runtime release policy must be explicit.");
            }
        }

        private static void ValidateReleaseRequest(RuntimeReleaseRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Runtime release request must be valid.", nameof(request));
            }
        }

        private static void ValidateReleaseResult(RuntimeReleaseResult result)
        {
            if (!result.Request.IsValid || result.Status == RuntimeReleaseStatus.Unknown)
            {
                throw new ArgumentException("Runtime release result must be valid.", nameof(result));
            }
        }
    }
}
