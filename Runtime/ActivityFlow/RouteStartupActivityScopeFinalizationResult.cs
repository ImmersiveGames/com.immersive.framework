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
            RuntimeRootRegistryOperationResult scopeRemovalResult,
            string message)
        {
            ScopeRemovalResult = scopeRemovalResult;
            Message = message ?? string.Empty;
        }

        internal RuntimeRootRegistryOperationResult ScopeRemovalResult { get; }
        internal string Message { get; }

        internal bool Succeeded =>
            ScopeRemovalResult != null &&
            !ScopeRemovalResult.Rejected;

        internal string ToDiagnosticString() =>
            $"succeeded='{Succeeded}' scopeRemoval=[{(ScopeRemovalResult != null ? ScopeRemovalResult.ToDiagnosticString() : string.Empty)}] " +
            $"message='{Message}'";
    }
}
