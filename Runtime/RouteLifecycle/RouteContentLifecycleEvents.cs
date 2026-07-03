using UnityEngine;
using UnityEngine.Events;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Scene-authored UnityEvent bridge for Route content lifecycle.
    /// Use this under a RouteContentBinding root when no custom gameplay script is needed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Route Content Lifecycle Events")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route local content callbacks are active in the F3 Route baseline and may still change before stabilization.")]
    public sealed class RouteContentLifecycleEvents : RouteContentBehaviour
    {
        [Header("Route Content Lifecycle")]
        [SerializeField] private UnityEvent entered = new UnityEvent();
        [SerializeField] private UnityEvent exited = new UnityEvent();

        public UnityEvent Entered => entered;

        public UnityEvent Exited => exited;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            entered?.Invoke();
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            exited?.Invoke();
        }
    }
}
