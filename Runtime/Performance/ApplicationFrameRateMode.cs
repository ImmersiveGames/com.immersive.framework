using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Performance
{
    /// <summary>
    /// Selects the application-level frame pacing policy applied during framework boot.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Application-level frame pacing authoring introduced by IF-APPLICATION-FRAME-RATE-01.")]
    public enum ApplicationFrameRateMode
    {
        /// <summary>
        /// Leaves Unity's current VSync and target frame-rate values unchanged.
        /// </summary>
        UseUnityDefaults = 0,

        /// <summary>
        /// Disables VSync and applies an explicit Application.targetFrameRate value.
        /// </summary>
        TargetFrameRate = 10,

        /// <summary>
        /// Restores Application.targetFrameRate to -1 and applies an explicit VSync count.
        /// </summary>
        VerticalSync = 20
    }
}
