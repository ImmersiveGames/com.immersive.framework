using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityAsset))]
    internal sealed class ActivityAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _activityName;
        private SerializedProperty _description;
        private SerializedProperty _activityContentProfile;
        private SerializedProperty _visualTransitionMode;

        private void OnEnable()
        {
            _activityName = serializedObject.FindProperty("activityName");
            _description = serializedObject.FindProperty("description");
            _activityContentProfile = serializedObject.FindProperty("activityContentProfile");
            _visualTransitionMode = serializedObject.FindProperty("visualTransitionMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "An Activity is a gameplay step inside a Route. It has identity, visual transition policy and an optional Activity Content Profile used by Activity scene composition and release.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_activityName, new GUIContent("Activity Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            DrawActivityContentProfile();

            EditorGUILayout.Space(6);
            DrawVisualOperationPolicy();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Activity is an identity target for Activity Flow and scene-authored ActivityLocalVisibilityAdapter. It can declare Activity-owned scene content and select Activity presentation policy, but it does not own TransitionSurface, LoadingSurface, actors, input, camera, save, pause or pooling.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawActivityContentProfile()
        {
            EditorGUILayout.LabelField("Activity Content", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_activityContentProfile, new GUIContent("Content Profile"));
            EditorGUILayout.HelpBox(
                "Optional. Declares Activity-owned scenes used by Activity operation planning, additive composition and release.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Content Profile"))
                {
                    var profile = ImmersiveFrameworkEditorSettingsUtility.CreateActivityContentProfileAsset();
                    if (profile != null)
                    {
                        _activityContentProfile.objectReferenceValue = profile;
                        Selection.activeObject = profile;
                    }
                }

                using (new EditorGUI.DisabledScope(_activityContentProfile.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Content Profile"))
                    {
                        Selection.activeObject = _activityContentProfile.objectReferenceValue;
                    }
                }
            }
        }

        private void DrawVisualOperationPolicy()
        {
            EditorGUILayout.LabelField("Visual Operation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _visualTransitionMode,
                new GUIContent(
                    "Transition Mode",
                    "Controls whether Activity requests use the Session UIGlobal TransitionSurface. Route transitions remain mandatory and are not configured here."));

            var mode = _visualTransitionMode != null && !_visualTransitionMode.hasMultipleDifferentValues
                ? (ActivityVisualTransitionMode)_visualTransitionMode.intValue
                : ActivityVisualTransitionMode.Seamless;

            switch (mode)
            {
                case ActivityVisualTransitionMode.Seamless:
                    EditorGUILayout.HelpBox(
                        "Default. Activity operations run without TransitionSurface and without canonical LoadingSurface. Activity scene load/release may still execute.",
                        MessageType.Info);
                    break;
                case ActivityVisualTransitionMode.Fade:
                    EditorGUILayout.HelpBox(
                        "Activity operations use the Session UIGlobal TransitionSurface and skip canonical LoadingSurface. Activity scene load/release may still execute.",
                        MessageType.Info);
                    break;
                case ActivityVisualTransitionMode.FadeWithLoading:
                    EditorGUILayout.HelpBox(
                        "Activity operations use the Session UIGlobal TransitionSurface and the canonical LoadingSurface when the Activity operation requests loading presentation.",
                        MessageType.Info);
                    break;
            }
        }

        private void DrawAuthoringValidation()
        {
            var report = FrameworkAuthoringValidator.ValidateActivity((ActivityAsset)target);

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
