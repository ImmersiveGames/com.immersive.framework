using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.Performance;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(GameApplicationAsset))]
    internal sealed class GameApplicationAssetEditor : UnityEditor.Editor
    {
        private static readonly GUIContent ApplicationNameLabel =
            new GUIContent(
                "Application Name",
                "Designer-facing name used in selection and diagnostics.");

        private static readonly GUIContent StartupRouteLabel =
            new GUIContent(
                "Startup Route",
                "Route requested when this Game Application starts.");

        private static readonly GUIContent DuplicateActorsLabel =
            new GUIContent(
                "Duplicate Actors",
                "Controls whether more than one local Player Slot may select the same Actor.");

        private static readonly GUIContent PlayerSessionEnabledLabel =
            new GUIContent(
                "Enabled",
                "Creates the Player Session from the Default Player Session Profile during application boot.");

        private static readonly GUIContent DefaultPlayerSessionProfileLabel =
            new GUIContent(
                "Default Player Session Profile (Required)",
                "Reusable authored initial configuration resolved once when Player Session starts.");

        private static readonly GUIContent FrameRateModeLabel =
            new GUIContent(
                "Mode",
                "Use Unity Defaults preserves current values. Target Frame Rate disables VSync. Vertical Sync restores target frame rate to -1 and applies VSync Count.");

        private static readonly GUIContent TargetFrameRateLabel =
            new GUIContent(
                "Target Frame Rate",
                "Application.targetFrameRate requested during framework boot. Must be greater than zero.");

        private static readonly GUIContent VSyncCountLabel =
            new GUIContent(
                "VSync Count",
                "QualitySettings.vSyncCount requested during framework boot. Supported values are 1 through 4.");

        private static readonly GUIContent ContentSceneLabel =
            new GUIContent(
                "Content Scene",
                "Scene kept for the lifetime of this Game Application. It owns application-persistent Camera, UI and other shared content.");

        private static readonly GUIContent ValidationModeLabel =
            new GUIContent(
                "Mode",
                "Controls validation strictness for this Game Application graph.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates this Game Application and its configured dependencies without modifying them.");

        private SerializedProperty _applicationName;
        private SerializedProperty _startupRoute;
        private SerializedProperty _localPlayerSlots;
        private SerializedProperty _playerSessionEnabled;
        private SerializedProperty _defaultPlayerSessionProfile;
        private SerializedProperty _playerActorSelectionDuplicatePolicy;
        private SerializedProperty _frameRatePolicy;
        private SerializedProperty _frameRateMode;
        private SerializedProperty _targetFrameRate;
        private SerializedProperty _vSyncCount;
        private SerializedProperty _persistentContent;
        private SerializedProperty _containerScene;
        private SerializedProperty _validationMode;

        private ReorderableList _localPlayerSlotsList;
        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _serializedBindingsDirty = true;
        private bool _validationOutdated;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _serializedBindingsDirty = true;
        }

        private void RefreshSerializedBindings()
        {
            _applicationName =
                serializedObject.FindProperty("applicationName");
            _startupRoute =
                serializedObject.FindProperty("startupRoute");
            _localPlayerSlots =
                serializedObject.FindProperty("localPlayerSlots");
            _playerSessionEnabled =
                serializedObject.FindProperty("playerSessionEnabled");
            _defaultPlayerSessionProfile =
                serializedObject.FindProperty("defaultPlayerSessionProfile");
            _playerActorSelectionDuplicatePolicy =
                serializedObject.FindProperty(
                    "playerActorSelectionDuplicatePolicy");
            _frameRatePolicy =
                serializedObject.FindProperty("frameRatePolicy");
            _frameRateMode =
                _frameRatePolicy?.FindPropertyRelative("mode");
            _targetFrameRate =
                _frameRatePolicy?.FindPropertyRelative(
                    "targetFrameRate");
            _vSyncCount =
                _frameRatePolicy?.FindPropertyRelative("vSyncCount");
            _persistentContent =
                serializedObject.FindProperty("persistentContent");
            _containerScene =
                _persistentContent?.FindPropertyRelative(
                    "containerScene");
            _validationMode =
                serializedObject.FindProperty("validationMode");

            _localPlayerSlotsList =
                new ReorderableList(
                    serializedObject,
                    _localPlayerSlots,
                    true,
                    true,
                    true,
                    true);

            _localPlayerSlotsList.drawHeaderCallback =
                rect =>
                    EditorGUI.LabelField(
                        rect,
                        $"Player Slots — {_localPlayerSlots.arraySize}");

            _localPlayerSlotsList.elementHeight =
                EditorGUIUtility.singleLineHeight + 4f;

            _localPlayerSlotsList.drawElementCallback =
                (rect, index, active, focused) =>
                {
                    SerializedProperty element =
                        _localPlayerSlots
                            .GetArrayElementAtIndex(index);

                    rect.y += 2f;
                    rect.height =
                        EditorGUIUtility.singleLineHeight;

                    EditorGUI.PropertyField(
                        rect,
                        element,
                        new GUIContent($"{index + 1}."));
                };

            _serializedBindingsDirty = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (_serializedBindingsDirty)
            {
                RefreshSerializedBindings();
            }

            DrawApplication();
            DrawStartup();
            DrawPlayerSession();
            DrawPerformance();
            DrawPersistentContent();
            DrawValidation();
            DrawAdvancedDebug();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (modified &&
                _lastValidationReport != null)
            {
                _validationOutdated = true;
            }
        }

        private void DrawApplication()
        {
            DrawSection("Application");

            EditorGUILayout.PropertyField(
                _applicationName,
                ApplicationNameLabel);

            GameApplicationAsset gameApplication =
                (GameApplicationAsset)target;

            GameApplicationAsset activeGameApplication =
                ImmersiveFrameworkEditorSettingsUtility
                    .GetActiveGameApplication();

            bool isActive =
                activeGameApplication == gameApplication;

            DrawStatusRow(
                "Project Status",
                isActive
                    ? "Active"
                    : activeGameApplication == null
                        ? "No Active Application"
                        : $"Inactive — {activeGameApplication.ApplicationName}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!isActive &&
                    GUILayout.Button(
                        new GUIContent(
                            "Set Active",
                            "Assigns this asset as the active Game Application in Framework Settings.")))
                {
                    ImmersiveFrameworkEditorSettingsUtility
                        .AssignActiveGameApplication(
                            gameApplication);
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Open Framework Settings",
                            "Opens the project-level Immersive Framework settings.")))
                {
                    SettingsService.OpenProjectSettings(
                        "Project/Immersive Framework");
                }
            }
        }

        private void DrawStartup()
        {
            DrawSection("Startup");

            EditorGUILayout.PropertyField(
                _startupRoute,
                StartupRouteLabel);

            RouteAsset route =
                _startupRoute.objectReferenceValue as RouteAsset;

            if (route == null)
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Create Startup Route",
                            "Creates a new Route asset and assigns it as the startup Route.")))
                {
                    RouteAsset created =
                        ImmersiveFrameworkEditorSettingsUtility
                            .CreateStartupRouteAsset();

                    if (created != null)
                    {
                        _startupRoute.objectReferenceValue =
                            created;
                        Selection.activeObject = created;
                    }
                }

                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Open Route",
                            "Selects and pings the assigned startup Route.")))
                {
                    Selection.activeObject = route;
                    EditorGUIUtility.PingObject(route);
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Replace",
                            "Clears the current Route reference so another Route can be assigned.")))
                {
                    _startupRoute.objectReferenceValue = null;
                    GUI.FocusControl(null);
                }
            }
        }

        private void DrawPlayerSession()
        {
            DrawSection("Player Session");

            EditorGUILayout.PropertyField(
                _playerSessionEnabled,
                PlayerSessionEnabledLabel);

            if (_playerSessionEnabled == null ||
                _playerSessionEnabled.hasMultipleDifferentValues ||
                !_playerSessionEnabled.boolValue)
            {
                DrawStatusRow(
                    "Configuration",
                    "Disabled — no Player Session is created.");
                return;
            }

            EditorGUILayout.PropertyField(
                _defaultPlayerSessionProfile,
                DefaultPlayerSessionProfileLabel);

            PlayerSessionProfile profile =
                _defaultPlayerSessionProfile.objectReferenceValue as
                    PlayerSessionProfile;
            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Player Session is enabled and requires a Default Player Session Profile.",
                    MessageType.Error);
                return;
            }

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(profile);
            if (!resolution.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    $"Player Session Profile is not resolvable ({resolution.Failure}). {resolution.Message}",
                    MessageType.Error);
                return;
            }

            DrawStatusRow(
                "Configuration",
                $"Ready — {resolution.Configuration.SupportedSlotCount} Slot(s), resolved once at Session creation.");
        }

        private void DrawLegacyLocalPlayers()
        {
            DrawSection("Legacy Local Player Configuration");

            EditorGUILayout.HelpBox(
                "These technical fields are retained for existing assets. An enabled Player Session uses the Default Player Session Profile instead.",
                MessageType.Warning);

            EditorGUILayout.PropertyField(
                _playerActorSelectionDuplicatePolicy,
                DuplicateActorsLabel);

            EditorGUILayout.Space(3f);
            _localPlayerSlotsList?.DoLayoutList();
        }

        private void DrawPerformance()
        {
            DrawSection("Performance");
            EditorGUILayout.LabelField(
                "Frame Rate",
                EditorStyles.miniBoldLabel);

            if (_frameRateMode == null)
            {
                EditorGUILayout.HelpBox(
                    "Application Frame Rate policy is unavailable. Reimport the Game Application asset and validate the package version.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(
                _frameRateMode,
                FrameRateModeLabel);

            if (_frameRateMode.hasMultipleDifferentValues)
            {
                return;
            }

            ApplicationFrameRateMode mode =
                (ApplicationFrameRateMode)
                _frameRateMode.intValue;

            switch (mode)
            {
                case ApplicationFrameRateMode.UseUnityDefaults:
                    EditorGUILayout.HelpBox(
                        "The framework will preserve the current Unity VSync and target frame-rate values.",
                        MessageType.Info);
                    break;

                case ApplicationFrameRateMode.TargetFrameRate:
                    EditorGUILayout.PropertyField(
                        _targetFrameRate,
                        TargetFrameRateLabel);
                    EditorGUILayout.HelpBox(
                        "Target Frame Rate mode applies VSync Count 0 before requesting the configured FPS.",
                        MessageType.Info);
                    break;

                case ApplicationFrameRateMode.VerticalSync:
                    EditorGUILayout.PropertyField(
                        _vSyncCount,
                        VSyncCountLabel);
                    EditorGUILayout.HelpBox(
                        "Vertical Sync mode restores Application.targetFrameRate to -1. Mobile platforms may ignore VSync Count.",
                        MessageType.Info);
                    break;

                default:
                    EditorGUILayout.HelpBox(
                        $"Frame Rate Mode value '{_frameRateMode.intValue}' is invalid.",
                        MessageType.Error);
                    break;
            }
        }

        private void DrawPersistentContent()
        {
            DrawSection("Persistent Content");

            SceneAsset currentScene =
                _containerScene?.objectReferenceValue as SceneAsset;

            SceneAsset selectedScene =
                (SceneAsset)EditorGUILayout.ObjectField(
                    ContentSceneLabel,
                    currentScene,
                    typeof(SceneAsset),
                    false);

            if (selectedScene != currentScene &&
                _containerScene != null)
            {
                _containerScene.objectReferenceValue =
                    selectedScene;
            }

            if (selectedScene == null)
            {
                EditorGUILayout.HelpBox(
                    "Select the Persistent Content Scene.",
                    MessageType.Error);
                return;
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Open Content Scene",
                        "Opens the assigned Persistent Content Scene.")))
            {
                serializedObject.ApplyModifiedProperties();
                _serializedBindingsDirty = true;

                AssetDatabase.OpenAsset(
                    selectedScene);

                GUIUtility.ExitGUI();
            }
        }

        private void DrawValidation()
        {
            DrawSection("Validation");

            EditorGUILayout.PropertyField(
                _validationMode,
                ValidationModeLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        ValidateLabel,
                        GUILayout.Width(96f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    RunAuthoringValidation();
                    _serializedBindingsDirty = true;

                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(8f);

                EditorGUILayout.LabelField(
                    GetValidationStatus(),
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();
            }

            DrawFirstActionableValidationIssue();
        }

        private void DrawFirstActionableValidationIssue()
        {
            if (_lastValidationReport == null ||
                _validationOutdated ||
                (_lastValidationReport.ErrorCount == 0 &&
                 _lastValidationReport.WarningCount == 0))
            {
                return;
            }

            for (int index = 0;
                 index < _lastValidationReport.Issues.Count;
                 index++)
            {
                FrameworkAuthoringValidationIssue issue =
                    _lastValidationReport.Issues[index];

                if (issue.Severity !=
                        FrameworkAuthoringValidationSeverity.Error &&
                    issue.Severity !=
                        FrameworkAuthoringValidationSeverity.Warning)
                {
                    continue;
                }

                EditorGUILayout.HelpBox(
                    issue.Message,
                    issue.Severity ==
                        FrameworkAuthoringValidationSeverity.Error
                            ? MessageType.Error
                            : MessageType.Warning);

                return;
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);

            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows read-only technical evidence and the complete validation report."),
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            GameApplicationAsset activeGameApplication =
                ImmersiveFrameworkEditorSettingsUtility
                    .GetActiveGameApplication();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Active Game Application",
                    activeGameApplication,
                    typeof(GameApplicationAsset),
                    false);

                EditorGUILayout.ObjectField(
                    "Startup Route",
                    _startupRoute?.objectReferenceValue,
                    typeof(RouteAsset),
                    false);

                EditorGUILayout.ObjectField(
                    "Content Scene",
                    _containerScene?.objectReferenceValue,
                    typeof(SceneAsset),
                    false);

                EditorGUILayout.IntField(
                    "Configured Player Capacity",
                    _localPlayerSlots?.arraySize ?? 0);

                EditorGUILayout.EnumPopup(
                    "Frame Rate Mode",
                    _frameRateMode != null
                        ? (ApplicationFrameRateMode)
                            _frameRateMode.intValue
                        : ApplicationFrameRateMode
                            .UseUnityDefaults);

                EditorGUILayout.IntField(
                    "Target Frame Rate",
                    _targetFrameRate?.intValue ?? 0);

                EditorGUILayout.IntField(
                    "VSync Count",
                    _vSyncCount?.intValue ?? 0);

                EditorGUILayout.TextField(
                    "Validation Status",
                    GetValidationStatus());
            }

            DrawPlayerSessionAdvancedEvidence();
            DrawLegacyLocalPlayers();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);

            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);

            EditorGUI.indentLevel--;
        }

        private void DrawPlayerSessionAdvancedEvidence()
        {
            DrawSection("Player Session Resolution");

            if (_playerSessionEnabled == null ||
                !_playerSessionEnabled.boolValue)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Disabled",
                    EditorStyles.miniLabel);
                return;
            }

            PlayerSessionProfile profile =
                _defaultPlayerSessionProfile != null
                    ? _defaultPlayerSessionProfile.objectReferenceValue as
                        PlayerSessionProfile
                    : null;
            PlayerSessionInspectorGui.DrawResolution(
                profile,
                includeHeader: false);
        }

        private void RunAuthoringValidation()
        {
            GameApplicationAsset gameApplication =
                (GameApplicationAsset)target;

            _lastValidationReport =
                FrameworkAuthoringValidator
                    .ValidateGameApplication(
                        gameApplication,
                        true);

            _lastValidationReport.AddRange(
                PlayerParticipationAuthoringValidator
                    .ValidateGameApplication(
                        gameApplication));

            _validationOutdated = false;
        }

        private string GetValidationStatus()
        {
            if (_lastValidationReport == null)
            {
                return "Not Validated";
            }

            if (_validationOutdated)
            {
                return "Outdated";
            }

            if (_lastValidationReport.ErrorCount > 0)
            {
                return "Invalid";
            }

            if (_lastValidationReport.WarningCount > 0)
            {
                return "Warning";
            }

            return "Valid";
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }

        private static void DrawStatusRow(
            string label,
            string status)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.LabelField(
                    status,
                    EditorStyles.miniBoldLabel);
            }
        }
    }
}
