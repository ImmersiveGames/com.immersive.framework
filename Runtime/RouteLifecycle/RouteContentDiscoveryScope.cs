using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using UnityEngine;

namespace Immersive.Framework.RouteLifecycle
{
    internal readonly struct RouteContentDiscoveryScope
    {
        private readonly RouteContentDiscoveryScene[] _routeOwnedScenes;
        private readonly GameObject[] _routeOwnedRoots;

        internal RouteContentDiscoveryScope(
            RouteAsset route,
            IReadOnlyList<RouteContentDiscoveryScene> routeOwnedScenes,
            IReadOnlyList<GameObject> routeOwnedRoots)
        {
            Route = route;
            _routeOwnedScenes = CopyScenes(routeOwnedScenes);
            _routeOwnedRoots = CopyRoots(routeOwnedRoots, _routeOwnedScenes);
        }

        internal RouteAsset Route { get; }

        internal IReadOnlyList<RouteContentDiscoveryScene> RouteOwnedScenes =>
            _routeOwnedScenes ?? Array.Empty<RouteContentDiscoveryScene>();

        internal IReadOnlyList<GameObject> RouteOwnedRoots =>
            _routeOwnedRoots ?? Array.Empty<GameObject>();

        internal static RouteContentDiscoveryScope FromCompositionResult(
            RouteSceneCompositionResult result,
            IReadOnlyList<GameObject> routeOwnedRoots)
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

            return new RouteContentDiscoveryScope(result.Route, scenes, routeOwnedRoots);
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

        private static GameObject[] CopyRoots(
            IReadOnlyList<GameObject> roots,
            IReadOnlyList<RouteContentDiscoveryScene> ownedScenes)
        {
            if (roots == null || roots.Count == 0 || ownedScenes == null || ownedScenes.Count == 0)
            {
                return Array.Empty<GameObject>();
            }

            var copy = new List<GameObject>(roots.Count);
            for (int i = 0; i < roots.Count; i++)
            {
                GameObject root = roots[i];
                if (root == null || !BelongsToOwnedRouteScene(root, ownedScenes))
                {
                    continue;
                }

                copy.Add(root);
            }

            return copy.ToArray();
        }

        private static bool BelongsToOwnedRouteScene(
            GameObject root,
            IReadOnlyList<RouteContentDiscoveryScene> ownedScenes)
        {
            if (root == null || !root.scene.IsValid() || !root.scene.isLoaded)
            {
                return false;
            }

            for (int i = 0; i < ownedScenes.Count; i++)
            {
                RouteContentDiscoveryScene ownedScene = ownedScenes[i];
                if (!string.IsNullOrWhiteSpace(ownedScene.ScenePath))
                {
                    if (string.Equals(root.scene.path, ownedScene.ScenePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    continue;
                }

                if (string.Equals(root.scene.name, ownedScene.SceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
