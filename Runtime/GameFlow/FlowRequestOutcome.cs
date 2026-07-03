using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Public coarse outcome for authored Game Flow requests.
    /// Specific framework result kinds remain internal implementation details.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public enum FlowRequestOutcome
    {
        None = 0,
        Submitted = 1,
        Succeeded = 2,
        Ignored = 3,
        Failed = 4
    }
}
