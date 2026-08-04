using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Performance
{
    /// <summary>
    /// Immutable evidence describing the requested and effective Unity frame pacing values.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Runtime diagnostic result introduced by IF-APPLICATION-FRAME-RATE-01.")]
    public readonly struct ApplicationFrameRateApplicationResult
    {
        internal ApplicationFrameRateApplicationResult(
            ApplicationFrameRateApplicationStatus status,
            ApplicationFrameRateMode requestedMode,
            int requestedTargetFrameRate,
            int requestedVSyncCount,
            int previousTargetFrameRate,
            int previousVSyncCount,
            int appliedTargetFrameRate,
            int appliedVSyncCount,
            RuntimePlatform platform,
            string message)
        {
            Status = status;
            RequestedMode = requestedMode;
            RequestedTargetFrameRate = requestedTargetFrameRate;
            RequestedVSyncCount = requestedVSyncCount;
            PreviousTargetFrameRate = previousTargetFrameRate;
            PreviousVSyncCount = previousVSyncCount;
            AppliedTargetFrameRate = appliedTargetFrameRate;
            AppliedVSyncCount = appliedVSyncCount;
            Platform = platform;
            Message = message ?? string.Empty;
        }

        public ApplicationFrameRateApplicationStatus Status { get; }

        public ApplicationFrameRateMode RequestedMode { get; }

        public int RequestedTargetFrameRate { get; }

        public int RequestedVSyncCount { get; }

        public int PreviousTargetFrameRate { get; }

        public int PreviousVSyncCount { get; }

        public int AppliedTargetFrameRate { get; }

        public int AppliedVSyncCount { get; }

        public RuntimePlatform Platform { get; }

        public string Message { get; }

        public bool Succeeded =>
            Status == ApplicationFrameRateApplicationStatus.Applied ||
            Status == ApplicationFrameRateApplicationStatus.AppliedNoChange ||
            Status == ApplicationFrameRateApplicationStatus.AppliedPlatformLimited ||
            Status == ApplicationFrameRateApplicationStatus.AppliedNoChangePlatformLimited ||
            Status == ApplicationFrameRateApplicationStatus.SkippedUnityDefaults;

        public bool Changed =>
            Status == ApplicationFrameRateApplicationStatus.Applied ||
            Status == ApplicationFrameRateApplicationStatus.AppliedPlatformLimited;

        public bool IsPlatformLimited =>
            Status == ApplicationFrameRateApplicationStatus.AppliedPlatformLimited ||
            Status == ApplicationFrameRateApplicationStatus.AppliedNoChangePlatformLimited;
    }
}
