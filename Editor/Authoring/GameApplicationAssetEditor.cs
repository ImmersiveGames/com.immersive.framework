using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(GameApplicationAsset))]
    internal sealed class GameApplicationAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _applicationName;
        private SerializedProperty _startupRoute;
        private SerializedProperty _localPlayerSlots;
        private SerializedProperty _playerActorSelectionDuplicatePolicy;
        private SerializedProperty _globalUiScenePolicy;
        private SerializedProperty _globalUiScenePath;
        private SerializedProperty _globalUiSceneName;
        private SerializedProperty _validationMode;
        private ReorderableList _localPlayerSlotsList;
        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _showAdvancedDiagnostics;

        private void OnEnable()
        {
            _applicationName = serializedObject.FindProperty("applicationName");
            _startupRoute = serializedObject.FindProperty("startupRoute");
            _localPlayerSlots = serializedObject.FindProperty("localPlayerSlots");
            _playerActorSelectionDuplicatePolicy = serializedObject.FindProperty("playerActorSelectionDuplicatePolicy");
            _globalUiScenePolicy = serializedObject.FindProperty("globalUiScenePolicy");
            _globalUiScenePath = serializedObject.FindProperty("globalUiScenePath");
            _globalUiSceneName = serializedObject.FindProperty("globalUiSceneName");
            _validationMode = serializedObject.FindProperty("validationMode");

            _localPlayerSlotsList = new ReorderableList(
                serializedObject,
                _localPlayerSlots,
                true,
                true,
                true,
                true);
            _localPlayerSlotsList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, $"Player Slots — {_localPlayerSlots.arraySize} configured");
            _localPlayerSlotsList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
            _localPlayerSlotsList.drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = _localPlayerSlots.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element, new GUIContent($"{index + 1}."));
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Game Application", EditorStyles.boldLabel);

            EditorGUILayout.Space(6);
            DrawApplication();

            EditorGUILayout.Space(8);
            DrawStartup();

            EditorGUILayout.Space(8);
            DrawLocalPlayers();

            EditorGUILayout.Space(8);
            DrawGlobalUi();

            EditorGUILayout.Space(8);
            DrawValidation();

            EditorGUILayout.Space(8);
            DrawAdvancedDiagnostics();

            if (serializedObject.ApplyModifiedProperties())
            {
                _lastValidationReport = null;
            }
        }

        private void DrawApplication()
        {
            EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_applicationName, new GUIContent("Application Name"));

            var gameApplication = (GameApplicationAsset)target;
            var activeGameApplication = ImmersiveFrameworkEditorSettingsUtility.GetActiveGameApplication();
            bool isActive = activeGameApplication == gameApplication;

            DrawStatusRow(
                "Project Status",
                isActive
                    ? "● Active"
                    : activeGameApplication == null
                        ? "○ No active application"
                        : $"○ Inactive — {activeGameApplication.ApplicationName}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!isActive && GUILayout.Button("Set Active"))
                {
                    ImmersiveFrameworkEditorSettingsUtility.AssignActiveGameApplication(gameApplication);
                }

                if (GUILayout.Button("Open Framework Settings"))
                {
                    SettingsService.OpenProjectSettings("Project/Immersive Framework");
                }
            }
        }

        private void DrawStartup()
        {
            EditorGUILayout.LabelField("Startup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_startupRoute, new GUIContent("Startup Route"));

            var route = _startupRoute.objectReferenceValue as RouteAsset;
            if (route == null)
            {
                DrawStatusRow("Status", "○ Not configured");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Startup Route"))
                    {
                        var created = ImmersiveFrameworkEditorSettingsUtility.CreateStartupRouteAsset();
                        if (created != null)
                        {
                            _startupRoute.objectReferenceValue = created;
                            Selection.activeObject = created;
                        }
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Assign an existing Route above", EditorStyles.miniLabel);
                }

                return;
            }

            string sceneStatus = route.HasPrimaryScene
                ? $"● Ready — {route.PrimarySceneName}"
                : "● Route assigned — Primary Scene missing";
            DrawStatusRow("Status", sceneStatus);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Route"))
                {
                    Selection.activeObject = route;
                    EditorGUIUtility.PingObject(route);
                }

                if (GUILayout.Button("Replace"))
                {
                    _startupRoute.objectReferenceValue = null;
                    GUI.FocusControl(null);
                }
            }
        }

        private void DrawLocalPlayers()
        {
            EditorGUILayout.LabelField("Local Players", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _playerActorSelectionDuplicatePolicy,
                new GUIContent("Duplicate Actors"));

            var duplicatePolicy =
                (PlayerActorSelectionDuplicatePolicy)_playerActorSelectionDuplicatePolicy.intValue;
            bool validPolicy = Enum.IsDefined(typeof(PlayerActorSelectionDuplicatePolicy), duplicatePolicy) &&
                               duplicatePolicy != PlayerActorSelectionDuplicatePolicy.Unspecified;
            if (!validPolicy)
            {
                EditorGUILayout.HelpBox(
                    "Duplicate Actors must be explicit. Choose Allow Duplicates or Unique Across Joined Slots.",
                    MessageType.Error);
            }

            EditorGUILayout.Space(4);
            _localPlayerSlotsList?.DoLayoutList();

            int configuredCount = _localPlayerSlots != null ? _localPlayerSlots.arraySize : 0;
            DrawStatusRow(
                "Allocation Order",
                configuredCount > 0
                    ? $"● {configuredCount} slot{(configuredCount == 1 ? string.Empty : "s")} configured — top to bottom"
                    : "○ No slots configured");

            if (configuredCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "At least one Local Player Slot is required. The framework does not create a fallback Player 1 slot.",
                    MessageType.Error);
            }
        }

        private void DrawGlobalUi()
        {
            EditorGUILayout.LabelField("Global UI", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_globalUiScenePolicy, new GUIContent("Policy"));

            var currentScene = LoadCurrentGlobalUiSceneAsset();
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Scene"),
                currentScene,
                typeof(SceneAsset),
                false);

            if (selectedScene != currentScene)
            {
                SetGlobalUiScene(selectedScene);
            }

            var policy = (GlobalUiScenePolicy)_globalUiScenePolicy.intValue;
            string status;
            switch (policy)
            {
                case GlobalUiScenePolicy.Required when selectedScene != null:
                    status = $"● Ready — {selectedScene.name}";
                    break;
                case GlobalUiScenePolicy.Required:
                    status = "● Required scene missing";
                    break;
                default:
                    status = selectedScene == null
                        ? "○ Optional — no scene assigned"
                        : $"○ Scene assigned but policy is {policy}";
                    break;
            }

            DrawStatusRow("Status", status);
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_validationMode, new GUIContent("Mode"));
            DrawStatusRow("Status", GetValidationStatus());

            if (GUILayout.Button("Validate Configuration"))
            {
                RunAuthoringValidation();
            }
        }

        private void DrawAdvancedDiagnostics()
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

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Global UI Scene Path", _globalUiScenePath.stringValue ?? string.Empty);
                EditorGUILayout.IntField("Configured Player Capacity", _localPlayerSlots?.arraySize ?? 0);
            }

            var duplicatePolicy =
                (PlayerActorSelectionDuplicatePolicy)_playerActorSelectionDuplicatePolicy.intValue;
            EditorGUILayout.LabelField("Session Duplicate Rule", duplicatePolicy.ToString());
            EditorGUILayout.HelpBox(
                "Runtime capacity, joined players and mutable Actor selections belong to the scoped Session runtime context. This asset stores only application intent.",
                MessageType.None);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Validation Report", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(_lastValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(_lastValidationReport, false);

            EditorGUI.indentLevel--;
        }

        private void RunAuthoringValidation()
        {
            var gameApplication = (GameApplicationAsset)target;
            _lastValidationReport = FrameworkAuthoringValidator.ValidateGameApplication(gameApplication, true);
            _lastValidationReport.AddRange(
                PlayerParticipationAuthoringValidator.ValidateGameApplication(gameApplication));
            _lastValidationReport.AddRange(
                PlayerParticipationAuthoringValidator.ValidateProjectProfiles(gameApplication.ValidationMode));
        }

        private string GetValidationStatus()
        {
            if (_lastValidationReport == null)
            {
                return "○ Not validated";
            }

            if (_lastValidationReport.ErrorCount > 0)
            {
                return $"● Configuration has {_lastValidationReport.ErrorCount} error{(_lastValidationReport.ErrorCount == 1 ? string.Empty : "s")}";
            }

            if (_lastValidationReport.WarningCount > 0)
            {
                return $"● Valid with {_lastValidationReport.WarningCount} warning{(_lastValidationReport.WarningCount == 1 ? string.Empty : "s")}";
            }

            return "● Valid — no errors or warnings";
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

        private SceneAsset LoadCurrentGlobalUiSceneAsset()
        {
            if (string.IsNullOrWhiteSpace(_globalUiScenePath.stringValue))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(_globalUiScenePath.stringValue);
        }

        private void SetGlobalUiScene(SceneAsset sceneAsset)
        {
            if (sceneAsset == null)
            {
                _globalUiScenePath.stringValue = string.Empty;
                _globalUiSceneName.stringValue = string.Empty;
                return;
            }

            _globalUiScenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            _globalUiSceneName.stringValue = sceneAsset.name;
        }
    }
}
