using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal sealed class RouteEnteredEvent : IEvent
    {
        public RouteEnteredEvent(RouteAsset route, RouteAsset previousRoute, string source, string reason)
        {
            Route = route;
            PreviousRoute = previousRoute;
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
        }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}
