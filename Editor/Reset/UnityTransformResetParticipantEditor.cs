using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityTransformResetParticipant))]
    internal sealed class UnityTransformResetParticipantEditor : UnityEditor.Editor
    {
        private SerializedProperty participantId, requiredness, order, displayName, source, reason;
        private SerializedProperty targetTransform, captureOnEnable, resetPosition, resetRotation, resetScale, baselinePosition, baselineRotation, baselineScale;
        private bool showBaseline, showAdvanced, showDiagnostics;
        private string validationMessage;
        private MessageType validationMessageType;

        private void OnEnable()
        {
            participantId = serializedObject.FindProperty("participantId"); requiredness = serializedObject.FindProperty("requiredness"); order = serializedObject.FindProperty("order"); displayName = serializedObject.FindProperty("displayName"); source = serializedObject.FindProperty("source"); reason = serializedObject.FindProperty("reason");
            targetTransform = serializedObject.FindProperty("target"); captureOnEnable = serializedObject.FindProperty("captureBaselineOnEnable"); resetPosition = serializedObject.FindProperty("resetPosition"); resetRotation = serializedObject.FindProperty("resetRotation"); resetScale = serializedObject.FindProperty("resetScale"); baselinePosition = serializedObject.FindProperty("baselineLocalPosition"); baselineRotation = serializedObject.FindProperty("baselineLocalEulerAngles"); baselineScale = serializedObject.FindProperty("baselineLocalScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            UnityResetParticipantEditorUtility.DrawCommon(displayName, requiredness, order);
            EditorGUILayout.Space(6f); EditorGUILayout.LabelField("Transform Reset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetTransform, new GUIContent("Target"));
            EditorGUILayout.PropertyField(captureOnEnable, new GUIContent("Capture Baseline On Enable"));
            EditorGUILayout.LabelField("Restore", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(resetPosition, new GUIContent("Position")); EditorGUILayout.PropertyField(resetRotation, new GUIContent("Rotation")); EditorGUILayout.PropertyField(resetScale, new GUIContent("Scale"));
            EditorGUILayout.HelpBox(captureOnEnable.boolValue ? "The baseline is captured from the target Transform when the Participant becomes enabled." : "The serialized baseline below is used until an explicit capture updates it.", MessageType.None);
            DrawBaseline(); DrawActions();
            UnityResetParticipantEditorUtility.DrawIdentityAndDiagnostics((UnityResetParticipantBehaviour)target, participantId, source, reason, ref showAdvanced, ref showDiagnostics);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBaseline()
        {
            showBaseline = EditorGUILayout.Foldout(showBaseline, "Baseline", true);
            if (!showBaseline) return;
            using (new EditorGUI.DisabledScope(true)) { EditorGUILayout.PropertyField(baselinePosition); EditorGUILayout.PropertyField(baselineRotation); EditorGUILayout.PropertyField(baselineScale); }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6f); EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Capture Current Transform As Baseline")) CaptureCurrentBaseline();
                if (GUILayout.Button("Generate Missing ID")) { Undo.RecordObject(target, "Generate Reset Participant ID"); if (ResetAuthoringIdentityUtility.GenerateMissingParticipantId(participantId)) { serializedObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(target); } }
                if (GUILayout.Button("Validate Participant")) ValidateTransformParticipant();
            }
            if (!string.IsNullOrWhiteSpace(validationMessage)) EditorGUILayout.HelpBox(validationMessage, validationMessageType);
        }

        private void CaptureCurrentBaseline()
        {
            if (Application.isPlaying) { EditorUtility.DisplayDialog("Capture Baseline", "Baseline capture is available only in Edit Mode.", "OK"); return; }
            Transform selected = targetTransform.objectReferenceValue as Transform ?? ((UnityTransformResetParticipant)target).transform;
            Undo.RecordObject(target, "Capture Transform Reset Baseline");
            if (resetPosition.boolValue) baselinePosition.vector3Value = selected.localPosition;
            if (resetRotation.boolValue) baselineRotation.vector3Value = selected.localEulerAngles;
            if (resetScale.boolValue) baselineScale.vector3Value = selected.localScale;
            serializedObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(target);
        }

        private void ValidateTransformParticipant()
        {
            if (!UnityResetParticipantEditorUtility.ValidateCommon(participantId, requiredness, out string issue)) { SetValidation(issue, MessageType.Error); return; }
            if (targetTransform.objectReferenceValue == null) { SetValidation("Target is missing. The runtime falls back to this component Transform, but assign a Target to make authoring explicit.", MessageType.Warning); return; }
            if (!resetPosition.boolValue && !resetRotation.boolValue && !resetScale.boolValue) { SetValidation("No restore channels are selected. Reset will be a no-op.", MessageType.Warning); return; }
            SetValidation("Authoring evidence is valid. Subject discovery and runtime registration are runtime-dependent.", MessageType.Info);
        }

        private void SetValidation(string message, MessageType type)
        {
            validationMessage = message;
            validationMessageType = type;
        }
    }
}
