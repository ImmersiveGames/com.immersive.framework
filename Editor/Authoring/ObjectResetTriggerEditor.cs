using Immersive.Framework.ObjectReset;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ObjectResetTrigger))]
    [CanEditMultipleObjects]
    internal sealed class ObjectResetTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetSubject;
        private SerializedProperty _reason;
        private SerializedProperty _allowNoParticipants;
        private SerializedProperty _stopOnFailure;

        private void OnEnable()
        {
            _targetSubject = serializedObject.FindProperty("targetSubject");
            _reason = serializedObject.FindProperty("reason");
            _allowNoParticipants = serializedObject.FindProperty("allowNoParticipants");
            _stopOnFailure = serializedObject.FindProperty("stopOnFailure");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Object Reset Trigger", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Requests reset for one ResetSubject through ResetExecutor. This trigger no longer resolves targets through ObjectEntryDeclaration or ObjectEntry snapshots.",
                MessageType.Info);

            if (_targetSubject != null)
            {
                EditorGUILayout.PropertyField(
                    _targetSubject,
                    new GUIContent(
                        "Target Subject",
                        "Assign a UnityResetSubjectAdapter or provide an explicit ResetSubjectId text."),
                    includeChildren: true);
            }

            if (targets.Length == 1)
            {
                var trigger = target as ObjectResetTrigger;
                if (trigger != null && string.IsNullOrWhiteSpace(trigger.ResolvedTargetSubjectId))
                {
                    EditorGUILayout.HelpBox(
                        "Assign a Target Subject adapter or provide a ResetSubjectId text. The adapter must be registered before the request is executed.",
                        MessageType.Error);
                }
            }

            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostics reason for this Object Reset request."));

            EditorGUILayout.PropertyField(
                _allowNoParticipants,
                new GUIContent(
                    "Allow No Participants",
                    "When enabled, a selected ResetSubject with no participants succeeds as SucceededNoParticipants."));

            EditorGUILayout.PropertyField(
                _stopOnFailure,
                new GUIContent(
                    "Stop On Failure",
                    "Stops execution after the first blocking failure inside this single-subject request."));

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "preview.12D: ObjectResetTrigger is now a request surface over ResetSubject + ResetExecutor. ObjectEntryDeclaration is no longer a reset target.",
                MessageType.Info);

            DrawRuntimeResult();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeResult()
        {
            if (!Application.isPlaying || targets.Length != 1)
            {
                return;
            }

            var trigger = target as ObjectResetTrigger;
            if (trigger == null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Result", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Phase", trigger.LastEventPhase.ToString());
            EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString());
            EditorGUILayout.LabelField("Last Result Status", trigger.LastResultStatus.ToString());

            if (!string.IsNullOrWhiteSpace(trigger.ResolvedTargetSubjectId))
            {
                EditorGUILayout.LabelField("Resolved Reset Subject Id", trigger.ResolvedTargetSubjectId);
            }

            if (!string.IsNullOrWhiteSpace(trigger.LastReason))
            {
                EditorGUILayout.LabelField("Last Reason", trigger.LastReason);
            }

            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                EditorGUILayout.HelpBox(trigger.LastMessage, ResolveRuntimeMessageType(trigger));
            }

            if (trigger.HasLastResult)
            {
                EditorGUILayout.LabelField("Participants", trigger.LastParticipantCount.ToString());
                EditorGUILayout.LabelField("Succeeded / Skipped / Failed", $"{trigger.LastSucceededParticipantCount} / {trigger.LastSkippedParticipantCount} / {trigger.LastFailedParticipantCount}");
                EditorGUILayout.LabelField("Blocking / Non-blocking Issues", $"{trigger.LastBlockingIssueCount} / {trigger.LastNonBlockingIssueCount}");
                EditorGUILayout.HelpBox(trigger.LastResultSummary, MessageType.None);
            }
        }

        private static MessageType ResolveRuntimeMessageType(ObjectResetTrigger trigger)
        {
            if (trigger.LastRequestFailed)
            {
                return MessageType.Error;
            }

            if (trigger.LastRequestIgnored || trigger.LastResultCompletedWithWarnings)
            {
                return MessageType.Warning;
            }

            return MessageType.Info;
        }
    }
}
