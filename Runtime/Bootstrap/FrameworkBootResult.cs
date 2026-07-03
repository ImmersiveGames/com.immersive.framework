using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Bootstrap
{
    /// <summary>
    /// Minimal immutable result for the framework boot attempt.
    /// This is diagnostics data, not a service locator entry point.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public readonly struct FrameworkBootResult
    {
        public FrameworkBootResult(
            bool succeeded,
            string message,
            GameApplicationAsset gameApplication,
            RouteAsset startupRoute,
            FrameworkValidationMode validationMode)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            GameApplication = gameApplication;
            StartupRoute = startupRoute;
            ValidationMode = validationMode;
        }

        public bool Succeeded { get; }

        public string Message { get; }

        public GameApplicationAsset GameApplication { get; }

        public RouteAsset StartupRoute { get; }

        public FrameworkValidationMode ValidationMode { get; }

        public static FrameworkBootResult Failed(string message)
        {
            return new FrameworkBootResult(
                false,
                message,
                null,
                null,
                FrameworkValidationMode.Strict);
        }

        public static FrameworkBootResult Started(GameApplicationAsset gameApplication)
        {
            var startupRoute = gameApplication.StartupRoute;
            return new FrameworkBootResult(
                true,
                $"Game Application '{gameApplication.ApplicationName}' resolved. Startup Route '{startupRoute.RouteName}' resolved. Primary Scene '{startupRoute.PrimarySceneName}' declared.",
                gameApplication,
                startupRoute,
                gameApplication.ValidationMode);
        }
    }
}
