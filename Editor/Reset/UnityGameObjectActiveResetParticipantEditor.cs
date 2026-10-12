using Immersive.Framework.Editor.Common;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityGameObjectActiveResetParticipant))]
    internal sealed class UnityGameObjectActiveResetParticipantEditor : UnityEditor.Editor
    {
        private SerializedProperty _requiredness, _order, _displayName;
        private SerializedProperty _target, _captureOnEnable, _baselineActive;
        private bool _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _requiredness = serializedObject.FindProperty("requiredness");
            _order = serializedObject.FindProperty("order");
            _displayName = serializedObject.FindProperty("displayName");
            _target = serializedObject.FindProperty("target");
            _captureOnEnable = serializedObject.FindProperty("captureBaselineOnEnable");
            _baselineActive = serializedObject.FindProperty("baselineActive");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var participant = (UnityGameObjectActiveResetParticipant)target;
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
            EditorGUILayout.PropertyField(_target, new GUIContent(
                "Target",
                "GameObject whose active state is restored. Falls back to this component's GameObject when empty."));
            EditorGUILayout.PropertyField(_captureOnEnable, new GUIContent(
                "Capture Baseline On Enable",
                "When enabled, the target's activeSelf state is captured whenever this capability becomes enabled."));
            UnityResetParticipantEditorUtility.DrawExecution(_displayName, _requiredness, _order);
        }

        private void DrawBaseline()
        {
            FrameworkAuthoringInspectorGui.Section("Baseline");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_baselineActive, new GUIContent("Active"));
            }
        }

        private void DrawConfigurationStatus(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            bool ready = UnityResetParticipantEditorUtility.ValidateCommon(context, _requiredness, out string issue);
            FrameworkAuthoringInspectorGui.Status(ready ? "Ready" : "Incomplete");
            if (!ready)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
            else if (_target.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Target is empty; Capture Baseline and Reset use this component's GameObject.", MessageType.Info);
            }
        }

        private void DrawActions(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("Capture Current Active State As Baseline")) CaptureCurrentActiveState();
                }

                if (GUILayout.Button("Validate Capability")) ValidateParticipant(context);
            }

            if (!string.IsNullOrWhiteSpace(_validationMessage))
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Manual baseline capture is available only in Edit Mode.", MessageType.Info);
            }
        }

        private void CaptureCurrentActiveState()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Capture Baseline", "Baseline capture is available only in Edit Mode.", "OK");
                return;
            }

            var participant = (UnityGameObjectActiveResetParticipant)target;
            GameObject resolvedTarget = _target.objectReferenceValue as GameObject ?? participant.gameObject;
            Undo.RecordObject(target, "Capture Active Reset Baseline");
            _baselineActive.boolValue = resolvedTarget.activeSelf;
            serializedObject.ApplyModifiedProperties();
            ResetAuthoringIdentityUtility.RecordPrefabModification(target);
        }

        private void ValidateParticipant(UnityResetParticipantAuthoringContext context)
        {
            if (!UnityResetParticipantEditorUtility.ValidateCommon(context, _requiredness, out string issue))
            {
                SetValidation(issue, MessageType.Error);
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
