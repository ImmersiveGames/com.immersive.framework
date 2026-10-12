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
            "When enabled, the baseline below is captured from the target Transform every time the capability becomes enabled.");
        private SerializedProperty _requiredness, _order, _displayName;
        private SerializedProperty _targetTransform, _captureOnEnable, _resetPosition, _resetRotation, _resetScale, _baselinePosition, _baselineRotation, _baselineScale;
        private bool _showBaseline, _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _requiredness = serializedObject.FindProperty("requiredness");
            _order = serializedObject.FindProperty("order");
            _displayName = serializedObject.FindProperty("displayName");
            _targetTransform = serializedObject.FindProperty("target");
            _captureOnEnable = serializedObject.FindProperty("captureBaselineOnEnable");
            _resetPosition = serializedObject.FindProperty("resetPosition");
            _resetRotation = serializedObject.FindProperty("resetRotation");
            _resetScale = serializedObject.FindProperty("resetScale");
            _baselinePosition = serializedObject.FindProperty("baselineLocalPosition");
            _baselineRotation = serializedObject.FindProperty("baselineLocalEulerAngles");
            _baselineScale = serializedObject.FindProperty("baselineLocalScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var participant = (UnityTransformResetParticipant)target;
            UnityResetParticipantAuthoringContext context = UnityResetParticipantEditorUtility.ResolveContext(participant);

            DrawConfiguration();
            DrawBaseline();
            DrawConfigurationStatus(context);
            DrawActions(context);
            UnityResetParticipantEditorUtility.DrawDiagnostics(context, ref _showDiagnostics);
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
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_baselinePosition);
                EditorGUILayout.PropertyField(_baselineRotation);
                EditorGUILayout.PropertyField(_baselineScale);
            }
        }

        private void DrawConfigurationStatus(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            bool ready = UnityResetParticipantEditorUtility.ValidateCommon(context, _requiredness, out string issue) &&
                         (_resetPosition.boolValue || _resetRotation.boolValue || _resetScale.boolValue);
            FrameworkAuthoringInspectorGui.Status(ready ? "Ready" : "Incomplete");
            if (!ready)
            {
                EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(issue) ? "Select at least one Transform channel to restore." : issue, MessageType.Warning);
            }
        }

        private void DrawActions(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Capture Current Transform As Baseline")) CaptureCurrentBaseline();
                if (GUILayout.Button("Validate Capability")) ValidateTransformParticipant(context);
            }

            if (!string.IsNullOrWhiteSpace(_validationMessage))
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
            }
        }

        private void CaptureCurrentBaseline()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Capture Baseline", "Baseline capture is available only in Edit Mode.", "OK");
                return;
            }

            Transform selected = _targetTransform.objectReferenceValue as Transform ?? ((UnityTransformResetParticipant)target).transform;
            Undo.RecordObject(target, "Capture Transform Reset Baseline");
            if (_resetPosition.boolValue) _baselinePosition.vector3Value = selected.localPosition;
            if (_resetRotation.boolValue) _baselineRotation.vector3Value = selected.localEulerAngles;
            if (_resetScale.boolValue) _baselineScale.vector3Value = selected.localScale;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            ResetAuthoringIdentityUtility.RecordPrefabModification(target);
        }

        private void ValidateTransformParticipant(UnityResetParticipantAuthoringContext context)
        {
            if (!UnityResetParticipantEditorUtility.ValidateCommon(context, _requiredness, out string issue))
            {
                SetValidation(issue, MessageType.Error);
                return;
            }

            if (!_resetPosition.boolValue && !_resetRotation.boolValue && !_resetScale.boolValue)
            {
                SetValidation("No restore channels are selected. Reset will be a no-op.", MessageType.Warning);
                return;
            }

            SetValidation("Capability configuration is valid. Owner-aware runtime registration is transaction-dependent.", MessageType.Info);
        }

        private void SetValidation(string message, MessageType type)
        {
            _validationMessage = message;
            _validationMessageType = type;
        }
    }
}
