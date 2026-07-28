using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityResetParticipantBehaviour), true)]
    internal sealed class UnityResetParticipantBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty participantId, requiredness, order, displayName, source, reason;
        private bool showAdvanced, showDiagnostics;
        private string validationMessage;
        private MessageType validationMessageType;

        private void OnEnable()
        {
            participantId = serializedObject.FindProperty("participantId"); requiredness = serializedObject.FindProperty("requiredness"); order = serializedObject.FindProperty("order"); displayName = serializedObject.FindProperty("displayName"); source = serializedObject.FindProperty("source"); reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            UnityResetParticipantEditorUtility.DrawCommon(displayName, requiredness, order);
            DrawActions();
            UnityResetParticipantEditorUtility.DrawIdentityAndDiagnostics((UnityResetParticipantBehaviour)target, participantId, source, reason, ref showAdvanced, ref showDiagnostics);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6f); EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Generate Missing ID")) { Undo.RecordObject(target, "Generate Reset Participant ID"); if (ResetAuthoringIdentityUtility.GenerateMissingParticipantId(participantId)) { serializedObject.ApplyModifiedPropertiesWithoutUndo(); ResetAuthoringIdentityUtility.RecordPrefabModification(target); } }
                if (GUILayout.Button("Validate Participant")) { bool valid = UnityResetParticipantEditorUtility.ValidateCommon(participantId, requiredness, out string issue); validationMessage = valid ? "Authoring evidence is valid. Subject discovery and runtime registration are runtime-dependent." : issue; validationMessageType = valid ? MessageType.Info : MessageType.Error; }
            }
            if (!string.IsNullOrWhiteSpace(validationMessage)) EditorGUILayout.HelpBox(validationMessage, validationMessageType);
        }
    }
}
