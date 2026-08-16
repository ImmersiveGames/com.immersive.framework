using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    /// <summary>
    /// Explicit Editor-only authoring support for the Scene List used by the active Build Profile.
    /// This utility never runs from validation, import, template instantiation or runtime flow.
    /// </summary>
    internal static class BuildSceneListAuthoringUtility
    {
        internal readonly struct SceneListState
        {
            internal SceneListState(
                int sceneCount,
                int enabledCount,
                int disabledCount,
                int missingCount)
            {
                SceneCount = sceneCount;
                EnabledCount = enabledCount;
                DisabledCount = disabledCount;
                MissingCount = missingCount;
            }

            internal int SceneCount { get; }
            internal int EnabledCount { get; }
            internal int DisabledCount { get; }
            internal int MissingCount { get; }

            internal bool HasScenes => SceneCount > 0;
            internal bool AllEnabled =>
                HasScenes && EnabledCount == SceneCount;
        }

        internal static IReadOnlyList<string> GetScenePaths(
            RouteAsset route)
        {
            var paths = new List<string>();

            if (route == null)
            {
                return paths;
            }

            AddPath(paths, route.PrimaryScenePath);
            AddProfilePaths(paths, route.RouteContentProfile);
            return paths;
        }

        internal static IReadOnlyList<string> GetScenePaths(
            ActivityAsset activity)
        {
            var paths = new List<string>();

            if (activity == null)
            {
                return paths;
            }

            AddProfilePaths(paths, activity.ActivityContentProfile);
            return paths;
        }

        internal static IReadOnlyList<string> GetScenePaths(
            RouteContentProfileAsset profile)
        {
            var paths = new List<string>();
            AddProfilePaths(paths, profile);
            return paths;
        }

        internal static IReadOnlyList<string> GetScenePaths(
            ActivityContentProfileAsset profile)
        {
            var paths = new List<string>();
            AddProfilePaths(paths, profile);
            return paths;
        }

        internal static SceneListState GetState(
            string scenePath)
        {
            return GetState(
                new[] { scenePath });
        }

        internal static SceneListState GetState(
            IEnumerable<string> scenePaths)
        {
            List<string> validPaths =
                CollectValidDistinctScenePaths(scenePaths);

            EditorBuildSettingsScene[] buildScenes =
                EditorBuildSettings.scenes;

            int enabledCount = 0;
            int disabledCount = 0;
            int missingCount = 0;

            for (int index = 0;
                 index < validPaths.Count;
                 index++)
            {
                int buildSceneIndex =
                    FindBuildSceneIndex(
                        buildScenes,
                        validPaths[index]);

                if (buildSceneIndex < 0)
                {
                    missingCount++;
                    continue;
                }

                if (buildScenes[buildSceneIndex].enabled)
                {
                    enabledCount++;
                }
                else
                {
                    disabledCount++;
                }
            }

            return new SceneListState(
                validPaths.Count,
                enabledCount,
                disabledCount,
                missingCount);
        }

        internal static bool DrawAction(
            string scenePath)
        {
            return DrawAction(
                new[] { scenePath });
        }

        internal static bool DrawAction(
            IEnumerable<string> scenePaths)
        {
            List<string> validPaths =
                CollectValidDistinctScenePaths(scenePaths);

            SceneListState state =
                GetState(validPaths);

            string buttonLabel =
                GetButtonLabel(state);

            string tooltip =
                GetButtonTooltip(state);

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying ||
                       !state.HasScenes ||
                       state.AllEnabled))
            {
                if (!GUILayout.Button(
                        new GUIContent(
                            buttonLabel,
                            tooltip)))
                {
                    return false;
                }
            }

            return AddOrEnable(validPaths) > 0;
        }

        internal static int AddOrEnable(
            IEnumerable<string> scenePaths)
        {
            List<string> validPaths =
                CollectValidDistinctScenePaths(scenePaths);

            if (validPaths.Count == 0)
            {
                return 0;
            }

            var buildScenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);

            int changedCount = 0;

            for (int index = 0;
                 index < validPaths.Count;
                 index++)
            {
                string scenePath =
                    validPaths[index];

                int buildSceneIndex =
                    FindBuildSceneIndex(
                        buildScenes,
                        scenePath);

                if (buildSceneIndex >= 0)
                {
                    if (buildScenes[buildSceneIndex].enabled)
                    {
                        continue;
                    }

                    buildScenes[buildSceneIndex] =
                        new EditorBuildSettingsScene(
                            scenePath,
                            true);

                    changedCount++;
                    continue;
                }

                buildScenes.Add(
                    new EditorBuildSettingsScene(
                        scenePath,
                        true));

                changedCount++;
            }

            if (changedCount > 0)
            {
                EditorBuildSettings.scenes =
                    buildScenes.ToArray();
            }

            return changedCount;
        }

        private static void AddProfilePaths(
            ICollection<string> paths,
            RouteContentProfileAsset profile)
        {
            if (profile == null)
            {
                return;
            }

            for (int index = 0;
                 index < profile.AdditionalSceneCount;
                 index++)
            {
                RouteContentSceneEntry entry =
                    profile.AdditionalScenes[index];

                if (entry != null)
                {
                    AddPath(paths, entry.ScenePath);
                }
            }
        }

        private static void AddProfilePaths(
            ICollection<string> paths,
            ActivityContentProfileAsset profile)
        {
            if (profile == null)
            {
                return;
            }

            for (int index = 0;
                 index < profile.SceneCount;
                 index++)
            {
                ActivityContentSceneEntry entry =
                    profile.Scenes[index];

                if (entry != null)
                {
                    AddPath(paths, entry.ScenePath);
                }
            }
        }

        private static void AddPath(
            ICollection<string> paths,
            string scenePath)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                paths.Add(scenePath.Trim());
            }
        }

        private static List<string> CollectValidDistinctScenePaths(
            IEnumerable<string> scenePaths)
        {
            var validPaths = new List<string>();
            var seenPaths = new HashSet<string>(
                StringComparer.Ordinal);

            if (scenePaths == null)
            {
                return validPaths;
            }

            foreach (string rawPath in scenePaths)
            {
                if (string.IsNullOrWhiteSpace(rawPath))
                {
                    continue;
                }

                string scenePath =
                    rawPath.Trim();

                if (!seenPaths.Add(scenePath))
                {
                    continue;
                }

                SceneAsset scene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        scenePath);

                if (scene == null)
                {
                    continue;
                }

                validPaths.Add(scenePath);
            }

            return validPaths;
        }

        private static int FindBuildSceneIndex(
            IReadOnlyList<EditorBuildSettingsScene> buildScenes,
            string scenePath)
        {
            for (int index = 0;
                 index < buildScenes.Count;
                 index++)
            {
                if (string.Equals(
                        buildScenes[index].path,
                        scenePath,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string GetButtonLabel(
            SceneListState state)
        {
            if (!state.HasScenes)
            {
                return "No Scenes to Add";
            }

            if (state.AllEnabled)
            {
                return "In Scene List";
            }

            if (state.SceneCount == 1)
            {
                return state.DisabledCount > 0
                    ? "Enable in Scene List"
                    : "Add to Scene List";
            }

            if (state.MissingCount == state.SceneCount)
            {
                return "Add Scenes to Scene List";
            }

            return "Add / Enable Scenes in Scene List";
        }

        private static string GetButtonTooltip(
            SceneListState state)
        {
            if (!state.HasScenes)
            {
                return "No valid referenced Scene assets are available for this authoring owner.";
            }

            if (state.AllEnabled)
            {
                return state.SceneCount == 1
                    ? "The referenced Scene is already enabled in the Scene List used by the active Build Profile."
                    : $"All {state.SceneCount} referenced Scenes are already enabled in the Scene List used by the active Build Profile.";
            }

            if (state.SceneCount == 1)
            {
                return state.DisabledCount > 0
                    ? "Enables the existing referenced Scene entry in the Scene List used by the active Build Profile."
                    : "Adds the referenced Scene, enabled, to the Scene List used by the active Build Profile.";
            }

            return
                $"Adds missing referenced Scenes and enables disabled entries in the Scene List used by the active Build Profile. " +
                $"Current state: {state.EnabledCount} enabled, {state.DisabledCount} disabled, {state.MissingCount} missing.";
        }
    }
}
