using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Public phase for authored Game Flow request events.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable Game Flow request outcome/phase vocabulary. Breaking changes require ADR/migration.")]
    public enum FlowRequestEventPhase
    {
        Submitted = 0,
        Completed = 1
    }
}
