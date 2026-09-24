using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal immutable result for exiting the previous Route during a Route switch.
    /// This is state/diagnostics data only; it does not execute release, loading, local callbacks or materialization.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route exit result boundary introduced by F3C; not game-facing API.")]
    internal readonly struct RouteExitResult
    {
        public RouteExitResult(
            bool completed,
            RouteAsset route,
            RouteRuntimeState routeState,
            RouteAsset nextRoute,
            string source,
            string reason,
            string message)
        {
            Completed = completed;
            Route = route;
            RouteState = routeState;
            NextRoute = nextRoute;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Completed { get; }

        public RouteAsset Route { get; }

        public RouteRuntimeState RouteState { get; }

        public RouteAsset NextRoute { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool HasRoute => Route != null;

        public bool HasRouteState => RouteState.HasRoute;

        public bool HasNextRoute => NextRoute != null;

        public string RouteName => Route != null ? Route.RouteName : string.Empty;

        public string NextRouteName => NextRoute != null ? NextRoute.RouteName : string.Empty;

        public string RouteIdentity => RouteState.DiagnosticIdentity;

        public string DiagnosticStatus => Completed ? "Exited" : "None";

        public static RouteExitResult None(RouteAsset nextRoute, string source, string reason)
        {
            string nextRouteName = nextRoute.ToDiagnosticText(x => x.RouteName, "<missing>");
            return new RouteExitResult(
                false,
                null,
                RouteRuntimeState.Empty(),
                nextRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                $"Route Exit skipped. No previous Route was active. nextRoute='{nextRouteName}'.");
        }

        public static RouteExitResult Exited(
            RouteRuntimeState previousRouteState,
            RouteAsset nextRoute,
            string source,
            string reason)
        {
            if (!previousRouteState.HasRoute)
            {
                return None(nextRoute, source, reason);
            }

            string nextRouteName = nextRoute.ToDiagnosticText(x => x.RouteName, "<missing>");
            string identityMessage = previousRouteState.HasIdentity
                ? $" routeIdentity='{previousRouteState.DiagnosticIdentity}'."
                : string.Empty;

            return new RouteExitResult(
                true,
                previousRouteState.Route,
                previousRouteState,
                nextRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                $"Route Exit completed. exitedRoute='{previousRouteState.RouteName}' nextRoute='{nextRouteName}'.{identityMessage}");
        }

        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback("Unknown");
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback("None");
        }
    }
}
