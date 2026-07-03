using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Common;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Immutable result for a completed runtime route request.
    /// This is diagnostics data and does not expose a service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct FrameworkRouteRequestResult
    {
        public FrameworkRouteRequestResult(
            FrameworkRouteRequestKind kind,
            string message,
            RouteAsset targetRoute,
            string source,
            string reason,
            RouteLifecycleStartResult routeLifecycleResult,
            FrameworkTransitionDiagnostics transitionDiagnostics = default,
            TransitionGateDiagnostics transitionGateDiagnostics = default)
        {
            Kind = kind;
            Message = message.NormalizeText();
            TargetRoute = targetRoute;
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
            RouteLifecycleResult = routeLifecycleResult;
            TransitionDiagnostics = transitionDiagnostics;
            TransitionGateDiagnostics = transitionGateDiagnostics;
        }

        public FrameworkRouteRequestKind Kind { get; }

        public string Message { get; }

        public RouteAsset TargetRoute { get; }

        public string Source { get; }

        public string Reason { get; }

        internal RouteLifecycleStartResult RouteLifecycleResult { get; }

        internal FrameworkTransitionDiagnostics TransitionDiagnostics { get; }

        internal TransitionGateDiagnostics TransitionGateDiagnostics { get; }

        public bool Succeeded => Kind == FrameworkRouteRequestKind.Succeeded;

        public static FrameworkRouteRequestResult FailedInvalidConfig(
            string message,
            RouteAsset targetRoute = null,
            string source = null,
            string reason = null,
            TransitionGateDiagnostics transitionGateDiagnostics = default)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.FailedInvalidConfig,
                message,
                targetRoute,
                source,
                reason,
                default,
                default,
                transitionGateDiagnostics);
        }

        public static FrameworkRouteRequestResult FailedRuntimeUnavailable(
            string message,
            RouteAsset targetRoute = null,
            string source = null,
            string reason = null)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.FailedRuntimeUnavailable,
                message,
                targetRoute,
                source,
                reason,
                default);
        }

        public static FrameworkRouteRequestResult IgnoredAlreadyActive(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.IgnoredAlreadyActive,
                $"Route Request ignored. {FormatRequestContext(source, reason)} Route '{targetRoute.RouteName}' is already active.",
                targetRoute,
                source,
                reason,
                default);
        }

        public static FrameworkRouteRequestResult IgnoredAlreadyInFlight(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            string routeName = targetRoute.ToDiagnosticText(x => x.RouteName, "<missing>");
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.IgnoredAlreadyInFlight,
                $"Route Request ignored. {FormatRequestContext(source, reason)} Another route request is already in flight. targetRoute='{routeName}'.",
                targetRoute,
                source,
                reason,
                default);
        }

        internal static FrameworkRouteRequestResult IgnoredBlockedByGate(
            RouteAsset targetRoute,
            string source,
            string reason,
            GateEvaluationResult gateEvaluation,
            TransitionGateDiagnostics transitionGateDiagnostics = default)
        {
            string routeName = targetRoute.ToDiagnosticText(x => x.RouteName, "<missing>");
            bool blockedByTransitionGate = transitionGateDiagnostics.HasBlockingEvaluation;
            return new FrameworkRouteRequestResult(
                blockedByTransitionGate
                    ? FrameworkRouteRequestKind.RejectedByTransitionGate
                    : FrameworkRouteRequestKind.IgnoredAlreadyInFlight,
                $"Route Request ignored. {FormatRequestContext(source, reason)} targetRoute='{routeName}'. {GateRequestAdmission.FormatBlockedMessage("Route Request", gateEvaluation)}",
                targetRoute,
                source,
                reason,
                default,
                default,
                transitionGateDiagnostics);
        }

        internal static FrameworkRouteRequestResult SucceededWith(
            RouteAsset targetRoute,
            string source,
            string reason,
            RouteLifecycleStartResult routeLifecycleResult,
            FrameworkTransitionDiagnostics transitionDiagnostics = default,
            TransitionGateDiagnostics transitionGateDiagnostics = default)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.Succeeded,
                $"Route Request completed. {FormatRequestContext(source, reason)} {routeLifecycleResult.Message}",
                targetRoute,
                source,
                reason,
                routeLifecycleResult,
                transitionDiagnostics,
                transitionGateDiagnostics);
        }

        private static string FormatRequestContext(string source, string reason)
        {
            return $"source='{source.NormalizeTextOrFallback("Unknown")}' reason='{reason.NormalizeTextOrFallback("None")}'.";
        }
    }
}
