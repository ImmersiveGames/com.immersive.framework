using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.ProgressionSave;
using Immersive.Logging.Unity;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Settings
{
    internal static class ImmersiveFrameworkEditorSettingsUtility
    {
        internal const string SettingsPath = "Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset";
        internal const string LoggingConfigDefaultPath = "Assets/_Project/Settings/ImmersiveFramework/Logging/LoggingConfig.asset";
        internal const string UsageGuidePath = "Packages/com.immersive.framework/Documentation~/Guides/Usage/index.html";

        internal static ImmersiveFrameworkSettingsAsset LoadOrCreateSettingsAsset()
        {
            var existingSettings = FindExistingSettingsAssets();
            if (existingSettings.Count == 1)
            {
                return existingSettings[0];
            }

            if (existingSettings.Count > 1)
            {
                var paths = string.Join("\n", existingSettings.Select(AssetDatabase.GetAssetPath));
                FrameworkLogger.Create(typeof(ImmersiveFrameworkEditorSettingsUtility)).Error(
                    $"Multiple Immersive Framework settings assets were found in Resources folders. Keep exactly one {nameof(ImmersiveFrameworkSettingsAsset)} named {ImmersiveFrameworkSettingsAsset.ResourcesPath}.asset.\n{paths}");
                return null;
            }

            EnsureDirectory("Assets/_Project/Settings/ImmersiveFramework/Resources");

            var settings = ScriptableObject.CreateInstance<ImmersiveFrameworkSettingsAsset>();
            settings.name = ImmersiveFrameworkSettingsAsset.ResourcesPath;
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return settings;
        }

        internal static string GetSettingsAssetPath(ImmersiveFrameworkSettingsAsset settings)
        {
            if (settings == null)
            {
                return "Not found";
            }

            var path = AssetDatabase.GetAssetPath(settings);
            return string.IsNullOrWhiteSpace(path) ? "Not saved" : path;
        }

        internal static GameApplicationAsset CreateGameApplicationAsset()
        {
            return CreateAuthoredAsset<GameApplicationAsset>(
                "Create Game Application",
                "GameApplication.asset",
                "Choose where to save the Game Application.");
        }

        internal static RouteAsset CreateStartupRouteAsset()
        {
            return CreateAuthoredAsset<RouteAsset>(
                "Create Startup Route",
                "StartupRoute.asset",
                "Choose where to save the startup Route.",
                AssignNewRouteId);
        }

        internal static PlayerSessionProfile CreatePlayerSessionProfileAsset()
        {
            return CreateAuthoredAsset<PlayerSessionProfile>(
                "Create Player Session Profile",
                "PlayerSessionProfile.asset",
                "Choose where to save the Player Session Profile.");
        }

        internal static PlayerSlotProfile CreatePlayerSlotProfileAsset()
        {
            return CreateAuthoredAsset<PlayerSlotProfile>(
                "Create Player Slot Profile",
                "PlayerSlotProfile.asset",
                "Choose where to save the Player Slot Profile.");
        }

        internal static ProgressionSaveProfile CreateProgressionSaveProfileAsset(
            string suggestedName)
        {
            string defaultName =
                string.IsNullOrWhiteSpace(suggestedName)
                    ? "ProgressionSaveProfile.asset"
                    : suggestedName;

            return CreateAuthoredAsset<ProgressionSaveProfile>(
                "Create Progression Save Profile",
                defaultName,
                "Choose where to save the Progression Save Profile.");
        }

        internal static string GenerateRouteIdText() => GenerateAuthoringIdText();

        internal static string GenerateActivityIdText() => GenerateAuthoringIdText();

        private static void AssignNewRouteId(RouteAsset route)
        {
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = GenerateRouteIdText();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GenerateAuthoringIdText() => Guid.NewGuid().ToString("N");

        internal static ActivityAsset CreateStartupActivityAsset()
        {
            return CreateAuthoredAsset<ActivityAsset>(
                "Create First Activity",
                "StartupActivity.asset",
                "Choose where to save the startup Activity.");
        }

        internal static RouteContentProfileAsset CreateRouteContentProfileAsset()
        {
            return CreateAuthoredAsset<RouteContentProfileAsset>(
                "Create Route Content Profile",
                "RouteContentProfile.asset",
                "Choose where to save the Route Content Profile.");
        }

        internal static ActivityContentProfileAsset CreateActivityContentProfileAsset()
        {
            return CreateAuthoredAsset<ActivityContentProfileAsset>(
                "Create Activity Content Profile",
                "ActivityContentProfile.asset",
                "Choose where to save the Activity Content Profile.");
        }

        internal static LoggingConfigAsset CreateLoggingConfigAsset()
        {
            EnsureDirectory("Assets/_Project/Settings/ImmersiveFramework/Logging");

            var path = AssetDatabase.GenerateUniqueAssetPath(LoggingConfigDefaultPath);
            var loggingConfig = ScriptableObject.CreateInstance<LoggingConfigAsset>();
            AssetDatabase.CreateAsset(loggingConfig, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return loggingConfig;
        }

        internal static GameApplicationAsset GetActiveGameApplication()
        {
            var settings = LoadOrCreateSettingsAsset();
            return settings != null ? settings.ActiveGameApplication : null;
        }

        internal static bool IsActiveGameApplication(GameApplicationAsset gameApplication)
        {
            return gameApplication != null && GetActiveGameApplication() == gameApplication;
        }

        internal static void AssignActiveGameApplication(GameApplicationAsset gameApplication)
        {
            if (gameApplication == null)
            {
                return;
            }

            var settings = LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                return;
            }

            Undo.RecordObject(settings, "Set Active Game Application");

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("activeGameApplication").objectReferenceValue = gameApplication;
            serializedSettings.ApplyModifiedProperties();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        internal static void AssignLoggingConfig(LoggingConfigAsset loggingConfig)
        {
            if (loggingConfig == null)
            {
                return;
            }

            var settings = LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                return;
            }

            Undo.RecordObject(settings, "Set Logging Config");

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("loggingConfig").objectReferenceValue = loggingConfig;
            serializedSettings.ApplyModifiedProperties();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        internal static void SelectSettingsAsset()
        {
            var settings = LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        internal static void OpenUsageGuide()
        {
            var absolutePath = Path.GetFullPath(UsageGuidePath).Replace("\\", "/");
            Application.OpenURL($"file:///{absolutePath}");
        }

        internal static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static T CreateAuthoredAsset<T>(
            string title,
            string defaultName,
            string message,
            Action<T> initialize = null)
            where T : ScriptableObject
        {
            string initialFolder = ResolveAuthoredAssetCreationFolder();
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                title,
                defaultName,
                "asset",
                message,
                initialFolder);

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return null;
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(selectedPath);
            var asset = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(asset);

            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, title);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }

        private static string ResolveAuthoredAssetCreationFolder()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return "Assets";
            }

            string normalizedPath = selectedPath.Replace('\\', '/');
            bool isAssetsPath =
                string.Equals(normalizedPath, "Assets", StringComparison.Ordinal) ||
                normalizedPath.StartsWith("Assets/", StringComparison.Ordinal);
            if (!isAssetsPath)
            {
                return "Assets";
            }

            if (AssetDatabase.IsValidFolder(normalizedPath))
            {
                return normalizedPath;
            }

            string directory =
                Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');

            return !string.IsNullOrWhiteSpace(directory) &&
                   AssetDatabase.IsValidFolder(directory)
                ? directory
                : "Assets";
        }

        private static List<ImmersiveFrameworkSettingsAsset> FindExistingSettingsAssets()
        {
            return AssetDatabase.FindAssets($"t:{nameof(ImmersiveFrameworkSettingsAsset)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsValidSettingsResourcesPath)
                .Select(AssetDatabase.LoadAssetAtPath<ImmersiveFrameworkSettingsAsset>)
                .Where(asset => asset != null)
                .ToList();
        }

        private static bool IsValidSettingsResourcesPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.EndsWith($"/{ImmersiveFrameworkSettingsAsset.ResourcesPath}.asset", StringComparison.Ordinal))
            {
                return false;
            }

            var directory = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');
            return string.Equals(Path.GetFileName(directory), "Resources", StringComparison.Ordinal);
        }
    }
}
