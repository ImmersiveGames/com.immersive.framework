using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Scene-scoped component lookup for loaded authored content.
    /// This is not a registry, service locator, materializer, loader, or lifecycle owner.
    /// It only enumerates roots from already-loaded Unity scenes selected by an explicit Route scene scope.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Loaded scene component query used by explicit Route and Activity content discovery scopes.")]
    internal static class SceneScopedComponentQuery
    {
        public static IReadOnlyList<T> GetComponentsInRoutePrimaryScene<T>(RouteAsset route)
            where T : Component
        {
            if (!TryGetLoadedPrimaryScene(route, out var scene))
            {
                return Array.Empty<T>();
            }

            return GetComponentsInScene<T>(scene);
        }

        public static IReadOnlyList<T> GetComponentsInLoadedScenes<T>()
            where T : Component
        {
            var components = new List<T>();
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                AddComponentsInScene(scene, components);
            }

            return components;
        }

        public static IReadOnlyList<T> GetComponentsInLoadedScene<T>(string scenePath, string sceneName)
            where T : Component
        {
            var scene = FindLoadedScene(scenePath, sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Array.Empty<T>();
            }

            return GetComponentsInScene<T>(scene);
        }

        internal static IReadOnlyList<T> GetComponentsInRouteContentScope<T>(
            RouteContentDiscoveryScope scope)
            where T : Component
        {
            var components = new List<T>();
            var seen = new HashSet<T>();
            IReadOnlyList<RouteContentDiscoveryScene> scenes = scope.RouteOwnedScenes;
            for (int i = 0; i < scenes.Count; i++)
            {
                RouteContentDiscoveryScene scene = scenes[i];
                AddDistinct(GetComponentsInLoadedScene<T>(scene.ScenePath, scene.SceneName), components, seen);
            }

            return components;
        }

        internal static IReadOnlyList<T> GetComponentsInActivityContentScope<T>(
            ActivityContentDiscoveryScope scope,
            ActivityAsset activity)
            where T : Component
        {
            var components = new List<T>();
            var seen = new HashSet<T>();

            AddDistinct(GetComponentsInRouteContentScope<T>(scope.RouteScope), components, seen);

            IReadOnlyList<ActivityContentDiscoveryScene> scenes = scope.ActivityOwnedScenes;
            for (int i = 0; i < scenes.Count; i++)
            {
                ActivityContentDiscoveryScene scene = scenes[i];
                if (!scene.MatchesActivity(activity))
                {
                    continue;
                }

                AddDistinct(
                    GetComponentsInLoadedScene<T>(scene.ScenePath, scene.SceneName),
                    components,
                    seen);
            }

            return components;
        }

        public static bool TryGetLoadedPrimaryScene(RouteAsset route, out Scene scene)
        {
            scene = default;

            if (route == null || !route.HasPrimaryScene)
            {
                return false;
            }

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var candidate = SceneManager.GetSceneAt(i);
                if (!candidate.IsValid() || !candidate.isLoaded)
                {
                    continue;
                }

                if (MatchesRoutePrimaryScene(candidate, route))
                {
                    scene = candidate;
                    return true;
                }
            }

            return false;
        }

        private static Scene FindLoadedScene(string scenePath, string sceneName)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var candidate = SceneManager.GetSceneAt(i);
                if (!candidate.IsValid() || !candidate.isLoaded)
                {
                    continue;
                }

                if (MatchesScene(candidate, scenePath, sceneName))
                {
                    return candidate;
                }
            }

            return default;
        }

        private static IReadOnlyList<T> GetComponentsInScene<T>(Scene scene)
            where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Array.Empty<T>();
            }

            var components = new List<T>();
            AddComponentsInScene(scene, components);
            return components;
        }

        private static void AddComponentsInScene<T>(Scene scene, List<T> components)
            where T : Component
        {
            if (components == null || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                return;
            }

            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T[] found = root.GetComponentsInChildren<T>(true);
                if (found == null || found.Length == 0)
                {
                    continue;
                }

                for (int j = 0; j < found.Length; j++)
                {
                    if (found[j] != null)
                    {
                        components.Add(found[j]);
                    }
                }
            }
        }

        private static void AddDistinct<T>(
            IReadOnlyList<T> candidates,
            List<T> destination,
            HashSet<T> seen)
            where T : Component
        {
            if (candidates == null || destination == null || seen == null)
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                T candidate = candidates[i];
                if (candidate != null && seen.Add(candidate))
                {
                    destination.Add(candidate);
                }
            }
        }

        private static bool MatchesRoutePrimaryScene(Scene scene, RouteAsset route)
        {
            if (route == null || !scene.IsValid())
            {
                return false;
            }

            return MatchesScene(scene, route.PrimaryScenePath, route.PrimarySceneName);
        }

        private static bool MatchesScene(Scene scene, string scenePath, string sceneName)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            // Path is the functional scene identity when authored. Do not fall back to name
            // when a path is present but does not match (homonym scenes / stale paths).
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase);
            }

            return !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(scene.name, sceneName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
