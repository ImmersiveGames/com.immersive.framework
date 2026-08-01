using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    internal readonly struct RouteContentDiscoveryScope
    {
        private readonly RouteContentDiscoveryScene[] _routeOwnedScenes;

        internal RouteContentDiscoveryScope(RouteAsset route, IReadOnlyList<RouteContentDiscoveryScene> routeOwnedScenes)
        {
            Route = route;
            _routeOwnedScenes = CopyScenes(routeOwnedScenes);
        }

        internal RouteAsset Route { get; }

        internal IReadOnlyList<RouteContentDiscoveryScene> RouteOwnedScenes =>
            _routeOwnedScenes ?? Array.Empty<RouteContentDiscoveryScene>();

        internal static RouteContentDiscoveryScope FromCompositionResult(RouteSceneCompositionResult result)
        {
            var scenes = new List<RouteContentDiscoveryScene>();
            var sceneKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < result.Entries.Count; i++)
            {
                RouteSceneCompositionResultEntry entry = result.Entries[i];
                if (!entry.IsOwnedLoaded || !AddSceneKey(sceneKeys, entry.ScenePath, entry.SceneName))
                {
                    continue;
                }

                scenes.Add(new RouteContentDiscoveryScene(entry));
            }

            return new RouteContentDiscoveryScope(result.Route, scenes);
        }

        private static RouteContentDiscoveryScene[] CopyScenes(IReadOnlyList<RouteContentDiscoveryScene> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<RouteContentDiscoveryScene>();
            }

            var copy = new RouteContentDiscoveryScene[scenes.Count];
            for (int i = 0; i < scenes.Count; i++)
            {
                copy[i] = scenes[i];
            }

            return copy;
        }

        private static bool AddSceneKey(HashSet<string> sceneKeys, string scenePath, string sceneName)
        {
            if (sceneKeys == null)
            {
                return false;
            }

            string sceneKey = !string.IsNullOrWhiteSpace(scenePath)
                ? $"path:{scenePath.Trim()}"
                : !string.IsNullOrWhiteSpace(sceneName) ? $"name:{sceneName.Trim()}" : string.Empty;
            return !string.IsNullOrWhiteSpace(sceneKey) && sceneKeys.Add(sceneKey);
        }
    }
}
