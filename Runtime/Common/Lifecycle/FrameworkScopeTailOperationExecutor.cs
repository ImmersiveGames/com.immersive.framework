using System;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Common
{
    internal static class FrameworkScopeTailOperationExecutor
    {
        internal static FrameworkScopeTailOperationResult Execute(
            FrameworkScopeTailOperationRequest request,
            Func<FrameworkScopeTailOperationRequest, ContentAnchorBindingLifecycleResult> cleanupBinding,
            Func<FrameworkScopeTailOperationRequest, RuntimeRootRegistryOperationResult> removePreviousScopeRoot)
        {
            ContentAnchorBindingLifecycleResult bindingCleanupResult = default(ContentAnchorBindingLifecycleResult);
            RuntimeRootRegistryOperationResult previousScopeRootRemoveResult = null;
            bool bindingCleanupInvoked = false;
            bool bindingCleanupSkipped = !request.HasDistinctPreviousOwner;
            bool previousScopeRootRemoveInvoked = false;
            bool previousScopeRootRemoveSkipped = !request.HasDistinctPreviousOwner;

            if (request.HasDistinctPreviousOwner)
            {
                if (cleanupBinding == null)
                {
                    throw new ArgumentNullException(nameof(cleanupBinding));
                }

                if (removePreviousScopeRoot == null)
                {
                    throw new ArgumentNullException(nameof(removePreviousScopeRoot));
                }

                bindingCleanupResult = cleanupBinding(request);
                if (!bindingCleanupResult.Executed)
                {
                    throw new InvalidOperationException("Scope tail binding cleanup delegate returned no executed result.");
                }

                bindingCleanupInvoked = true;
                bindingCleanupSkipped = false;

                previousScopeRootRemoveResult = removePreviousScopeRoot(request);
                if (previousScopeRootRemoveResult == null)
                {
                    throw new InvalidOperationException("Scope tail previous scope root remove delegate returned no result.");
                }

                previousScopeRootRemoveInvoked = true;
                previousScopeRootRemoveSkipped = false;
            }

            RuntimeContentOwner scopeOwner = request.HasCurrentOwner
                ? request.CurrentOwner
                : request.PreviousOwner;
            RuntimeScopeLifecycleResult scopeResult = new RuntimeScopeLifecycleResult(
                request.Scope,
                scopeOwner,
                request.HasCurrentOwner ? request.EnterRootResult : null,
                previousScopeRootRemoveResult,
                request.HasCurrentOwner ? request.Context : default(RuntimeScopeContext),
                request.RootCount,
                request.Source,
                request.Reason);

            return new FrameworkScopeTailOperationResult(
                scopeOwner,
                request.PreviousOwner,
                scopeResult,
                bindingCleanupResult,
                previousScopeRootRemoveResult,
                bindingCleanupInvoked,
                bindingCleanupSkipped,
                previousScopeRootRemoveInvoked,
                previousScopeRootRemoveSkipped);
        }
    }
}
