using Immersive.Framework.ObjectReset;
using Immersive.Framework.ObjectReset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ObjectResetGameObjectActiveParticipant))]
    [CanEditMultipleObjects]
    internal sealed class ObjectResetGameObjectActiveParticipantEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetDeclaration;
        private SerializedProperty _participantId;
        private SerializedProperty _requiredness;
        private SerializedProperty _order;
        private SerializedProperty _displayName;
        private SerializedProperty _source;
        private SerializedProperty _reason;
        private SerializedProperty _targetGameObjectOverride;
        private SerializedProperty _baselineConfigured;
        private SerializedProperty _baselineActiveSelf;

        private void OnEnable()
        {
            _targetDeclaration = serializedObject.FindProperty("targetDeclaration");
            _participantId = serializedObject.FindProperty("participantId");
            _requiredness = serializedObject.FindProperty("requiredness");
            _order = serializedObject.FindProperty("order");
            _displayName = serializedObject.FindProperty("displayName");
            _source = serializedObject.FindProperty("source");
            _reason = serializedObject.FindProperty("reason");
            _targetGameObjectOverride = serializedObject.FindProperty("targetGameObjectOverride");
            _baselineConfigured = serializedObject.FindProperty("baselineConfigured");
            _baselineActiveSelf = serializedObject.FindProperty("baselineActiveSelf");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("GameObject Active Reset Participant", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Restores an authored GameObject.activeSelf baseline for one logical ObjectEntry. It does not reset activeInHierarchy, children, components, physics, animation or gameplay state.",
                MessageType.Info);

            DrawObjectEntrySection();
            DrawParticipantSection();
            DrawGameObjectSection();
            DrawBaselineSection();
            DrawAuthoringMessages();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawObjectEntrySection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Object Entry Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _targetDeclaration,
                new GUIContent(
                    "Target Declaration",
                    "Required. The Object Entry Declaration that provides ObjectEntryId, scope and owner identity for this reset participant."));
        }

        private void DrawParticipantSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Participant", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _participantId,
                new GUIContent(
                    "Participant Id",
                    "Optional. Leave empty to derive a stable participant id from ObjectEntryId and adapter type."));
            EditorGUILayout.PropertyField(
                _requiredness,
                new GUIContent(
                    "Requiredness",
                    "Required failures block Object Reset. Optional failures complete with warnings."));
            EditorGUILayout.PropertyField(
                _order,
                new GUIContent(
                    "Order",
                    "Deterministic execution order among participants for the same target."));
            EditorGUILayout.PropertyField(
                _displayName,
                new GUIContent("Display Name", "Optional diagnostics label."));
            EditorGUILayout.PropertyField(
                _source,
                new GUIContent("Source", "Optional diagnostics source."));
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent("Reason", "Optional diagnostics reason."));
        }

        private void DrawGameObjectSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("GameObject Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _targetGameObjectOverride,
                new GUIContent(
                    "Target GameObject Override",
                    "Optional. Leave empty to reset this component's own GameObject."));
        }

        private void DrawBaselineSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Authored Active State Baseline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _baselineConfigured,
                new GUIContent(
                    "Baseline Configured",
                    "Must be enabled for the participant to restore activeSelf."));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Current Active State Baseline"))
                {
                    CaptureCurrentActiveStateBaseline();
                }
            }

            using (new EditorGUI.DisabledScope(!_baselineConfigured.boolValue && !_baselineConfigured.hasMultipleDifferentValues))
            {
                EditorGUILayout.PropertyField(
                    _baselineActiveSelf,
                    new GUIContent(
                        "Baseline Active Self",
                        "The local GameObject.activeSelf value restored by this participant."));
            }
        }

        private void DrawAuthoringMessages()
        {
            EditorGUILayout.Space(6);

            if (_targetDeclaration != null
                && !_targetDeclaration.hasMultipleDifferentValues
                && _targetDeclaration.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Target Declaration. GameObject Active Reset must target a logical ObjectEntry, not a GameObject name or hierarchy path.",
                    MessageType.Error);
            }

            if (_baselineConfigured != null
                && !_baselineConfigured.hasMultipleDifferentValues
                && !_baselineConfigured.boolValue)
            {
                var requiredness = ResolveRequiredness();
                var messageType = requiredness == ObjectResetParticipantRequiredness.Required
                    ? MessageType.Error
                    : MessageType.Warning;
                var label = requiredness == ObjectResetParticipantRequiredness.Required
                    ? "Required participant without activeSelf baseline will block Object Reset."
                    : "Optional participant without activeSelf baseline will complete Object Reset with warnings.";
                EditorGUILayout.HelpBox(label, messageType);
            }

            EditorGUILayout.HelpBox(
                "Use this adapter for primitive activeSelf restore only. Prefer contextual reset components for Player, Actor, Timer, Pickup, Door, NPC or gameplay state.",
                MessageType.None);
        }

        private ObjectResetParticipantRequiredness ResolveRequiredness()
        {
            if (_requiredness == null || _requiredness.hasMultipleDifferentValues)
            {
                return ObjectResetParticipantRequiredness.Unknown;
            }

            return (ObjectResetParticipantRequiredness)_requiredness.intValue;
        }

        private void CaptureCurrentActiveStateBaseline()
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var participant = targets[i] as ObjectResetGameObjectActiveParticipant;
                if (participant == null)
                {
                    continue;
                }

                var participantObject = new SerializedObject(participant);
                var targetGameObjectOverride = participantObject.FindProperty("targetGameObjectOverride");
                var baselineConfigured = participantObject.FindProperty("baselineConfigured");
                var baselineActiveSelf = participantObject.FindProperty("baselineActiveSelf");

                var targetGameObject = targetGameObjectOverride != null && targetGameObjectOverride.objectReferenceValue != null
                    ? targetGameObjectOverride.objectReferenceValue as GameObject
                    : participant.gameObject;
                if (targetGameObject == null)
                {
                    continue;
                }

                Undo.RecordObject(participant, "Capture GameObject Active Reset Baseline");
                baselineConfigured.boolValue = true;
                baselineActiveSelf.boolValue = targetGameObject.activeSelf;
                participantObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(participant);
            }

            serializedObject.Update();
        }
    }
}
