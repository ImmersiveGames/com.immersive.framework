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
        private static bool _showAdvancedDiagnostics;
        private static bool _hasBootValidation;
        private static bool _lastBootValidationSucceeded;
        private static string _lastBootValidationMessage = string.Empty;

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
                    "Unable to resolve a unique Immersive Framework settings asset.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Immersive Framework", EditorStyles.boldLabel);

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();
            var activeGameApplication = serializedSettings.FindProperty("activeGameApplication");
            var editorPlayModeStartup = serializedSettings.FindProperty("editorPlayModeStartup");
            var loggingConfig = serializedSettings.FindProperty("loggingConfig");

            EditorGUILayout.Space(6);
            DrawEditorPlayMode(editorPlayModeStartup);

            EditorGUILayout.Space(8);
            DrawApplication(activeGameApplication);

            EditorGUILayout.Space(8);
            DrawLoggingSettings(loggingConfig);

            EditorGUILayout.Space(8);
            DrawBootValidation(settings);

            EditorGUILayout.Space(8);
            DrawAdvancedDiagnostics(settings, loggingConfig.objectReferenceValue);

            if (serializedSettings.ApplyModifiedProperties())
            {
                _hasBootValidation = false;
                _lastModelReadinessReport = null;
            }
        }

        private static void DrawEditorPlayMode(SerializedProperty editorPlayModeStartup)
        {
            EditorGUILayout.LabelField("Editor Play Mode", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(editorPlayModeStartup, new GUIContent("Startup"));
        }

        private static void DrawApplication(SerializedProperty activeGameApplication)
        {
            EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(activeGameApplication, new GUIContent("Active Game Application"));

            var gameApplication = activeGameApplication.objectReferenceValue as GameApplicationAsset;
            DrawStatusRow(
                "Project Status",
                gameApplication != null
                    ? $"● Active — {gameApplication.ApplicationName}"
                    : "○ Not configured");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (gameApplication == null)
                {
                    if (GUILayout.Button("Create Game Application"))
                    {
                        var created = ImmersiveFrameworkEditorSettingsUtility.CreateGameApplicationAsset();
                        if (created != null)
                        {
                            activeGameApplication.objectReferenceValue = created;
                            Selection.activeObject = created;
                        }
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Assign an existing asset above", EditorStyles.miniLabel);
                }
                else
                {
                    if (GUILayout.Button("Open Application"))
                    {
                        Selection.activeObject = gameApplication;
                        EditorGUIUtility.PingObject(gameApplication);
                    }

                    if (GUILayout.Button("Replace"))
                    {
                        activeGameApplication.objectReferenceValue = null;
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        private static void DrawLoggingSettings(SerializedProperty loggingConfig)
        {
            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(loggingConfig, new GUIContent("Logging Config"));

            var config = loggingConfig.objectReferenceValue;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (config == null)
                {
                    if (GUILayout.Button("Create Logging Config"))
                    {
                        var created = ImmersiveFrameworkEditorSettingsUtility.CreateLoggingConfigAsset();
                        if (created != null)
                        {
                            loggingConfig.objectReferenceValue = created;
                            Selection.activeObject = created;
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Open Logging Config"))
                    {
                        Selection.activeObject = config;
                        EditorGUIUtility.PingObject(config);
                    }

                    if (GUILayout.Button("Replace"))
                    {
                        loggingConfig.objectReferenceValue = null;
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        private static void DrawBootValidation(ImmersiveFrameworkSettingsAsset settings)
        {
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            DrawStatusRow(
                "Status",
                !_hasBootValidation
                    ? "○ Not validated"
                    : _lastBootValidationSucceeded
                        ? $"● Valid — {_lastBootValidationMessage}"
                        : $"● Configuration error — {_lastBootValidationMessage}");

            if (GUILayout.Button("Validate Configuration"))
            {
                var bootStatus = FrameworkBootValidator.Validate(settings);
                _hasBootValidation = true;
                _lastBootValidationSucceeded = bootStatus.Succeeded;
                _lastBootValidationMessage = bootStatus.Message;
            }
        }

        private static void DrawAdvancedDiagnostics(
            ImmersiveFrameworkSettingsAsset settings,
            Object loggingConfig)
        {
            _showAdvancedDiagnostics = EditorGUILayout.Foldout(
                _showAdvancedDiagnostics,
                "Advanced / Diagnostics",
                true);
            if (!_showAdvancedDiagnostics)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawModelReadiness(settings);

            EditorGUILayout.Space(6);
            DrawConfigurationFiles(settings, loggingConfig);

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Project Settings owns the active Game Application, Editor Play Mode startup and logging configuration. Mutable player participation, Actor selection and scene runtime state remain outside this asset.",
                MessageType.None);

            EditorGUI.indentLevel--;
        }

        private static void DrawModelReadiness(ImmersiveFrameworkSettingsAsset settings)
        {
            EditorGUILayout.LabelField("Model Readiness", EditorStyles.boldLabel);

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

        private static void DrawConfigurationFiles(
            ImmersiveFrameworkSettingsAsset settings,
            Object loggingConfig)
        {
            EditorGUILayout.LabelField("Configuration Files", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Settings Asset",
                    ImmersiveFrameworkEditorSettingsUtility.GetSettingsAssetPath(settings));
                EditorGUILayout.TextField(
                    "Logging Config",
                    loggingConfig != null ? AssetDatabase.GetAssetPath(loggingConfig) : "Not assigned");
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

        private static void DrawStatusRow(string label, string status)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.SelectableLabel(
                    status,
                    EditorStyles.label,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
    }
}
