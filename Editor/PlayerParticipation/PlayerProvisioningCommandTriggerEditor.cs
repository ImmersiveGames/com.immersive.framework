using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerProvisioningCommandTrigger))]
    internal sealed class PlayerProvisioningCommandTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _operation;
        private SerializedProperty _consumerAccessBinding;
        private SerializedProperty _controlScheme;
        private SerializedProperty _defaultActorSelectionRequest;
        private SerializedProperty _selectedPlayerSlot;
        private SerializedProperty _expectedSelectionRevision;
        private SerializedProperty _leavePlayerSlot;
        private SerializedProperty _expectedLeaveOccurrenceRevision;
        private SerializedProperty _reason;
        private bool _showAdvanced;
        private bool _hasValidation;
        private string _validationMessage;
        private MessageType _validationType;

        private void OnEnable()
        {
            _operation = serializedObject.FindProperty("operation");
            _consumerAccessBinding = serializedObject.FindProperty(
                "consumerAccessBinding");
            _controlScheme = serializedObject.FindProperty("controlScheme");
            _defaultActorSelectionRequest = serializedObject.FindProperty(
                "defaultActorSelectionRequest");
            _selectedPlayerSlot = serializedObject.FindProperty("selectedPlayerSlot");
            _expectedSelectionRevision = serializedObject.FindProperty(
                "expectedSelectionRevision");
            _leavePlayerSlot = serializedObject.FindProperty("leavePlayerSlot");
            _expectedLeaveOccurrenceRevision = serializedObject.FindProperty(
                "expectedLeaveOccurrenceRevision");
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            var trigger = (PlayerProvisioningCommandTrigger)target;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Player Provisioning Command Trigger",
                "Invokes one supported Player command only when called by a Button, UnityEvent or other explicit consumer action.");
            FrameworkAuthoringInspectorGui.IntentSummary(
                "Runtime request, binding, result and rejection evidence is written to the Console. No automatic lifecycle command path exists.");

            FrameworkAuthoringInspectorGui.Section("Command");
            EditorGUILayout.PropertyField(
                _operation,
                new GUIContent(
                    "Operation",
                    "Choose the supported Player command invoked by Invoke Configured Operation."));

            FrameworkAuthoringInspectorGui.Section("Scoped Consumer Access");
            EditorGUILayout.PropertyField(
                _consumerAccessBinding,
                new GUIContent(
                    "Consumer Access Binding",
                    "Explicit Route or Activity scoped access. This is not a Player authority reference."));

            DrawOperationParameters();

            FrameworkAuthoringInspectorGui.Section("Request Metadata");
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostic reason. The selected operation is used when empty."));

            DrawActions(trigger);
            DrawConfigurationStatus(trigger);

            if ((PlayerProvisioningCommandOperation)_operation.intValue ==
                PlayerProvisioningCommandOperation.RequestLeave)
            {
                _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
                if (_showAdvanced)
                {
                    DrawLeaveAdvanced();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                _hasValidation = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOperationParameters()
        {
            PlayerProvisioningCommandOperation selected =
                (PlayerProvisioningCommandOperation)_operation.intValue;
            switch (selected)
            {
                case PlayerProvisioningCommandOperation.RequestJoin:
                    FrameworkAuthoringInspectorGui.Section("Request Join");
                    EditorGUILayout.PropertyField(
                        _controlScheme,
                        new GUIContent(
                            "Control Scheme Hint",
                            "Optional input hint forwarded to the Local Player Join request."));
                    break;

                case PlayerProvisioningCommandOperation.RequestDefaultActorSelection:
                    FrameworkAuthoringInspectorGui.Section(
                        "Request Default Actor Selection");
                    EditorGUILayout.PropertyField(
                        _defaultActorSelectionRequest,
                        new GUIContent(
                            "Actor Selection Requests",
                            "Existing public selection authoring surface; it owns the selection command boundary."));
                    EditorGUILayout.PropertyField(
                        _selectedPlayerSlot,
                        new GUIContent(
                            "Player Slot Profile",
                            "Provides the typed Slot identity. The Actor remains the Slot configured default."));
                    EditorGUILayout.PropertyField(
                        _expectedSelectionRevision,
                        new GUIContent(
                            "Expected Selection Revision",
                            "Use -1 when no optimistic revision check is required."));
                    break;

                case PlayerProvisioningCommandOperation.RequestLeave:
                    FrameworkAuthoringInspectorGui.Section("Request Leave");
                    EditorGUILayout.PropertyField(
                        _leavePlayerSlot,
                        new GUIContent(
                            "Player Slot Profile",
                            "Exact Player target. The current joined occurrence revision is resolved from the same scoped observation when invoked."));
                    EditorGUILayout.HelpBox(
                        "Leave always targets an explicit Player Slot. With the Advanced occurrence override at -1, the trigger resolves the current joined occurrence from scoped observation.",
                        MessageType.None);
                    break;
            }
        }

        private void DrawActions(PlayerProvisioningCommandTrigger trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Actions");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    serializedObject.ApplyModifiedProperties();
                    _hasValidation = true;
                    if (trigger.TryValidateConfiguration(out _validationMessage))
                    {
                        _validationType = MessageType.Info;
                        _validationMessage =
                            "Configuration is valid. Runtime scope availability is checked only when explicitly invoked.";
                    }
                    else
                    {
                        _validationType = MessageType.Error;
                    }
                }

                using (new EditorGUI.DisabledScope(
                           !Application.isPlaying || targets.Length != 1))
                {
                    if (GUILayout.Button("Invoke Configured Operation"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        trigger.InvokeConfiguredOperation();
                    }
                }
            }
        }

        private void DrawConfigurationStatus(
            PlayerProvisioningCommandTrigger trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            if (_hasValidation)
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationType);
                return;
            }

            if (trigger.TryValidateConfiguration(out string issue))
            {
                EditorGUILayout.HelpBox(
                    "The current authored configuration is structurally valid.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }

        private void DrawLeaveAdvanced()
        {
            FrameworkAuthoringInspectorGui.Section("Leave Correlation Override");
            EditorGUILayout.PropertyField(
                _expectedLeaveOccurrenceRevision,
                new GUIContent(
                    "Expected Occurrence Revision",
                    "Advanced/debug authoring input. -1 resolves the current joined occurrence from scoped observation; a non-negative value sends that exact revision."));
        }
    }
}
