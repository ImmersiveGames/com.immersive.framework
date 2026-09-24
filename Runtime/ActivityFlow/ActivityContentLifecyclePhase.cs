using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Identifies the lifecycle phase represented by an Activity content callback context.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public enum ActivityContentLifecyclePhase
    {
        None = 0,
        Entered = 1,
        Exited = 2
    }
}
