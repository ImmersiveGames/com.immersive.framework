using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(RouteAsset))]
    internal sealed class RouteAssetEditor : UnityEditor.Editor
    {
        private const int RouteScenePickerControlId = 1842202;
        private static readonly GUIContent RouteNameLabel =
            new GUIContent(
                "Route Name",
                "Designer-facing name used for presentation and diagnostics.");

        private static readonly GUIContent DescriptionLabel =
            new GUIContent(
                "Description",
                "Optional note describing the purpose of this destination.");

        private static readonly GUIContent PrimarySceneLabel =
            new GUIContent(
                "Scene",
                "Main Unity scene opened when this Route starts.");

        private static readonly GUIContent StartupActivityLabel =
            new GUIContent(
                "Activity",
                "Optional Activity started after the Primary Scene is ready.");

        private static readonly GUIContent ContentProfileLabel =
            new GUIContent(
                "Content Profile",
                "Optional additional Route-scoped scenes composed with the Primary Scene.");

        private static readonly GUIContent TransitionGateLabel =
            new GUIContent(
                "Block During Transition",
                "Controls which requests and capabilities remain blocked while this Route transition runs.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates this Route and its configured dependencies without modifying them.");

        private SerializedProperty _routeName;
        private SerializedProperty _routeId;
        private SerializedProperty _primaryScenePath;
        private SerializedProperty _primarySceneName;
        private SerializedProperty _routeContentProfile;
        private SerializedProperty _startupActivity;
        private SerializedProperty _transitionGateMode;
        private SerializedProperty _description;

        private SceneAsset _primarySceneAsset;
        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _serializedBindingsDirty = true;
        private bool _validationOutdated;
        private bool _showAdvancedDebug;
        private string _sceneActionMessage = string.Empty;

        private void OnEnable()
        {
            _serializedBindingsDirty = true;
        }

        private void RefreshSerializedBindings()
        {
            _routeName =
                serializedObject.FindProperty("routeName");
            _routeId =
                serializedObject.FindProperty("routeId");
            _primaryScenePath =
                serializedObject.FindProperty("primaryScenePath");
            _primarySceneName =
                serializedObject.FindProperty("primarySceneName");
            _routeContentProfile =
                serializedObject.FindProperty("routeContentProfile");
            _startupActivity =
                serializedObject.FindProperty("startupActivity");
            _transitionGateMode =
                serializedObject.FindProperty("transitionGateMode");
            _description =
                serializedObject.FindProperty("description");

            _primarySceneAsset =
                ResolvePrimarySceneAsset();

            _serializedBindingsDirty = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (_serializedBindingsDirty)
            {
                RefreshSerializedBindings();
            }

            HandleRouteScenePicker();

            EditorGUILayout.LabelField(
                "Route",
                EditorStyles.boldLabel);

            DrawOverview();
            DrawPrimaryScene();
            DrawFirstActivity();
            DrawAdditionalContent();
            DrawTransition();
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

        private void DrawOverview()
        {
            DrawSection("Overview");

            EditorGUILayout.PropertyField(
                _routeName,
                RouteNameLabel);

            EditorGUILayout.PropertyField(
                _description,
                DescriptionLabel);
        }

        private void DrawPrimaryScene()
        {
            DrawSection("Primary Scene");

            SceneAsset selectedScene =
                (SceneAsset)EditorGUILayout.ObjectField(
                    PrimarySceneLabel,
                    _primarySceneAsset,
                    typeof(SceneAsset),
                    false);

            if (selectedScene != _primarySceneAsset)
            {
                SetPrimaryScene(selectedScene);
                _primarySceneAsset = selectedScene;
            }

            if (_primarySceneAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "Select the Primary Scene for this Route.",
                    MessageType.Error);
                return;
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Open Primary Scene",
                        "Opens the assigned Primary Scene.")))
            {
                serializedObject.ApplyModifiedProperties();
                _serializedBindingsDirty = true;

                AssetDatabase.OpenAsset(
                    _primarySceneAsset);

                GUIUtility.ExitGUI();
            }
        }

        private void DrawFirstActivity()
        {
            DrawSection("First Activity");

            EditorGUILayout.PropertyField(
                _startupActivity,
                StartupActivityLabel);

            Object activity =
                _startupActivity.objectReferenceValue;

            if (activity == null)
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Create First Activity",
                            "Creates and assigns an optional startup Activity.")))
                {
                    ActivityAsset created =
                        ImmersiveFrameworkEditorSettingsUtility
                            .CreateStartupActivityAsset();

                    if (created != null)
                    {
                        _startupActivity.objectReferenceValue =
                            created;
                        Selection.activeObject = created;
                        EditorGUIUtility.PingObject(created);
                    }
                }

                return;
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Open Activity",
                        "Selects and pings the assigned startup Activity.")))
            {
                Selection.activeObject = activity;
                EditorGUIUtility.PingObject(activity);
            }
        }

        private void DrawAdditionalContent()
        {
            DrawSection("Additional Content");

            EditorGUILayout.PropertyField(
                _routeContentProfile,
                ContentProfileLabel);

            RouteContentProfileAsset profile =
                _routeContentProfile.objectReferenceValue
                    as RouteContentProfileAsset;

            if (profile == null)
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Create Content Profile",
                            "Creates and assigns a Route Content Profile before additional scenes are added.")))
                {
                    RouteContentProfileAsset created =
                        ImmersiveFrameworkEditorSettingsUtility
                            .CreateRouteContentProfileAsset();

                    if (created != null)
                    {
                        _routeContentProfile.objectReferenceValue =
                            created;
                        Selection.activeObject = created;
                        EditorGUIUtility.PingObject(created);
                    }
                }

                return;
            }

            DrawRouteSceneSummary(profile);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Add Scene...",
                            "Adds an additional Route Scene to the assigned Content Profile and suggests a stable Content Id.")))
                {
                    EditorGUIUtility.ShowObjectPicker<SceneAsset>(
                        null,
                        false,
                        string.Empty,
                        RouteScenePickerControlId);
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "Open Content Profile",
                            "Opens the assigned Profile for IDs and detailed validation.")))
                {
                    Selection.activeObject = profile;
                    EditorGUIUtility.PingObject(profile);
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    _sceneActionMessage))
            {
                EditorGUILayout.LabelField(
                    _sceneActionMessage,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawRouteSceneSummary(
            RouteContentProfileAsset profile)
        {
            EditorGUILayout.LabelField(
                "Additional Scenes",
                profile.AdditionalSceneCount.ToString());

            for (int index = 0;
                 index < profile.AdditionalSceneCount;
                 index++)
            {
                RouteContentSceneEntry entry =
                    profile.AdditionalScenes[index];

                string sceneName =
                    entry != null &&
                    !string.IsNullOrWhiteSpace(
                        entry.SceneName)
                        ? entry.SceneName
                        : "<missing Scene>";

                EditorGUILayout.LabelField(
                    $"- {sceneName}",
                    EditorStyles.miniLabel);
            }
        }

        private void HandleRouteScenePicker()
        {
            Event currentEvent =
                Event.current;

            if (currentEvent == null ||
                currentEvent.commandName !=
                    "ObjectSelectorClosed" ||
                EditorGUIUtility
                    .GetObjectPickerControlID() !=
                    RouteScenePickerControlId)
            {
                return;
            }

            SceneAsset selectedScene =
                EditorGUIUtility
                    .GetObjectPickerObject()
                    as SceneAsset;

            RouteContentProfileAsset profile =
                _routeContentProfile != null
                    ? _routeContentProfile
                        .objectReferenceValue
                        as RouteContentProfileAsset
                    : null;

            if (selectedScene != null &&
                profile != null)
            {
                if (!ContentProfileSceneAuthoringUtility
                        .TryAddRouteScene(
                            profile,
                            selectedScene,
                            out _sceneActionMessage))
                {
                    EditorUtility.DisplayDialog(
                        "Route Scene Not Added",
                        _sceneActionMessage,
                        "OK");
                }
                else
                {
                    _lastValidationReport = null;
                    Repaint();
                }
            }

            if (currentEvent.type != EventType.Layout)
            {
                if (currentEvent.type != EventType.Layout)
            {
                currentEvent.Use();
            }
            }
        }

        private void DrawTransition()
        {
            DrawSection("Transition");

            EditorGUILayout.PropertyField(
                _transitionGateMode,
                TransitionGateLabel);
        }

        private void DrawValidation()
        {
            DrawSection("Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        ValidateLabel,
                        GUILayout.Width(96f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    RunValidation();
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

        private void RunValidation()
        {
            RouteAsset route =
                (RouteAsset)target;

            _lastValidationReport =
                FrameworkAuthoringValidator.ValidateRoute(
                    route,
                    true);

            _lastValidationReport.AddRange(
                FrameworkIdentityAuthoringValidator
                    .ValidateProjectAssets(
                        FrameworkValidationMode.Standard));

            _primarySceneAsset =
                ResolvePrimarySceneAsset();

            _validationOutdated = false;
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
                        "Shows stable identity, technical references and the complete validation report."),
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawRouteId();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Primary Scene Path",
                    _primaryScenePath != null
                        ? _primaryScenePath.stringValue ?? string.Empty
                        : string.Empty);

                EditorGUILayout.TextField(
                    "Primary Scene Name",
                    _primarySceneName != null
                        ? _primarySceneName.stringValue ?? string.Empty
                        : string.Empty);

                EditorGUILayout.ObjectField(
                    "Startup Activity Reference",
                    _startupActivity != null
                        ? _startupActivity.objectReferenceValue
                        : null,
                    typeof(ActivityAsset),
                    false);

                EditorGUILayout.ObjectField(
                    "Content Profile Reference",
                    _routeContentProfile != null
                        ? _routeContentProfile.objectReferenceValue
                        : null,
                    typeof(RouteContentProfileAsset),
                    false);

                EditorGUILayout.TextField(
                    "Transition Gate",
                    GetSerializedEnumLabel(
                        _transitionGateMode));

                EditorGUILayout.TextField(
                    "Validation Status",
                    GetValidationStatus());
            }

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

        private void DrawRouteId()
        {
            string routeId =
                _routeId != null
                    ? _routeId.stringValue ?? string.Empty
                    : string.Empty;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Route ID",
                            "Stable functional identity independent from Route Name and asset filename."),
                        routeId);
                }

                using (new EditorGUI.DisabledScope(
                           !string.IsNullOrWhiteSpace(routeId)))
                {
                    if (GUILayout.Button(
                            "Generate ID",
                            GUILayout.Width(90f)))
                    {
                        _routeId.stringValue =
                            ImmersiveFrameworkEditorSettingsUtility
                                .GenerateRouteIdText();
                    }
                }

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(routeId)))
                {
                    if (GUILayout.Button(
                            "Copy ID",
                            GUILayout.Width(70f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            routeId;
                    }
                }
            }
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

        private static string GetSerializedEnumLabel(
            SerializedProperty property)
        {
            if (property == null)
            {
                return "Unavailable";
            }

            if (property.hasMultipleDifferentValues)
            {
                return "Mixed Values";
            }

            string[] displayNames =
                property.enumDisplayNames;

            int selectedIndex =
                property.enumValueIndex;

            if (selectedIndex >= 0 &&
                selectedIndex < displayNames.Length)
            {
                return displayNames[selectedIndex];
            }

            return $"Serialized value {property.intValue}";
        }

        private SceneAsset ResolvePrimarySceneAsset()
        {
            if (_primaryScenePath == null ||
                string.IsNullOrWhiteSpace(
                    _primaryScenePath.stringValue))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(
                _primaryScenePath.stringValue);
        }

        private void SetPrimaryScene(
            SceneAsset sceneAsset)
        {
            if (_primaryScenePath == null ||
                _primarySceneName == null)
            {
                return;
            }

            if (sceneAsset == null)
            {
                _primaryScenePath.stringValue =
                    string.Empty;
                _primarySceneName.stringValue =
                    string.Empty;
                return;
            }

            _primaryScenePath.stringValue =
                AssetDatabase.GetAssetPath(sceneAsset);

            _primarySceneName.stringValue =
                sceneAsset.name;
        }

        private static void DrawSection(
            string title)
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }
    }
}
