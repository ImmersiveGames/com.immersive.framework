using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Performance
{
    /// <summary>
    /// Applies validated application frame pacing intent exactly once per host startup pass.
    /// The operation is idempotent and performs no partial mutation for invalid policy.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "Framework runtime implementation detail; not game-facing API.")]
    internal static class ApplicationFrameRatePolicyApplier
    {
        internal static ApplicationFrameRateApplicationResult Apply(
            ApplicationFrameRatePolicy policy)
        {
            int previousTargetFrameRate =
                Application.targetFrameRate;
            int previousVSyncCount =
                QualitySettings.vSyncCount;
            RuntimePlatform platform =
                Application.platform;

            if (policy == null)
            {
                return CreateResult(
                    ApplicationFrameRateApplicationStatus
                        .RejectedInvalidPolicy,
                    ApplicationFrameRateMode.UseUnityDefaults,
                    0,
                    0,
                    previousTargetFrameRate,
                    previousVSyncCount,
                    previousTargetFrameRate,
                    previousVSyncCount,
                    platform,
                    "Application frame-rate policy is missing.");
            }

            if (!policy.TryValidate(out string issue))
            {
                return CreateResult(
                    ApplicationFrameRateApplicationStatus
                        .RejectedInvalidPolicy,
                    policy.Mode,
                    policy.TargetFrameRate,
                    policy.VSyncCount,
                    previousTargetFrameRate,
                    previousVSyncCount,
                    previousTargetFrameRate,
                    previousVSyncCount,
                    platform,
                    issue);
            }

            if (policy.Mode ==
                ApplicationFrameRateMode.UseUnityDefaults)
            {
                return CreateResult(
                    ApplicationFrameRateApplicationStatus
                        .SkippedUnityDefaults,
                    policy.Mode,
                    policy.TargetFrameRate,
                    policy.VSyncCount,
                    previousTargetFrameRate,
                    previousVSyncCount,
                    previousTargetFrameRate,
                    previousVSyncCount,
                    platform,
                    "Unity frame pacing values were preserved by policy.");
            }

            int requestedTargetFrameRate;
            int requestedVSyncCount;

            if (policy.Mode ==
                ApplicationFrameRateMode.TargetFrameRate)
            {
                requestedTargetFrameRate =
                    policy.TargetFrameRate;
                requestedVSyncCount = 0;
            }
            else
            {
                requestedTargetFrameRate = -1;
                requestedVSyncCount =
                    policy.VSyncCount;
            }

            bool noChange =
                previousTargetFrameRate ==
                    requestedTargetFrameRate &&
                previousVSyncCount ==
                    requestedVSyncCount;

            if (!noChange)
            {
                // Apply both values only after the complete policy has passed validation.
                Application.targetFrameRate =
                    requestedTargetFrameRate;
                QualitySettings.vSyncCount =
                    requestedVSyncCount;
            }

            bool platformLimited =
                policy.Mode ==
                    ApplicationFrameRateMode.VerticalSync &&
                IsMobilePlatform(platform);

            ApplicationFrameRateApplicationStatus status =
                platformLimited
                    ? noChange
                        ? ApplicationFrameRateApplicationStatus
                            .AppliedNoChangePlatformLimited
                        : ApplicationFrameRateApplicationStatus
                            .AppliedPlatformLimited
                    : noChange
                        ? ApplicationFrameRateApplicationStatus
                            .AppliedNoChange
                        : ApplicationFrameRateApplicationStatus
                            .Applied;

            string message = platformLimited
                ? "Vertical Sync values were applied, but this mobile platform controls frame pacing through Application.targetFrameRate."
                : noChange
                    ? "Application frame-rate policy already matched the current Unity values."
                    : "Application frame-rate policy was applied.";

            return CreateResult(
                status,
                policy.Mode,
                policy.TargetFrameRate,
                policy.VSyncCount,
                previousTargetFrameRate,
                previousVSyncCount,
                Application.targetFrameRate,
                QualitySettings.vSyncCount,
                platform,
                message);
        }

        private static bool IsMobilePlatform(
            RuntimePlatform platform)
        {
            return platform == RuntimePlatform.Android ||
                   platform == RuntimePlatform.IPhonePlayer ||
                   platform == RuntimePlatform.tvOS;
        }

        private static ApplicationFrameRateApplicationResult CreateResult(
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
            return new ApplicationFrameRateApplicationResult(
                status,
                requestedMode,
                requestedTargetFrameRate,
                requestedVSyncCount,
                previousTargetFrameRate,
                previousVSyncCount,
                appliedTargetFrameRate,
                appliedVSyncCount,
                platform,
                message);
        }
    }
}
