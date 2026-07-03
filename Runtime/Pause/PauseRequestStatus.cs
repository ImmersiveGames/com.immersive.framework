using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive status for a logical Pause request result.
    /// This is diagnostics data and does not imply input, overlay or timescale execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B Pause request status primitive; passive result status only.")]
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
