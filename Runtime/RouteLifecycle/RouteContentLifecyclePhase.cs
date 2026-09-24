using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Identifies the lifecycle phase represented by a Route content callback context.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route local content callbacks are active in the F3 Route baseline and may still change before stabilization.")]
    public enum RouteContentLifecyclePhase
    {
        None = 0,
        Entered = 1,
        Exited = 2
    }
}
