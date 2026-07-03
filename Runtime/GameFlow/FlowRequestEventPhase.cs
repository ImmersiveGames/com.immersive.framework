using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Public phase for authored Game Flow request events.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public enum FlowRequestEventPhase
    {
        Submitted = 0,
        Completed = 1
    }
}
