using Immersive.Framework.Editor.Common;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(UnityResetParticipantBehaviour), true)]
    internal sealed class UnityResetParticipantBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty _requiredness;
        private SerializedProperty _order;
        private SerializedProperty _displayName;
        private bool _showDiagnostics;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _requiredness = serializedObject.FindProperty("requiredness");
            _order = serializedObject.FindProperty("order");
            _displayName = serializedObject.FindProperty("displayName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            UnityResetParticipantBehaviour participant = (UnityResetParticipantBehaviour)target;
            UnityResetParticipantAuthoringContext context = UnityResetParticipantEditorUtility.ResolveContext(participant);

            UnityResetParticipantEditorUtility.DrawExecution(_displayName, _requiredness, _order);
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            bool ready = UnityResetParticipantEditorUtility.ValidateCommon(context, _requiredness, out string issue);
            FrameworkAuthoringInspectorGui.Status(ready ? "Ready" : "Incomplete");
            if (!ready)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }

            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (GUILayout.Button("Validate Capability"))
                {
                    _validationMessage = ready
                        ? "Capability is inside a Resettable boundary. Runtime registration is transaction-dependent."
                        : issue;
                    _validationMessageType = ready ? MessageType.Info : MessageType.Error;
                }
            }

            if (!string.IsNullOrWhiteSpace(_validationMessage))
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
            }

            UnityResetParticipantEditorUtility.DrawDiagnostics(context, ref _showDiagnostics);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
