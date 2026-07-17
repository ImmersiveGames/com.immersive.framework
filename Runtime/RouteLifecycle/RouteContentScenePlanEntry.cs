using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Runtime planning record for one additional Route scene declaration.
    /// This is not a loaded scene handle yet.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Deferred, "Route content profile planning data only; additive execution is deferred to F6.")]
    internal readonly struct RouteContentScenePlanEntry
    {
        public RouteContentScenePlanEntry(
            string contentId,
            string sceneName,
            string scenePath,
            FrameworkContentRequiredness requiredness,
            int declarationIndex)
        {
            ContentId = Normalize(contentId);
            SceneName = Normalize(sceneName);
            ScenePath = Normalize(scenePath);
            Requiredness = requiredness;
            DeclarationIndex = declarationIndex;
        }

        public string ContentId { get; }

        public string SceneName { get; }

        public string ScenePath { get; }

        public FrameworkContentRequiredness Requiredness { get; }

        public int DeclarationIndex { get; }

        public bool HasScene => !string.IsNullOrWhiteSpace(SceneName) || !string.IsNullOrWhiteSpace(ScenePath);

        public string ToDiagnosticString()
        {
            string scene = !string.IsNullOrWhiteSpace(SceneName) ? SceneName : ScenePath;
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "<missing>";
            }

            return $"id='{ContentId}' scene='{scene}' requiredness='{Requiredness}' index='{DeclarationIndex}'";
        }

        public static RouteContentScenePlanEntry FromEntry(RouteContentSceneEntry entry, int index)
        {
            if (entry == null)
            {
                return new RouteContentScenePlanEntry(
                    $"route-scene:{index}",
                    string.Empty,
                    string.Empty,
                    FrameworkContentRequiredness.Optional,
                    index);
            }

            return new RouteContentScenePlanEntry(
                entry.HasExplicitContentId ? entry.ExplicitContentId : string.Empty,
                entry.SceneName,
                entry.ScenePath,
                entry.Requiredness,
                index);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
