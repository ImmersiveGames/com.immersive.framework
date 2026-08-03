using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Minimal immutable result for starting the Game Flow.
    /// This is diagnostics data for the first route handoff, not a global runtime service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct FrameworkGameFlowStartResult
    {
        public FrameworkGameFlowStartResult(
            bool started,
            string message,
            RouteAsset startupRoute,
            RouteLifecycleStartResult routeLifecycleResult,
            bool destinationAuthoritative = false,
            ActivityEntryReadinessExecutionStatus entryReadinessStatus =
                ActivityEntryReadinessExecutionStatus.Unknown)
        {
            Started = started;
            Message = message ?? string.Empty;
            StartupRoute = startupRoute;
            RouteLifecycleResult = routeLifecycleResult;
            _destinationAuthoritative = destinationAuthoritative;
            EntryReadinessStatus = entryReadinessStatus;
        }

        public bool Started { get; }

        public string Message { get; }

        public RouteAsset StartupRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

        public bool DestinationAuthoritative => Started || _destinationAuthoritative;

        internal ActivityEntryReadinessExecutionStatus EntryReadinessStatus { get; }

        private readonly bool _destinationAuthoritative;

        public SceneLifecycleLoadResult SceneLifecycleResult => RouteLifecycleResult.SceneLifecycleResult;

        public static FrameworkGameFlowStartResult Failed(string message)
        {
            return new FrameworkGameFlowStartResult(false, message, null, default);
        }

        public static FrameworkGameFlowStartResult StartedWith(RouteAsset startupRoute, RouteLifecycleStartResult routeLifecycleResult)
        {
            return new FrameworkGameFlowStartResult(
                true,
                $"Game Flow started with Startup Route '{startupRoute.RouteName}'. {routeLifecycleResult.Message}",
                startupRoute,
                routeLifecycleResult);
        }

        internal static FrameworkGameFlowStartResult FailedCommittedDestination(
            string message,
            RouteAsset startupRoute,
            RouteLifecycleStartResult routeLifecycleResult,
            ActivityEntryReadinessExecutionStatus entryReadinessStatus)
        {
            return new FrameworkGameFlowStartResult(
                false,
                message,
                startupRoute,
                routeLifecycleResult,
                true,
                entryReadinessStatus);
        }
    }
}
