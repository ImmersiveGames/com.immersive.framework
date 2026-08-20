using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using UnityEditor;
namespace Immersive.Framework.Editor.Validation
{
    internal static class RouteContentProfileAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport Validate(
            RouteContentProfileAsset profile)
        {
            var report =
                new FrameworkAuthoringValidationReport(
                    FrameworkValidationMode.Standard);

            if (profile == null)
            {
                report.AddError(
                    "Route Content Profile is missing.",
                    null);

                return report;
            }

            var contentIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            var scenePaths =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            for (int index = 0;
                 index < profile.AdditionalScenes.Count;
                 index++)
            {
                RouteContentSceneEntry entry =
                    profile.AdditionalScenes[index];

                string label =
                    $"Additional Scene {index + 1}";

                if (entry == null)
                {
                    report.AddError(
                        $"{label} is missing.",
                        profile);

                    continue;
                }

                string contentId =
                    entry.ContentId;

                if (string.IsNullOrWhiteSpace(contentId))
                {
                    report.AddError(
                        $"{label} requires an explicit Content Id.",
                        profile);
                }
                else if (!contentIds.Add(contentId))
                {
                    report.AddError(
                        $"{label} duplicates Content Id '{contentId}'. Content Ids must be unique within the profile.",
                        profile);
                }

                string scenePath =
                    entry.ScenePath;

                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    report.AddError(
                        $"{label} requires a Scene.",
                        profile);

                    continue;
                }

                if (!scenePaths.Add(scenePath))
                {
                    report.AddError(
                        $"{label} duplicates Scene '{scenePath}'.",
                        profile);
                }

                SceneAsset scene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        scenePath);

                if (scene == null)
                {
                    report.AddError(
                        $"{label} references a missing Scene at '{scenePath}'. Reassign the Scene or restore the asset.",
                        profile);

                    continue;
                }

                if (!string.Equals(
                        entry.SceneName,
                        scene.name,
                        StringComparison.Ordinal))
                {
                    report.AddWarning(
                        $"{label} has stale cached Scene name '{entry.SceneName}'. Current asset name is '{scene.name}'. Reassign the Scene to synchronize it.",
                        profile);
                }

                if (!IsSceneEnabledInBuildProfile(scenePath))
                {
                    report.AddError(
                        $"{label} Scene '{scene.name}' is not enabled in the Build Profile.",
                        profile);
                }

                if (!Enum.IsDefined(
                        typeof(FrameworkContentRequiredness),
                        entry.Requiredness))
                {
                    report.AddError(
                        $"{label} has an unsupported Requiredness value.",
                        profile);
                }
            }

            if (report.ErrorCount == 0 &&
                report.WarningCount == 0)
            {
                report.AddInfo(
                    profile.AdditionalSceneCount == 0
                        ? "Route Content Profile has no additional scenes."
                        : "Route Content Profile authoring is valid.",
                    profile);
            }

            return report;
        }

        private static bool IsSceneEnabledInBuildProfile(
            string scenePath)
        {
            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;

            for (int index = 0;
                 index < scenes.Length;
                 index++)
            {
                EditorBuildSettingsScene scene =
                    scenes[index];

                if (scene != null &&
                    scene.enabled &&
                    string.Equals(
                        scene.path,
                        scenePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
