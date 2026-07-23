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
        private bool _showIdentityDebug;

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

            EditorGUILayout.LabelField("Route", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A Route is an entry in the Game Flow. It declares a Primary Scene and can optionally declare the first Activity to start after the scene is resolved.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_routeName, new GUIContent("Route Name", "Designer-facing name used for presentation and diagnostics only."));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            if (_routeId == null || !RouteId.IsValidText(_routeId.stringValue))
            {
                EditorGUILayout.HelpBox("Route ID is missing or malformed. Create Routes through the official authoring flow; validation never repairs identity silently.", MessageType.Error);
            }

            DrawIdentityDebug();

            EditorGUILayout.Space(6);
            DrawPrimaryScene();

            EditorGUILayout.Space(6);
            DrawRouteContentProfile();

            EditorGUILayout.Space(6);
            DrawStartupActivity();

            EditorGUILayout.Space(6);
            DrawTransitionPolicy();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Route declares identity, one Primary Scene, an optional Route Content Profile executed by Route scene composition, and an optional Startup Activity. Release/unload, Activity materialization, actors, input, camera, save, pause and pooling come later.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
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

        private void DrawAuthoringValidation()
        {
            RouteAsset route = (RouteAsset)target;
            FrameworkAuthoringValidationReport report =
                FrameworkAuthoringValidator.ValidateRoute(route, true);
            report.AddRange(
                FrameworkIdentityAuthoringValidator.ValidateProjectAssets(
                    FrameworkValidationMode.Standard));

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private void DrawIdentityDebug()
        {
            _showIdentityDebug = EditorGUILayout.Foldout(
                _showIdentityDebug,
                "Advanced / Debug",
                true);
            if (!_showIdentityDebug)
            {
                return;
            }

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
                            "Stable functional identity. It is independent from Route Name and Primary Scene."),
                        routeId);
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(routeId)))
                {
                    if (GUILayout.Button("Copy ID", GUILayout.Width(70)))
                    {
                        EditorGUIUtility.systemCopyBuffer = routeId;
                    }
                }
            }
        }

        private void DrawPrimaryScene()
        {
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

            var currentScene = LoadCurrentSceneAsset();
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Primary Scene"),
                currentScene,
                typeof(SceneAsset),
                false);

            if (selectedScene != currentScene)
            {
                SetPrimaryScene(selectedScene);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Scene Path", _primaryScenePath.stringValue ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(_primaryScenePath.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Primary Scene is required for the Startup Route to pass boot validation.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Primary Scene is loaded by Scene Lifecycle when this Route starts.",
                    MessageType.Info);
            }
        }


        private void DrawRouteContentProfile()
        {
            EditorGUILayout.LabelField("Route Content", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_routeContentProfile, new GUIContent("Content Profile"));
            EditorGUILayout.HelpBox(
                "Optional. Additional scenes declared in the profile are loaded additively by F6E Route scene composition. Release/unload remains deferred to a later cut.",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Content Profile"))
                {
                    var profile = ImmersiveFrameworkEditorSettingsUtility.CreateRouteContentProfileAsset();
                    if (profile != null)
                    {
                        _routeContentProfile.objectReferenceValue = profile;
                        Selection.activeObject = profile;
                    }
                }

                using (new EditorGUI.DisabledScope(_routeContentProfile.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Content Profile"))
                    {
                        Selection.activeObject = _routeContentProfile.objectReferenceValue;
                    }
                }
            }
        }

        private void DrawStartupActivity()
        {
            EditorGUILayout.LabelField("Activity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_startupActivity, new GUIContent("Startup Activity"));
            EditorGUILayout.HelpBox(
                "Optional. If assigned, Activity Flow starts this Activity after the Route Primary Scene is resolved through the Activity operation path. Activity-owned scenes may be composed; actors, input, camera, save and pause remain outside Route startup ownership.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Startup Activity"))
                {
                    var activity = ImmersiveFrameworkEditorSettingsUtility.CreateStartupActivityAsset();
                    if (activity != null)
                    {
                        _startupActivity.objectReferenceValue = activity;
                        Selection.activeObject = activity;
                    }
                }

                using (new EditorGUI.DisabledScope(_startupActivity.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Startup Activity"))
                    {
                        Selection.activeObject = _startupActivity.objectReferenceValue;
                    }
                }
            }
        }

        private void DrawTransitionPolicy()
        {
            EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _transitionGateMode,
                new GUIContent(
                    "Transition Gate",
                    "Controls which capabilities are blocked while this Route transition/lifecycle window is running."));
            EditorGUILayout.HelpBox(
                "Route transitions normally use the UIGlobal TransitionSurface. Use InputInteractionAndGameplay for First Game style fades so UI clicks and gameplay actions are blocked until the transition closes.",
                MessageType.Info);
        }

        private SceneAsset LoadCurrentSceneAsset()
        {
            if (string.IsNullOrWhiteSpace(_primaryScenePath.stringValue))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(_primaryScenePath.stringValue);
        }

        private void SetPrimaryScene(SceneAsset sceneAsset)
        {
            if (sceneAsset == null)
            {
                _primaryScenePath.stringValue = string.Empty;
                _primarySceneName.stringValue = string.Empty;
                return;
            }

            _primaryScenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            _primarySceneName.stringValue = sceneAsset.name;
        }
    }
}
