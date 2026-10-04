using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>Observes committed Activity transitions while its Route composition is active.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route-scoped observation of committed Activity transitions.")]
    public interface IRouteActivityTransitionObserver
    {
        void OnActivityTransitionCommitted(RouteActivityTransitionContext context);
    }
}
