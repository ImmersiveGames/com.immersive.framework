using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Performance;
using Immersive.Framework.ProgressionSave;

namespace Immersive.Framework.Bootstrap
{
    /// <summary>
    /// Shared internal validation for the minimal framework boot configuration.
    /// Used by runtime boot and editor status preview to avoid duplicated rules.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal static class FrameworkBootValidator
    {
        internal static FrameworkBootResult Validate(ImmersiveFrameworkSettingsAsset settings)
        {
            if (settings == null)
            {
                return FrameworkBootResult.Failed(
                    "No framework settings asset was found. Open Project Settings > Immersive Framework and configure the project.");
            }

            ApplicationFrameRatePolicy frameRatePolicy =
                settings.FrameRatePolicy;
            if (frameRatePolicy == null)
            {
                return FrameworkBootResult.Failed(
                    "Project Frame Rate policy is missing. Configure Performance > Frame Rate in Project Settings > Immersive Framework.");
            }

            if (!frameRatePolicy.TryValidate(out string frameRateIssue))
            {
                return FrameworkBootResult.Failed(
                    $"Project Frame Rate policy is invalid. {frameRateIssue}");
            }

            var gameApplication = settings.ActiveGameApplication;
            if (gameApplication == null)
            {
                return FrameworkBootResult.Failed(
                    "Active Game Application is missing. Assign one in Project Settings > Immersive Framework.");
            }

            if (gameApplication.ProgressionSaveEnabled)
            {
                ProgressionSaveProfile progressionSaveProfile =
                    gameApplication.DefaultProgressionSaveProfile;

                if (progressionSaveProfile == null)
                {
                    return FrameworkBootResult.Failed(
                        "Progression Save is enabled but Default Progression Save Profile is missing. Assign one in the active Game Application asset.");
                }

                if (!progressionSaveProfile.TryValidate(
                        out string progressionSaveIssue))
                {
                    return FrameworkBootResult.Failed(
                        $"Default Progression Save Profile '{progressionSaveProfile.name}' is invalid. {progressionSaveIssue}");
                }
            }

            var startupRoute = gameApplication.StartupRoute;
            if (startupRoute == null)
            {
                return FrameworkBootResult.Failed(
                    "Startup Route is missing. Assign one in the active Game Application asset.");
            }

            if (!startupRoute.HasPrimaryScene)
            {
                return FrameworkBootResult.Failed(
                    "Startup Route Primary Scene is missing. Assign a Primary Scene in the Startup Route asset.");
            }

            return FrameworkBootResult.Started(gameApplication);
        }
    }
}
