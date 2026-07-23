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
        private SerializedProperty _activityId;
        private SerializedProperty _description;
        private SerializedProperty _playerParticipationProjectionMode;
        private SerializedProperty _playerParticipationZeroParticipantPolicy;
        private SerializedProperty _playerParticipationExplicitSlotProfiles;
        private SerializedProperty _playerParticipationRequirementLevel;
        private SerializedProperty _activityContentProfile;
        private SerializedProperty _visualTransitionMode;
        private SerializedProperty _transitionGateMode;
        private bool _showIdentityDebug;
        private bool _showParticipationDebug;

        private void OnEnable()
        {
            _activityName = serializedObject.FindProperty("activityName");
            _activityId = serializedObject.FindProperty("activityId");
            _description = serializedObject.FindProperty("description");
            _playerParticipationProjectionMode =
                serializedObject.FindProperty("playerParticipationProjectionMode");
            _playerParticipationZeroParticipantPolicy =
                serializedObject.FindProperty("playerParticipationZeroParticipantPolicy");
            _playerParticipationExplicitSlotProfiles =
                serializedObject.FindProperty("playerParticipationExplicitSlotProfiles");
            _playerParticipationRequirementLevel =
                serializedObject.FindProperty("playerParticipationRequirementLevel");
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
            EditorGUILayout.PropertyField(_activityName, new GUIContent("Activity Name", "Designer-facing name used for presentation and diagnostics only."));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));
            if (_activityId == null || !ActivityId.IsValidText(_activityId.stringValue))
            {
                EditorGUILayout.HelpBox("Activity ID is missing or malformed. Generate a new canonical ID explicitly, or use Activity ID Migration for a coordinated project migration.", MessageType.Error);
                if (_activityId != null && GUILayout.Button("Generate ID"))
                {
                    _activityId.stringValue =
                        ImmersiveFrameworkEditorSettingsUtility.GenerateActivityIdText();
                }
            }

            DrawIdentityDebug();

            EditorGUILayout.Space(6);
            DrawPlayerParticipation();

            EditorGUILayout.Space(6);
            DrawActivityContentProfile();

            EditorGUILayout.Space(6);
            DrawVisualOperationPolicy();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Activity owns its Player Slot projection and readiness requirement. Runtime systems evaluate this explicit configuration without mutating the asset.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawPlayerParticipation()
        {
            EditorGUILayout.LabelField("Player Participation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Projection selects which Session Slots are evaluated. Requirement Level defines the progressive readiness those projected Slots must satisfy. Use No Slots + None for an Activity with no Players.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _playerParticipationProjectionMode,
                new GUIContent("Slot Projection"));

            ActivityParticipationProjectionMode projectionMode =
                _playerParticipationProjectionMode != null &&
                !_playerParticipationProjectionMode.hasMultipleDifferentValues
                    ? (ActivityParticipationProjectionMode)
                        _playerParticipationProjectionMode.intValue
                    : ActivityParticipationProjectionMode.NoSlots;

            if (projectionMode == ActivityParticipationProjectionMode.ExplicitSlots)
            {
                EditorGUILayout.PropertyField(
                    _playerParticipationExplicitSlotProfiles,
                    new GUIContent("Explicit Slots"),
                    true);
            }

            EditorGUILayout.PropertyField(
                _playerParticipationZeroParticipantPolicy,
                new GUIContent("Zero Participants"));
            EditorGUILayout.PropertyField(
                _playerParticipationRequirementLevel,
                new GUIContent("Requirement Level"));

            DrawParticipationSummary();

            _showParticipationDebug = EditorGUILayout.Foldout(
                _showParticipationDebug,
                "Advanced / Debug",
                true);
            if (_showParticipationDebug)
            {
                DrawParticipationDebug();
            }
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

            string activityId = _activityId != null
                ? _activityId.stringValue ?? string.Empty
                : string.Empty;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Activity ID",
                            "Stable functional identity. It is independent from Activity Name and asset filename."),
                        activityId);
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(activityId)))
                {
                    if (GUILayout.Button("Copy ID", GUILayout.Width(70)))
                    {
                        EditorGUIUtility.systemCopyBuffer = activityId;
                    }
                }
            }
        }

        private void DrawParticipationSummary()
        {
            ActivityParticipationProjectionMode projectionMode =
                (ActivityParticipationProjectionMode)
                _playerParticipationProjectionMode.intValue;
            PlayerParticipationRequirementLevel requirementLevel =
                (PlayerParticipationRequirementLevel)
                _playerParticipationRequirementLevel.intValue;
            EditorGUILayout.HelpBox(
                $"Projection: {projectionMode}\n" +
                $"Requirement Level: {requirementLevel}",
                MessageType.None);
        }

        private void DrawParticipationDebug()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Projection Mode",
                    ((ActivityParticipationProjectionMode)
                        _playerParticipationProjectionMode.intValue).ToString());
                EditorGUILayout.TextField(
                    "Zero Participant Policy",
                    ((ActivityParticipationZeroParticipantPolicy)
                        _playerParticipationZeroParticipantPolicy.intValue).ToString());
                EditorGUILayout.IntField(
                    "Explicit Slot Count",
                    _playerParticipationExplicitSlotProfiles != null
                        ? _playerParticipationExplicitSlotProfiles.arraySize
                        : 0);
                EditorGUILayout.TextField(
                    "Requirement Level",
                    ((PlayerParticipationRequirementLevel)
                        _playerParticipationRequirementLevel.intValue).ToString());
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
                FrameworkIdentityAuthoringValidator.ValidateProjectAssets(
                    FrameworkValidationMode.Standard));
            report.AddRange(
                ActivityParticipationProjectionAuthoringValidator.ValidateActivity(activity));

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
