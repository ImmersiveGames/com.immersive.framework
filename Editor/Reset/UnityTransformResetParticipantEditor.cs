using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityTransformResetParticipant))]
    internal sealed class UnityTransformResetParticipantEditor : UnityEditor.Editor
    {
        private SerializedProperty _participantId, _requiredness, _order, _displayName, _source, _reason;
        private SerializedProperty _targetTransform, _captureOnEnable, _resetPosition, _resetRotation, _resetScale, _baselinePosition, _baselineRotation, _baselineScale;
        private bool _showBaseline, _showAdvanced, _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _participantId = serializedObject.FindProperty("participantId"); _requiredness = serializedObject.FindProperty("requiredness"); _order = serializedObject.FindProperty("order"); _displayName = serializedObject.FindProperty("displayName"); _source = serializedObject.FindProperty("source"); _reason = serializedObject.FindProperty("reason");
            _targetTransform = serializedObject.FindProperty("target"); _captureOnEnable = serializedObject.FindProperty("captureBaselineOnEnable"); _resetPosition = serializedObject.FindProperty("resetPosition"); _resetRotation = serializedObject.FindProperty("resetRotation"); _resetScale = serializedObject.FindProperty("resetScale"); _baselinePosition = serializedObject.FindProperty("baselineLocalPosition"); _baselineRotation = serializedObject.FindProperty("baselineLocalEulerAngles"); _baselineScale = serializedObject.FindProperty("baselineLocalScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            UnityResetParticipantEditorUtility.DrawCommon(_displayName, _requiredness, _order);
            EditorGUILayout.Space(6f); EditorGUILayout.LabelField("Transform Reset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_targetTransform, new GUIContent("Target"));
            EditorGUILayout.PropertyField(_captureOnEnable, new GUIContent("Capture Baseline On Enable"));
            EditorGUILayout.LabelField("Restore", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_resetPosition, new GUIContent("Position")); EditorGUILayout.PropertyField(_resetRotation, new GUIContent("Rotation")); EditorGUILayout.PropertyField(_resetScale, new GUIContent("Scale"));
            EditorGUILayout.HelpBox(_captureOnEnable.boolValue ? "The baseline is captured from the target Transform when the Participant becomes enabled." : "The serialized baseline below is used until an explicit capture updates it.", MessageType.None);
            DrawBaseline(); DrawActions();
            UnityResetParticipantEditorUtility.DrawIdentityAndDiagnostics((UnityResetParticipantBehaviour)target, _participantId, _source, _reason, ref _showAdvanced, ref _showDiagnostics);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBaseline()
        {
            _showBaseline = EditorGUILayout.Foldout(_showBaseline, "Baseline", true);
            if (!_showBaseline) return;
            using (new EditorGUI.DisabledScope(true)) { EditorGUILayout.PropertyField(_baselinePosition); EditorGUILayout.PropertyField(_baselineRotation); EditorGUILayout.PropertyField(_baselineScale); }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6f); EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Capture Current Transform As Baseline")) CaptureCurrentBaseline();
                if (GUILayout.Button("Generate Missing ID")) { Undo.RecordObject(target, "Generate Reset Participant ID"); if (ResetAuthoringIdentityUtility.GenerateMissingParticipantId(_participantId)) { serializedObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(target); } }
                if (GUILayout.Button("Validate Participant")) ValidateTransformParticipant();
            }
            if (!string.IsNullOrWhiteSpace(_validationMessage)) EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
        }

        private void CaptureCurrentBaseline()
        {
            if (Application.isPlaying) { EditorUtility.DisplayDialog("Capture Baseline", "Baseline capture is available only in Edit Mode.", "OK"); return; }
            Transform selected = _targetTransform.objectReferenceValue as Transform ?? ((UnityTransformResetParticipant)target).transform;
            Undo.RecordObject(target, "Capture Transform Reset Baseline");
            if (_resetPosition.boolValue) _baselinePosition.vector3Value = selected.localPosition;
            if (_resetRotation.boolValue) _baselineRotation.vector3Value = selected.localEulerAngles;
            if (_resetScale.boolValue) _baselineScale.vector3Value = selected.localScale;
            serializedObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(target);
        }

        private void ValidateTransformParticipant()
        {
            if (!UnityResetParticipantEditorUtility.ValidateCommon(_participantId, _requiredness, out string issue)) { SetValidation(issue, MessageType.Error); return; }
            if (_targetTransform.objectReferenceValue == null) { SetValidation("Target is missing. The runtime falls back to this component Transform, but assign a Target to make authoring explicit.", MessageType.Warning); return; }
            if (!_resetPosition.boolValue && !_resetRotation.boolValue && !_resetScale.boolValue) { SetValidation("No restore channels are selected. Reset will be a no-op.", MessageType.Warning); return; }
            SetValidation("Authoring evidence is valid. Subject discovery and runtime registration are runtime-dependent.", MessageType.Info);
        }

        private void SetValidation(string message, MessageType type)
        {
            _validationMessage = message;
            _validationMessageType = type;
        }
    }
}
