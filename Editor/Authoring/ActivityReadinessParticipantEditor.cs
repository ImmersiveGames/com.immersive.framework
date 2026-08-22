using System;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Common;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ActivityReadinessParticipant))]
    internal sealed class ActivityReadinessParticipantEditor :
        UnityEditor.Editor
    {
        private static readonly GUIContent RequirednessLabel =
            new GUIContent(
                "Contribution",
                "Required blocks Activity readiness until this participant completes. Optional remains diagnostic and does not block readiness.");

        private static readonly GUIContent PreparationStartedLabel =
            new GUIContent(
                "When Preparation Starts",
                "Invoked when ActivityFlow starts preparation for the current Activity occurrence. Start the real local work here, then complete or fail the participant explicitly.");

        private static readonly GUIContent PreparationReleasedLabel =
            new GUIContent(
                "When Preparation Is Released",
                "Invoked when the owning Activity occurrence releases this preparation. Cancel or release local preparation work here.");

        private static readonly GUIContent ParticipantIdLabel =
            new GUIContent(
                "Participant ID",
                "Stable technical identity for this readiness contribution. GameObject names and hierarchy paths are not fallback identity.");

        private static readonly GUIContent OrderLabel =
            new GUIContent(
                "Execution Order",
                "Technical execution order relative to other readiness participants discovered in the same Activity content scope.");

        private SerializedProperty _participantId;
        private SerializedProperty _requiredness;
        private SerializedProperty _order;
        private SerializedProperty _preparationStarted;
        private SerializedProperty _preparationReleased;

        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _participantId =
                serializedObject.FindProperty("participantId");

            _requiredness =
                serializedObject.FindProperty("requiredness");

            _order =
                serializedObject.FindProperty("order");

            _preparationStarted =
                serializedObject.FindProperty("preparationStarted");

            _preparationReleased =
                serializedObject.FindProperty("preparationReleased");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Activity Readiness Participant",
                "Represents one piece of Activity preparation that contributes to the current Activity readiness occurrence.");

            FrameworkAuthoringInspectorGui.IntentSummary(
                "Author this component in Activity-owned content. ActivityFlow starts preparation for the active occurrence; the owning content completes it with CompletePreparation() or fails it with FailPreparation(reason). The Activity scope comes from content composition, not from an authored Activity reference on this component.");

            DrawReadiness();
            DrawPreparation();
            DrawConfigurationStatus();
            DrawRuntimeStatus();
            DrawAdvancedDebug();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawReadiness()
        {
            FrameworkAuthoringInspectorGui.Section("Readiness");

            EditorGUILayout.PropertyField(
                _requiredness,
                RequirednessLabel);

            if (_requiredness == null ||
                _requiredness.hasMultipleDifferentValues)
            {
                return;
            }

            switch ((ActivityContentExecutionRequiredness)
                    _requiredness.intValue)
            {
                case ActivityContentExecutionRequiredness.Required:
                    EditorGUILayout.HelpBox(
                        "Required preparation must reach Completed before the Activity readiness occurrence can become Ready. A failure remains blocking and diagnostic.",
                        MessageType.Info);
                    break;

                case ActivityContentExecutionRequiredness.Optional:
                    EditorGUILayout.HelpBox(
                        "Optional preparation is observable diagnostics. It does not block the Activity readiness occurrence from becoming Ready.",
                        MessageType.Info);
                    break;
            }
        }

        private void DrawPreparation()
        {
            FrameworkAuthoringInspectorGui.Section("Preparation");

            EditorGUILayout.PropertyField(
                _preparationStarted,
                PreparationStartedLabel);

            EditorGUILayout.PropertyField(
                _preparationReleased,
                PreparationReleasedLabel);

            EditorGUILayout.HelpBox(
                "Preparation remains Preparing until the real work explicitly calls CompletePreparation(). If the work cannot complete, call FailPreparation(reason). ActivityFlow does not invent completion or apply a hidden timeout.",
                MessageType.None);
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            FrameworkAuthoringInspectorGui.Status(
                GetConfigurationStatus());

            if (_participantId == null ||
                string.IsNullOrWhiteSpace(
                    _participantId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "A stable Participant ID is required. Open Advanced / Debug and generate or enter one.",
                    MessageType.Error);
                return;
            }

            if (!IsRequirednessValid())
            {
                EditorGUILayout.HelpBox(
                    "Contribution must be Required or Optional.",
                    MessageType.Error);
            }
        }

        private string GetConfigurationStatus()
        {
            if (_participantId == null ||
                string.IsNullOrWhiteSpace(
                    _participantId.stringValue))
            {
                return "Incomplete";
            }

            return IsRequirednessValid()
                ? "Configured"
                : "Invalid";
        }

        private bool IsRequirednessValid()
        {
            if (_requiredness == null ||
                _requiredness.hasMultipleDifferentValues)
            {
                return false;
            }

            var requiredness =
                (ActivityContentExecutionRequiredness)
                _requiredness.intValue;

            return requiredness !=
                       ActivityContentExecutionRequiredness.Unknown &&
                   Enum.IsDefined(
                       typeof(ActivityContentExecutionRequiredness),
                       requiredness);
        }

        private void DrawRuntimeStatus()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Runtime Status");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Runtime readiness evidence is available in Play Mode.",
                    MessageType.None);
                return;
            }

            ActivityReadinessParticipant participant =
                (ActivityReadinessParticipant)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "State",
                    participant.State.ToString());

                EditorGUILayout.TextField(
                    "Blocks Activity Ready",
                    IsBlockingReadiness(participant)
                        ? "Yes"
                        : "No");

                EditorGUILayout.IntField(
                    "Occurrence",
                    participant.Occurrence);

                EditorGUILayout.TextField(
                    "Last Reason",
                    string.IsNullOrWhiteSpace(
                        participant.LastReason)
                            ? "<none>"
                            : participant.LastReason);
            }

            switch (participant.State)
            {
                case ActivityReadinessParticipantState.Preparing:
                    EditorGUILayout.HelpBox(
                        participant.Requiredness ==
                            ActivityContentExecutionRequiredness.Required
                            ? "This Required participant is currently preparing and is blocking Activity Ready."
                            : "This Optional participant is currently preparing but does not block Activity Ready.",
                        MessageType.Info);
                    break;

                case ActivityReadinessParticipantState.Failed:
                    EditorGUILayout.HelpBox(
                        participant.Requiredness ==
                            ActivityContentExecutionRequiredness.Required
                            ? "This Required participant failed. Activity readiness remains blocked until the owning operation resolves or recovers according to the ActivityFlow contract."
                            : "This Optional participant failed. The failure remains diagnostic but does not block Activity Ready.",
                        participant.Requiredness ==
                            ActivityContentExecutionRequiredness.Required
                            ? MessageType.Error
                            : MessageType.Warning);
                    break;
            }
        }

        private static bool IsBlockingReadiness(
            ActivityReadinessParticipant participant)
        {
            if (participant == null ||
                participant.Requiredness !=
                    ActivityContentExecutionRequiredness.Required)
            {
                return false;
            }

            return participant.State ==
                       ActivityReadinessParticipantState.Preparing ||
                   participant.State ==
                       ActivityReadinessParticipantState.Failed;
        }

        private void DrawAdvancedDebug()
        {
            _showAdvancedDebug =
                FrameworkAuthoringInspectorGui.AdvancedFoldout(
                    _showAdvancedDebug);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawParticipantId();

            EditorGUILayout.PropertyField(
                _order,
                OrderLabel);

            EditorGUI.indentLevel--;
        }

        private void DrawParticipantId()
        {
            string participantId =
                _participantId != null
                    ? _participantId.stringValue ?? string.Empty
                    : string.Empty;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(
                    _participantId,
                    ParticipantIdLabel);

                using (new EditorGUI.DisabledScope(
                           !string.IsNullOrWhiteSpace(
                               participantId)))
                {
                    if (GUILayout.Button(
                            "Generate ID",
                            GUILayout.Width(90f)))
                    {
                        _participantId.stringValue =
                            Guid.NewGuid().ToString("N");
                    }
                }

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(
                               participantId)))
                {
                    if (GUILayout.Button(
                            "Copy ID",
                            GUILayout.Width(70f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            participantId;
                    }
                }
            }
        }
    }
}
