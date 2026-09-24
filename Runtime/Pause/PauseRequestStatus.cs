
using Immersive.Framework.ApiStatus;
namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Stable. Passive status for a logical Pause request result.
    /// This is diagnostics data and does not imply input, overlay or timescale execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-player Pause/Input/Gate product surface. Multiplayer policy is out of scope.")]
    public enum PauseRequestStatus
    {
        /// <summary>Invalid default value. Do not use for canonical Pause results.</summary>
        Unknown = 0,

        Applied = 10,
        Rejected = 20,
        IgnoredNoChange = 30,
        Failed = 40
    }
}
