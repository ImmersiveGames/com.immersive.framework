using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Component discovery over explicit, already-loaded Route and Activity scene compositions.
    /// It does not load scenes, infer ownership, or fall back to unrelated loaded scenes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Composition-scoped component query for explicit Route and Activity content discovery scopes.")]
    internal static class SceneCompositionComponentQuery
    {
        internal static IReadOnlyList<T> GetComponents<T>(
            RouteContentDiscoveryScope scope)
            where T : Component
        {
            var components = new List<T>();
            var seen = new HashSet<T>();
            IReadOnlyList<RouteContentDiscoveryScene> scenes = scope.RouteOwnedScenes;
            for (int i = 0; i < scenes.Count; i++)
            {
                RouteContentDiscoveryScene scene = scenes[i];
                AddComponentsInLoadedScene(
                    scene.ScenePath,
                    scene.SceneName,
                    components,
                    seen);
            }

            return components;
        }

        internal static IReadOnlyList<T> GetComponents<T>(
            ActivityContentDiscoveryScope scope,
            ActivityAsset activity)
            where T : Component
        {
            var components = new List<T>();
            var seen = new HashSet<T>();

            AddDistinct(GetComponents<T>(scope.RouteScope), components, seen);

            IReadOnlyList<ActivityContentDiscoveryScene> scenes = scope.ActivityOwnedScenes;
            for (int i = 0; i < scenes.Count; i++)
            {
                ActivityContentDiscoveryScene scene = scenes[i];
                if (!scene.MatchesActivity(activity))
                {
                    continue;
                }

                AddComponentsInLoadedScene(
                    scene.ScenePath,
                    scene.SceneName,
                    components,
                    seen);
            }

            return components;
        }

        private static void AddComponentsInLoadedScene<T>(
            string scenePath,
            string sceneName,
            List<T> destination,
            HashSet<T> seen)
            where T : Component
        {
            Scene scene = FindLoadedScene(scenePath, sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
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
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T[] found = root.GetComponentsInChildren<T>(true);
                if (found == null || found.Length == 0)
                {
                    continue;
                }

                AddDistinct(found, destination, seen);
            }
        }

        private static Scene FindLoadedScene(string scenePath, string sceneName)
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
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

        private static bool MatchesScene(Scene scene, string scenePath, string sceneName)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            // Path is the authored scene identity when present. A divergent path must
            // never fall back to a matching name.
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase);
            }

            return !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(scene.name, sceneName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
