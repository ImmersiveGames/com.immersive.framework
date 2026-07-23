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

        private FrameworkAuthoringValidationReport _lastValidationReport;
        private bool _validationOutdated;
        private bool _showAdditionalContent;
        private bool _showAdvancedDiagnostics;

        private void OnEnable()
        {
            _routeName = serializedObject.FindProperty("routeName");
            _routeId = serializedObject.FindProperty("routeId");
            _primaryScenePath = serializedObject.FindProperty("primaryScenePath");
            _primarySceneName = serializedObject.FindProperty("primarySceneName");
            _routeContentProfile = serializedObject.FindProperty("routeContentProfile");
            _startupActivity = serializedObject.FindProperty("startupActivity");
            _transitionGateMode = serializedObject.FindProperty("transitionGateMode");
            _description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();

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
            RouteAsset route = ImmersiveFrameworkEditorSettingsUtility.CreateStartupRouteAsset();
            if (route != null)
            {
                Selection.activeObject = route;
                EditorGUIUtility.PingObject(route);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Route", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A Route represents a major destination in the game flow, such as Main Menu, Gameplay, Results or Credits.",
                MessageType.Info);
        }

        private void DrawOverview()
        {
            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
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

            DrawApplicationRole();
            DrawIdentityProblem();
        }

        private void DrawApplicationRole()
        {
            RouteAsset route = (RouteAsset)target;
            GameApplicationAsset activeApplication =
                ImmersiveFrameworkEditorSettingsUtility.GetActiveGameApplication();

            if (activeApplication == null)
            {
                EditorGUILayout.HelpBox(
                    "No active Game Application is assigned. This Route can still be authored, but its application role cannot be determined.",
                    MessageType.None);
                return;
            }

            if (activeApplication.StartupRoute == route)
            {
                EditorGUILayout.HelpBox(
                    "Startup Route — this Route is the active application's entry point.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "Application Route — this Route can be entered later through Game Flow.",
                MessageType.None);
        }

        private void DrawIdentityProblem()
        {
            if (_routeId != null && RouteId.IsValidText(_routeId.stringValue))
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Route ID is missing or malformed. Generate a canonical ID explicitly; validation never repairs identity silently.",
                MessageType.Error);

            if (_routeId != null && GUILayout.Button("Generate Route ID"))
            {
                _routeId.stringValue =
                    ImmersiveFrameworkEditorSettingsUtility.GenerateRouteIdText();
            }
        }

        private void DrawPrimaryScene()
        {
            EditorGUILayout.LabelField("Primary Scene", EditorStyles.boldLabel);

            SceneAsset currentScene = LoadCurrentSceneAsset();
            SceneAsset selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Scene",
                    "Main Unity scene opened when this Route starts."),
                currentScene,
                typeof(SceneAsset),
                false);

            if (selectedScene != currentScene)
            {
                SetPrimaryScene(selectedScene);
                currentScene = selectedScene;
            }

            if (currentScene == null)
            {
                EditorGUILayout.HelpBox(
                    "A Primary Scene is required before this Route can run.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "Ready — this scene is the main environment opened by this Route. Use Validate Route to verify Build Profile availability.",
                MessageType.Info);

            if (GUILayout.Button("Open Primary Scene"))
            {
                AssetDatabase.OpenAsset(currentScene);
            }
        }

        private void DrawFirstActivity()
        {
            EditorGUILayout.LabelField("First Activity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _startupActivity,
                new GUIContent(
                    "Activity",
                    "Optional Activity started after the Primary Scene is ready."));

            Object activity = _startupActivity.objectReferenceValue;
            if (activity == null)
            {
                EditorGUILayout.HelpBox(
                    "This Route opens its Primary Scene without starting an Activity.",
                    MessageType.None);

                if (GUILayout.Button("Create First Activity"))
                {
                    ActivityAsset created =
                        ImmersiveFrameworkEditorSettingsUtility.CreateStartupActivityAsset();
                    if (created != null)
                    {
                        _startupActivity.objectReferenceValue = created;
                        Selection.activeObject = created;
                        EditorGUIUtility.PingObject(created);
                    }
                }

                return;
            }

            EditorGUILayout.HelpBox(
                "This Activity starts after the Primary Scene is ready.",
                MessageType.Info);

            if (GUILayout.Button("Open Activity"))
            {
                Selection.activeObject = activity;
                EditorGUIUtility.PingObject(activity);
            }
        }

        private void DrawAdditionalContent()
        {
            _showAdditionalContent = EditorGUILayout.Foldout(
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

            Object profile = _routeContentProfile.objectReferenceValue;
            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "No additional Route content. This Route currently uses only its Primary Scene.",
                    MessageType.None);

                if (GUILayout.Button("Add Content Profile"))
                {
                    RouteContentProfileAsset created =
                        ImmersiveFrameworkEditorSettingsUtility.CreateRouteContentProfileAsset();
                    if (created != null)
                    {
                        _routeContentProfile.objectReferenceValue = created;
                        Selection.activeObject = created;
                        EditorGUIUtility.PingObject(created);
                    }
                }

                return;
            }

            EditorGUILayout.HelpBox(
                "Additional scenes from this Profile are composed with the Route.",
                MessageType.Info);

            if (GUILayout.Button("Open Content Profile"))
            {
                Selection.activeObject = profile;
                EditorGUIUtility.PingObject(profile);
            }
        }

        private void DrawTransition()
        {
            EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _transitionGateMode,
                new GUIContent(
                    "Block During Transition",
                    "Controls which capabilities remain blocked while this Route transition is running."));

            EditorGUILayout.HelpBox(
                BuildTransitionSummary(),
                MessageType.None);
        }

        private string BuildTransitionSummary()
        {
            if (_transitionGateMode == null)
            {
                return "Transition policy is unavailable.";
            }

            string[] displayNames = _transitionGateMode.enumDisplayNames;
            int selectedIndex = _transitionGateMode.enumValueIndex;
            if (selectedIndex < 0 || selectedIndex >= displayNames.Length)
            {
                return "Transition policy contains an invalid serialized value. Run Validate Route for details.";
            }

            return $"Current policy: {displayNames[selectedIndex]}. The selected capabilities remain blocked until the Route transition completes.";
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

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
                serializedObject.Update();
                RunValidation();
            }
        }

        private void RunValidation()
        {
            RouteAsset route = (RouteAsset)target;

            _lastValidationReport =
                FrameworkAuthoringValidator.ValidateRoute(route, true);
            _lastValidationReport.AddRange(
                FrameworkIdentityAuthoringValidator.ValidateProjectAssets(
                    FrameworkValidationMode.Standard));

            _validationOutdated = false;
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

            DrawRouteId();
            DrawTechnicalReferences();
            DrawCurrentScope();
            DrawFullValidationReport();
        }

        private void DrawRouteId()
        {
            string routeId = _routeId != null
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

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(routeId)))
                {
                    if (GUILayout.Button("Copy ID", GUILayout.Width(70f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = routeId;
                    }
                }
            }
        }

        private void DrawTechnicalReferences()
        {
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
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Route declares identity, one Primary Scene, optional Route-scoped additional content, an optional First Activity and transition blocking policy. Runtime lifecycle, materialization, Player participation, input, camera, pause and gameplay remain owned by their respective systems.",
                MessageType.None);
        }

        private void DrawFullValidationReport()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Validation Report", EditorStyles.boldLabel);

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

            FrameworkAuthoringValidationGui.DrawSummary(_lastValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(
                _lastValidationReport,
                false);
        }

        private SceneAsset LoadCurrentSceneAsset()
        {
            if (_primaryScenePath == null ||
                string.IsNullOrWhiteSpace(_primaryScenePath.stringValue))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(
                _primaryScenePath.stringValue);
        }

        private void SetPrimaryScene(SceneAsset sceneAsset)
        {
            if (_primaryScenePath == null || _primarySceneName == null)
            {
                return;
            }

            if (sceneAsset == null)
            {
                _primaryScenePath.stringValue = string.Empty;
                _primarySceneName.stringValue = string.Empty;
                return;
            }

            _primaryScenePath.stringValue =
                AssetDatabase.GetAssetPath(sceneAsset);
            _primarySceneName.stringValue = sceneAsset.name;
        }
    }
}
