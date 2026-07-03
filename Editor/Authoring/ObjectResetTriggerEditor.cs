using Immersive.Framework.ObjectReset;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ObjectResetTrigger))]
    [CanEditMultipleObjects]
    internal sealed class ObjectResetTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetDeclaration;
        private SerializedProperty _objectEntryId;
        private SerializedProperty _reason;
        private SerializedProperty _allowNoParticipants;

        private void OnEnable()
        {
            _targetDeclaration = serializedObject.FindProperty("targetDeclaration");
            _objectEntryId = serializedObject.FindProperty("objectEntryId");
            _reason = serializedObject.FindProperty("reason");
            _allowNoParticipants = serializedObject.FindProperty("allowNoParticipants");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Object Reset Trigger", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Requests Object Reset for one logical ObjectEntry through the framework runtime. The target owner is resolved from the current Object Entry snapshot; this trigger does not reset Transform, Rigidbody, Animator, GameObject activation, Player, Actor, prefab, pool, save or scene reload state.",
                MessageType.Info);

            EditorGUILayout.PropertyField(
                _targetDeclaration,
                new GUIContent(
                    "Target Declaration",
                    "Optional Object Entry Declaration to use as the reset target. This is the preferred path for scene-authored objects."));

            var hasDeclaration = _targetDeclaration != null
                && !_targetDeclaration.hasMultipleDifferentValues
                && _targetDeclaration.objectReferenceValue != null;

            using (new EditorGUI.DisabledScope(hasDeclaration))
            {
                EditorGUILayout.PropertyField(
                    _objectEntryId,
                    new GUIContent(
                        "Object Entry Id",
                        "Manual Object Entry Id when Target Declaration is empty. The id must exist in the current Object Entry snapshot."));
            }

            if (hasDeclaration)
            {
                EditorGUILayout.HelpBox(
                    "Target Declaration is assigned. The trigger will use its Object Entry Id and resolve scope/owner from the current Object Entry snapshot.",
                    MessageType.Info);
            }
            else if (_objectEntryId != null && !_objectEntryId.hasMultipleDifferentValues && string.IsNullOrWhiteSpace(_objectEntryId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Assign a Target Declaration or provide an Object Entry Id that exists in the current Object Entry snapshot.",
                    MessageType.Error);
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
                    "When enabled, a valid target with no Object Reset participants reports SucceededNoParticipants. This is expected until Unity reset adapters exist."));

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "F14 Object Reset is logical foundation only. Physical reset of Transform, Rigidbody, Animator and GameObject state belongs to F15 adapters.",
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

            var resolvedTarget = trigger.ResolvedAuthoringObjectEntryId;
            if (!string.IsNullOrWhiteSpace(resolvedTarget))
            {
                EditorGUILayout.LabelField("Resolved Object Entry Id", resolvedTarget);
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
