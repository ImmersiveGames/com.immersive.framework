using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Internal diagnostics for the previous Activity scope cleanup that becomes
    /// possible only after a Route Startup Player handoff commits.
    /// </summary>
    internal readonly struct RouteStartupActivityScopeFinalizationResult
    {
        internal RouteStartupActivityScopeFinalizationResult(
            ContentAnchorBindingLifecycleResult bindingCleanupResult,
            RuntimeRootRegistryOperationResult scopeRemovalResult,
            string message)
        {
            BindingCleanupResult = bindingCleanupResult;
            ScopeRemovalResult = scopeRemovalResult;
            Message = message ?? string.Empty;
        }

        internal ContentAnchorBindingLifecycleResult BindingCleanupResult { get; }
        internal RuntimeRootRegistryOperationResult ScopeRemovalResult { get; }
        internal string Message { get; }

        internal bool Succeeded =>
            BindingCleanupResult.Succeeded &&
            ScopeRemovalResult != null &&
            !ScopeRemovalResult.Rejected;

        internal string ToDiagnosticString() =>
            $"succeeded='{Succeeded}' bindingCleanup=[{BindingCleanupResult.ToDiagnosticString()}] " +
            $"scopeRemoval=[{(ScopeRemovalResult != null ? ScopeRemovalResult.ToDiagnosticString() : string.Empty)}] " +
            $"message='{Message}'";
    }
}
