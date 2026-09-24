using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Inert planning record for one scene declared by a Route scene composition plan.
    /// This is not a loaded scene handle and it does not authorize release/unload.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Route scene composition plan entry consumed by F6E; release is deferred.")]
    internal readonly struct RouteSceneCompositionPlanEntry
    {
        public RouteSceneCompositionPlanEntry(
            FrameworkContentIdentity contentIdentity,
            string contentId,
            string sceneName,
            string scenePath,
            RouteSceneRole sceneRole,
            FrameworkContentRequiredness requiredness,
            RouteContentOwnership ownership,
            RouteSceneLoadMode loadMode,
            int executionOrder,
            bool hasExplicitContentId)
        {
            ContentIdentity = contentIdentity;
            ContentId = Normalize(contentId);
            SceneName = Normalize(sceneName);
            ScenePath = Normalize(scenePath);
            SceneRole = sceneRole;
            Requiredness = requiredness;
            Ownership = ownership;
            LoadMode = loadMode;
            ExecutionOrder = executionOrder;
            HasExplicitContentId = hasExplicitContentId;
        }

        public FrameworkContentIdentity ContentIdentity { get; }

        public string ContentId { get; }

        public string SceneName { get; }

        public string ScenePath { get; }

        public RouteSceneRole SceneRole { get; }

        public FrameworkContentScope Scope => FrameworkContentScope.Route;

        public FrameworkContentKind Kind => FrameworkContentKind.Scene;

        public FrameworkContentRequiredness Requiredness { get; }

        public RouteContentOwnership Ownership { get; }

        public RouteSceneLoadMode LoadMode { get; }

        public int ExecutionOrder { get; }

        public bool HasExplicitContentId { get; }

        public bool HasContentIdentity => ContentIdentity.IsValid;

        public bool HasScene => !string.IsNullOrWhiteSpace(SceneName) || !string.IsNullOrWhiteSpace(ScenePath);

        public bool IsPrimary => SceneRole == RouteSceneRole.Primary;

        public bool IsAdditive => SceneRole == RouteSceneRole.Additive;

        public bool IsExecutionReady => HasScene && HasContentIdentity && (IsPrimary || HasExplicitContentId);

        public string ToDiagnosticString()
        {
            string scene = !string.IsNullOrWhiteSpace(SceneName) ? SceneName : ScenePath;
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "<missing>";
            }

            string identity = HasContentIdentity ? ContentIdentity.StableText : "<missing>";
            string contentId = ContentId.ToDiagnosticText("<missing>");
            return $"identity='{identity}' id='{contentId}' scene='{scene}' role='{SceneRole}' requiredness='{Requiredness}' ownership='{Ownership}' loadMode='{LoadMode}' order='{ExecutionOrder}' explicitId='{HasExplicitContentId}' executionReady='{IsExecutionReady}'";
        }

        public static RouteSceneCompositionPlanEntry Primary(RouteAsset route, string routeOwnerId)
        {
            if (route == null || !route.HasPrimaryScene)
            {
                return MissingPrimary();
            }

            string sceneName = Normalize(route.PrimarySceneName);
            string scenePath = Normalize(route.PrimaryScenePath);
            string sceneIdentity = !string.IsNullOrWhiteSpace(sceneName) ? sceneName : scenePath;
            string contentId = $"primary-scene:{sceneIdentity}";
            var identity = FrameworkContentIdentity.FromOwnerValue(
                FrameworkContentScope.Route,
                FrameworkContentKind.Scene,
                routeOwnerId,
                contentId);

            return new RouteSceneCompositionPlanEntry(
                identity,
                contentId,
                sceneName,
                scenePath,
                RouteSceneRole.Primary,
                FrameworkContentRequiredness.Required,
                RouteContentOwnership.Owned,
                RouteSceneLoadMode.Single,
                0,
                true);
        }

        public static RouteSceneCompositionPlanEntry Additive(
            RouteContentSceneEntry entry,
            int declarationIndex,
            string routeOwnerId)
        {
            string explicitContentId = entry != null ? Normalize(entry.ExplicitContentId) : string.Empty;
            string sceneName = entry != null ? Normalize(entry.SceneName) : string.Empty;
            string scenePath = entry != null ? Normalize(entry.ScenePath) : string.Empty;
            var requiredness = entry?.Requiredness ?? FrameworkContentRequiredness.Optional;
            bool hasExplicitContentId = !string.IsNullOrWhiteSpace(explicitContentId);
            FrameworkContentIdentity identity = default;
            if (hasExplicitContentId)
            {
                identity = FrameworkContentIdentity.FromOwnerValue(
                    FrameworkContentScope.Route,
                    FrameworkContentKind.Scene,
                    routeOwnerId,
                    explicitContentId);
            }

            return new RouteSceneCompositionPlanEntry(
                identity,
                explicitContentId,
                sceneName,
                scenePath,
                RouteSceneRole.Additive,
                requiredness,
                RouteContentOwnership.Owned,
                RouteSceneLoadMode.Additive,
                declarationIndex + 1,
                hasExplicitContentId);
        }

        private static RouteSceneCompositionPlanEntry MissingPrimary()
        {
            return new RouteSceneCompositionPlanEntry(
                default,
                string.Empty,
                string.Empty,
                string.Empty,
                RouteSceneRole.Primary,
                FrameworkContentRequiredness.Required,
                RouteContentOwnership.Owned,
                RouteSceneLoadMode.Single,
                0,
                false);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
