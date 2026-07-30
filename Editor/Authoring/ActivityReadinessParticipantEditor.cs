using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Common;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityReadinessParticipant))]
    internal sealed class ActivityReadinessParticipantEditor : UnityEditor.Editor
    {
        private SerializedProperty _participantId;
        private SerializedProperty _requiredness;
        private SerializedProperty _order;
        private SerializedProperty _preparationStarted;
        private SerializedProperty _preparationReleased;
        private bool _advanced;

        private void OnEnable()
        {
            _participantId = serializedObject.FindProperty("participantId");
            _requiredness = serializedObject.FindProperty("requiredness");
            _order = serializedObject.FindProperty("order");
            _preparationStarted = serializedObject.FindProperty("preparationStarted");
            _preparationReleased = serializedObject.FindProperty("preparationReleased");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var participant = (ActivityReadinessParticipant)target;
            FrameworkAuthoringInspectorGui.ProductHeader(
                "Activity Readiness Participant",
                "Starts real preparation when the official ActivityFlow runtime enters this Activity. Complete or fail it from your callback; this component never owns Activity readiness.");
            FrameworkAuthoringInspectorGui.Section("Identity and Readiness Contribution");
            EditorGUILayout.PropertyField(_participantId, new GUIContent("Participant Id", "Stable explicit identity. GameObject names and hierarchy paths are never used as identity."));
            EditorGUILayout.PropertyField(_requiredness, new GUIContent("Requiredness", "Required preparation blocks readiness until completed. Optional preparation stays diagnostic when it fails or remains pending."));
            EditorGUILayout.PropertyField(_order, new GUIContent("Order"));
            FrameworkAuthoringInspectorGui.Section("Preparation Callbacks");
            EditorGUILayout.PropertyField(_preparationStarted, new GUIContent("Preparation Started", "Invoke your real visual/gameplay preparation here, then call CompletePreparation or FailPreparation."));
            EditorGUILayout.PropertyField(_preparationReleased, new GUIContent("Preparation Released", "Release or cancel local preparation when this Activity exits."));
            if (string.IsNullOrWhiteSpace(_participantId.stringValue))
            {
                EditorGUILayout.HelpBox("Participant Id is required. No identity fallback is used.", MessageType.Error);
            }
            else if (_requiredness.enumValueIndex == (int)ActivityContentExecutionRequiredness.Unknown)
            {
                EditorGUILayout.HelpBox("Requiredness must be Required or Optional.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Ready for scoped discovery under the Route primary scene or this Activity's loaded content scenes.", MessageType.Info);
            }

            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced);
            if (_advanced)
            {
                EditorGUILayout.LabelField("State", participant.State.ToString());
                EditorGUILayout.LabelField("Occurrence", participant.Occurrence.ToString());
                EditorGUILayout.LabelField("Last Reason", string.IsNullOrWhiteSpace(participant.LastReason) ? "<none>" : participant.LastReason);
                EditorGUILayout.HelpBox("Late complete/fail calls are rejected after release and do not affect a later Activity occurrence.", MessageType.None);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
