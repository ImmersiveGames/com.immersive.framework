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
                ActivityEntryReadinessExecutionStatus.Unknown,
            bool committedTargetRevealFailed = false,
            bool preCommitTransitionFailed = false)
        {
            Started = started;
            Message = message ?? string.Empty;
            StartupRoute = startupRoute;
            RouteLifecycleResult = routeLifecycleResult;
            _destinationAuthoritative = destinationAuthoritative;
            EntryReadinessStatus = entryReadinessStatus;
            CommittedTargetRevealFailed = committedTargetRevealFailed;
            PreCommitTransitionFailed = preCommitTransitionFailed;
        }

        public bool Started { get; }

        public string Message { get; }

        public RouteAsset StartupRoute { get; }

        public RouteLifecycleStartResult RouteLifecycleResult { get; }

        public bool DestinationAuthoritative => Started || _destinationAuthoritative;

        /// <summary>
        /// True when Startup destination was committed but Transition After / reveal failed.
        /// Distinct from readiness failure.
        /// </summary>
        public bool CommittedTargetRevealFailed { get; }

        /// <summary>
        /// True when Transition Before failed before Startup destination lifecycle/commit.
        /// </summary>
        public bool PreCommitTransitionFailed { get; }

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

        internal static FrameworkGameFlowStartResult FailedPreCommitTransition(
            string message,
            RouteAsset startupRoute = null)
        {
            return new FrameworkGameFlowStartResult(
                false,
                message,
                startupRoute,
                default,
                destinationAuthoritative: false,
                entryReadinessStatus: ActivityEntryReadinessExecutionStatus.Unknown,
                committedTargetRevealFailed: false,
                preCommitTransitionFailed: true);
        }

        internal static FrameworkGameFlowStartResult FailedCommittedTargetReveal(
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
                destinationAuthoritative: true,
                entryReadinessStatus: entryReadinessStatus,
                committedTargetRevealFailed: true,
                preCommitTransitionFailed: false);
        }
    }
}
