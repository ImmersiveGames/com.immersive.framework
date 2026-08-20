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
                "This component has no automatic Awake, OnEnable, Start or OnValidate command path.");

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
                    "Explicit Route or Activity scoped P1 access. This is not a Player authority reference."));

            DrawOperationParameters();

            FrameworkAuthoringInspectorGui.Section("Request Metadata");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", nameof(PlayerProvisioningCommandTrigger));
            }

            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostic reason. The selected operation is used when empty."));

            DrawActions(trigger);
            DrawConfigurationStatus(trigger);
            DrawRuntimeEvidence(trigger);

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                DrawAdvanced(trigger);
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
                            "Optional input hint forwarded to the existing LocalPlayerJoinRequest."));
                    break;

                case PlayerProvisioningCommandOperation
                    .RequestDefaultActorSelection:
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
                        "Leave always targets an explicit Player Slot. With the Advanced occurrence override at -1, the trigger captures the current joined occurrence and reuses that exact correlation if the same Leave must be retried.",
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
                        _validationMessage = "Configuration is valid. Runtime scope availability is checked only when explicitly invoked.";
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
                    "Not validated in this Inspector session. The current authored configuration is structurally valid.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }

        private static void DrawRuntimeEvidence(
            PlayerProvisioningCommandTrigger trigger)
        {
            if (!Application.isPlaying || !trigger)
            {
                return;
            }

            FrameworkAuthoringInspectorGui.RuntimeBinding(
                trigger.ScopeBindingStatus,
                trigger.ScopeBindingDiagnostic,
                "Place the Consumer Access Binding in the active Route or Activity scope, and ensure its scope matches that content.");
            FrameworkAuthoringInspectorGui.Section("Last Typed Result");
            EditorGUILayout.LabelField("Invocations", trigger.InvocationCount.ToString());
            EditorGUILayout.LabelField("Result Contract", trigger.LastResultKind.ToString());
            EditorGUILayout.HelpBox(
                trigger.LastResultSummary,
                trigger.HasLastTypedResult ? MessageType.Info : MessageType.Warning);
        }

        private void DrawAdvanced(PlayerProvisioningCommandTrigger trigger)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Operation", trigger.Operation);
                EditorGUILayout.TextField("Scope Binding", trigger.ScopeBindingStatus);
                EditorGUILayout.TextField("Scope Diagnostic", trigger.ScopeBindingDiagnostic);
                EditorGUILayout.EnumPopup("Last Result Contract", trigger.LastResultKind);
            }

            if ((PlayerProvisioningCommandOperation)_operation.intValue ==
                PlayerProvisioningCommandOperation.RequestLeave)
            {
                FrameworkAuthoringInspectorGui.Section("Leave Correlation Override");
                EditorGUILayout.PropertyField(
                    _expectedLeaveOccurrenceRevision,
                    new GUIContent(
                        "Expected Occurrence Revision",
                        "Advanced/debug only. -1 resolves the current joined occurrence from scoped observation; a non-negative value sends that exact revision."));
            }

            if (!Application.isPlaying || !trigger)
            {
                EditorGUILayout.HelpBox(
                    "Runtime result and scope correlation are available in Play Mode for one selected trigger.",
                    MessageType.None);
                return;
            }

            if (trigger.LastLeaveRequest.IsValid)
            {
                FrameworkAuthoringInspectorGui.Section("Last Leave Correlation");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        "Player Slot",
                        trigger.LastLeaveRequest.PlayerSlotId.StableText);
                    EditorGUILayout.IntField(
                        "Expected Occurrence Revision",
                        trigger.LastLeaveRequest.ExpectedOccurrenceRevision);
                }
            }

            if (trigger.LastParticipationResult != null)
            {
                EditorGUILayout.TextArea(
                    trigger.LastParticipationResult.ToDiagnosticString(),
                    GUILayout.MinHeight(48f));
            }
            else if (trigger.LastJoinResult != null)
            {
                EditorGUILayout.TextArea(
                    trigger.LastJoinResult.ToDiagnosticString(),
                    GUILayout.MinHeight(48f));
            }
            else if (trigger.LastActorSelectionResult != null)
            {
                EditorGUILayout.TextArea(
                    trigger.LastActorSelectionResult.ToDiagnosticString(),
                    GUILayout.MinHeight(48f));
            }
            else if (trigger.LastLeaveResult != null)
            {
                EditorGUILayout.TextArea(
                    trigger.LastLeaveResult.ToDiagnosticString(),
                    GUILayout.MinHeight(64f));
            }
        }
    }
}
