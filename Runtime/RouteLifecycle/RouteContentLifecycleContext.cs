using Immersive.Framework.Authoring;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Local context passed to scene-authored Route content receivers.
    /// This describes one Route content root reacting to the canonical Route lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route local content callbacks are active in the F3 Route baseline and may still change before stabilization.")]
    public readonly struct RouteContentLifecycleContext
    {
        internal RouteContentLifecycleContext(
            RouteContentLifecyclePhase phase,
            RouteAsset route,
            RouteAsset previousRoute,
            RouteAsset nextRoute,
            RouteContentBinding binding,
            GameObject contentRoot,
            string source,
            string reason)
        {
            Phase = phase;
            Route = route;
            PreviousRoute = previousRoute;
            NextRoute = nextRoute;
            Binding = binding;
            ContentRoot = contentRoot;
            Source = source.NormalizeTextOrFallback("Unknown");
            Reason = reason.NormalizeTextOrFallback("None");
        }

        public RouteContentLifecyclePhase Phase { get; }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }

        public RouteAsset NextRoute { get; }

        public RouteContentBinding Binding { get; }

        public GameObject ContentRoot { get; }

        public string Source { get; }

        public string Reason { get; }

        public string RouteName => Route != null ? Route.RouteName : string.Empty;

        public string PreviousRouteName => PreviousRoute != null ? PreviousRoute.RouteName : string.Empty;

        public string NextRouteName => NextRoute != null ? NextRoute.RouteName : string.Empty;

        public bool IsEntered => Phase == RouteContentLifecyclePhase.Entered;

        public bool IsExited => Phase == RouteContentLifecyclePhase.Exited;

        internal static RouteContentLifecycleContext Entered(
            RouteAsset route,
            RouteAsset previousRoute,
            RouteContentBinding binding,
            string source,
            string reason)
        {
            return new RouteContentLifecycleContext(
                RouteContentLifecyclePhase.Entered,
                route,
                previousRoute,
                route,
                binding,
                binding != null ? binding.gameObject : null,
                source,
                reason);
        }

        internal static RouteContentLifecycleContext Exited(
            RouteAsset route,
            RouteAsset nextRoute,
            RouteContentBinding binding,
            string source,
            string reason)
        {
            return new RouteContentLifecycleContext(
                RouteContentLifecyclePhase.Exited,
                route,
                route,
                nextRoute,
                binding,
                binding != null ? binding.gameObject : null,
                source,
                reason);
        }
    }
}
