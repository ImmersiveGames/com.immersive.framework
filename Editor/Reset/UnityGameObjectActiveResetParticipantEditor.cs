using Immersive.Framework.Editor.Common;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityGameObjectActiveResetParticipant))]
    internal sealed class UnityGameObjectActiveResetParticipantEditor : UnityEditor.Editor
    {
        private SerializedProperty _participantId;
        private SerializedProperty _requiredness;
        private SerializedProperty _order;
        private SerializedProperty _displayName;
        private SerializedProperty _source;
        private SerializedProperty _reason;
        private SerializedProperty _target;
        private SerializedProperty _captureOnEnable;
        private SerializedProperty _baselineActive;
        private bool _showAdvanced;
        private bool _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _participantId = serializedObject.FindProperty("participantId");
            _requiredness = serializedObject.FindProperty("requiredness");
            _order = serializedObject.FindProperty("order");
            _displayName = serializedObject.FindProperty("displayName");
            _source = serializedObject.FindProperty("source");
            _reason = serializedObject.FindProperty("reason");
            _target = serializedObject.FindProperty("target");
            _captureOnEnable = serializedObject.FindProperty("captureBaselineOnEnable");
            _baselineActive = serializedObject.FindProperty("baselineActive");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            UnityGameObjectActiveResetParticipant participant = (UnityGameObjectActiveResetParticipant)target;
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
            EditorGUILayout.PropertyField(_target, new GUIContent(
                "Target",
                "GameObject whose active state is restored. Falls back to this component's GameObject when empty."));
            EditorGUILayout.PropertyField(_captureOnEnable, new GUIContent(
                "Capture Baseline On Enable",
                "When enabled, the target's activeSelf state is captured whenever this participant becomes enabled."));
            UnityResetParticipantEditorUtility.DrawExecution(_displayName, _requiredness, _order);
        }

        private void DrawBaseline()
        {
            FrameworkAuthoringInspectorGui.Section("Baseline");
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(_baselineActive, new GUIContent("Active"));
        }

        private void DrawConfigurationStatus(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            bool ready = UnityResetParticipantEditorUtility.ValidateCommon(
                context, _participantId, _requiredness, out string issue);
            FrameworkAuthoringInspectorGui.Status(ready ? "Ready" : "Incomplete");
            if (!ready)
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            else if (_target.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Target is empty; Capture Baseline and Reset use this component's GameObject.", MessageType.Info);
        }

        private void DrawActions(UnityResetParticipantAuthoringContext context)
        {
            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("Capture Current Active State As Baseline"))
                        CaptureCurrentActiveState();
                }

                if (context.UsesAdapter && GUILayout.Button("Generate Missing ID"))
                {
                    Undo.RecordObject(target, "Generate Reset Participant ID");
                    if (ResetAuthoringIdentityUtility.GenerateMissingParticipantId(_participantId))
                    {
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        ResetAuthoringIdentityUtility.RecordPrefabModification(target);
                    }
                }

                if (GUILayout.Button("Validate Participant"))
                    ValidateParticipant(context);
            }

            if (!string.IsNullOrWhiteSpace(_validationMessage))
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
            if (Application.isPlaying)
                EditorGUILayout.HelpBox("Manual baseline capture is available only in Edit Mode.", MessageType.Info);
        }

        private void CaptureCurrentActiveState()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Capture Baseline", "Baseline capture is available only in Edit Mode.", "OK");
                return;
            }

            UnityGameObjectActiveResetParticipant participant = (UnityGameObjectActiveResetParticipant)target;
            GameObject resolvedTarget = _target.objectReferenceValue as GameObject ?? participant.gameObject;
            Undo.RecordObject(target, "Capture Active Reset Baseline");
            _baselineActive.boolValue = resolvedTarget.activeSelf;
            serializedObject.ApplyModifiedProperties();
            ResetAuthoringIdentityUtility.RecordPrefabModification(target);
        }

        private void ValidateParticipant(UnityResetParticipantAuthoringContext context)
        {
            if (!UnityResetParticipantEditorUtility.ValidateCommon(
                    context, _participantId, _requiredness, out string issue))
            {
                SetValidation(issue, MessageType.Error);
                return;
            }

            SetValidation(
                "Authoring evidence is valid. Subject discovery and runtime registration are runtime-dependent.",
                MessageType.Info);
        }

        private void SetValidation(string message, MessageType type)
        {
            _validationMessage = message;
            _validationMessageType = type;
        }
    }
}
