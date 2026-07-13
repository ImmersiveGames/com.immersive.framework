using Immersive.Framework.Editor.Editor.Validation;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(ActivityParticipationProjectionProfile))]
    internal sealed class ActivityParticipationProjectionProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _projectionMode;
        private SerializedProperty _zeroParticipantPolicy;
        private SerializedProperty _explicitSlotProfiles;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _projectionMode = serializedObject.FindProperty("projectionMode");
            _zeroParticipantPolicy = serializedObject.FindProperty("zeroParticipantPolicy");
            _explicitSlotProfiles = serializedObject.FindProperty("explicitSlotProfiles");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity Participation Projection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Selects which Session Player Slots an Activity evaluates. Readiness requirements remain in a separate Player Participation Requirements Profile.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Designer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Projection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_projectionMode, new GUIContent("Slot Selection"));

            ActivityParticipationProjectionMode mode =
                _projectionMode != null && !_projectionMode.hasMultipleDifferentValues
                    ? (ActivityParticipationProjectionMode)_projectionMode.intValue
                    : ActivityParticipationProjectionMode.NoSlots;

            switch (mode)
            {
                case ActivityParticipationProjectionMode.NoSlots:
                    EditorGUILayout.HelpBox(
                        "Projects no Player Slots. Zero Participant Policy must be Allowed. Pair with an explicit None Requirements Profile for a no-Player Activity.",
                        MessageType.Info);
                    EditorGUILayout.PropertyField(
                        _zeroParticipantPolicy,
                        new GUIContent("Zero Participants"));
                    break;

                case ActivityParticipationProjectionMode.AllJoinedSlots:
                    EditorGUILayout.HelpBox(
                        "Projects every currently Joined Session Slot in canonical Game/Application configured order. Zero-player behavior must be selected explicitly.",
                        MessageType.Info);
                    EditorGUILayout.PropertyField(
                        _zeroParticipantPolicy,
                        new GUIContent("Zero Participants"));
                    break;

                case ActivityParticipationProjectionMode.ExplicitSlots:
                    EditorGUILayout.HelpBox(
                        "Projects the exact ordered PlayerSlotProfile references below. The runtime must not infer Slot identity from PlayerInput.playerIndex. Zero Participant Policy must be Rejected.",
                        MessageType.Info);
                    EditorGUILayout.PropertyField(
                        _explicitSlotProfiles,
                        new GUIContent("Explicit Slots"),
                        true);
                    EditorGUILayout.PropertyField(
                        _zeroParticipantPolicy,
                        new GUIContent("Zero Participants"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            _showAdvanced = EditorGUILayout.Foldout(
                _showAdvanced,
                "Advanced / Debug",
                true);
            if (_showAdvanced)
            {
                DrawAdvancedSummary((ActivityParticipationProjectionProfile)target);
            }

            EditorGUILayout.Space(6);
            DrawValidation();
        }

        private static void DrawAdvancedSummary(ActivityParticipationProjectionProfile profile)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Asset Path", AssetDatabase.GetAssetPath(profile));
                EditorGUILayout.TextField("Projection Mode", profile.ProjectionMode.ToString());
                EditorGUILayout.TextField("Zero Policy", profile.ZeroParticipantPolicy.ToString());
                EditorGUILayout.IntField("Explicit Slot Count", profile.ExplicitSlotProfiles.Count);
            }
        }

        private void DrawValidation()
        {
            FrameworkAuthoringValidationReport report =
                ActivityParticipationProjectionAuthoringValidator.ValidateProjectionProfile(
                    (ActivityParticipationProjectionProfile)target);

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}
