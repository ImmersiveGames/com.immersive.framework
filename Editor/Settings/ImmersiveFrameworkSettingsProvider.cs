using Immersive.Framework.Authoring;
using Immersive.Framework.Bootstrap;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Settings
{
    internal static class ImmersiveFrameworkSettingsProvider
    {
        private static FrameworkAuthoringValidationReport _lastModelReadinessReport;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Immersive Framework", SettingsScope.Project)
            {
                label = "Immersive Framework",
                guiHandler = _ => DrawSettingsGui(),
                keywords = new[]
                {
                    "Immersive",
                    "Framework",
                    "Game Application",
                    "Validation Mode",
                    "Bootstrap",
                    "Usage Guide",
                    "Boot Status",
                    "Editor Play Mode",
                    "Current Scene Only",
                    "Logging",
                    "Logging Config",
                    "Namespace",
                    "Verbose",
                    "Minimum Level",
                    "Player Slot",
                    "Player Participation",
                    "Model Readiness"
                }
            };
        }

        private static void DrawSettingsGui()
        {
            var settings = ImmersiveFrameworkEditorSettingsUtility.LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Unable to create Immersive Framework settings asset.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Immersive Framework", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Project-level entry point for the framework. Assign one Game Application here; gameplay lifecycle concepts are added only when a real framework cut needs them.",
                MessageType.Info);

            var serializedSettings = new SerializedObject(settings);
            var activeGameApplication = serializedSettings.FindProperty("activeGameApplication");
            var editorPlayModeStartup = serializedSettings.FindProperty("editorPlayModeStartup");
            var loggingConfig = serializedSettings.FindProperty("loggingConfig");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Editor Play Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Controls how the framework behaves when entering Play Mode in the Unity Editor. Player builds always use Framework Startup.",
                MessageType.None);
            EditorGUILayout.PropertyField(editorPlayModeStartup, new GUIContent("Startup"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(activeGameApplication, new GUIContent("Active Game Application"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Game Application"))
                {
                    var created = ImmersiveFrameworkEditorSettingsUtility.CreateGameApplicationAsset();
                    if (created != null)
                    {
                        activeGameApplication.objectReferenceValue = created;
                        Selection.activeObject = created;
                    }
                }

                using (new EditorGUI.DisabledScope(activeGameApplication.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Game Application"))
                    {
                        Selection.activeObject = activeGameApplication.objectReferenceValue;
                    }
                }
            }

            EditorGUILayout.Space(6);
            DrawLoggingSettings(loggingConfig);

            serializedSettings.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            DrawBootStatus(settings);

            EditorGUILayout.Space(8);
            DrawModelReadiness(settings);

            EditorGUILayout.Space(8);
            DrawConfigurationFiles(settings, loggingConfig.objectReferenceValue);

            EditorGUILayout.Space(8);
            DrawCurrentScope();
        }

        private static void DrawModelReadiness(ImmersiveFrameworkSettingsAsset settings)
        {
            EditorGUILayout.LabelField("Model Readiness", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs the Editor-only readiness check for the minimum 1.0 authoring model, including ordered Local Player Slots, reusable participation Profiles and explicit Activity Projection/Requirements authoring. The check reports issues only; it does not create assets, modify settings or apply fallback.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Model Readiness Check"))
                {
                    _lastModelReadinessReport =
                        FrameworkAuthoringModelReadinessAggregator.ValidateProjectReadiness(settings, true);
                    FrameworkAuthoringValidationGui.LogReport("Model Readiness", _lastModelReadinessReport);
                }

                using (new EditorGUI.DisabledScope(_lastModelReadinessReport == null))
                {
                    if (GUILayout.Button("Log Last Report"))
                    {
                        FrameworkAuthoringValidationGui.LogReport("Model Readiness", _lastModelReadinessReport);
                    }
                }
            }

            FrameworkAuthoringValidationGui.DrawSummary(_lastModelReadinessReport);
            FrameworkAuthoringValidationGui.DrawIssues(_lastModelReadinessReport, false);
        }

        private static void DrawLoggingSettings(SerializedProperty loggingConfig)
        {
            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Optional configuration for framework logs. Leave empty to use the built-in default: Info and above, single-line console output, and no stack trace for regular Log entries.",
                MessageType.None);
            EditorGUILayout.PropertyField(loggingConfig, new GUIContent("Logging Config"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Logging Config"))
                {
                    var created = ImmersiveFrameworkEditorSettingsUtility.CreateLoggingConfigAsset();
                    if (created != null)
                    {
                        loggingConfig.objectReferenceValue = created;
                        Selection.activeObject = created;
                    }
                }

                using (new EditorGUI.DisabledScope(loggingConfig.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Logging Config"))
                    {
                        Selection.activeObject = loggingConfig.objectReferenceValue;
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Logging Config rules are evaluated by owner type first, then namespace/category prefix, then the default minimum level. Use namespace rules to hide verbose framework areas without changing code.",
                MessageType.None);
        }

        private static void DrawBootStatus(ImmersiveFrameworkSettingsAsset settings)
        {
            EditorGUILayout.LabelField("Boot Status", EditorStyles.boldLabel);

            if (settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.CurrentSceneOnly)
            {
                EditorGUILayout.HelpBox(
                    "Skipped in Editor Play Mode: Startup is set to Current Scene Only. The open scene will run without Game Application, Game Flow, or Scene Lifecycle boot.",
                    MessageType.Info);
                return;
            }

            var bootStatus = FrameworkBootValidator.Validate(settings);
            if (bootStatus.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    $"Ready: {bootStatus.Message} Validation Mode: {bootStatus.ValidationMode}. {FrameworkValidationModePolicy.GetSummary(bootStatus.ValidationMode)}",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Required: {bootStatus.Message}",
                    MessageType.Error);
            }
        }

        private static void DrawConfigurationFiles(ImmersiveFrameworkSettingsAsset settings, Object loggingConfig)
        {
            EditorGUILayout.LabelField("Configuration Files", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Settings Asset", ImmersiveFrameworkEditorSettingsUtility.GetSettingsAssetPath(settings));
                string loggingConfigPath = loggingConfig != null
                    ? AssetDatabase.GetAssetPath(loggingConfig)
                    : "Not assigned";
                EditorGUILayout.TextField("Logging Config", loggingConfigPath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Settings Asset"))
                {
                    ImmersiveFrameworkEditorSettingsUtility.SelectSettingsAsset();
                }

                if (GUILayout.Button("Open Usage Guide"))
                {
                    ImmersiveFrameworkEditorSettingsUtility.OpenUsageGuide();
                }
            }
        }

        private static void DrawCurrentScope()
        {
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This settings page assigns the active Game Application, controls Editor Play Mode startup, configures framework logging, previews boot validation, and runs the complete Model Readiness aggregation for ordered Slots and Activity participation authoring. Mutable join, selection, runtime projection, materialization and occupancy remain outside Project Settings.",
                MessageType.None);
        }
    }
}
