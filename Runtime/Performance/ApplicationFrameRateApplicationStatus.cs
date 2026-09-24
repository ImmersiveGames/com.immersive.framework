using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Performance
{
    /// <summary>
    /// Typed terminal status for one application frame-rate policy application.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Runtime diagnostic result introduced by IF-APPLICATION-FRAME-RATE-01.")]
    public enum ApplicationFrameRateApplicationStatus
    {
        Unknown = 0,
        Applied = 10,
        AppliedNoChange = 20,
        AppliedPlatformLimited = 30,
        AppliedNoChangePlatformLimited = 35,
        SkippedUnityDefaults = 40,
        RejectedInvalidPolicy = 100
    }
}
