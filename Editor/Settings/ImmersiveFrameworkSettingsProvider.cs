using Immersive.Framework.Authoring;
using Immersive.Framework.Bootstrap;
using Immersive.Framework.Editor.PlayMode;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.Performance;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Settings
{
    internal static class ImmersiveFrameworkSettingsProvider
    {
        private static readonly GUIContent FrameRateModeLabel =
            new GUIContent(
                "Mode",
                "Required project frame pacing baseline. Use Unity Defaults explicitly preserves current Unity values.");

        private static readonly GUIContent TargetFrameRateLabel =
            new GUIContent(
                "Target Frame Rate",
                "Application.targetFrameRate requested during framework boot. Must be greater than zero.");

        private static readonly GUIContent VSyncCountLabel =
            new GUIContent(
                "VSync Count",
                "QualitySettings.vSyncCount requested during framework boot. Supported values are 1 through 4.");

        private static FrameworkAuthoringValidationReport _lastModelReadinessReport;
        private static FrameworkAuthoringValidationReport _lastFrameRateReport;
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
                    "Performance",
                    "Frame Rate",
                    "FPS",
                    "VSync",
                    "Target Frame Rate",
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
            var frameRatePolicy = serializedSettings.FindProperty("frameRatePolicy");
            var loggingConfig = serializedSettings.FindProperty("loggingConfig");

            EditorGUILayout.Space(6);
            DrawEditorPlayMode(editorPlayModeStartup);

            EditorGUILayout.Space(8);
            DrawApplication(activeGameApplication);

            EditorGUILayout.Space(8);
            DrawPerformance(settings, frameRatePolicy);

            EditorGUILayout.Space(8);
            DrawLoggingSettings(loggingConfig);

            EditorGUILayout.Space(8);
            DrawBootValidation(settings);

            EditorGUILayout.Space(8);
            DrawAdvancedDiagnostics(settings, loggingConfig.objectReferenceValue);

            if (serializedSettings.ApplyModifiedProperties())
            {
                FrameworkEditorPlayModeStartupController.SynchronizeFromSettings();

                _hasBootValidation = false;
                _lastModelReadinessReport = null;
                _lastFrameRateReport = null;
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

        private static void DrawPerformance(
            ImmersiveFrameworkSettingsAsset settings,
            SerializedProperty frameRatePolicy)
        {
            EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Frame Rate", EditorStyles.miniBoldLabel);

            if (frameRatePolicy == null)
            {
                EditorGUILayout.HelpBox(
                    "Project Frame Rate policy is unavailable. Reimport the package and validate the Framework Settings asset.",
                    MessageType.Error);
                return;
            }

            SerializedProperty mode =
                frameRatePolicy.FindPropertyRelative("mode");
            SerializedProperty targetFrameRate =
                frameRatePolicy.FindPropertyRelative("targetFrameRate");
            SerializedProperty vSyncCount =
                frameRatePolicy.FindPropertyRelative("vSyncCount");

            if (mode == null ||
                targetFrameRate == null ||
                vSyncCount == null)
            {
                EditorGUILayout.HelpBox(
                    "Project Frame Rate serialized fields could not be resolved.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(
                mode,
                FrameRateModeLabel);

            if (!mode.hasMultipleDifferentValues)
            {
                ApplicationFrameRateMode selectedMode =
                    (ApplicationFrameRateMode)mode.intValue;

                switch (selectedMode)
                {
                    case ApplicationFrameRateMode.UseUnityDefaults:
                        EditorGUILayout.HelpBox(
                            "Explicit project baseline: preserve current Unity VSync and target frame-rate values.",
                            MessageType.Info);
                        break;

                    case ApplicationFrameRateMode.TargetFrameRate:
                        EditorGUILayout.PropertyField(
                            targetFrameRate,
                            TargetFrameRateLabel);
                        EditorGUILayout.HelpBox(
                            "Target Frame Rate applies VSync Count 0 before requesting the configured FPS.",
                            MessageType.Info);
                        break;

                    case ApplicationFrameRateMode.VerticalSync:
                        EditorGUILayout.PropertyField(
                            vSyncCount,
                            VSyncCountLabel);
                        EditorGUILayout.HelpBox(
                            "Vertical Sync restores Application.targetFrameRate to -1. Mobile platforms may ignore VSync Count.",
                            MessageType.Info);
                        break;

                    default:
                        EditorGUILayout.HelpBox(
                            $"Frame Rate Mode value '{mode.intValue}' is invalid.",
                            MessageType.Error);
                        break;
                }
            }

            if (GUILayout.Button("Validate Frame Rate"))
            {
                _lastFrameRateReport =
                    ApplicationFrameRateAuthoringValidator.Validate(
                        settings);
                FrameworkAuthoringValidationGui.LogReport(
                    "Project Frame Rate",
                    _lastFrameRateReport);
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastFrameRateReport);
            FrameworkAuthoringValidationGui.DrawIssues(
                _lastFrameRateReport,
                false);
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
                "Project Settings owns the active Game Application, Editor Play Mode startup, project Frame Rate baseline and logging configuration. Mutable player participation, Actor selection and scene runtime state remain outside this asset.",
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
