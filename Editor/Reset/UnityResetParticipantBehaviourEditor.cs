using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityResetParticipantBehaviour), true)]
    internal sealed class UnityResetParticipantBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty _participantId, _requiredness, _order, _displayName, _source, _reason;
        private bool _showAdvanced, _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _participantId = serializedObject.FindProperty("participantId"); _requiredness = serializedObject.FindProperty("requiredness"); _order = serializedObject.FindProperty("order"); _displayName = serializedObject.FindProperty("displayName"); _source = serializedObject.FindProperty("source"); _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            UnityResetParticipantEditorUtility.DrawCommon(_displayName, _requiredness, _order);
            DrawActions();
            UnityResetParticipantEditorUtility.DrawIdentityAndDiagnostics((UnityResetParticipantBehaviour)target, _participantId, _source, _reason, ref _showAdvanced, ref _showDiagnostics);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6f); EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Generate Missing ID")) { Undo.RecordObject(target, "Generate Reset Participant ID"); if (ResetAuthoringIdentityUtility.GenerateMissingParticipantId(_participantId)) { serializedObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(target); } }
                if (GUILayout.Button("Validate Participant")) { bool valid = UnityResetParticipantEditorUtility.ValidateCommon(_participantId, _requiredness, out string issue); _validationMessage = valid ? "Authoring evidence is valid. Subject discovery and runtime registration are runtime-dependent." : issue; _validationMessageType = valid ? MessageType.Info : MessageType.Error; }
            }
            if (!string.IsNullOrWhiteSpace(_validationMessage)) EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
        }
    }
}
