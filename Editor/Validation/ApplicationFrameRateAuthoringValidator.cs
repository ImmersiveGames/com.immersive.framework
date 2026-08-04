using Immersive.Framework.Authoring;
using Immersive.Framework.Performance;
using UnityEditor;

namespace Immersive.Framework.Editor.Editor.Validation
{
    internal static class ApplicationFrameRateAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport Validate(
            GameApplicationAsset gameApplication)
        {
            FrameworkValidationMode validationMode =
                gameApplication != null
                    ? gameApplication.ValidationMode
                    : FrameworkValidationMode.Standard;

            var report =
                new FrameworkAuthoringValidationReport(
                    validationMode);

            if (gameApplication == null)
            {
                report.AddError(
                    "Game Application is missing for frame-rate validation.",
                    null);
                return report;
            }

            ApplicationFrameRatePolicy policy =
                gameApplication.FrameRatePolicy;

            if (policy == null)
            {
                report.AddError(
                    "Application Frame Rate policy is missing.",
                    gameApplication);
                return report;
            }

            if (!policy.TryValidate(out string issue))
            {
                report.AddError(
                    issue,
                    gameApplication);
                return report;
            }

            switch (policy.Mode)
            {
                case ApplicationFrameRateMode.UseUnityDefaults:
                    report.AddInfo(
                        "Application Frame Rate uses Unity defaults and will not override VSync or target frame rate.",
                        gameApplication);
                    break;

                case ApplicationFrameRateMode.TargetFrameRate:
                    report.AddInfo(
                        $"Application Frame Rate will disable VSync and request {policy.TargetFrameRate} FPS during framework boot.",
                        gameApplication);
                    break;

                case ApplicationFrameRateMode.VerticalSync:
                    if (IsMobileBuildTarget(
                            EditorUserBuildSettings.activeBuildTarget))
                    {
                        report.AddWarning(
                            "Vertical Sync is selected for a mobile build target. Mobile platforms use Application.targetFrameRate for frame pacing and may ignore QualitySettings.vSyncCount.",
                            gameApplication);
                    }
                    else
                    {
                        report.AddInfo(
                            $"Application Frame Rate will restore target frame rate to -1 and request VSync Count {policy.VSyncCount} during framework boot.",
                            gameApplication);
                    }

                    break;
            }

            return report;
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
