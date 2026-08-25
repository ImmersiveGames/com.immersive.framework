using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSessionStatus))]
    internal sealed class PlayerProvisioningStatusBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _consumerAccessBinding;
        private SerializedProperty _commandTrigger;
        private bool _hasValidation;
        private string _validationMessage;
        private MessageType _validationType;

        private void OnEnable()
        {
            _consumerAccessBinding = serializedObject.FindProperty(
                "consumerAccessBinding");
            _commandTrigger = serializedObject.FindProperty("commandTrigger");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            var binding = (PlayerSessionStatus)target;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Player Session Status",
                "Provides read-only access to the current scoped Player provisioning observation for presentation and diagnostics consumers.");
            FrameworkAuthoringInspectorGui.IntentSummary(
                "The Inspector contains only authoring relationships. Runtime lifecycle observability is emitted by the provisioning runtime through structured Console logs.");

            FrameworkAuthoringInspectorGui.Section("Observation Source");
            EditorGUILayout.PropertyField(
                _consumerAccessBinding,
                new GUIContent(
                    "Consumer Access Binding",
                    "Explicit Route or Activity scoped binding used to read the public observation."));

            FrameworkAuthoringInspectorGui.Section("Last Command (Optional)");
            EditorGUILayout.PropertyField(
                _commandTrigger,
                new GUIContent(
                    "Command Trigger",
                    "Optional explicit command trigger using the same Consumer Access Binding. It is the only Last Operation source."));

            DrawValidation(binding);

            if (EditorGUI.EndChangeCheck())
            {
                _hasValidation = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidation(PlayerSessionStatus binding)
        {
            FrameworkAuthoringInspectorGui.Section("Validation");
            if (GUILayout.Button("Validate"))
            {
                serializedObject.ApplyModifiedProperties();
                _hasValidation = true;
                if (binding.TryValidateConfiguration(out _validationMessage))
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
            else if (binding.TryValidateConfiguration(out string issue))
            {
                EditorGUILayout.HelpBox(
                    "The authored binding relationship is structurally valid.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }
    }
}
