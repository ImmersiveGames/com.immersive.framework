using System;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSessionCommandTriggerBase), true)]
    internal sealed class PlayerSessionCommandTriggerEditor : UnityEditor.Editor
    {
        private bool _hasValidation;
        private bool _validationIsValid;
        private bool _validationOutdated;
        private string _validationIssue;
        private bool _showAdvanced;

        public override void OnInspectorGUI()
        {
            if (!TryGetLiveEditorState(
                    out PlayerSessionCommandTriggerBase trigger,
                    out SerializedObject commandSerializedObject))
            {
                return;
            }

            commandSerializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();

            FrameworkAuthoringInspectorGui.ProductHeader(GetTitle(trigger), string.Empty);
            FrameworkAuthoringInspectorGui.Section("Scope");
            if (!DrawProperty(
                    commandSerializedObject.FindProperty("scope"),
                    new GUIContent(
                        "Scope",
                        "Explicit Route or Activity scope for this command. Framework Core supplies scoped access directly at runtime.")))
            {
                EditorGUI.EndChangeCheck();
                return;
            }

            DrawCommandFields(trigger, commandSerializedObject);
            DrawValidation(trigger, commandSerializedObject);

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                DrawAdvanced(trigger, commandSerializedObject);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _validationOutdated = _hasValidation;
                _hasValidation = false;
            }

            commandSerializedObject.ApplyModifiedProperties();
        }

        private static void DrawCommandFields(
            PlayerSessionCommandTriggerBase trigger,
            SerializedObject commandSerializedObject)
        {
            if (trigger is PlayerSessionJoinCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Control Scheme Hint");
                DrawProperty(
                    commandSerializedObject.FindProperty("controlScheme"),
                    new GUIContent(
                        "Control Scheme Hint",
                        "Optional input hint forwarded to the Local Player Join request."));
                return;
            }

            if (trigger is PlayerSessionSelectActorCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
                DrawProperty(
                    commandSerializedObject.FindProperty("playerSlot"),
                    new GUIContent(
                        "Player Slot Profile",
                        "Provides the typed Slot identity that receives the selected Actor."));
                FrameworkAuthoringInspectorGui.Section("Actor Profile");
                DrawProperty(
                    commandSerializedObject.FindProperty("actorProfile"),
                    new GUIContent(
                        "Actor Profile",
                        "Typed Actor identity to select for the Player Slot."));
                return;
            }

            if (trigger is PlayerSessionDefaultActorSelectionCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
                DrawProperty(
                    commandSerializedObject.FindProperty("playerSlot"),
                    new GUIContent(
                        "Player Slot Profile",
                        "Provides the typed Slot identity. The Actor remains the Slot configured default."));
                return;
            }

            if (trigger is PlayerSessionReplaceActorSelectionCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
                DrawProperty(
                    commandSerializedObject.FindProperty("playerSlot"),
                    new GUIContent(
                        "Player Slot Profile",
                        "Provides the typed Slot identity whose Actor selection will be replaced."));
                FrameworkAuthoringInspectorGui.Section("Actor Profile");
                DrawProperty(
                    commandSerializedObject.FindProperty("actorProfile"),
                    new GUIContent(
                        "Actor Profile",
                        "Typed Actor identity that replaces the current selection."));
                return;
            }

            if (trigger is PlayerSessionClearActorSelectionCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
                DrawProperty(
                    commandSerializedObject.FindProperty("playerSlot"),
                    new GUIContent(
                        "Player Slot Profile",
                        "Provides the typed Slot identity whose Actor selection will be cleared."));
                return;
            }

            if (trigger is PlayerSessionLeaveCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
                DrawProperty(
                    commandSerializedObject.FindProperty("playerSlot"),
                    new GUIContent(
                        "Player Slot Profile",
                        "Exact Player target. At -1, the occurrence revision is read from the same scoped observation when invoked."));
            }
        }

        private void DrawValidation(
            PlayerSessionCommandTriggerBase trigger,
            SerializedObject commandSerializedObject)
        {
            FrameworkAuthoringInspectorGui.Section("Validation");
            if (GUILayout.Button("Validate"))
            {
                commandSerializedObject.ApplyModifiedProperties();
                _hasValidation = true;
                _validationOutdated = false;
                _validationIsValid = trigger.TryValidateConfiguration(out _validationIssue);
            }

            string state = !_hasValidation
                ? _validationOutdated ? "Outdated" : "Not Validated"
                : _validationIsValid ? "Valid" : "Issue";
            EditorGUILayout.LabelField("Status", state);
            if (_hasValidation && !_validationIsValid)
            {
                EditorGUILayout.LabelField(
                    "Issue",
                    _validationIssue,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawAdvanced(
            PlayerSessionCommandTriggerBase trigger,
            SerializedObject commandSerializedObject)
        {
            DrawProperty(
                commandSerializedObject.FindProperty("reason"),
                new GUIContent(
                    "Reason",
                    "Optional diagnostic reason. The command name is used when left empty."));

            if (trigger is PlayerSessionSelectActorCommandTrigger ||
                trigger is PlayerSessionDefaultActorSelectionCommandTrigger ||
                trigger is PlayerSessionReplaceActorSelectionCommandTrigger ||
                trigger is PlayerSessionClearActorSelectionCommandTrigger)
            {
                DrawProperty(
                    commandSerializedObject.FindProperty("expectedSelectionRevision"),
                    new GUIContent(
                        "Expected Selection Revision",
                        "Use -1 when no optimistic revision check is required."));
            }

            if (trigger is PlayerSessionLeaveCommandTrigger)
            {
                DrawProperty(
                    commandSerializedObject.FindProperty("expectedLeaveOccurrenceRevision"),
                    new GUIContent(
                        "Expected Leave Occurrence Revision",
                        "Use -1 to read the current joined occurrence from scoped observation; a non-negative value sends that exact revision."));
            }

            EditorGUILayout.LabelField("Scoped Access State", trigger.ScopedAccessState.ToString());
            EditorGUILayout.LabelField(
                "Diagnostic",
                trigger.LastDiagnostic,
                EditorStyles.wordWrappedMiniLabel);
            FrameworkAuthoringInspectorGui.Section("Last Operation");
            EditorGUILayout.LabelField("Invocation Count", trigger.InvocationCount.ToString());
            EditorGUILayout.LabelField("Outcome", trigger.LastOutcome);
            EditorGUILayout.LabelField("Typed Result", GetLastResultType(trigger));
            EditorGUILayout.LabelField(
                "Diagnostic",
                trigger.LastDiagnostic,
                EditorStyles.wordWrappedMiniLabel);

            FrameworkAuthoringInspectorGui.Section("Runtime Test");
            using (new EditorGUI.DisabledScope(!Application.isPlaying || targets.Length != 1))
            {
                if (GUILayout.Button("Invoke"))
                {
                    commandSerializedObject.ApplyModifiedProperties();
                    trigger.Invoke();
                }
            }
        }

        private static bool DrawProperty(SerializedProperty property, GUIContent label)
        {
            if (property == null)
            {
                EditorGUILayout.HelpBox(
                    $"The selected Player Session command does not expose the required '{label.text}' field.",
                    MessageType.Error);
                return false;
            }

            EditorGUILayout.PropertyField(property, label);
            return true;
        }

        private static string GetTitle(PlayerSessionCommandTriggerBase trigger)
        {
            return trigger switch
            {
                PlayerSessionOpenJoiningCommandTrigger => "OPEN JOINING COMMAND",
                PlayerSessionCloseJoiningCommandTrigger => "CLOSE JOINING COMMAND",
                PlayerSessionJoinCommandTrigger => "JOIN COMMAND",
                PlayerSessionSelectActorCommandTrigger => "SELECT ACTOR COMMAND",
                PlayerSessionDefaultActorSelectionCommandTrigger => "DEFAULT ACTOR SELECTION COMMAND",
                PlayerSessionReplaceActorSelectionCommandTrigger => "REPLACE ACTOR SELECTION COMMAND",
                PlayerSessionClearActorSelectionCommandTrigger => "CLEAR ACTOR SELECTION COMMAND",
                PlayerSessionLeaveCommandTrigger => "LEAVE COMMAND",
                _ => "PLAYER SESSION COMMAND"
            };
        }

        private static string GetLastResultType(PlayerSessionCommandTriggerBase trigger)
        {
            return trigger switch
            {
                PlayerSessionOpenJoiningCommandTrigger command when
                    command.LastOpenJoiningResult != null =>
                    nameof(PlayerParticipationOperationResult),
                PlayerSessionCloseJoiningCommandTrigger command when
                    command.LastCloseJoiningResult != null =>
                    nameof(PlayerParticipationOperationResult),
                PlayerSessionJoinCommandTrigger command when
                    command.LastJoinResult != null => nameof(LocalPlayerJoinResult),
                PlayerSessionSelectActorCommandTrigger command when
                    command.LastActorSelectionResult != null =>
                    nameof(PlayerActorSelectionResult),
                PlayerSessionDefaultActorSelectionCommandTrigger command when
                    command.LastActorSelectionResult != null =>
                    nameof(PlayerActorSelectionResult),
                PlayerSessionReplaceActorSelectionCommandTrigger command when
                    command.LastActorSelectionResult != null =>
                    nameof(PlayerActorSelectionResult),
                PlayerSessionClearActorSelectionCommandTrigger command when
                    command.LastActorSelectionResult != null =>
                    nameof(PlayerActorSelectionResult),
                PlayerSessionLeaveCommandTrigger command when
                    command.LastLeaveResult != null =>
                    nameof(SessionPlayerLeaveResult),
                _ => "None"
            };
        }

        private bool TryGetLiveEditorState(
            out PlayerSessionCommandTriggerBase trigger,
            out SerializedObject commandSerializedObject)
        {
            trigger = null;
            commandSerializedObject = null;

            if (target == null || targets == null || targets.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] is not PlayerSessionCommandTriggerBase command ||
                    command == null)
                {
                    return false;
                }
            }

            trigger = target as PlayerSessionCommandTriggerBase;
            if (trigger == null)
            {
                return false;
            }

            try
            {
                commandSerializedObject = serializedObject;
                return commandSerializedObject != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
