using Immersive.Framework.ContentFlow;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    internal readonly struct RouteContentDiscoveryScene
    {
        internal RouteContentDiscoveryScene(RouteSceneCompositionResultEntry entry)
        {
            ScenePath = entry.ScenePath.NormalizeText();
            SceneName = entry.SceneName.NormalizeText();
            ContentId = entry.ContentId.NormalizeText();
            Requiredness = entry.Requiredness;
            Source = entry.SceneRole == RouteSceneRole.Primary
                ? RouteContentDiscoverySceneSource.Primary
                : RouteContentDiscoverySceneSource.Additional;
        }

        internal string ScenePath { get; }

        internal string SceneName { get; }

        internal string ContentId { get; }

        internal FrameworkContentRequiredness Requiredness { get; }

        internal RouteContentDiscoverySceneSource Source { get; }
    }
}
