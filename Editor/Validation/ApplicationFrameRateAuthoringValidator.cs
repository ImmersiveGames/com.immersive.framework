using Immersive.Framework.Authoring;
using Immersive.Framework.Performance;
using UnityEditor;

namespace Immersive.Framework.Editor.Editor.Validation
{
    internal static class ApplicationFrameRateAuthoringValidator
    {
        /// <summary>
        /// Project-level frame-rate validation. Project Settings is the only authored
        /// authority for the current ADR-017 Stage A boundary.
        /// </summary>
        internal static FrameworkAuthoringValidationReport Validate(
            ImmersiveFrameworkSettingsAsset settings)
        {
            FrameworkValidationMode validationMode =
                settings != null &&
                settings.ActiveGameApplication != null
                    ? settings.ActiveGameApplication.ValidationMode
                    : FrameworkValidationMode.Standard;

            var report =
                new FrameworkAuthoringValidationReport(
                    validationMode);

            if (settings == null)
            {
                report.AddError(
                    "Framework Settings asset is missing for frame-rate validation.",
                    null);
                return report;
            }

            ApplicationFrameRatePolicy policy =
                settings.FrameRatePolicy;

            if (policy == null)
            {
                report.AddError(
                    "Project Frame Rate policy is missing.",
                    settings);
                return report;
            }

            if (!policy.TryValidate(out string issue))
            {
                report.AddError(
                    issue,
                    settings);
                return report;
            }

            switch (policy.Mode)
            {
                case ApplicationFrameRateMode.UseUnityDefaults:
                    report.AddInfo(
                        "Project Frame Rate uses Unity defaults and will not override VSync or target frame rate.",
                        settings);
                    break;

                case ApplicationFrameRateMode.TargetFrameRate:
                    report.AddInfo(
                        $"Project Frame Rate will disable VSync and request {policy.TargetFrameRate} FPS during framework boot.",
                        settings);
                    break;

                case ApplicationFrameRateMode.VerticalSync:
                    if (IsMobileBuildTarget(
                            EditorUserBuildSettings.activeBuildTarget))
                    {
                        report.AddWarning(
                            "Vertical Sync is selected for a mobile build target. Mobile platforms use Application.targetFrameRate for frame pacing and may ignore QualitySettings.vSyncCount.",
                            settings);
                    }
                    else
                    {
                        report.AddInfo(
                            $"Project Frame Rate will restore target frame rate to -1 and request VSync Count {policy.VSyncCount} during framework boot.",
                            settings);
                    }

                    break;
            }

            return report;
        }

        /// <summary>
        /// GameApplicationAsset no longer owns frame-rate authoring. This overload
        /// intentionally contributes no frame-rate findings to Game Application validation.
        /// </summary>
        internal static FrameworkAuthoringValidationReport Validate(
            GameApplicationAsset gameApplication)
        {
            FrameworkValidationMode validationMode =
                gameApplication != null
                    ? gameApplication.ValidationMode
                    : FrameworkValidationMode.Standard;

            return new FrameworkAuthoringValidationReport(
                validationMode);
        }

        private static bool IsMobileBuildTarget(
            BuildTarget buildTarget)
        {
            return buildTarget == BuildTarget.Android ||
                   buildTarget == BuildTarget.iOS ||
                   buildTarget == BuildTarget.tvOS;
        }
    }
}
