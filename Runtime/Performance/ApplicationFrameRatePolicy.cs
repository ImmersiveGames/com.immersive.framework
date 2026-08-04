using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Performance
{
    /// <summary>
    /// Immutable-at-runtime application intent for Unity frame pacing.
    /// Runtime state remains owned by Unity and is applied by the framework host.
    /// </summary>
    [Serializable]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Application-level frame pacing authoring introduced by IF-APPLICATION-FRAME-RATE-01.")]
    public sealed class ApplicationFrameRatePolicy
    {
        [SerializeField]
        [Tooltip("Use Unity Defaults leaves current values unchanged. Target Frame Rate disables VSync. Vertical Sync restores target frame rate to -1 and applies the selected VSync interval.")]
        private ApplicationFrameRateMode mode =
            ApplicationFrameRateMode.UseUnityDefaults;

        [SerializeField]
        [Min(1)]
        [Tooltip("Requested Application.targetFrameRate when Mode is Target Frame Rate.")]
        private int targetFrameRate = 60;

        [SerializeField]
        [Range(1, 4)]
        [Tooltip("Requested QualitySettings.vSyncCount when Mode is Vertical Sync.")]
        private int vSyncCount = 1;

        public ApplicationFrameRateMode Mode => mode;

        public int TargetFrameRate => targetFrameRate;

        public int VSyncCount => vSyncCount;

        /// <summary>
        /// Validates authored intent without mutating Unity runtime settings.
        /// </summary>
        public bool TryValidate(out string issue)
        {
            if (!Enum.IsDefined(
                    typeof(ApplicationFrameRateMode),
                    mode))
            {
                issue =
                    $"Application frame-rate mode '{mode}' is not defined.";
                return false;
            }

            switch (mode)
            {
                case ApplicationFrameRateMode.UseUnityDefaults:
                    issue = string.Empty;
                    return true;

                case ApplicationFrameRateMode.TargetFrameRate:
                    if (targetFrameRate <= 0)
                    {
                        issue =
                            "Target Frame Rate mode requires a value greater than zero.";
                        return false;
                    }

                    issue = string.Empty;
                    return true;

                case ApplicationFrameRateMode.VerticalSync:
                    if (vSyncCount < 1 ||
                        vSyncCount > 4)
                    {
                        issue =
                            "Vertical Sync mode requires a VSync Count between 1 and 4.";
                        return false;
                    }

                    issue = string.Empty;
                    return true;

                default:
                    issue =
                        $"Application frame-rate mode '{mode}' is unsupported.";
                    return false;
            }
        }
    }
}
