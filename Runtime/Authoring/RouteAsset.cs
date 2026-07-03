using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Public authoring asset retained for the baseline before F1/F3 identity and route-state hardening.
    /// Public authoring asset that identifies an entry in the game flow.
    /// This asset declares the route primary scene, optional route content profile, and optional startup activity.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Route",
        menuName = "Immersive Framework/Route",
        order = 10)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class RouteAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Human-readable route name shown in framework diagnostics. If empty, the asset name is used.")]
        private string routeName = "Startup Route";

        [SerializeField]
        [Tooltip("Project-relative path of the primary Unity scene declared by this route. Managed by the Route Inspector.")]
        private string primaryScenePath = string.Empty;

        [SerializeField]
        [Tooltip("Cached human-readable scene name shown in framework diagnostics.")]
        private string primarySceneName = string.Empty;

        [SerializeField]
        [Tooltip("Optional Route Content Profile. Route scene composition loads execution-ready additional scenes additively.")]
        private RouteContentProfileAsset routeContentProfile;

        [SerializeField]
        [Tooltip("Optional first Activity started after this route primary scene is resolved.")]
        private ActivityAsset startupActivity;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Optional authoring note for the route. This has no runtime behavior yet.")]
        private string description = string.Empty;

        public string RouteName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(routeName))
                {
                    return routeName.Trim();
                }

                return !string.IsNullOrWhiteSpace(name) ? name : "Route";
            }
        }

        public string PrimaryScenePath => primaryScenePath ?? string.Empty;

        public string PrimarySceneName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(primarySceneName))
                {
                    return primarySceneName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(primaryScenePath))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(primaryScenePath);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        return fileName;
                    }
                }

                return string.Empty;
            }
        }

        public bool HasPrimaryScene => !string.IsNullOrWhiteSpace(primaryScenePath);

        public RouteContentProfileAsset RouteContentProfile => routeContentProfile;

        public bool HasRouteContentProfile => routeContentProfile != null;

        public ActivityAsset StartupActivity => startupActivity;

        public bool HasStartupActivity => startupActivity != null;

        public string Description => description ?? string.Empty;
    }
}
