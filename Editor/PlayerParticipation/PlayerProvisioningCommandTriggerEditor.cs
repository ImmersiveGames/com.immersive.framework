using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerProvisioningCommandTrigger))]
    internal sealed class PlayerProvisioningCommandTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty operation;
        private SerializedProperty consumerAccessBinding;
        private SerializedProperty controlScheme;
        private SerializedProperty defaultActorSelectionRequest;
        private SerializedProperty selectedPlayerSlot;
        private SerializedProperty expectedSelectionRevision;
        private SerializedProperty reason;
        private bool showAdvanced;
        private bool hasValidation;
        private string validationMessage;
        private MessageType validationType;

        private void OnEnable()
        {
            operation = serializedObject.FindProperty("operation");
            consumerAccessBinding = serializedObject.FindProperty(
                "consumerAccessBinding");
            controlScheme = serializedObject.FindProperty("controlScheme");
            defaultActorSelectionRequest = serializedObject.FindProperty(
                "defaultActorSelectionRequest");
            selectedPlayerSlot = serializedObject.FindProperty("selectedPlayerSlot");
            expectedSelectionRevision = serializedObject.FindProperty(
                "expectedSelectionRevision");
            reason = serializedObject.FindProperty("reason");
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
                operation,
                new GUIContent(
                    "Operation",
                    "Choose the supported Player command invoked by Invoke Configured Operation."));

            FrameworkAuthoringInspectorGui.Section("Scoped Consumer Access");
            EditorGUILayout.PropertyField(
                consumerAccessBinding,
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
                reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostic reason. The selected operation is used when empty."));

            DrawActions(trigger);
            DrawConfigurationStatus(trigger);
            DrawRuntimeEvidence(trigger);

            showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(showAdvanced);
            if (showAdvanced)
            {
                DrawAdvanced(trigger);
            }

            if (EditorGUI.EndChangeCheck())
            {
                hasValidation = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOperationParameters()
        {
            PlayerProvisioningCommandOperation selected =
                (PlayerProvisioningCommandOperation)operation.intValue;
            switch (selected)
            {
                case PlayerProvisioningCommandOperation.RequestJoin:
                    FrameworkAuthoringInspectorGui.Section("Request Join");
                    EditorGUILayout.PropertyField(
                        controlScheme,
                        new GUIContent(
                            "Control Scheme Hint",
                            "Optional input hint forwarded to the existing LocalPlayerJoinRequest."));
                    break;

                case PlayerProvisioningCommandOperation
                    .RequestDefaultActorSelection:
                    FrameworkAuthoringInspectorGui.Section(
                        "Request Default Actor Selection");
                    EditorGUILayout.PropertyField(
                        defaultActorSelectionRequest,
                        new GUIContent(
                            "Actor Selection Requests",
                            "Existing public selection authoring surface; it owns the selection command boundary."));
                    EditorGUILayout.PropertyField(
                        selectedPlayerSlot,
                        new GUIContent(
                            "Player Slot Profile",
                            "Provides the typed Slot identity. The Actor remains the Slot configured default."));
                    EditorGUILayout.PropertyField(
                        expectedSelectionRevision,
                        new GUIContent(
                            "Expected Selection Revision",
                            "Use -1 when no optimistic revision check is required."));
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
                    hasValidation = true;
                    if (trigger.TryValidateConfiguration(out validationMessage))
                    {
                        validationType = MessageType.Info;
                        validationMessage = "Configuration is valid. Runtime scope availability is checked only when explicitly invoked.";
                    }
                    else
                    {
                        validationType = MessageType.Error;
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
            if (hasValidation)
            {
                EditorGUILayout.HelpBox(validationMessage, validationType);
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

        private static void DrawAdvanced(PlayerProvisioningCommandTrigger trigger)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Operation", trigger.Operation);
                EditorGUILayout.TextField("Scope Binding", trigger.ScopeBindingStatus);
                EditorGUILayout.TextField("Scope Diagnostic", trigger.ScopeBindingDiagnostic);
                EditorGUILayout.EnumPopup("Last Result Contract", trigger.LastResultKind);
            }

            if (!Application.isPlaying || !trigger)
            {
                EditorGUILayout.HelpBox(
                    "Runtime result and scope correlation are available in Play Mode for one selected trigger.",
                    MessageType.None);
                return;
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
        }
    }
}
