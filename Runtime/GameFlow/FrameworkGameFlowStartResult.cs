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
            RouteLifecycleStartResult routeLifecycleResult)
        {
            Started = started;
            Message = message ?? string.Empty;
            StartupRoute = startupRoute;
            RouteLifecycleResult = routeLifecycleResult;
        }

        public bool Started { get; }

        public string Message { get; }

        public RouteAsset StartupRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

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
    }
}
