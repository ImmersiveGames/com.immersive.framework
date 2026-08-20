using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.PlayerParticipation;
using Immersive.Framework.Editor.ProgressionSave;
using Immersive.Framework.Editor.Settings;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.ProgressionSave;
using UnityEditor;
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
        private SerializedProperty _playerSessionEnabled;
        private SerializedProperty _defaultPlayerSessionProfile;
        private SerializedProperty _playerActorSelectionDuplicatePolicy;
        private SerializedProperty _progressionSaveEnabled;
        private SerializedProperty _defaultProgressionSaveProfile;
        private SerializedProperty _persistentContent;
        private SerializedProperty _containerScene;
        private SerializedProperty _validationMode;

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
            _persistentContent =
                serializedObject.FindProperty("persistentContent");
            _containerScene =
                _persistentContent?.FindPropertyRelative(
                    "containerScene");
            _validationMode =
                serializedObject.FindProperty("validationMode");

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
                    "Tip: create the starting Persistent Content Scene with File > New Scene > Immersive Persistent Content.",
                    MessageType.Info);

                EditorGUILayout.HelpBox(
                    "Select the Persistent Content Scene.",
                    MessageType.Error);
                return;
            }

            string scenePath =
                AssetDatabase.GetAssetPath(selectedScene);

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
