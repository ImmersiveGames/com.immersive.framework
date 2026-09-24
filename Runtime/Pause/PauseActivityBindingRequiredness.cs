using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Declares whether an Activity requires product Pause binding for its admitted Local Player.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "P2.1A explicit Pause Activity binding intent requiredness.")]
    public enum PauseActivityBindingRequiredness
    {
        Unknown = 0,
        Required = 10
    }
}
