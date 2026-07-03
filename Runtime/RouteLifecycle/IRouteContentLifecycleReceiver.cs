using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Implement this on a component under a Route Content Binding root when scene-authored content
    /// needs to react to Route enter/exit without owning Route Lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route local content callbacks are active in the F3 Route baseline and may still change before stabilization.")]
    public interface IRouteContentLifecycleReceiver
    {
        void OnRouteContentEntered(RouteContentLifecycleContext context);

        void OnRouteContentExited(RouteContentLifecycleContext context);
    }
}
