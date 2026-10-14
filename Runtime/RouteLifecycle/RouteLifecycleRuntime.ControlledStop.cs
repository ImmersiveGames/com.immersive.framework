using System;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed partial class RouteLifecycleRuntime
    {
        private ControlledStopRouteExitLedger _controlledStopRouteExitLedger;

        internal async Task<RouteExitResult> ExitCurrentRouteForControlledStopAsync(
            string source,
            string reason)
        {
            try
            {
                return await ExecuteCurrentRouteControlledStopExitAsync(source, reason);
            }
            catch (Exception exception)
            {
                RouteRuntimeState retainedRouteState = _controlledStopRouteExitLedger != null
                    ? _controlledStopRouteExitLedger.RouteState
                    : _currentRouteState;
                return RouteExitResult.Failed(
                    retainedRouteState,
                    null,
                    source,
                    reason,
                    $"Controlled Route exit threw before it could confirm cleanup. {exception.GetType().Name}: {exception.Message}");
            }
        }

        private async Task<RouteExitResult> ExecuteCurrentRouteControlledStopExitAsync(
            string source,
            string reason)
        {
            string resolvedSource = source.NormalizeTextOrFallback("FrameworkRuntimeHost");
            string resolvedReason = reason.NormalizeTextOrFallback("controlled-stop-route-exit");
            if (!_hasCurrentRouteContext || CurrentRoute == null)
            {
                _controlledStopRouteExitLedger = null;
                return RouteExitResult.None(
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            RouteRuntimeState routeState = _currentRouteState;
            RouteAsset route = routeState.Route;
            if (_activityFlowRuntime.HasActiveActivity)
            {
                return RouteExitResult.Failed(
                    routeState,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "Canonical Activity exit must complete before controlled Route exit.");
            }

            if (_controlledStopRouteExitLedger == null)
            {
                _controlledStopRouteExitLedger = new ControlledStopRouteExitLedger(
                    routeState,
                    _currentRouteContentDiscoveryScope);
            }
            else if (!ReferenceEquals(_controlledStopRouteExitLedger.RouteState.Route, route))
            {
                return RouteExitResult.Failed(
                    routeState,
                    null,
                    resolvedSource,
                    resolvedReason,
                    "A different Route became current while controlled Route exit receipts were retained.");
            }

            ControlledStopRouteExitLedger ledger = _controlledStopRouteExitLedger;
            if (!ledger.RouteContentCallbacksExited)
            {
                try
                {
                    ledger.RouteContentExitResult = _routeContentRuntime.ExitRouteContent(
                        ledger.DiscoveryScope,
                        null,
                        resolvedSource,
                        resolvedReason);
                    ledger.RouteContentCallbacksExited = true;
                    if (ledger.RouteContentExitResult.HasFailures)
                    {
                        Debug.LogWarning(
                            "Controlled Route exit observed best-effort Route Content callback failures. " +
                            $"route='{route.RouteName}' failedReceivers='{ledger.RouteContentExitResult.FailedReceiverCount}'.");
                    }
                }
                catch (Exception exception)
                {
                    return RouteExitResult.Failed(
                        routeState,
                        null,
                        resolvedSource,
                        resolvedReason,
                        $"Route Content exit callbacks threw before their stage receipt was recorded. {exception.GetType().Name}: {exception.Message}");
                }
            }

            if (!ledger.PlayerSpatialEntryExited)
            {
                try
                {
                    ExitCurrentPlayerSpatialEntry();
                    ledger.PlayerSpatialEntryExited = true;
                }
                catch (Exception exception)
                {
                    return RouteExitResult.Failed(
                        routeState,
                        null,
                        resolvedSource,
                        resolvedReason,
                        $"Route Player spatial exit failed; the exact occurrence context remains retained for retry. {exception.GetType().Name}: {exception.Message}");
                }
            }

            if (!ledger.ResettableOwnersReleased)
            {
                if (!TryReleasePreviousRouteResettableRegistration(
                        route,
                        resolvedSource,
                        "controlled-stop-route-exit",
                        out string registrationIssue))
                {
                    return RouteExitResult.Failed(
                        routeState,
                        null,
                        resolvedSource,
                        resolvedReason,
                        registrationIssue);
                }

                ledger.ResettableOwnersReleased = true;
            }

            if (!ledger.RouteContentReleased)
            {
                ContentReleasePlan releasePlan = routeState.RouteContentSet.CreateReleasePlan(
                    resolvedSource,
                    resolvedReason);
                ContentReleaseResult releaseResult = await _contentReleaseRuntime.ExecuteAsync(releasePlan);
                if (releaseResult.Failed || releaseResult.HasBlockingIssues)
                {
                    return RouteExitResult.Failed(
                        routeState,
                        null,
                        resolvedSource,
                        resolvedReason,
                        releaseResult.ToDiagnosticString());
                }

                ledger.RouteContentReleased = true;
                if (releaseResult.HasIssues)
                {
                    Debug.LogWarning(
                        "Controlled Route exit completed Route content release with non-blocking issues. " +
                        releaseResult.ToDiagnosticString());
                }
            }

            while (ledger.NextAdditionalSceneScopeIndex < routeState.RouteSceneCompositionResult.Entries.Count)
            {
                RouteSceneCompositionResultEntry entry =
                    routeState.RouteSceneCompositionResult.Entries[
                        ledger.NextAdditionalSceneScopeIndex];
                if (!entry.Loaded || entry.SceneRole == RouteSceneRole.Primary)
                {
                    ledger.NextAdditionalSceneScopeIndex++;
                    continue;
                }

                if (!_sceneLifecycleRuntime.IsSceneLoaded(entry.SceneName, entry.ScenePath))
                {
                    ledger.NextAdditionalSceneScopeIndex++;
                    continue;
                }

                SceneCompositionResult releaseResult =
                    _sceneLifecycleRuntime.ReleaseLoadedSceneScope(
                        entry.SceneName,
                        entry.ScenePath,
                        "controlled-route-stop");
                if (!releaseResult.Succeeded)
                {
                    return RouteExitResult.Failed(
                        routeState,
                        null,
                        resolvedSource,
                        resolvedReason,
                        releaseResult.Diagnostic);
                }

                ledger.NextAdditionalSceneScopeIndex++;
            }

            if (!ledger.PrimarySceneScopeReleased)
            {
                SceneLifecycleLoadResult primaryScene = routeState.SceneLifecycleResult;
                SceneCompositionResult releaseResult =
                    _sceneLifecycleRuntime.ReleaseLoadedPrimarySceneScope(
                        primaryScene.SceneName,
                        primaryScene.ScenePath,
                        "controlled-route-stop");
                if (!releaseResult.Succeeded)
                {
                    return RouteExitResult.Failed(
                        routeState,
                        null,
                        resolvedSource,
                        resolvedReason,
                        releaseResult.Diagnostic);
                }

                ledger.PrimarySceneScopeReleased = true;
            }

            if (!ledger.RouteScopeRootRemoved)
            {
                RuntimeRootRegistryOperationResult removeRootResult =
                    _runtimeContentRuntime.RemoveScopeRoot(
                        CreateRouteOwner(route),
                        resolvedSource,
                        "controlled-stop-route-exit");
                if (removeRootResult.Rejected)
                {
                    return RouteExitResult.Failed(
                        routeState,
                        null,
                        resolvedSource,
                        resolvedReason,
                        removeRootResult.ToDiagnosticString());
                }

                ledger.RouteScopeRootRemoved = true;
            }

            _activityFlowRuntime.SetRouteContentDiscoveryScope(default);
            _currentRouteState = RouteRuntimeState.Empty();
            _currentRouteContentDiscoveryScope = default;
            _currentPlayerSpatialEntryContext = default;
            _currentRouteResult = default;
            _hasCurrentRouteContext = false;
            _controlledStopRouteExitLedger = null;
            _routeExitedEvents.Publish(new RouteExitedEvent(
                route,
                null,
                resolvedSource,
                resolvedReason));
            return RouteExitResult.Exited(
                routeState,
                null,
                resolvedSource,
                resolvedReason);
        }

        private sealed class ControlledStopRouteExitLedger
        {
            internal ControlledStopRouteExitLedger(
                RouteRuntimeState routeState,
                RouteContentDiscoveryScope discoveryScope)
            {
                RouteState = routeState;
                DiscoveryScope = discoveryScope;
            }

            internal RouteRuntimeState RouteState { get; }

            internal RouteContentDiscoveryScope DiscoveryScope { get; }

            internal RouteContentLifecycleDispatchResult RouteContentExitResult { get; set; }

            internal bool RouteContentCallbacksExited { get; set; }

            internal bool PlayerSpatialEntryExited { get; set; }

            internal bool ResettableOwnersReleased { get; set; }

            internal bool RouteContentReleased { get; set; }

            internal bool PrimarySceneScopeReleased { get; set; }

            internal int NextAdditionalSceneScopeIndex { get; set; }

            internal bool RouteScopeRootRemoved { get; set; }
        }
    }
}
