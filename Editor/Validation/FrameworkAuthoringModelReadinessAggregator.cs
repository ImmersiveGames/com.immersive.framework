using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;

namespace Immersive.Framework.Editor.Editor.Validation
{
    /// <summary>
    /// Canonical Editor-only aggregation point for the complete Model Readiness report.
    /// Domain validators remain independent and non-mutating; this class only composes evidence.
    /// </summary>
    internal static class FrameworkAuthoringModelReadinessAggregator
    {
        internal static FrameworkAuthoringValidationReport ValidateProjectReadiness(
            ImmersiveFrameworkSettingsAsset settings,
            bool includeOpenSceneBindings)
        {
            FrameworkAuthoringValidationReport report =
                FrameworkAuthoringModelReadinessValidator.ValidateProjectReadiness(
                    settings,
                    includeOpenSceneBindings);

            if (settings == null || settings.ActiveGameApplication == null)
            {
                return report;
            }

            GameApplicationAsset gameApplication = settings.ActiveGameApplication;
            FrameworkValidationMode validationMode = gameApplication.ValidationMode;

            report.AddRange(
                PlayerParticipationAuthoringValidator.ValidateGameApplication(
                    gameApplication,
                    includeConfiguredProfileValidation: false));
            report.AddRange(
                PlayerParticipationAuthoringValidator.ValidateProjectProfiles(validationMode));
            report.AddRange(
                ActivityParticipationProjectionAuthoringValidator.ValidateProjectAssets(
                    validationMode));
            report.AddInfo(
                $"P3D Player participation readiness aggregated. totalIssues='{report.TotalIssueCount}' blockingIssues='{report.ErrorCount}' warnings='{report.WarningCount}' optionalSkips='{report.OptionalSkipCount}'.",
                gameApplication);

            return report;
        }
    }
}
