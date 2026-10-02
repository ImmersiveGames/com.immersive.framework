using Immersive.Framework.Editor.Common;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityTransformResetParticipant))]
    internal sealed class UnityTransformResetParticipantEditor : UnityEditor.Editor
    {
        private static readonly GUIContent TargetLabel = new GUIContent(
            "Target",
            "Transform to restore. Falls back to this component's own Transform when empty.");
        private static readonly GUIContent CaptureOnEnableLabel = new GUIContent(
            "Capture Baseline On Enable",
            "When enabled, the baseline below is captured from the target Transform every time the Participant becomes enabled.");
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
            UnityResetParticipantBehaviour participant = (UnityResetParticipantBehaviour)target;
            UnityResetParticipantAuthoringContext context = UnityResetParticipantEditorUtility.ResolveContext(participant);

            DrawConfiguration();
            DrawBaseline();
            DrawConfigurationStatus(context);
            DrawActions(context);
            UnityResetParticipantEditorUtility.DrawIdentityAndDiagnostics(
                context,
                _participantId,
                _source,
                _reason,
                _requiredness,
                ref _showAdvanced,
                ref _showDiagnostics);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section("Configuration");
            EditorGUILayout.PropertyField(_targetTransform, TargetLabel);
            EditorGUILayout.PropertyField(_captureOnEnable, CaptureOnEnableLabel);

            EditorGUILayout.LabelField("Restore", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_resetPosition, new GUIContent("Position"));
            EditorGUILayout.PropertyField(_resetRotation, new GUIContent("Rotation"));
            EditorGUILayout.PropertyField(_resetScale, new GUIContent("Scale"));

            UnityResetParticipantEditorUtility.DrawExecution(_displayName, _requiredness, _order);

            if (!_resetPosition.boolValue && !_resetRotation.boolValue && !_resetScale.boolValue)
            {
                EditorGUILayout.HelpBox("No Restore channel is selected. Reset will be a no-op.", MessageType.Warning);
            }
        }

        private void DrawBaseline()
        {
            _showBaseline = EditorGUILayout.Foldout(_showBaseline, "Baseline", true);
            if (!_showBaseline) return;
            using (new EditorGUI.DisabledScope(true)) { EditorGUILayout.PropertyField(_baselinePosition); EditorGUILayout.PropertyField(_baselineRotation); EditorGUILayout.PropertyField(_baselineScale); }
        }

        private void DrawConfigurationStatus(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            bool ready = UnityResetParticipantEditorUtility.ValidateCommon(context, _participantId, _requiredness, out string issue) &&
                (_resetPosition.boolValue || _resetRotation.boolValue || _resetScale.boolValue);
            FrameworkAuthoringInspectorGui.Status(ready ? "Ready" : "Incomplete");
            if (!ready && !string.IsNullOrWhiteSpace(issue))
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
        }

        private void DrawActions(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Capture Current Transform As Baseline")) CaptureCurrentBaseline();
                if (context.UsesAdapter && GUILayout.Button("Generate Missing ID"))
                {
                    Undo.RecordObject(target, "Generate Reset Participant ID");
                    if (ResetAuthoringIdentityUtility.GenerateMissingParticipantId(_participantId))
                    {
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        ResetAuthoringIdentityUtility.RecordPrefabModification(target);
                    }
                }
                if (GUILayout.Button("Validate Participant")) ValidateTransformParticipant(context);
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

        private void ValidateTransformParticipant(UnityResetParticipantAuthoringContext context)
        {
            if (!UnityResetParticipantEditorUtility.ValidateCommon(context, _participantId, _requiredness, out string issue)) { SetValidation(issue, MessageType.Error); return; }
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
