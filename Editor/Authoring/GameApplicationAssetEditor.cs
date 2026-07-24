using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Editor.PersistentContent;
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
        private SerializedProperty _validationMode;

        private ReorderableList _localPlayerSlotsList;
        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _serializedBindingsDirty = true;
        private bool _validationOutdated;
        private bool _showAdvancedDiagnostics;

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
            _playerActorSelectionDuplicatePolicy =
                serializedObject.FindProperty(
                    "playerActorSelectionDuplicatePolicy");
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
                        $"Player Slots — {_localPlayerSlots.arraySize} configured");

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
                "The Content Scene is the complete composition authority. Author Camera, presentation, future Audio or Lighting and any other application-persistent objects directly in that scene. Prefabs and Prefab Variants remain optional Unity building blocks.",
                MessageType.None);

            SceneAsset currentScene =
                _containerScene?.objectReferenceValue as SceneAsset;
            SceneAsset selectedScene =
                (SceneAsset)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Content Scene",
                        "Scene containing the complete composition retained throughout the application."),
                    currentScene,
                    typeof(SceneAsset),
                    false);

            if (selectedScene != currentScene &&
                _containerScene != null)
            {
                _containerScene.objectReferenceValue =
                    selectedScene;
            }

            EditorGUILayout.HelpBox(
                "Create the scene from the official Persistent Content Scene Template when available, or author an equivalent scene manually. The Game Application references the created scene, never the Scene Template asset.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(
                       selectedScene == null))
            {
                if (GUILayout.Button(
                        "Open Content Scene"))
                {
                    // Opening a scene replaces the current Editor context and
                    // disposes this Inspector's SerializedObject immediately.
                    // Persist pending edits, invalidate cached bindings and stop
                    // the current IMGUI event before DrawValidation executes.
                    serializedObject.ApplyModifiedProperties();
                    _serializedBindingsDirty = true;

                    AssetDatabase.OpenAsset(
                        selectedScene);

                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Template Preparation",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Use this explicit one-shot action only on an open Content Scene that has no Camera composition yet. It creates the minimum Session Camera baseline with Undo and leaves the scene unsaved for review. Existing partial Camera content blocks the operation and is never repaired automatically.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(
                       selectedScene == null))
            {
                if (GUILayout.Button(
                        "Add Minimum Camera to Open Scene"))
                {
                    serializedObject.ApplyModifiedProperties();

                    bool succeeded =
                        PersistentContentCameraBaselineUtility
                            .TryCreateOrPreserve(
                                (GameApplicationAsset)target,
                                out _,
                                out string diagnostic);

                    if (_lastValidationReport != null &&
                        succeeded)
                    {
                        _validationOutdated = true;
                    }

                    _serializedBindingsDirty = true;

                    EditorUtility.DisplayDialog(
                        succeeded
                            ? "Persistent Content Camera"
                            : "Persistent Content Camera — Blocked",
                        diagnostic,
                        "OK");

                    GUIUtility.ExitGUI();
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
                "Scene-content and project-profile validation runs only when Validate Configuration is pressed. Inspector repaint does not open or inspect the Content Scene.",
                MessageType.None);

            DrawStatusRow(
                "Last Result",
                GetValidationStatus());

            if (GUILayout.Button(
                    "Validate Configuration"))
            {
                serializedObject.ApplyModifiedProperties();
                RunAuthoringValidation();
                _serializedBindingsDirty = true;

                // Validation may open and close scenes, which can invalidate
                // cached SerializedProperty instances during the current IMGUI
                // event. Stop drawing now and let Unity rebuild the Inspector
                // safely on the next repaint.
                GUIUtility.ExitGUI();
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
                    "Content Scene",
                    _containerScene?.objectReferenceValue,
                    typeof(SceneAsset),
                    false);
                EditorGUILayout.IntField(
                    "Configured Player Capacity",
                    _localPlayerSlots?.arraySize ?? 0);
            }

            EditorGUILayout.HelpBox(
                "The Game Application declares one concrete Content Scene. The framework validates the contracts present in that scene and consumes its complete authored hierarchy without creating, applying, rebuilding or repairing content.",
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

    }
}
