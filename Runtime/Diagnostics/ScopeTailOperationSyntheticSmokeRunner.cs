#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for the internal scope tail operation shell.
    /// It validates Common/Lifecycle mechanics only and does not touch Route or Activity runtimes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Synthetic scope tail smoke; Common/Lifecycle only.")]
    internal static class ScopeTailOperationSyntheticSmokeRunner
    {
        internal const string SmokeName = "Scope Tail Operation Synthetic Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            string normalizedSource = source.NormalizeTextOrFallback(nameof(ScopeTailOperationSyntheticSmokeRunner));

            bool mergeWithoutPreviousOwnerPassed = ValidateMergeWithoutPreviousOwner(logger, normalizedSource);
            bool skipWithoutPreviousOwnerPassed = ValidateSkipWithoutPreviousOwner(logger, normalizedSource);
            bool skipWithSameOwnerPassed = ValidateSkipWithSameOwner(logger, normalizedSource);
            bool orderingAndSuccessPassed = ValidateCleanupOrderingAndSuccess(logger, normalizedSource);
            bool bindingRejectedPassed = ValidateBindingCleanupRejected(logger, normalizedSource);
            bool removeRejectedPassed = ValidatePreviousScopeRootRemoveRejected(logger, normalizedSource);

            return Task.FromResult(mergeWithoutPreviousOwnerPassed
                && skipWithoutPreviousOwnerPassed
                && skipWithSameOwnerPassed
                && orderingAndSuccessPassed
                && bindingRejectedPassed
                && removeRejectedPassed);
        }

        private static bool ValidateMergeWithoutPreviousOwner(FrameworkLogger logger, string source)
        {
            RuntimeContentOwner currentOwner = RuntimeContentOwner.Session("scope-tail-current", "ScopeTail.Current");
            string reason = "scope-tail-merge";

            FrameworkScopeTailOperationRequest request = CreateRequest(
                currentOwner,
                default(RuntimeContentOwner),
                source,
                reason,
                rootCount: 1);

            bool bindingInvoked = false;
            bool removeInvoked = false;
            FrameworkScopeTailOperationResult result = FrameworkScopeTailOperationExecutor.Execute(
                request,
                delegate (FrameworkScopeTailOperationRequest cleanupRequest)
                {
                    bindingInvoked = true;
                    return CreateBindingCleanupSucceededResult(cleanupRequest.CurrentOwner, cleanupRequest.Source, cleanupRequest.Reason);
                },
                delegate (FrameworkScopeTailOperationRequest removeRequest)
                {
                    removeInvoked = true;
                    return CreatePreviousScopeRootRemovedResult(removeRequest.CurrentOwner, removeRequest.Source, removeRequest.Reason);
                });

            bool passed = !bindingInvoked
                && !removeInvoked
                && result.CurrentOwner == currentOwner
                && !result.HasPreviousOwner
                && result.BindingCleanupSkipped
                && result.PreviousScopeRootRemoveSkipped
                && result.ScopeResult.HasOwner
                && result.ScopeResult.Owner == currentOwner
                && result.ScopeResult.HasEnterRootResult
                && !result.ScopeResult.HasExitRootResult
                && result.ScopeResult.HasContext
                && string.Equals(result.Source, source, StringComparison.Ordinal)
                && string.Equals(result.Reason, reason, StringComparison.Ordinal)
                && !result.HasBlockingIssues
                && result.ScopeResult.Applied;

            LogStep(logger, "merge-without-previous-owner", result, passed);
            return passed;
        }

        private static bool ValidateSkipWithoutPreviousOwner(FrameworkLogger logger, string source)
        {
            RuntimeContentOwner currentOwner = RuntimeContentOwner.Session("scope-tail-no-previous", "ScopeTail.NoPrevious");
            string reason = "scope-tail-no-previous";

            FrameworkScopeTailOperationRequest request = CreateRequest(
                currentOwner,
                default(RuntimeContentOwner),
                source,
                reason,
                rootCount: 1);

            bool bindingInvoked = false;
            bool removeInvoked = false;
            FrameworkScopeTailOperationResult result = FrameworkScopeTailOperationExecutor.Execute(
                request,
                delegate (FrameworkScopeTailOperationRequest cleanupRequest)
                {
                    bindingInvoked = true;
                    return CreateBindingCleanupSucceededResult(cleanupRequest.CurrentOwner, cleanupRequest.Source, cleanupRequest.Reason);
                },
                delegate (FrameworkScopeTailOperationRequest removeRequest)
                {
                    removeInvoked = true;
                    return CreatePreviousScopeRootRemovedResult(removeRequest.CurrentOwner, removeRequest.Source, removeRequest.Reason);
                });

            bool passed = !bindingInvoked
                && !removeInvoked
                && result.BindingCleanupInvoked == false
                && result.PreviousScopeRootRemoveInvoked == false
                && result.BindingCleanupSkipped
                && result.PreviousScopeRootRemoveSkipped
                && !result.HasBlockingIssues;

            LogStep(logger, "skip-without-previous-owner", result, passed);
            return passed;
        }

        private static bool ValidateSkipWithSameOwner(FrameworkLogger logger, string source)
        {
            RuntimeContentOwner currentOwner = RuntimeContentOwner.Session("scope-tail-same-owner", "ScopeTail.SameOwner");
            string reason = "scope-tail-same-owner";

            FrameworkScopeTailOperationRequest request = CreateRequest(
                currentOwner,
                currentOwner,
                source,
                reason,
                rootCount: 1);

            bool bindingInvoked = false;
            bool removeInvoked = false;
            FrameworkScopeTailOperationResult result = FrameworkScopeTailOperationExecutor.Execute(
                request,
                delegate (FrameworkScopeTailOperationRequest cleanupRequest)
                {
                    bindingInvoked = true;
                    return CreateBindingCleanupSucceededResult(cleanupRequest.CurrentOwner, cleanupRequest.Source, cleanupRequest.Reason);
                },
                delegate (FrameworkScopeTailOperationRequest removeRequest)
                {
                    removeInvoked = true;
                    return CreatePreviousScopeRootRemovedResult(removeRequest.CurrentOwner, removeRequest.Source, removeRequest.Reason);
                });

            bool passed = !bindingInvoked
                && !removeInvoked
                && !result.BindingCleanupInvoked
                && !result.PreviousScopeRootRemoveInvoked
                && result.BindingCleanupSkipped
                && result.PreviousScopeRootRemoveSkipped
                && !result.HasBlockingIssues;

            LogStep(logger, "skip-with-same-owner", result, passed);
            return passed;
        }

        private static bool ValidateCleanupOrderingAndSuccess(FrameworkLogger logger, string source)
        {
            RuntimeContentOwner currentOwner = RuntimeContentOwner.Session("scope-tail-order-current", "ScopeTail.OrderCurrent");
            RuntimeContentOwner previousOwner = RuntimeContentOwner.Session("scope-tail-order-previous", "ScopeTail.OrderPrevious");
            string reason = "scope-tail-order";

            FrameworkScopeTailOperationRequest request = CreateRequest(
                currentOwner,
                previousOwner,
                source,
                reason,
                rootCount: 2);

            var order = new List<string>(2);
            FrameworkScopeTailOperationResult result = FrameworkScopeTailOperationExecutor.Execute(
                request,
                delegate (FrameworkScopeTailOperationRequest cleanupRequest)
                {
                    order.Add("bindingCleanup");
                    return CreateBindingCleanupSucceededResult(cleanupRequest.PreviousOwner, cleanupRequest.Source, cleanupRequest.Reason);
                },
                delegate (FrameworkScopeTailOperationRequest removeRequest)
                {
                    order.Add("removeRoot");
                    return CreatePreviousScopeRootRemovedResult(removeRequest.PreviousOwner, removeRequest.Source, removeRequest.Reason);
                });

            bool passed = order.Count == 2
                && string.Equals(order[0], "bindingCleanup", StringComparison.Ordinal)
                && string.Equals(order[1], "removeRoot", StringComparison.Ordinal)
                && result.CurrentOwner == currentOwner
                && result.PreviousOwner == previousOwner
                && result.BindingCleanupInvoked
                && result.PreviousScopeRootRemoveInvoked
                && result.BindingCleanupSucceeded
                && !result.BindingCleanupRejected
                && !result.PreviousScopeRootRemoveRejected
                && result.ScopeResult.HasExitRootResult
                && result.ScopeResult.ExitRootResult != null
                && result.ScopeResult.ExitRootResult.Status == RuntimeRootRegistryOperationStatus.RootRemoved
                && !result.HasBlockingIssues
                && string.Equals(result.Source, source, StringComparison.Ordinal)
                && string.Equals(result.Reason, reason, StringComparison.Ordinal);

            LogStep(logger, "cleanup-ordering-success", result, passed);
            return passed;
        }

        private static bool ValidateBindingCleanupRejected(FrameworkLogger logger, string source)
        {
            RuntimeContentOwner currentOwner = RuntimeContentOwner.Session("scope-tail-binding-rejected-current", "ScopeTail.BindingRejectedCurrent");
            RuntimeContentOwner previousOwner = RuntimeContentOwner.Session("scope-tail-binding-rejected-previous", "ScopeTail.BindingRejectedPrevious");
            string reason = "scope-tail-binding-rejected";

            FrameworkScopeTailOperationRequest request = CreateRequest(
                currentOwner,
                previousOwner,
                source,
                reason,
                rootCount: 2);

            var order = new List<string>(2);
            FrameworkScopeTailOperationResult result = FrameworkScopeTailOperationExecutor.Execute(
                request,
                delegate (FrameworkScopeTailOperationRequest cleanupRequest)
                {
                    order.Add("bindingCleanup");
                    return CreateBindingCleanupRejectedResult(cleanupRequest.PreviousOwner, cleanupRequest.Source, cleanupRequest.Reason);
                },
                delegate (FrameworkScopeTailOperationRequest removeRequest)
                {
                    order.Add("removeRoot");
                    return CreatePreviousScopeRootRemovedResult(removeRequest.PreviousOwner, removeRequest.Source, removeRequest.Reason);
                });

            bool passed = order.Count == 2
                && string.Equals(order[0], "bindingCleanup", StringComparison.Ordinal)
                && string.Equals(order[1], "removeRoot", StringComparison.Ordinal)
                && result.BindingCleanupInvoked
                && result.BindingCleanupRejected
                && result.PreviousScopeRootRemoveInvoked
                && !result.PreviousScopeRootRemoveRejected
                && result.HasBlockingIssues
                && string.Equals(result.Source, source, StringComparison.Ordinal)
                && string.Equals(result.Reason, reason, StringComparison.Ordinal);

            LogStep(logger, "binding-cleanup-rejected", result, passed);
            return passed;
        }

        private static bool ValidatePreviousScopeRootRemoveRejected(FrameworkLogger logger, string source)
        {
            RuntimeContentOwner currentOwner = RuntimeContentOwner.Session("scope-tail-remove-rejected-current", "ScopeTail.RemoveRejectedCurrent");
            RuntimeContentOwner previousOwner = RuntimeContentOwner.Session("scope-tail-remove-rejected-previous", "ScopeTail.RemoveRejectedPrevious");
            string reason = "scope-tail-remove-rejected";

            FrameworkScopeTailOperationRequest request = CreateRequest(
                currentOwner,
                previousOwner,
                source,
                reason,
                rootCount: 2);

            var order = new List<string>(2);
            FrameworkScopeTailOperationResult result = FrameworkScopeTailOperationExecutor.Execute(
                request,
                delegate (FrameworkScopeTailOperationRequest cleanupRequest)
                {
                    order.Add("bindingCleanup");
                    return CreateBindingCleanupSucceededResult(cleanupRequest.PreviousOwner, cleanupRequest.Source, cleanupRequest.Reason);
                },
                delegate (FrameworkScopeTailOperationRequest removeRequest)
                {
                    order.Add("removeRoot");
                    return CreatePreviousScopeRootRejectedResult(removeRequest.PreviousOwner, removeRequest.Source, removeRequest.Reason);
                });

            bool passed = order.Count == 2
                && string.Equals(order[0], "bindingCleanup", StringComparison.Ordinal)
                && string.Equals(order[1], "removeRoot", StringComparison.Ordinal)
                && result.BindingCleanupInvoked
                && result.BindingCleanupSucceeded
                && result.PreviousScopeRootRemoveInvoked
                && result.PreviousScopeRootRemoveRejected
                && result.ScopeResult.HasExitRootResult
                && result.ScopeResult.ExitRootResult != null
                && result.ScopeResult.ExitRootResult.Status == RuntimeRootRegistryOperationStatus.RejectedMissingRoot
                && result.HasBlockingIssues
                && result.ScopeResult.Rejected
                && string.Equals(result.Source, source, StringComparison.Ordinal)
                && string.Equals(result.Reason, reason, StringComparison.Ordinal);

            LogStep(logger, "remove-rejected", result, passed);
            return passed;
        }

        private static FrameworkScopeTailOperationRequest CreateRequest(
            RuntimeContentOwner currentOwner,
            RuntimeContentOwner previousOwner,
            string source,
            string reason,
            int rootCount)
        {
            RuntimeScopeRoot currentRoot = new RuntimeScopeRoot(currentOwner, source, reason);
            RuntimeRootRegistryOperationResult enterRootResult = RuntimeRootRegistryOperationResult.RootCreated(currentOwner, currentRoot, source, reason);
            RuntimeScopeContext context = new RuntimeScopeContext(currentOwner, source, reason);

            return new FrameworkScopeTailOperationRequest(
                currentOwner,
                previousOwner,
                enterRootResult,
                context,
                rootCount,
                source,
                reason);
        }

        private static ContentAnchorBindingLifecycleResult CreateBindingCleanupSucceededResult(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            return ContentAnchorBindingLifecycleResult.FromCounts(
                1,
                0,
                owner,
                default(RuntimeContentIdentity),
                default(ContentAnchorScope),
                default(FrameworkIdentityKey),
                default(ContentAnchorKind),
                default(ContentAnchorId),
                "binding-cleanup",
                source,
                reason,
                "Content Anchor binding cleanup completed.");
        }

        private static ContentAnchorBindingLifecycleResult CreateBindingCleanupRejectedResult(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            return new ContentAnchorBindingLifecycleResult(
                ContentAnchorBindingLifecycleStatus.RejectedInvalidRuntimeOwner,
                0,
                0,
                owner,
                default(RuntimeContentIdentity),
                default(ContentAnchorScope),
                default(FrameworkIdentityKey),
                default(ContentAnchorKind),
                default(ContentAnchorId),
                "binding-cleanup",
                source,
                reason,
                "Content Anchor binding cleanup rejected.");
        }

        private static RuntimeRootRegistryOperationResult CreatePreviousScopeRootRemovedResult(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            RuntimeScopeRoot root = new RuntimeScopeRoot(owner, source, reason);
            return RuntimeRootRegistryOperationResult.RootRemoved(owner, root, source, reason);
        }

        private static RuntimeRootRegistryOperationResult CreatePreviousScopeRootRejectedResult(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            return RuntimeRootRegistryOperationResult.RejectedMissingRoot(
                owner,
                default(RuntimeContentIdentity),
                false,
                source,
                reason);
        }

        private static void LogStep(FrameworkLogger logger, string step, FrameworkScopeTailOperationResult result, bool passed)
        {
            LogField[] fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("status", result.DiagnosticStatus),
                LogFields.Field("scopeStatus", result.ScopeResult.DiagnosticStatus),
                LogFields.Field("bindingCleanupInvoked", result.BindingCleanupInvoked),
                LogFields.Field("bindingCleanupSkipped", result.BindingCleanupSkipped),
                LogFields.Field("previousScopeRootRemoveInvoked", result.PreviousScopeRootRemoveInvoked),
                LogFields.Field("previousScopeRootRemoveSkipped", result.PreviousScopeRootRemoveSkipped),
                LogFields.Field("hasBlockingIssues", result.HasBlockingIssues),
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason));

            if (passed)
            {
                logger.Info("QA Scope Tail Operation Synthetic Smoke step completed.", fields);
                logger.Debug("QA Scope Tail Operation Synthetic Smoke details.", LogFields.Field("details", result.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Scope Tail Operation Synthetic Smoke step failed.", fields);
            logger.Debug("QA Scope Tail Operation Synthetic Smoke failure details.", LogFields.Field("details", result.ToDiagnosticString()));
        }
    }
}
#endif
