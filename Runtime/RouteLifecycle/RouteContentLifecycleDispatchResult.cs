using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Diagnostics-only result for Route Content local lifecycle callback dispatch.
    /// This records execution of the local callback boundary; it does not own content, release content or load scenes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline diagnostics surface introduced by F3D for Route Content callback execution.")]
    internal readonly struct RouteContentLifecycleDispatchResult
    {
        public RouteContentLifecycleDispatchResult(
            RouteContentLifecyclePhase phase,
            RouteAsset route,
            RouteAsset otherRoute,
            bool executed,
            int bindingCount,
            int receiverCount,
            int failedReceiverCount,
            string source,
            string reason)
        {
            Phase = phase;
            Route = route;
            OtherRoute = otherRoute;
            Executed = executed;
            BindingCount = bindingCount;
            ReceiverCount = receiverCount;
            FailedReceiverCount = failedReceiverCount;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public RouteContentLifecyclePhase Phase { get; }

        public RouteAsset Route { get; }

        public RouteAsset OtherRoute { get; }

        public bool Executed { get; }

        public int BindingCount { get; }

        public int ReceiverCount { get; }

        public int FailedReceiverCount { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool HasRoute => Route != null;

        public bool HasBindings => BindingCount > 0;

        public bool HasReceivers => ReceiverCount > 0;

        public bool HasFailures => FailedReceiverCount > 0;

        public string DiagnosticStatus => Executed ? "Executed" : "Skipped";

        public string DiagnosticPhase => Phase == RouteContentLifecyclePhase.Exited ? "Exited" : "Entered";

        public string RouteName => Route != null ? Route.RouteName : string.Empty;

        public static RouteContentLifecycleDispatchResult Skipped(
            RouteContentLifecyclePhase phase,
            RouteAsset route,
            RouteAsset otherRoute,
            string source,
            string reason)
        {
            return new RouteContentLifecycleDispatchResult(
                phase,
                route,
                otherRoute,
                false,
                0,
                0,
                0,
                source,
                reason);
        }

        public static RouteContentLifecycleDispatchResult ExecutedWith(
            RouteContentLifecyclePhase phase,
            RouteAsset route,
            RouteAsset otherRoute,
            int bindingCount,
            int receiverCount,
            int failedReceiverCount,
            string source,
            string reason)
        {
            return new RouteContentLifecycleDispatchResult(
                phase,
                route,
                otherRoute,
                true,
                bindingCount,
                receiverCount,
                failedReceiverCount,
                source,
                reason);
        }
    }
}
