using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSessionStatus))]
    internal sealed class PlayerSessionStatusEditor : UnityEditor.Editor
    {
        private SerializedProperty _scope;
        private SerializedProperty _commandTrigger;
        private bool _hasValidation;
        private string _validationMessage;
        private MessageType _validationType;

        private void OnEnable()
        {
            _scope = serializedObject.FindProperty("scope");
            _commandTrigger = serializedObject.FindProperty("commandTrigger");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            var status = (PlayerSessionStatus)target;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Player Session Status",
                "Provides read-only access to the current scoped Player provisioning observation for presentation and diagnostics consumers.");
            FrameworkAuthoringInspectorGui.IntentSummary(
                "The Inspector contains only authoring relationships. Runtime lifecycle observability is emitted by the provisioning runtime through structured Console logs.");

            FrameworkAuthoringInspectorGui.Section("Lifecycle Scope");
            EditorGUILayout.PropertyField(
                _scope,
                new GUIContent(
                    "Scope",
                    "Explicit Route or Activity scope for this status. Framework Core injects observation access directly at runtime."));

            FrameworkAuthoringInspectorGui.Section("Last Command (Optional)");
            EditorGUILayout.PropertyField(
                _commandTrigger,
                new GUIContent(
                    "Command Trigger",
                    "Optional explicit command trigger declaring the same scope. It is the only Last Operation source."));

            DrawValidation(status);

            if (EditorGUI.EndChangeCheck())
            {
                _hasValidation = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidation(PlayerSessionStatus status)
        {
            FrameworkAuthoringInspectorGui.Section("Validation");
            if (GUILayout.Button("Validate"))
            {
                serializedObject.ApplyModifiedProperties();
                _hasValidation = true;
                if (status.TryValidateConfiguration(out _validationMessage))
                {
                    _validationType = MessageType.Info;
                    _validationMessage =
                        "Configuration is valid. Runtime scope availability is evaluated when the observation is read.";
                }
                else
                {
                    _validationType = MessageType.Error;
                }
            }

            if (_hasValidation)
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationType);
            }
            else if (status.TryValidateConfiguration(out string issue))
            {
                EditorGUILayout.HelpBox(
                    "The authored Player Session Status relationship is structurally valid.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }
    }
}
