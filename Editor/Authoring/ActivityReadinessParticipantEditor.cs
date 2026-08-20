using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Common;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ActivityReadinessParticipant))]
    internal sealed class ActivityReadinessParticipantEditor :
        UnityEditor.Editor
    {
        private static readonly GUIContent ParticipantIdLabel =
            new GUIContent(
                "Participant Id",
                "Stable identity for this preparation contribution. GameObject names and hierarchy paths are not used as fallback identity.");

        private static readonly GUIContent RequirednessLabel =
            new GUIContent(
                "Requiredness",
                "Required blocks Activity readiness until this participant completes. Optional remains diagnostic and does not block readiness.");

        private static readonly GUIContent OrderLabel =
            new GUIContent(
                "Order",
                "Execution order relative to other readiness participants in the same Activity scope.");

        private static readonly GUIContent PreparationStartedLabel =
            new GUIContent(
                "Preparation Started",
                "Invoked when the official ActivityFlow runtime starts this preparation. Call CompletePreparation or FailPreparation when the real work reaches a terminal result.");

        private static readonly GUIContent PreparationReleasedLabel =
            new GUIContent(
                "Preparation Released",
                "Invoked when the Activity exits. Cancel or release local preparation work here.");

        private SerializedProperty _participantId;
        private SerializedProperty _requiredness;
        private SerializedProperty _order;
        private SerializedProperty _preparationStarted;
        private SerializedProperty _preparationReleased;

        private bool _advanced;

        private void OnEnable()
        {
            _participantId =
                serializedObject.FindProperty("participantId");

            _requiredness =
                serializedObject.FindProperty("requiredness");

            _order =
                serializedObject.FindProperty("order");

            _preparationStarted =
                serializedObject.FindProperty("preparationStarted");

            _preparationReleased =
                serializedObject.FindProperty("preparationReleased");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawReadinessContribution();
            DrawPreparationCallbacks();
            DrawConfigurationIssues();

            serializedObject.ApplyModifiedProperties();

            DrawAdvanced();
        }

        private void DrawReadinessContribution()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Readiness Contribution");

            EditorGUILayout.PropertyField(
                _participantId,
                ParticipantIdLabel);

            EditorGUILayout.PropertyField(
                _requiredness,
                RequirednessLabel);

            EditorGUILayout.PropertyField(
                _order,
                OrderLabel);
        }

        private void DrawPreparationCallbacks()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Preparation");

            EditorGUILayout.PropertyField(
                _preparationStarted,
                PreparationStartedLabel);

            EditorGUILayout.PropertyField(
                _preparationReleased,
                PreparationReleasedLabel);
        }

        private void DrawConfigurationIssues()
        {
            if (_participantId == null ||
                string.IsNullOrWhiteSpace(
                    _participantId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Enter an explicit Participant Id.",
                    MessageType.Error);
            }

            if (_requiredness != null &&
                _requiredness.intValue ==
                    (int)ActivityContentExecutionRequiredness.Unknown)
            {
                EditorGUILayout.HelpBox(
                    "Requiredness must be Required or Optional.",
                    MessageType.Error);
            }
        }

        private void DrawAdvanced()
        {
            EditorGUILayout.Space(6f);

            _advanced =
                EditorGUILayout.Foldout(
                    _advanced,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows read-only runtime evidence from the official ActivityFlow runtime."),
                    true);

            if (!_advanced)
            {
                return;
            }

            ActivityReadinessParticipant participant =
                (ActivityReadinessParticipant)target;

            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "State",
                    participant.State.ToString());

                EditorGUILayout.IntField(
                    "Occurrence",
                    participant.Occurrence);

                EditorGUILayout.TextField(
                    "Last Reason",
                    string.IsNullOrWhiteSpace(
                        participant.LastReason)
                            ? "<none>"
                            : participant.LastReason);
            }

            EditorGUI.indentLevel--;
        }
    }
}
