using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.PlayerParticipation;
using Immersive.Framework.Editor.Editor.Settings;
using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityAsset))]
    internal sealed class ActivityAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _activityName;
        private SerializedProperty _description;
        private SerializedProperty _playerParticipationProjectionProfile;
        private SerializedProperty _playerParticipationRequirementsProfile;
        private SerializedProperty _activityContentProfile;
        private SerializedProperty _visualTransitionMode;
        private SerializedProperty _transitionGateMode;
        private bool _showParticipationDebug;

        private void OnEnable()
        {
            _activityName = serializedObject.FindProperty("activityName");
            _description = serializedObject.FindProperty("description");
            _playerParticipationProjectionProfile =
                serializedObject.FindProperty("playerParticipationProjectionProfile");
            _playerParticipationRequirementsProfile =
                serializedObject.FindProperty("playerParticipationRequirementsProfile");
            _activityContentProfile = serializedObject.FindProperty("activityContentProfile");
            _visualTransitionMode = serializedObject.FindProperty("visualTransitionMode");
            _transitionGateMode = serializedObject.FindProperty("transitionGateMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "An Activity is a gameplay step inside a Route. It declares explicit Player participation intent, visual transition policy and optional Activity-owned content.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_activityName, new GUIContent("Activity Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            DrawPlayerParticipation();

            EditorGUILayout.Space(6);
            DrawActivityContentProfile();

            EditorGUILayout.Space(6);
            DrawVisualOperationPolicy();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Activity authors Player Slot projection and reusable readiness requirements, but P3D does not evaluate Session state or block runtime admission yet. Runtime participation authority and lifecycle evaluation remain later cuts.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawPlayerParticipation()
        {
            EditorGUILayout.LabelField("Player Participation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Projection selects which Session Slots are evaluated. Requirements defines the readiness level those projected Slots must satisfy. Both references are mandatory; use explicit No Slots + None Profiles for an Activity with no Players.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _playerParticipationProjectionProfile,
                new GUIContent("Projection Profile"));
            EditorGUILayout.PropertyField(
                _playerParticipationRequirementsProfile,
                new GUIContent("Requirements Profile"));

            DrawParticipationSummary();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           _playerParticipationProjectionProfile.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Projection"))
                    {
                        Selection.activeObject =
                            _playerParticipationProjectionProfile.objectReferenceValue;
                    }
                }

                using (new EditorGUI.DisabledScope(
                           _playerParticipationRequirementsProfile.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Requirements"))
                    {
                        Selection.activeObject =
                            _playerParticipationRequirementsProfile.objectReferenceValue;
                    }
                }
            }

            _showParticipationDebug = EditorGUILayout.Foldout(
                _showParticipationDebug,
                "Advanced / Debug",
                true);
            if (_showParticipationDebug)
            {
                DrawParticipationDebug();
            }
        }

        private void DrawParticipationSummary()
        {
            var projection =
                _playerParticipationProjectionProfile.objectReferenceValue as
                    ActivityParticipationProjectionProfile;
            var requirements =
                _playerParticipationRequirementsProfile.objectReferenceValue as
                    PlayerParticipationRequirementsProfile;

            if (projection == null || requirements == null)
            {
                EditorGUILayout.HelpBox(
                    "Incomplete Player participation authoring. Assign both explicit Profiles; null does not select a default.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Projection: {projection.DisplayName} ({projection.ProjectionMode})\n" +
                $"Requirements: {requirements.DisplayName} ({requirements.RequirementLevel})",
                MessageType.None);
        }

        private void DrawParticipationDebug()
        {
            var projection =
                _playerParticipationProjectionProfile.objectReferenceValue as
                    ActivityParticipationProjectionProfile;
            var requirements =
                _playerParticipationRequirementsProfile.objectReferenceValue as
                    PlayerParticipationRequirementsProfile;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Projection Mode",
                    projection != null ? projection.ProjectionMode.ToString() : "Missing");
                EditorGUILayout.TextField(
                    "Zero Participant Policy",
                    projection != null ? projection.ZeroParticipantPolicy.ToString() : "Missing");
                EditorGUILayout.IntField(
                    "Explicit Slot Count",
                    projection != null ? projection.ExplicitSlotProfiles.Count : 0);
                EditorGUILayout.TextField(
                    "Requirement Level",
                    requirements != null ? requirements.RequirementLevel.ToString() : "Missing");
            }
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
            EditorGUILayout.PropertyField(
                _transitionGateMode,
                new GUIContent(
                    "Transition Gate",
                    "Controls which capabilities are blocked while the Activity transition/lifecycle window is running."));

            var mode = _visualTransitionMode != null && !_visualTransitionMode.hasMultipleDifferentValues
                ? (ActivityVisualTransitionMode)_visualTransitionMode.intValue
                : ActivityVisualTransitionMode.Seamless;
            var gateMode = _transitionGateMode != null && !_transitionGateMode.hasMultipleDifferentValues
                ? (Immersive.Framework.Transition.TransitionGateMode)_transitionGateMode.intValue
                : Immersive.Framework.Transition.TransitionGateMode.LifecycleRequestsOnly;

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

            if (mode != ActivityVisualTransitionMode.Seamless && gateMode != Immersive.Framework.Transition.TransitionGateMode.InputInteractionAndGameplay)
            {
                EditorGUILayout.HelpBox(
                    "For visible Activity fades, Transition Gate = InputInteractionAndGameplay is recommended so repeated UI clicks do not reach ActivityRequestTrigger during the fade.",
                    MessageType.Warning);
            }
        }

        private void DrawAuthoringValidation()
        {
            ActivityAsset activity = (ActivityAsset)target;
            FrameworkAuthoringValidationReport report =
                FrameworkAuthoringValidator.ValidateActivity(activity);
            report.AddRange(
                ActivityParticipationProjectionAuthoringValidator.ValidateActivity(activity));

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
