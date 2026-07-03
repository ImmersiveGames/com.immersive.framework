using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Logging.Unity;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Settings
{
    internal static class ImmersiveFrameworkEditorSettingsUtility
    {
        internal const string SettingsPath = "Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset";
        internal const string GameApplicationDefaultPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/GameApplication.asset";
        internal const string StartupRouteDefaultPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Routes/StartupRoute.asset";
        internal const string StartupActivityDefaultPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Activities/StartupActivity.asset";
        internal const string RouteContentProfileDefaultPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/RouteContentProfiles/RouteContentProfile.asset";
        internal const string ActivityContentProfileDefaultPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/ActivityContentProfiles/ActivityContentProfile.asset";
        internal const string LoggingConfigDefaultPath = "Assets/_Project/Settings/ImmersiveFramework/Logging/LoggingConfig.asset";
        internal const string UsageGuidePath = "Packages/com.immersive.framework/Documentation~/Guides/Usage/index.html";

        internal static ImmersiveFrameworkSettingsAsset LoadOrCreateSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ImmersiveFrameworkSettingsAsset>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureDirectory("Assets/_Project/Settings/ImmersiveFramework/Resources");

            settings = ScriptableObject.CreateInstance<ImmersiveFrameworkSettingsAsset>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return settings;
        }

        internal static GameApplicationAsset CreateGameApplicationAsset()
        {
            EnsureDirectory("Assets/_Project/ScriptableObjects/ImmersiveFramework");

            var path = AssetDatabase.GenerateUniqueAssetPath(GameApplicationDefaultPath);
            var gameApplication = ScriptableObject.CreateInstance<GameApplicationAsset>();
            AssetDatabase.CreateAsset(gameApplication, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return gameApplication;
        }

        internal static RouteAsset CreateStartupRouteAsset()
        {
            EnsureDirectory("Assets/_Project/ScriptableObjects/ImmersiveFramework/Routes");

            var path = AssetDatabase.GenerateUniqueAssetPath(StartupRouteDefaultPath);
            var route = ScriptableObject.CreateInstance<RouteAsset>();
            AssetDatabase.CreateAsset(route, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return route;
        }

        internal static ActivityAsset CreateStartupActivityAsset()
        {
            EnsureDirectory("Assets/_Project/ScriptableObjects/ImmersiveFramework/Activities");

            var path = AssetDatabase.GenerateUniqueAssetPath(StartupActivityDefaultPath);
            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            AssetDatabase.CreateAsset(activity, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return activity;
        }

        internal static RouteContentProfileAsset CreateRouteContentProfileAsset()
        {
            EnsureDirectory("Assets/_Project/ScriptableObjects/ImmersiveFramework/RouteContentProfiles");

            var path = AssetDatabase.GenerateUniqueAssetPath(RouteContentProfileDefaultPath);
            var profile = ScriptableObject.CreateInstance<RouteContentProfileAsset>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return profile;
        }

        internal static ActivityContentProfileAsset CreateActivityContentProfileAsset()
        {
            EnsureDirectory("Assets/_Project/ScriptableObjects/ImmersiveFramework/ActivityContentProfiles");

            var path = AssetDatabase.GenerateUniqueAssetPath(ActivityContentProfileDefaultPath);
            var profile = ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return profile;
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
    }
}
