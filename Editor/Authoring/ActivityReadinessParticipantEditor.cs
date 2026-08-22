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
                "Required blocks Activity Ready until this participant completes. Optional remains diagnostic and does not block. The Activity scope comes from Activity-owned content composition.");

        private static readonly GUIContent PreparationStartedLabel =
            new GUIContent(
                "Preparation Started",
                "Invoked when ActivityFlow starts preparation for the current occurrence. Start the real work here, then call CompletePreparation() or FailPreparation(reason).");

        private static readonly GUIContent PreparationReleasedLabel =
            new GUIContent(
                "Preparation Released",
                "Invoked when the owning Activity occurrence releases this preparation. Cancel or release local work here.");

        private static readonly GUIContent ParticipantIdLabel =
            new GUIContent(
                "Participant ID",
                "Stable technical identity for this readiness contribution. GameObject names and hierarchy paths are not fallback identity.");

        private static readonly GUIContent OrderLabel =
            new GUIContent(
                "Execution Order",
                "Technical execution order relative to other readiness participants in the same Activity content scope.");

        private static readonly GUIContent RuntimeStateLabel =
            new GUIContent(
                "State",
                "Current state for this readiness participant in the active Activity occurrence.");

        private static readonly GUIContent BlocksReadyLabel =
            new GUIContent(
                "Blocks Activity Ready",
                "Yes when this Required participant is still Preparing or has Failed for the active occurrence.");

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

            EditorGUILayout.LabelField(
                "Activity Readiness Participant",
                EditorStyles.boldLabel);

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
                    "Participant ID is required. Generate one in Advanced / Debug.",
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
            if (!Application.isPlaying)
            {
                return;
            }

            FrameworkAuthoringInspectorGui.Section("Runtime");

            ActivityReadinessParticipant participant =
                (ActivityReadinessParticipant)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    RuntimeStateLabel,
                    participant.State.ToString());

                EditorGUILayout.TextField(
                    BlocksReadyLabel,
                    IsBlockingReadiness(participant)
                        ? "Yes"
                        : "No");
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

            if (Application.isPlaying)
            {
                ActivityReadinessParticipant participant =
                    (ActivityReadinessParticipant)target;

                using (new EditorGUI.DisabledScope(true))
                {
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
            }

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
