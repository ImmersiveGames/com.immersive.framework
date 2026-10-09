using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.PlayerParticipation;
using Immersive.Framework.Editor.ProgressionSave;
using Immersive.Framework.Editor.Settings;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.ProgressionSave;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
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

        private static readonly GUIContent PlayerSessionEnabledLabel =
            new GUIContent(
                "Enabled",
                "Creates the Player Session from the Default Player Session Profile during application boot.");

        private static readonly GUIContent DefaultPlayerSessionProfileLabel =
            new GUIContent(
                "Default Player Session Profile (Required)",
                "Reusable authored initial configuration resolved once when Player Session starts.");

        private static readonly GUIContent ProgressionSaveEnabledLabel =
            new GUIContent(
                "Enabled",
                "Creates one application-scoped Progression Save Runtime during framework boot.");

        private static readonly GUIContent DefaultProgressionSaveProfileLabel =
            new GUIContent(
                "Default Progression Save Profile (Required)",
                "Reusable authored backend intent materialized once during application boot.");

        private static readonly GUIContent CameraSessionLabel =
            new GUIContent(
                "Session Configuration",
                "Physical Camera Outputs and optional Player Slot -> Output bindings for local-player routing.");

        private static readonly GUIContent ContentSceneLabel =
            new GUIContent(
                "Content Scene",
                "Scene kept for the lifetime of this Game Application. It owns application-persistent UI and other shared scene content. Camera Outputs and Player Slot -> Output bindings are not authored here.");

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
        private SerializedProperty _playerSessionEnabled;
        private SerializedProperty _defaultPlayerSessionProfile;
        private SerializedProperty _playerActorSelectionDuplicatePolicy;
        private SerializedProperty _progressionSaveEnabled;
        private SerializedProperty _defaultProgressionSaveProfile;
        private SerializedProperty _cameraSession;
        private SerializedProperty _startupCameraAssignments;
        private ReorderableList _sessionCameraAssignmentList;
        private SerializedProperty _persistentContent;
        private SerializedProperty _scenePath;
        private SerializedProperty _sceneName;
        private SerializedProperty _legacyContainerScene;
        private SerializedProperty _validationMode;

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private GameApplicationAsset _activeGameApplication;
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
            _playerSessionEnabled =
                serializedObject.FindProperty("playerSessionEnabled");
            _defaultPlayerSessionProfile =
                serializedObject.FindProperty("defaultPlayerSessionProfile");
            _playerActorSelectionDuplicatePolicy =
                serializedObject.FindProperty(
                    "playerActorSelectionDuplicatePolicy");
            _progressionSaveEnabled =
                serializedObject.FindProperty("progressionSaveEnabled");
            _defaultProgressionSaveProfile =
                serializedObject.FindProperty("defaultProgressionSaveProfile");
            _cameraSession =
                serializedObject.FindProperty("cameraSession");
            _startupCameraAssignments =
                serializedObject.FindProperty("startupCameraAssignments");
            _sessionCameraAssignmentList = CreateSessionCameraAssignmentList();
            _persistentContent =
                serializedObject.FindProperty("persistentContent");
            _scenePath =
                _persistentContent?.FindPropertyRelative("scenePath");
            _sceneName =
                _persistentContent?.FindPropertyRelative("sceneName");
            _legacyContainerScene =
                _persistentContent?.FindPropertyRelative("containerScene");
            _validationMode =
                serializedObject.FindProperty("validationMode");

            _activeGameApplication =
                ImmersiveFrameworkEditorSettingsUtility.TryLoadExistingSettingsAsset(
                    out ImmersiveFrameworkSettingsAsset settings,
                    out _)
                    ? settings.ActiveGameApplication
                    : null;

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
            DrawProgressionSave();
            DrawCamera();
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

            GameApplicationAsset activeGameApplication = _activeGameApplication;

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
                    _activeGameApplication = gameApplication;
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Open Framework Settings",
                            "Opens the project-level Immersive Framework settings, including Performance / Frame Rate.")))
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

                if (GUILayout.Button(
                        new GUIContent(
                            "Create Player Session Profile",
                            "Creates a Player Session Profile asset and assigns it as the application default.")))
                {
                    PlayerSessionProfile created =
                        ImmersiveFrameworkEditorSettingsUtility
                            .CreatePlayerSessionProfileAsset();

                    if (created != null)
                    {
                        _defaultPlayerSessionProfile.objectReferenceValue =
                            created;
                        serializedObject.ApplyModifiedProperties();
                        Selection.activeObject = created;
                        EditorGUIUtility.PingObject(created);
                    }
                }

                return;
            }

            DrawStatusRow(
                "Configuration",
                $"Profile assigned — {profile.SupportedSlotCount} configured Slot(s). Use Validate for structural checks.");
        }

        private void DrawProgressionSave()
        {
            DrawSection("Progression Save");

            EditorGUILayout.PropertyField(
                _progressionSaveEnabled,
                ProgressionSaveEnabledLabel);

            if (_progressionSaveEnabled == null ||
                _progressionSaveEnabled.hasMultipleDifferentValues ||
                !_progressionSaveEnabled.boolValue)
            {
                DrawStatusRow(
                    "Configuration",
                    "Disabled — no Progression Save Runtime is created.");
                return;
            }

            EditorGUILayout.PropertyField(
                _defaultProgressionSaveProfile,
                DefaultProgressionSaveProfileLabel);

            ProgressionSaveProfile profile =
                _defaultProgressionSaveProfile.objectReferenceValue as
                    ProgressionSaveProfile;

            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Progression Save is enabled and requires a Default Progression Save Profile.",
                    MessageType.Error);

                if (GUILayout.Button(
                        new GUIContent(
                            "Create Progression Save Profile",
                            "Creates a Profile asset and assigns it as the application default.")))
                {
                    CreateAndAssignProgressionSaveProfile();
                }

                return;
            }

            if (!profile.TryValidate(
                    out string issue))
            {
                EditorGUILayout.HelpBox(
                    issue,
                    MessageType.Error);
                return;
            }

            string status =
                profile.Backend ==
                    ProgressionSaveBackendSelection.BuiltInJson
                    ? "Ready — Built-in JSON"
                    : $"Ready — Custom Provider: {profile.CustomProvider.name}";

            DrawStatusRow(
                "Configuration",
                status);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Open Profile",
                            "Selects and pings the assigned Progression Save Profile.")))
                {
                    Selection.activeObject = profile;
                    EditorGUIUtility.PingObject(profile);
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Replace",
                            "Clears the current Profile reference so another Profile can be assigned.")))
                {
                    _defaultProgressionSaveProfile.objectReferenceValue =
                        null;
                    GUI.FocusControl(null);
                }
            }
        }

        private void CreateAndAssignProgressionSaveProfile()
        {
            GameApplicationAsset gameApplication =
                (GameApplicationAsset)target;

            string suggestedName =
                $"{gameApplication.name}-ProgressionSaveProfile.asset";

            ProgressionSaveProfile created =
                ImmersiveFrameworkEditorSettingsUtility
                    .CreateProgressionSaveProfileAsset(
                        suggestedName);

            if (created == null)
            {
                return;
            }

            _defaultProgressionSaveProfile.objectReferenceValue =
                created;

            serializedObject.ApplyModifiedProperties();

            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        private void DrawActorSelectionPolicy()
        {
            DrawSection("Actor Selection Policy");

            EditorGUILayout.PropertyField(
                _playerActorSelectionDuplicatePolicy,
                new GUIContent(
                    "Actor Selection Duplicates",
                    "Session Actor-selection policy. This is distinct from Player Session initial Actor Resolution."));
        }

        private ReorderableList CreateSessionCameraAssignmentList()
        {
            if (_startupCameraAssignments == null || !_startupCameraAssignments.isArray)
                return null;

            var list = new ReorderableList(
                serializedObject,
                _startupCameraAssignments,
                true,
                true,
                true,
                true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(
                rect,
                "Session Camera Assignments",
                EditorStyles.boldLabel);
            list.elementHeightCallback = index => GetAssignmentElementHeight(index);
            list.drawElementCallback = DrawSessionCameraAssignmentElement;
            list.onAddCallback = AddSessionCameraAssignment;
            list.onRemoveCallback = current =>
            {
                if (current.index < 0 || current.index >= _startupCameraAssignments.arraySize)
                    return;
                _startupCameraAssignments.DeleteArrayElementAtIndex(current.index);
                serializedObject.ApplyModifiedProperties();
                _validationOutdated = true;
            };
            return list;
        }

        private float GetAssignmentElementHeight(int index) =>
            EditorGUIUtility.singleLineHeight + 6f;
        private void DrawSessionCameraAssignmentElement(
            Rect rect,
            int index,
            bool isActive,
            bool isFocused)
        {
            if (index < 0 || index >= _startupCameraAssignments.arraySize)
                return;
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(
                rect,
                _startupCameraAssignments.GetArrayElementAtIndex(index),
                new GUIContent($"Assignment {index + 1}"));
        }
        private void AddSessionCameraAssignment(ReorderableList list)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Session Camera Assignment",
                "SessionCameraAssignment",
                "asset",
                "Choose where to create the Assignment asset.");
            if (string.IsNullOrEmpty(path))
                return;

            SessionCameraAssignmentAsset asset = CreateInstance<SessionCameraAssignmentAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            serializedObject.Update();
            int index = _startupCameraAssignments.arraySize;
            _startupCameraAssignments.InsertArrayElementAtIndex(index);
            _startupCameraAssignments.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            serializedObject.ApplyModifiedProperties();
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            _validationOutdated = true;
            list.index = index;
        }
        private void DrawCamera()
        {
            DrawSection("Camera");

            _sessionCameraAssignmentList?.DoLayoutList();

            EditorGUILayout.Space(4f);

            EditorGUILayout.HelpBox(
                "Assignments choose normal cameras and map their Occurrences to these physical Outputs. Individual Assignments also own the Player Slot -> Output mapping used for PlayerInput.camera. Each Output retains its independent Fallback Camera. Persistent Content does not supply Camera Outputs.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _cameraSession,
                CameraSessionLabel,
                true);

            SerializedProperty outputPrefabs =
                _cameraSession?.FindPropertyRelative("outputPrefabs");
            int outputCount =
                outputPrefabs != null &&
                outputPrefabs.isArray
                    ? outputPrefabs.arraySize
                    : 0;
            if (outputCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "Camera Session requires at least one explicit Camera Output prefab.",
                    MessageType.Error);
            }
            else
            {
                DrawStatusRow(
                    "Physical Outputs",
                    $"{outputCount} explicit Output prefab(s).");
            }

        }

        private void DrawPersistentContent()
        {
            DrawSection("Persistent Content");

            string scenePath = _scenePath?.stringValue ?? string.Empty;
            SceneAsset currentScene =
                string.IsNullOrWhiteSpace(scenePath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            if (!string.IsNullOrWhiteSpace(scenePath) && currentScene == null)
            {
                EditorGUILayout.HelpBox(
                    $"Persistent Content scenePath '{scenePath}' does not resolve to a Scene asset. The path is authoritative; no name fallback will be used.",
                    MessageType.Error);
            }

            SceneAsset selectedScene =
                (SceneAsset)EditorGUILayout.ObjectField(
                    ContentSceneLabel,
                    currentScene,
                    typeof(SceneAsset),
                    false);

            if (selectedScene != currentScene &&
                _scenePath != null &&
                _sceneName != null)
            {
                if (selectedScene == null)
                {
                    _scenePath.stringValue = string.Empty;
                    _sceneName.stringValue = string.Empty;
                    if (_legacyContainerScene != null)
                        _legacyContainerScene.objectReferenceValue = null;
                }
                else
                {
                    string selectedPath = AssetDatabase.GetAssetPath(selectedScene);
                    if (selectedPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    {
                        _scenePath.stringValue = selectedPath;
                        _sceneName.stringValue = selectedScene.name;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "The selected asset is not a Unity Scene asset.",
                            MessageType.Error);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(scenePath) &&
                _legacyContainerScene?.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    "This Game Application has a legacy Persistent Content reference. Run Tools > Immersive Framework > Migrate Persistent Content Scene References. The runtime uses only serialized scenePath/sceneName values.",
                    MessageType.Warning);
            }

            string effectivePath = _scenePath?.stringValue ?? string.Empty;
            if (string.IsNullOrWhiteSpace(effectivePath) || selectedScene == null)
            {
                EditorGUILayout.HelpBox(
                    "Tip: create the starting Persistent Content Scene with File > New Scene > Immersive Persistent Content.",
                    MessageType.Info);

                EditorGUILayout.HelpBox(
                    "Assign a valid Persistent Content Scene or migrate the legacy reference.",
                    MessageType.Error);
                return;
            }

            scenePath = effectivePath;

            EditorBuildSettingsScene[] buildScenes =
                EditorBuildSettings.scenes;

            int buildSceneIndex =
                FindBuildSceneIndex(
                    buildScenes,
                    scenePath);

            bool isInSceneList =
                buildSceneIndex >= 0;

            bool isEnabledInSceneList =
                isInSceneList &&
                buildScenes[buildSceneIndex].enabled;

            using (new EditorGUILayout.HorizontalScope())
            {
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

                string sceneListButtonLabel =
                    !isInSceneList
                        ? "Add to Scene List"
                        : isEnabledInSceneList
                            ? "In Scene List"
                            : "Enable in Scene List";

                string sceneListButtonTooltip =
                    !isInSceneList
                        ? "Adds the assigned Persistent Content Scene, enabled, to the Scene List used by the active Build Profile."
                        : isEnabledInSceneList
                            ? "The assigned Persistent Content Scene is already enabled in the Scene List used by the active Build Profile."
                            : "Enables the existing Persistent Content Scene entry in the Scene List used by the active Build Profile.";

                using (new EditorGUI.DisabledScope(
                           Application.isPlaying ||
                           isEnabledInSceneList))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                sceneListButtonLabel,
                                sceneListButtonTooltip)))
                    {
                        AddOrEnableBuildScene(
                            buildScenes,
                            buildSceneIndex,
                            scenePath);

                        Repaint();
                    }
                }
            }
        }

        private static int FindBuildSceneIndex(
            EditorBuildSettingsScene[] buildScenes,
            string scenePath)
        {
            for (int index = 0;
                 index < buildScenes.Length;
                 index++)
            {
                if (string.Equals(
                        buildScenes[index].path,
                        scenePath,
                        System.StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private static void AddOrEnableBuildScene(
            EditorBuildSettingsScene[] buildScenes,
            int buildSceneIndex,
            string scenePath)
        {
            if (buildSceneIndex >= 0)
            {
                if (buildScenes[buildSceneIndex].enabled)
                {
                    return;
                }

                buildScenes[buildSceneIndex] =
                    new EditorBuildSettingsScene(
                        scenePath,
                        true);

                EditorBuildSettings.scenes =
                    buildScenes;
                return;
            }

            EditorBuildSettingsScene[] updatedBuildScenes =
                new EditorBuildSettingsScene[
                    buildScenes.Length + 1];

            for (int index = 0;
                 index < buildScenes.Length;
                 index++)
            {
                updatedBuildScenes[index] =
                    buildScenes[index];
            }

            updatedBuildScenes[updatedBuildScenes.Length - 1] =
                new EditorBuildSettingsScene(
                    scenePath,
                    true);

            EditorBuildSettings.scenes =
                updatedBuildScenes;
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

            GameApplicationAsset activeGameApplication = _activeGameApplication;
            string contentScenePath = _scenePath?.stringValue ?? string.Empty;
            SceneAsset contentScene = string.IsNullOrWhiteSpace(contentScenePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(contentScenePath);

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
                    contentScene,
                    typeof(SceneAsset),
                    false);

                EditorGUILayout.TextField(
                    "Frame Rate Authority",
                    "Project Settings > Immersive Framework");

                EditorGUILayout.TextField(
                    "Validation Status",
                    GetValidationStatus());
            }

            DrawPlayerSessionAdvancedEvidence();
            DrawProgressionSaveAdvancedEvidence();
            DrawActorSelectionPolicy();

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

        private void DrawProgressionSaveAdvancedEvidence()
        {
            DrawSection("Progression Save Resolution");

            if (_progressionSaveEnabled == null ||
                !_progressionSaveEnabled.boolValue)
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Disabled",
                    EditorStyles.miniLabel);
                return;
            }

            ProgressionSaveProfile profile =
                _defaultProgressionSaveProfile != null
                    ? _defaultProgressionSaveProfile.objectReferenceValue as
                        ProgressionSaveProfile
                    : null;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Profile",
                    profile,
                    typeof(ProgressionSaveProfile),
                    false);

                EditorGUILayout.TextField(
                    "Backend Selection",
                    profile != null
                        ? profile.Backend.ToString()
                        : "<missing>");

                EditorGUILayout.TextField(
                    "Runtime Owner",
                    "FrameworkRuntimeHost — Application Scope");

                EditorGUILayout.TextField(
                    "Fallback",
                    "None");

                if (profile != null &&
                    profile.Backend ==
                        ProgressionSaveBackendSelection.CustomProvider)
                {
                    EditorGUILayout.ObjectField(
                        "Custom Provider",
                        profile.CustomProvider,
                        typeof(ProgressionSaveStoreProviderAsset),
                        false);
                }
            }
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

            _lastValidationReport.AddRange(
                ProgressionSaveAuthoringValidator
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
