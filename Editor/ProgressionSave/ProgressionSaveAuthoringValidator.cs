using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Validation;
namespace Immersive.Framework.Editor.ProgressionSave
{
    internal static class ProgressionSaveAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport
            ValidateGameApplication(
                GameApplicationAsset gameApplication)
        {
            var report =
                new FrameworkAuthoringValidationReport(
                    gameApplication != null
                        ? gameApplication.ValidationMode
                        : FrameworkValidationMode.Standard);

            if (gameApplication == null)
            {
                report.AddError(
                    "Game Application is missing for Progression Save validation.",
                    null);
                return report;
            }

            if (!gameApplication.ProgressionSaveEnabled)
            {
                report.AddInfo(
                    "Progression Save is disabled. No application-scoped Progression Save Runtime will be created.",
                    gameApplication);
                return report;
            }

            var profile =
                gameApplication.DefaultProgressionSaveProfile;

            if (profile == null)
            {
                report.AddError(
                    "Progression Save is enabled but Default Progression Save Profile is missing.",
                    gameApplication);
                return report;
            }

            if (!profile.TryValidate(
                    out string issue))
            {
                report.AddError(
                    issue,
                    profile);
                return report;
            }

            if (profile.Backend ==
                global::Immersive.Framework.ProgressionSave.ProgressionSaveBackendSelection.BuiltInJson)
            {
                report.AddInfo(
                    "Progression Save is configured for the official Built-in JSON backend.",
                    profile);
            }
            else
            {
                report.AddInfo(
                    $"Progression Save is configured for Custom Provider '{profile.CustomProvider.name}'. No fallback backend is permitted.",
                    profile);
            }

            return report;
        }
    }
}
