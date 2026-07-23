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
                EditorGUI.LabelField(rect, "Local Player Slots — Allocation Order");
            _localPlayerSlotsList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
            _localPlayerSlotsList.drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = _localPlayerSlots.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element, new GUIContent($"Slot {index + 1}"));
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Game Application", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Root application asset used by Immersive Framework. Keep this asset small; it should grow only when a real framework cut needs a new game-level decision.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_applicationName, new GUIContent("Application Name"));

            EditorGUILayout.Space(6);
            DrawProjectAssignment();

            EditorGUILayout.Space(6);
            DrawStartup();

            EditorGUILayout.Space(6);
            DrawLocalPlayerParticipation();

            EditorGUILayout.Space(6);
            DrawGlobalUiScene();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_validationMode, new GUIContent("Validation Mode"));
            EditorGUILayout.HelpBox(
                "Validation Mode controls validation and diagnostics severity. Required configuration must still fail in every mode.",
                MessageType.None);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This asset controls application identity, project assignment, Startup Route, ordered Local Player Slots, the Session Actor selection policy, canonical UIGlobal scene, and validation mode. Mutable join and Actor selection state remains in the Session runtime context.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawAuthoringValidation()
        {
            var gameApplication = (GameApplicationAsset)target;
            var report = FrameworkAuthoringValidator.ValidateGameApplication(gameApplication, true);
            report.AddRange(PlayerParticipationAuthoringValidator.ValidateGameApplication(gameApplication));
            report.AddRange(PlayerParticipationAuthoringValidator.ValidateProjectProfiles(gameApplication.ValidationMode));

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private void DrawLocalPlayerParticipation()
        {
            EditorGUILayout.LabelField("Local Player Participation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose the Session duplicate-selection rule and add Player Slot Profiles in the exact order local join must allocate them. Slot defaults are static intent and are applied only by an explicit runtime selection operation after join.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _playerActorSelectionDuplicatePolicy,
                new GUIContent("Actor Duplicate Selection"));

            var duplicatePolicy =
                (PlayerActorSelectionDuplicatePolicy)_playerActorSelectionDuplicatePolicy.intValue;
            if (!Enum.IsDefined(typeof(PlayerActorSelectionDuplicatePolicy), duplicatePolicy) ||
                duplicatePolicy == PlayerActorSelectionDuplicatePolicy.Unspecified)
            {
                EditorGUILayout.HelpBox(
                    "Actor Duplicate Selection must be explicit. Choose Allow Duplicates or Unique Across Joined Slots.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Active Session duplicate-selection rule: {duplicatePolicy}.",
                    MessageType.None);
            }

            EditorGUILayout.Space(4);
            if (_localPlayerSlotsList != null)
            {
                _localPlayerSlotsList.DoLayoutList();
            }

            int configuredCount = _localPlayerSlots != null ? _localPlayerSlots.arraySize : 0;
            if (configuredCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Local Player Slots are configured. The framework will not invent Player 1 or any fallback Slot.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Configured local participation capacity: {configuredCount}. Runtime capacity, joined count and selected Actors remain separate Session state.",
                    MessageType.None);
            }
        }

        private void DrawGlobalUiScene()
        {
            EditorGUILayout.LabelField("UIGlobal Scene", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_globalUiScenePolicy, new GUIContent("Global UI Scene Policy"));

            var currentScene = LoadCurrentGlobalUiSceneAsset();
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("UIGlobal Scene"),
                currentScene,
                typeof(SceneAsset),
                false);

            if (selectedScene != currentScene)
            {
                SetGlobalUiScene(selectedScene);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Scene Path", _globalUiScenePath.stringValue ?? string.Empty);
            }

            EditorGUILayout.HelpBox(
                "Canonical app/session composition root. When Required, FrameworkRuntimeHost prepares the Startup Route Primary Scene first, then loads UIGlobal additively and persists its roots. Add exactly one Local Player Provisioning Host Registration here when manual local provisioning is used; it must reference the explicit Local Player Provisioning Authoring. Transition/Loading adapters are resolved here too.",
                MessageType.Info);
        }

        private void DrawProjectAssignment()
        {
            var gameApplication = (GameApplicationAsset)target;
            var activeGameApplication = ImmersiveFrameworkEditorSettingsUtility.GetActiveGameApplication();
            bool isActive = activeGameApplication == gameApplication;

            EditorGUILayout.LabelField("Project Assignment", EditorStyles.boldLabel);

            if (isActive)
            {
                EditorGUILayout.HelpBox(
                    "This Game Application is assigned as the active application in Project Settings > Immersive Framework.",
                    MessageType.Info);
            }
            else if (activeGameApplication == null)
            {
                EditorGUILayout.HelpBox(
                    "No active Game Application is assigned in Project Settings > Immersive Framework.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Another Game Application is currently active: '{activeGameApplication.ApplicationName}'.",
                    MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(isActive))
                {
                    if (GUILayout.Button("Set as Active Game Application"))
                    {
                        ImmersiveFrameworkEditorSettingsUtility.AssignActiveGameApplication(gameApplication);
                    }
                }

                if (GUILayout.Button("Select Framework Settings"))
                {
                    ImmersiveFrameworkEditorSettingsUtility.SelectSettingsAsset();
                }
            }
        }

        private void DrawStartup()
        {
            EditorGUILayout.LabelField("Startup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_startupRoute, new GUIContent("Startup Route"));
            EditorGUILayout.HelpBox(
                "The Startup Route is the first route accepted by Game Flow after framework boot. It must declare a Primary Scene, which Scene Lifecycle loads when the Route starts.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Startup Route"))
                {
                    var route = ImmersiveFrameworkEditorSettingsUtility.CreateStartupRouteAsset();
                    if (route != null)
                    {
                        _startupRoute.objectReferenceValue = route;
                        Selection.activeObject = route;
                    }
                }

                using (new EditorGUI.DisabledScope(_startupRoute.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Startup Route"))
                    {
                        Selection.activeObject = _startupRoute.objectReferenceValue;
                    }
                }
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
