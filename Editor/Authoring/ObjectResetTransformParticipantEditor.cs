using Immersive.Framework.ObjectReset;
using Immersive.Framework.ObjectReset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ObjectResetTransformParticipant))]
    [CanEditMultipleObjects]
    internal sealed class ObjectResetTransformParticipantEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetDeclaration;
        private SerializedProperty _participantId;
        private SerializedProperty _requiredness;
        private SerializedProperty _order;
        private SerializedProperty _displayName;
        private SerializedProperty _source;
        private SerializedProperty _reason;
        private SerializedProperty _targetTransformOverride;
        private SerializedProperty _baselineConfigured;
        private SerializedProperty _resetLocalPosition;
        private SerializedProperty _resetLocalRotation;
        private SerializedProperty _resetLocalScale;
        private SerializedProperty _baselineLocalPosition;
        private SerializedProperty _baselineLocalEulerAngles;
        private SerializedProperty _baselineLocalScale;

        private void OnEnable()
        {
            _targetDeclaration = serializedObject.FindProperty("targetDeclaration");
            _participantId = serializedObject.FindProperty("participantId");
            _requiredness = serializedObject.FindProperty("requiredness");
            _order = serializedObject.FindProperty("order");
            _displayName = serializedObject.FindProperty("displayName");
            _source = serializedObject.FindProperty("source");
            _reason = serializedObject.FindProperty("reason");
            _targetTransformOverride = serializedObject.FindProperty("targetTransformOverride");
            _baselineConfigured = serializedObject.FindProperty("baselineConfigured");
            _resetLocalPosition = serializedObject.FindProperty("resetLocalPosition");
            _resetLocalRotation = serializedObject.FindProperty("resetLocalRotation");
            _resetLocalScale = serializedObject.FindProperty("resetLocalScale");
            _baselineLocalPosition = serializedObject.FindProperty("baselineLocalPosition");
            _baselineLocalEulerAngles = serializedObject.FindProperty("baselineLocalEulerAngles");
            _baselineLocalScale = serializedObject.FindProperty("baselineLocalScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Transform Reset Participant", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Restores an authored local Transform baseline for one logical ObjectEntry. Identity comes from Object Entry Declaration; GameObject name, hierarchy path, scene path and InstanceID are not used as functional identity.",
                MessageType.Info);

            DrawObjectEntrySection();
            DrawParticipantSection();
            DrawTransformSection();
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
                new GUIContent(
                    "Display Name",
                    "Optional diagnostics label."));
            EditorGUILayout.PropertyField(
                _source,
                new GUIContent(
                    "Source",
                    "Optional diagnostics source."));
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostics reason."));
        }

        private void DrawTransformSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Transform Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _targetTransformOverride,
                new GUIContent(
                    "Target Transform Override",
                    "Optional. Leave empty to reset this component's own Transform."));
        }

        private void DrawBaselineSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Authored Local Baseline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _baselineConfigured,
                new GUIContent(
                    "Baseline Configured",
                    "Must be enabled for the participant to restore a Transform baseline."));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Current Local Transform Baseline"))
                {
                    CaptureCurrentLocalTransformBaseline();
                }
            }

            EditorGUILayout.PropertyField(
                _resetLocalPosition,
                new GUIContent(
                    "Reset Local Position",
                    "Restore localPosition from the authored baseline."));
            EditorGUILayout.PropertyField(
                _resetLocalRotation,
                new GUIContent(
                    "Reset Local Rotation",
                    "Restore localRotation from the authored baseline Euler angles."));
            EditorGUILayout.PropertyField(
                _resetLocalScale,
                new GUIContent(
                    "Reset Local Scale",
                    "Restore localScale from the authored baseline."));

            using (new EditorGUI.DisabledScope(!_baselineConfigured.boolValue && !_baselineConfigured.hasMultipleDifferentValues))
            {
                EditorGUILayout.PropertyField(
                    _baselineLocalPosition,
                    new GUIContent("Baseline Local Position"));
                EditorGUILayout.PropertyField(
                    _baselineLocalEulerAngles,
                    new GUIContent("Baseline Local Euler Angles"));
                EditorGUILayout.PropertyField(
                    _baselineLocalScale,
                    new GUIContent("Baseline Local Scale"));
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
                    "Assign a Target Declaration. Transform Reset must target a logical ObjectEntry, not a GameObject name or hierarchy path.",
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
                    ? "Required participant without baseline will block Object Reset."
                    : "Optional participant without baseline will complete Object Reset with warnings.";
                EditorGUILayout.HelpBox(label, messageType);
            }

            EditorGUILayout.HelpBox(
                "Scripts live in Packages/com.immersive.framework. Component instances live on scene objects or prefabs, next to ObjectEntryDeclaration. Register the components through Object Reset Unity Participant Source.",
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

        private void CaptureCurrentLocalTransformBaseline()
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var participant = targets[i] as ObjectResetTransformParticipant;
                if (participant == null)
                {
                    continue;
                }

                var participantObject = new SerializedObject(participant);
                var targetTransformOverride = participantObject.FindProperty("targetTransformOverride");
                var baselineConfigured = participantObject.FindProperty("baselineConfigured");
                var baselineLocalPosition = participantObject.FindProperty("baselineLocalPosition");
                var baselineLocalEulerAngles = participantObject.FindProperty("baselineLocalEulerAngles");
                var baselineLocalScale = participantObject.FindProperty("baselineLocalScale");

                var targetTransform = targetTransformOverride != null && targetTransformOverride.objectReferenceValue != null
                    ? targetTransformOverride.objectReferenceValue as Transform
                    : participant.transform;
                if (targetTransform == null)
                {
                    continue;
                }

                Undo.RecordObject(participant, "Capture Transform Reset Baseline");
                baselineConfigured.boolValue = true;
                baselineLocalPosition.vector3Value = targetTransform.localPosition;
                baselineLocalEulerAngles.vector3Value = targetTransform.localEulerAngles;
                baselineLocalScale.vector3Value = targetTransform.localScale;
                participantObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(participant);
            }

            serializedObject.Update();
        }
    }
}
