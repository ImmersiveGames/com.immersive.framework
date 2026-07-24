using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
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
        private SerializedProperty _persistentContent;
        private SerializedProperty _containerScene;
        private SerializedProperty _cameraOutputPrefab;
        private SerializedProperty _presentationCanvasPrefab;
        private SerializedProperty _validationMode;

        private ReorderableList _localPlayerSlotsList;
        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _showAdvancedDiagnostics;

        private void OnEnable()
        {
            _applicationName =
                serializedObject.FindProperty("applicationName");
            _startupRoute =
                serializedObject.FindProperty("startupRoute");
            _localPlayerSlots =
                serializedObject.FindProperty("localPlayerSlots");
            _playerActorSelectionDuplicatePolicy =
                serializedObject.FindProperty(
                    "playerActorSelectionDuplicatePolicy");
            _persistentContent =
                serializedObject.FindProperty("persistentContent");
            _containerScene =
                _persistentContent?.FindPropertyRelative("containerScene");
            _cameraOutputPrefab =
                _persistentContent?.FindPropertyRelative(
                    "cameraOutputPrefab");
            _presentationCanvasPrefab =
                _persistentContent?.FindPropertyRelative(
                    "presentationCanvasPrefab");
            _validationMode =
                serializedObject.FindProperty("validationMode");

            _localPlayerSlotsList = new ReorderableList(
                serializedObject,
                _localPlayerSlots,
                true,
                true,
                true,
                true);
            _localPlayerSlotsList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(
                    rect,
                    $"Player Slots — {_localPlayerSlots.arraySize} configured");
            _localPlayerSlotsList.elementHeight =
                EditorGUIUtility.singleLineHeight + 4f;
            _localPlayerSlotsList.drawElementCallback =
                (rect, index, active, focused) =>
                {
                    SerializedProperty element =
                        _localPlayerSlots.GetArrayElementAtIndex(index);
                    rect.y += 2f;
                    rect.height =
                        EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(
                        rect,
                        element,
                        new GUIContent($"{index + 1}."));
                };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(
                "Game Application",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Application-level authoring for startup flow, local Player policy and the concrete Persistent Content composition.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            DrawApplication();

            EditorGUILayout.Space(8f);
            DrawStartup();

            EditorGUILayout.Space(8f);
            DrawLocalPlayers();

            EditorGUILayout.Space(8f);
            DrawPersistentContent();

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

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
            EditorGUILayout.LabelField(
                "Application",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _applicationName,
                new GUIContent("Application Name"));

            var gameApplication =
                (GameApplicationAsset)target;
            var activeGameApplication =
                ImmersiveFrameworkEditorSettingsUtility
                    .GetActiveGameApplication();
            bool isActive =
                activeGameApplication == gameApplication;

            DrawStatusRow(
                "Project Status",
                isActive
                    ? "● Active"
                    : activeGameApplication == null
                        ? "○ No active application"
                        : $"○ Inactive — {activeGameApplication.ApplicationName}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!isActive &&
                    GUILayout.Button("Set Active"))
                {
                    ImmersiveFrameworkEditorSettingsUtility
                        .AssignActiveGameApplication(
                            gameApplication);
                }

                if (GUILayout.Button(
                        "Open Framework Settings"))
                {
                    SettingsService.OpenProjectSettings(
                        "Project/Immersive Framework");
                }
            }
        }

        private void DrawStartup()
        {
            EditorGUILayout.LabelField(
                "Startup",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _startupRoute,
                new GUIContent("Startup Route"));

            var route =
                _startupRoute.objectReferenceValue as RouteAsset;
            if (route == null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(
                            "Create Startup Route"))
                    {
                        var created =
                            ImmersiveFrameworkEditorSettingsUtility
                                .CreateStartupRouteAsset();
                        if (created != null)
                        {
                            _startupRoute.objectReferenceValue =
                                created;
                            Selection.activeObject = created;
                        }
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(
                        "Assign an existing Route above",
                        EditorStyles.miniLabel);
                }

                return;
            }

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
            EditorGUILayout.LabelField(
                "Local Players",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _playerActorSelectionDuplicatePolicy,
                new GUIContent("Duplicate Actors"));

            EditorGUILayout.Space(4f);
            _localPlayerSlotsList?.DoLayoutList();
        }

        private void DrawPersistentContent()
        {
            EditorGUILayout.LabelField(
                "Persistent Content",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Compose this scene manually in Unity. Add the selected prefab instances and configure positions, anchors and overrides through the normal Scene and Prefab workflows.",
                MessageType.None);

            SceneAsset currentScene =
                _containerScene?.objectReferenceValue as SceneAsset;
            SceneAsset selectedScene =
                (SceneAsset)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Container Scene",
                        "Scene used as the visual authoring container for content retained throughout the application."),
                    currentScene,
                    typeof(SceneAsset),
                    false);

            if (selectedScene != currentScene &&
                _containerScene != null)
            {
                _containerScene.objectReferenceValue =
                    selectedScene;
            }

            EditorGUILayout.PropertyField(
                _cameraOutputPrefab,
                new GUIContent(
                    "Camera Output Prefab",
                    "Exact prefab or Prefab Variant expected to provide the physical Camera output in the Container Scene."));
            EditorGUILayout.PropertyField(
                _presentationCanvasPrefab,
                new GUIContent(
                    "Presentation Canvas Prefab",
                    "Exact prefab or Prefab Variant expected to provide Transition and Loading presentation in the Container Scene."));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           selectedScene == null))
                {
                    if (GUILayout.Button(
                            "Open Container Scene"))
                    {
                        AssetDatabase.OpenAsset(selectedScene);
                    }
                }

                using (new EditorGUI.DisabledScope(
                           _cameraOutputPrefab?.objectReferenceValue == null))
                {
                    if (GUILayout.Button(
                            "Select Camera Prefab"))
                    {
                        SelectAndPing(
                            _cameraOutputPrefab.objectReferenceValue);
                    }
                }

                using (new EditorGUI.DisabledScope(
                           _presentationCanvasPrefab?.objectReferenceValue == null))
                {
                    if (GUILayout.Button(
                            "Select Canvas Prefab"))
                    {
                        SelectAndPing(
                            _presentationCanvasPrefab.objectReferenceValue);
                    }
                }
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _validationMode,
                new GUIContent("Mode"));

            EditorGUILayout.HelpBox(
                "Scene, prefab and project-profile validation runs only when Validate Configuration is pressed. Inspector repaint does not open scenes or inspect prefab hierarchies.",
                MessageType.None);

            DrawStatusRow(
                "Last Result",
                GetValidationStatus());

            if (GUILayout.Button(
                    "Validate Configuration"))
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                RunAuthoringValidation();
            }
        }

        private void DrawAdvancedDiagnostics()
        {
            _showAdvancedDiagnostics =
                EditorGUILayout.Foldout(
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
                EditorGUILayout.ObjectField(
                    "Container Scene",
                    _containerScene?.objectReferenceValue,
                    typeof(SceneAsset),
                    false);
                EditorGUILayout.ObjectField(
                    "Camera Output Prefab",
                    _cameraOutputPrefab?.objectReferenceValue,
                    typeof(GameObject),
                    false);
                EditorGUILayout.ObjectField(
                    "Presentation Canvas Prefab",
                    _presentationCanvasPrefab?.objectReferenceValue,
                    typeof(GameObject),
                    false);
                EditorGUILayout.IntField(
                    "Configured Player Capacity",
                    _localPlayerSlots?.arraySize ?? 0);
            }

            EditorGUILayout.HelpBox(
                "The Game Application declares the concrete composition. The framework validates and consumes the authored scene but does not create, apply, rebuild or repair it.",
                MessageType.None);

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

        private void RunAuthoringValidation()
        {
            var gameApplication =
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
            _lastValidationReport.AddRange(
                PlayerParticipationAuthoringValidator
                    .ValidateProjectProfiles(
                        gameApplication.ValidationMode));
            _validationOutdated = false;
        }

        private string GetValidationStatus()
        {
            if (_lastValidationReport == null)
            {
                return "○ Not validated";
            }

            if (_validationOutdated)
            {
                return "○ Outdated — configuration changed";
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

        private static void DrawStatusRow(
            string label,
            string status)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.SelectableLabel(
                    status,
                    EditorStyles.label,
                    GUILayout.Height(
                        EditorGUIUtility.singleLineHeight));
            }
        }

        private static void SelectAndPing(
            UnityEngine.Object targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            Selection.activeObject = targetObject;
            EditorGUIUtility.PingObject(targetObject);
        }
    }
}
