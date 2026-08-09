using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerProvisioningStatusBinding))]
    internal sealed class PlayerProvisioningStatusBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty consumerAccessBinding;
        private SerializedProperty commandTrigger;
        private bool showAdvanced;
        private bool hasValidation;
        private string validationMessage;
        private MessageType validationType;

        private void OnEnable()
        {
            consumerAccessBinding = serializedObject.FindProperty(
                "consumerAccessBinding");
            commandTrigger = serializedObject.FindProperty("commandTrigger");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            var binding = (PlayerProvisioningStatusBinding)target;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Player Provisioning Status Binding",
                "Reads the scoped public Player observation for presentation and diagnostics. It never executes Player commands or owns Player state.");
            FrameworkAuthoringInspectorGui.IntentSummary(
                "Read-only pull binding: no Update loop, scene search, runtime module access or cached Player lifecycle.");

            FrameworkAuthoringInspectorGui.Section("Scoped Observation");
            EditorGUILayout.PropertyField(
                consumerAccessBinding,
                new GUIContent(
                    "Consumer Access Binding",
                    "Explicit P1 Route or Activity binding used to read P2 observation."));

            FrameworkAuthoringInspectorGui.Section("Last Operation (Optional)");
            EditorGUILayout.PropertyField(
                commandTrigger,
                new GUIContent(
                    "Command Trigger",
                    "Optional explicit P3 trigger using the same Consumer Access Binding. No global Last Operation is inferred."));

            DrawValidation(binding);
            DrawNormalRuntimeStatus(binding);

            showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(showAdvanced);
            if (showAdvanced)
            {
                DrawAdvanced(binding);
            }

            if (EditorGUI.EndChangeCheck())
            {
                hasValidation = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidation(PlayerProvisioningStatusBinding binding)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            if (GUILayout.Button("Validate"))
            {
                serializedObject.ApplyModifiedProperties();
                hasValidation = true;
                if (binding.TryValidateConfiguration(out validationMessage))
                {
                    validationType = MessageType.Info;
                    validationMessage =
                        "Configuration is valid. Scope availability is evaluated only when the status is read.";
                }
                else
                {
                    validationType = MessageType.Error;
                }
            }

            if (hasValidation)
            {
                EditorGUILayout.HelpBox(validationMessage, validationType);
            }
            else if (binding.TryValidateConfiguration(out string issue))
            {
                EditorGUILayout.HelpBox(
                    "Not validated in this Inspector session. The authored binding relationship is structurally valid.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }

        private static void DrawNormalRuntimeStatus(
            PlayerProvisioningStatusBinding binding)
        {
            if (!Application.isPlaying || !binding)
            {
                return;
            }

            FrameworkAuthoringInspectorGui.RuntimeBinding(
                binding.Availability.ToString(),
                binding.Diagnostic,
                "Ensure the Consumer Access Binding belongs to the current Route or Activity and its configured scope matches that content.");

            if (!binding.TryGetObservation(
                    out LocalPlayerProvisioningConsumerObservationSnapshot
                        observation))
            {
                return;
            }

            FrameworkAuthoringInspectorGui.Section("Session");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Initialization",
                    binding.InitializationSummary);
                EditorGUILayout.Toggle(
                    "Joining Open",
                    observation.Participation.JoiningOpen);
                EditorGUILayout.IntField(
                    "Joined Slots",
                    observation.Participation.JoinedCount);
                EditorGUILayout.IntField(
                    "Configured Slots",
                    observation.Participation.ConfiguredSlotCount);
                EditorGUILayout.IntField(
                    "Available Slots",
                    observation.Participation.AvailableCount);
            }

            FrameworkAuthoringInspectorGui.Section("Activity");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Current Activity", binding.ActivitySummary);
                EditorGUILayout.EnumPopup(
                    "Lifecycle",
                    observation.Lifecycle.Status);
            }

            DrawSlots(binding, observation);
            DrawLastOperation(binding);
        }

        private static void DrawSlots(
            PlayerProvisioningStatusBinding binding,
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            FrameworkAuthoringInspectorGui.Section("Player Slots");
            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                string slotName = slot.Slot.Profile != null
                    ? slot.Slot.Profile.DisplayName
                    : slot.Slot.PlayerSlotId.StableText;
                EditorGUILayout.LabelField($"{index + 1}. {slotName}",
                    binding.DescribeSlotLifecycle(slot),
                    EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle("Joined", slot.IsJoined);
                    EditorGUILayout.TextField(
                        "Selected Actor",
                        binding.DescribeSelectedActor(slot));
                    EditorGUILayout.Toggle(
                        "Logical Actor Prepared",
                        slot.IsLogicalActorPrepared);
                    EditorGUILayout.Toggle(
                        "Physically Materialized",
                        slot.IsPhysicallyMaterialized);
                    EditorGUILayout.TextField(
                        "Gameplay",
                        binding.DescribeGameplay(slot));
                }
            }
        }

        private static void DrawLastOperation(
            PlayerProvisioningStatusBinding binding)
        {
            FrameworkAuthoringInspectorGui.Section("Last Operation");
            if (binding.CommandTrigger == null)
            {
                EditorGUILayout.HelpBox(
                    binding.LastOperationSummary,
                    MessageType.None);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup(
                    "Result Contract",
                    binding.LastOperationResultKind);
            }
            EditorGUILayout.HelpBox(
                binding.LastOperationSummary,
                binding.HasLastOperation ? MessageType.Info : MessageType.None);
        }

        private static void DrawAdvanced(PlayerProvisioningStatusBinding binding)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Availability", binding.Availability);
                EditorGUILayout.TextField("Diagnostic", binding.Diagnostic);
            }

            if (!Application.isPlaying || !binding.TryGetObservation(
                    out LocalPlayerProvisioningConsumerObservationSnapshot
                        observation))
            {
                EditorGUILayout.HelpBox(
                    "Current P2 observation and correlation evidence are available in Play Mode while the configured scope is live.",
                    MessageType.None);
                return;
            }

            FrameworkAuthoringInspectorGui.Section("Scope / Revisions");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Scope", observation.Scope);
                EditorGUILayout.TextField(
                    "Scope Owner",
                    observation.ScopeOwner.StableText);
                EditorGUILayout.IntField(
                    "Session Revision",
                    observation.SessionRevision);
                EditorGUILayout.IntField(
                    "Applied Session Revision",
                    observation.AppliedSessionRevision);
                EditorGUILayout.IntField(
                    "Requested Session Revision",
                    observation.Lifecycle.RequestedSessionRevision);
                EditorGUILayout.TextField(
                    "Activity Owner",
                    observation.ActivityOwner.IsValid
                        ? observation.ActivityOwner.StableText
                        : "<none>");
                EditorGUILayout.IntField(
                    "Activity Occurrence",
                    observation.ActivityOccurrence);
            }

            DrawInitializationEvidence(observation);
            DrawSlotCorrelation(observation);
            DrawRawLastOperation(binding);
        }

        private static void DrawInitializationEvidence(
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            FrameworkAuthoringInspectorGui.Section("Initialization Evidence");
            if (!observation.HasInitializationEvidence)
            {
                EditorGUILayout.HelpBox(
                    "P2 does not publish creation-time Session evidence for this scope.",
                    MessageType.None);
                return;
            }

            EffectivePlayerSessionConfiguration configuration =
                observation.InitializationConfiguration;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    "Initial Joining Open",
                    configuration.InitialJoiningOpen);
                EditorGUILayout.EnumPopup(
                    "Host Provisioning",
                    configuration.HostProvisioning);
                EditorGUILayout.EnumPopup(
                    "Actor Resolution Policy",
                    configuration.ActorResolutionPolicy);
            }

            for (int index = 0; index < configuration.Slots.Count; index++)
            {
                EffectivePlayerSlotProvisioning slot =
                    configuration.Slots[index];
                EditorGUILayout.LabelField(
                    $"{index + 1}. {slot.PlayerSlotId.StableText}");
            }
        }

        private static void DrawSlotCorrelation(
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            FrameworkAuthoringInspectorGui.Section("Slot / Host / Actor Correlation");
            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                EditorGUILayout.LabelField(
                    slot.Slot.PlayerSlotId.StableText,
                    EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup(
                        "Allocation",
                        slot.Slot.AllocationState);
                    EditorGUILayout.IntField("Slot Revision", slot.Slot.Revision);
                    EditorGUILayout.IntField(
                        "Selection Revision",
                        slot.Slot.SelectionRevision);
                    EditorGUILayout.TextField(
                        "Host Evidence",
                        slot.HasHostEvidence
                            ? slot.HostEvidence.HostBindingIdentity.StableText
                            : "<none>");
                    EditorGUILayout.TextField(
                        "Current Actor",
                        slot.HasCurrentActorEvidence
                            ? slot.CurrentActor.ActorEvidence.ActorId.StableText
                            : "<none>");
                    EditorGUILayout.TextField(
                        "Preparation",
                        slot.HasPreparationEvidence
                            ? slot.Preparation.ToDiagnosticString()
                            : "<none>");
                    EditorGUILayout.TextField(
                        "Gameplay Admission",
                        slot.HasGameplayAdmissionEvidence
                            ? slot.GameplayAdmission.ToDiagnosticString()
                            : "<none>");
                }
            }
        }

        private static void DrawRawLastOperation(
            PlayerProvisioningStatusBinding binding)
        {
            FrameworkAuthoringInspectorGui.Section("Raw Last Operation");
            if (binding.LastParticipationOperation != null)
            {
                EditorGUILayout.TextArea(
                    binding.LastParticipationOperation.ToDiagnosticString(),
                    GUILayout.MinHeight(48f));
            }
            else if (binding.LastJoinOperation != null)
            {
                EditorGUILayout.TextArea(
                    binding.LastJoinOperation.ToDiagnosticString(),
                    GUILayout.MinHeight(48f));
            }
            else if (binding.LastActorSelectionOperation != null)
            {
                EditorGUILayout.TextArea(
                    binding.LastActorSelectionOperation.ToDiagnosticString(),
                    GUILayout.MinHeight(48f));
            }
            else
            {
                EditorGUILayout.HelpBox(binding.LastOperationSummary, MessageType.None);
            }
        }
    }
}
