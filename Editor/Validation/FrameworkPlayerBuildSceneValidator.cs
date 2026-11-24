using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Editor.Validation
{
    internal sealed class FrameworkPlayerBuildSceneValidator : BuildPlayerProcessor
    {
        public override int callbackOrder => -100;

        public override void PrepareForBuild(BuildPlayerContext context)
        {
            var settings = Resources.Load<ImmersiveFrameworkSettingsAsset>(
                ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null || settings.ActiveGameApplication == null)
            {
                return;
            }

            string[] scheduled = context.BuildPlayerOptions.scenes;
            if (scheduled == null || scheduled.Length == 0)
            {
                scheduled = new[] { SceneManager.GetActiveScene().path };
            }

            var errors = new List<string>();
            var validator = new ScheduledSceneValidation(scheduled, errors);
            validator.Validate(settings.ActiveGameApplication);
            if (errors.Count > 0)
            {
                throw new BuildFailedException("Immersive Framework Player build scene validation failed.\n" +
                    string.Join("\n", errors));
            }
        }

        private sealed class ScheduledSceneValidation
        {
            private readonly HashSet<string> _scheduled;
            private readonly List<string> _errors;
            private readonly HashSet<RouteAsset> _routes = new();
            private readonly HashSet<ActivityAsset> _activities = new();

            internal ScheduledSceneValidation(string[] scheduled, List<string> errors)
            {
                _scheduled = new HashSet<string>(scheduled, StringComparer.OrdinalIgnoreCase);
                _scheduled.RemoveWhere(string.IsNullOrWhiteSpace);
                _errors = errors;
            }

            internal void Validate(GameApplicationAsset application)
            {
                PersistentContentComposition content = application.PersistentContent;
                string applicationPath = AssetDatabase.GetAssetPath(application);
                if (string.IsNullOrWhiteSpace(content?.ContainerScenePath))
                {
                    _errors.Add($"'{applicationPath}' Persistent Content is missing its authored scene path. " +
                        $"path='{content?.ContainerScenePath}', name='{content?.ContainerSceneName}'. " +
                        "Assign the scene in the Game Application or run " +
                        "Tools/Immersive Framework/Migrate Persistent Content Scene References for legacy assets.");
                }
                else
                {
                    CheckScene(applicationPath, "Persistent Content", content.ContainerScenePath, content.ContainerSceneName);
                }

                if (application.StartupRoute == null)
                {
                    _errors.Add($"'{applicationPath}' Startup Route is missing.");
                }
                else
                {
                    ValidateRoute(application.StartupRoute);
                }

                foreach (string scene in _scheduled)
                {
                    if (!IsSceneAsset(scene))
                    {
                        continue; // Unity reports invalid scheduled scenes itself.
                    }

                    foreach (string dependency in AssetDatabase.GetDependencies(scene, recursive: true))
                    {
                        Type type = AssetDatabase.GetMainAssetTypeAtPath(dependency);
                        if (type == typeof(RouteAsset))
                        {
                            ValidateRoute(AssetDatabase.LoadAssetAtPath<RouteAsset>(dependency));
                        }
                        else if (type == typeof(ActivityAsset))
                        {
                            ValidateActivity(AssetDatabase.LoadAssetAtPath<ActivityAsset>(dependency));
                        }
                    }
                }
            }

            private void ValidateRoute(RouteAsset route)
            {
                if (route == null || !_routes.Add(route))
                {
                    return;
                }

                CheckScene(AssetDatabase.GetAssetPath(route), "Route primary scene", route.PrimaryScenePath, route.PrimarySceneName);
                RouteContentProfileAsset profile = route.RouteContentProfile;
                if (profile != null)
                {
                    string owner = AssetDatabase.GetAssetPath(profile);
                    for (int index = 0; index < profile.AdditionalScenes.Count; index++)
                    {
                        RouteContentSceneEntry entry = profile.AdditionalScenes[index];
                        CheckScene(owner, $"Route Content entry {index + 1} [{entry?.ContentId}]", entry?.ScenePath, entry?.SceneName);
                    }
                }

                ValidateActivity(route.StartupActivity);
            }

            private void ValidateActivity(ActivityAsset activity)
            {
                if (activity == null || !_activities.Add(activity) || activity.ActivityContentProfile == null)
                {
                    return;
                }

                ActivityContentProfileAsset profile = activity.ActivityContentProfile;
                string owner = AssetDatabase.GetAssetPath(profile);
                for (int index = 0; index < profile.Scenes.Count; index++)
                {
                    ActivityContentSceneEntry entry = profile.Scenes[index];
                    CheckScene(owner, $"Activity Content entry {index + 1} [{entry?.ContentId}]", entry?.ScenePath, entry?.SceneName);
                }
            }

            private void CheckScene(string owner, string entry, string path, string name)
            {
                string problem;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    bool exists = IsSceneAsset(path);
                    if (exists && _scheduled.Contains(path))
                    {
                        return;
                    }
                    problem = exists ? "absent from the scheduled scene list" : "invalid or missing scene asset";
                }
                else if (string.IsNullOrWhiteSpace(name))
                {
                    problem = "missing scene reference";
                }
                else
                {
                    int matches = 0;
                    foreach (string candidate in _scheduled)
                    {
                        if (string.Equals(Path.GetFileNameWithoutExtension(candidate), name, StringComparison.OrdinalIgnoreCase) &&
                            IsSceneAsset(candidate))
                        {
                            matches++;
                        }
                    }

                    if (matches == 1)
                    {
                        return;
                    }
                    problem = matches == 0 ? "absent from the scheduled scene list" : "ambiguous legacy scene name";
                }

                _errors.Add($"'{owner}' {entry}: {problem}. path='{path}', name='{name}'.");
            }

            private static bool IsSceneAsset(string path) =>
                path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
                AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SceneAsset);
        }
    }
}
