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
        private bool _showAdditionalContent;
        private bool _showAdvancedDiagnostics;

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

            DrawInspectorHeader();

            EditorGUILayout.Space(6f);
            DrawOverview();

            EditorGUILayout.Space(8f);
            DrawPrimaryScene();

            EditorGUILayout.Space(8f);
            DrawFirstActivity();

            EditorGUILayout.Space(8f);
            DrawAdditionalContent();

            EditorGUILayout.Space(8f);
            DrawTransition();

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

            bool modified = serializedObject.ApplyModifiedProperties();
            if (modified && _lastValidationReport != null)
            {
                _validationOutdated = true;
            }
        }

        [MenuItem("Assets/Create/Immersive Framework/Route", false, 10)]
        private static void CreateRouteAsset()
        {
            RouteAsset route =
                ImmersiveFrameworkEditorSettingsUtility.CreateStartupRouteAsset();

            if (route == null)
            {
                return;
            }

            Selection.activeObject = route;
            EditorGUIUtility.PingObject(route);
        }

        private void DrawInspectorHeader()
        {
            EditorGUILayout.LabelField(
                "Route",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A Route represents a major destination in the game flow, such as Main Menu, Gameplay, Results or Credits.",
                MessageType.Info);
        }

        private void DrawOverview()
        {
            EditorGUILayout.LabelField(
                "Overview",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _routeName,
                new GUIContent(
                    "Route Name",
                    "Designer-facing name used for presentation and diagnostics."));
            EditorGUILayout.PropertyField(
                _description,
                new GUIContent(
                    "Description",
                    "Optional note explaining the purpose of this destination."));

            EditorGUILayout.HelpBox(
                "Stable identity and application-role checks run only through Validate Route. Technical identity remains visible under Advanced / Diagnostics.",
                MessageType.None);
        }

        private void DrawPrimaryScene()
        {
            EditorGUILayout.LabelField(
                "Primary Scene",
                EditorStyles.boldLabel);

            SceneAsset selectedScene =
                (SceneAsset)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Scene",
                        "Main Unity scene opened when this Route starts."),
                    _primarySceneAsset,
                    typeof(SceneAsset),
                    false);

            if (selectedScene != _primarySceneAsset)
            {
                SetPrimaryScene(selectedScene);
                _primarySceneAsset = selectedScene;
            }

            EditorGUILayout.HelpBox(
                "Assign the main environment for this Route. Scene existence and Build Profile checks run only through Validate Route.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(
                       _primarySceneAsset == null))
            {
                if (GUILayout.Button("Open Primary Scene"))
                {
                    // Scene opening invalidates the current Inspector serialized
                    // context. End this IMGUI event immediately after navigation.
                    serializedObject.ApplyModifiedProperties();
                    _serializedBindingsDirty = true;

                    AssetDatabase.OpenAsset(
                        _primarySceneAsset);

                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawFirstActivity()
        {
            EditorGUILayout.LabelField(
                "First Activity",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _startupActivity,
                new GUIContent(
                    "Activity",
                    "Optional Activity started after the Primary Scene is ready."));

            Object activity =
                _startupActivity.objectReferenceValue;

            if (activity == null)
            {
                EditorGUILayout.HelpBox(
                    "This Route opens without automatically starting an Activity.",
                    MessageType.None);

                if (GUILayout.Button("Create First Activity"))
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

            EditorGUILayout.HelpBox(
                "This Activity starts after the Primary Scene is ready.",
                MessageType.None);

            if (GUILayout.Button("Open Activity"))
            {
                Selection.activeObject = activity;
                EditorGUIUtility.PingObject(activity);
            }
        }

        private void DrawAdditionalContent()
        {
            _showAdditionalContent =
                EditorGUILayout.Foldout(
                    _showAdditionalContent,
                    "Additional Content",
                    true);

            if (!_showAdditionalContent)
            {
                return;
            }

            EditorGUILayout.PropertyField(
                _routeContentProfile,
                new GUIContent(
                    "Content Profile",
                    "Optional additional Route-scoped scenes composed with the Primary Scene."));

            Object profile =
                _routeContentProfile.objectReferenceValue;

            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "No additional Route content is assigned.",
                    MessageType.None);

                if (GUILayout.Button("Add Content Profile"))
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

            EditorGUILayout.HelpBox(
                "The assigned Profile declares additional Route-scoped scenes.",
                MessageType.None);

            if (GUILayout.Button("Open Content Profile"))
            {
                Selection.activeObject = profile;
                EditorGUIUtility.PingObject(profile);
            }
        }

        private void DrawTransition()
        {
            EditorGUILayout.LabelField(
                "Transition",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _transitionGateMode,
                new GUIContent(
                    "Block During Transition",
                    "Controls which capabilities remain blocked while this Route transition is running."));

            EditorGUILayout.HelpBox(
                "The selected policy is evaluated by the Route transition runtime. Serialized-value and policy checks run only through Validate Route.",
                MessageType.None);
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (_lastValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Run validation after configuring the Route.",
                    MessageType.None);
            }
            else if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the Route changed.",
                    MessageType.Warning);
            }
            else if (_lastValidationReport.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Ready — no blocking Route configuration issues were found.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Needs Attention — {_lastValidationReport.ErrorCount} blocking issue(s) were found.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Validate Route"))
            {
                serializedObject.ApplyModifiedProperties();
                RunValidation();
                _serializedBindingsDirty = true;

                // Project-wide identity validation can refresh imported assets.
                // End this IMGUI event before cached SerializedProperty
                // instances are drawn again.
                GUIUtility.ExitGUI();
            }
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

            DrawRouteId();
            DrawTechnicalReferences();
            DrawCurrentScope();
            DrawFullValidationReport();
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

            EditorGUILayout.HelpBox(
                "Route ID validity is checked only by Validate Route. Existing IDs are not replaced automatically.",
                MessageType.None);
        }

        private void DrawTechnicalReferences()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Technical References",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Primary Scene Path",
                    _primaryScenePath != null
                        ? _primaryScenePath.stringValue ?? string.Empty
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
            }
        }

        private void DrawCurrentScope()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Current Scope",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Route declares identity, one Primary Scene, optional Route-scoped additional content, an optional First Activity and transition blocking policy. Runtime lifecycle, materialization, Player participation, input, camera, pause and gameplay remain owned by their respective systems.",
                MessageType.None);
        }

        private void DrawFullValidationReport()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            if (_lastValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "No validation report is available.",
                    MessageType.None);
                return;
            }

            if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "This report is outdated. Run Validate Route again.",
                    MessageType.Warning);
            }

            FrameworkAuthoringValidationGui.DrawSummary(
                _lastValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);
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
    }
}
