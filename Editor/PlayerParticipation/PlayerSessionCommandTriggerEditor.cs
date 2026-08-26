using Immersive.Framework.Editor.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerParticipation
{
    [CustomEditor(typeof(PlayerSessionCommandTriggerBase), true)]
    internal sealed class PlayerSessionCommandTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _scope;
        private SerializedProperty _reason;
        private SerializedProperty _controlScheme;
        private SerializedProperty _actorSelectionRequests;
        private SerializedProperty _playerSlot;
        private SerializedProperty _expectedSelectionRevision;
        private SerializedProperty _expectedLeaveOccurrenceRevision;
        private bool _hasValidation;
        private bool _validationIsValid;
        private bool _validationOutdated;
        private string _validationIssue;
        private bool _showAdvanced;

        private void OnEnable()
        {
            if (!HasLiveTargets())
            {
                return;
            }

            _scope = serializedObject.FindProperty("scope");
            _reason = serializedObject.FindProperty("reason");
            _controlScheme = serializedObject.FindProperty("controlScheme");
            _actorSelectionRequests = serializedObject.FindProperty("actorSelectionRequests");
            _playerSlot = serializedObject.FindProperty("playerSlot");
            _expectedSelectionRevision = serializedObject.FindProperty("expectedSelectionRevision");
            _expectedLeaveOccurrenceRevision = serializedObject.FindProperty("expectedLeaveOccurrenceRevision");
        }

        public override void OnInspectorGUI()
        {
            if (!HasLiveTargets())
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            var trigger = (PlayerSessionCommandTriggerBase)target;

            FrameworkAuthoringInspectorGui.ProductHeader(GetTitle(trigger), string.Empty);
            FrameworkAuthoringInspectorGui.Section("Scope");
            EditorGUILayout.PropertyField(
                _scope,
                new GUIContent(
                    "Scope",
                    "Explicit Route or Activity scope for this command. Framework Core supplies scoped access directly at runtime."));

            DrawCommandFields(trigger);
            DrawValidation(trigger);

            _showAdvanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_showAdvanced);
            if (_showAdvanced)
            {
                DrawAdvanced(trigger);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _validationOutdated = _hasValidation;
                _hasValidation = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCommandFields(PlayerSessionCommandTriggerBase trigger)
        {
            if (trigger is PlayerSessionJoinCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Control Scheme Hint");
                EditorGUILayout.PropertyField(
                    _controlScheme,
                    new GUIContent(
                        "Control Scheme Hint",
                        "Optional input hint forwarded to the Local Player Join request."));
                return;
            }

            if (trigger is PlayerSessionDefaultActorSelectionCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Actor Selection Requests");
                EditorGUILayout.PropertyField(
                    _actorSelectionRequests,
                    new GUIContent(
                        "Actor Selection Requests",
                        "Existing public selection authoring surface; it owns the selection command boundary."));
                FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
                EditorGUILayout.PropertyField(
                    _playerSlot,
                    new GUIContent(
                        "Player Slot Profile",
                        "Provides the typed Slot identity. The Actor remains the Slot configured default."));
                return;
            }

            if (trigger is PlayerSessionLeaveCommandTrigger)
            {
                FrameworkAuthoringInspectorGui.Section("Player Slot Profile");
                EditorGUILayout.PropertyField(
                    _playerSlot,
                    new GUIContent(
                        "Player Slot Profile",
                        "Exact Player target. At -1, the occurrence revision is read from the same scoped observation when invoked."));
            }
        }

        private void DrawValidation(PlayerSessionCommandTriggerBase trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Validation");
            if (GUILayout.Button("Validate"))
            {
                serializedObject.ApplyModifiedProperties();
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

        private void DrawAdvanced(PlayerSessionCommandTriggerBase trigger)
        {
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostic reason. The command name is used when left empty."));

            if (trigger is PlayerSessionDefaultActorSelectionCommandTrigger)
            {
                EditorGUILayout.PropertyField(
                    _expectedSelectionRevision,
                    new GUIContent(
                        "Expected Selection Revision",
                        "Use -1 when no optimistic revision check is required."));
            }

            if (trigger is PlayerSessionLeaveCommandTrigger)
            {
                EditorGUILayout.PropertyField(
                    _expectedLeaveOccurrenceRevision,
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
                    serializedObject.ApplyModifiedProperties();
                    trigger.Invoke();
                }
            }
        }

        private static string GetTitle(PlayerSessionCommandTriggerBase trigger)
        {
            return trigger switch
            {
                PlayerSessionOpenJoiningCommandTrigger => "OPEN JOINING COMMAND",
                PlayerSessionCloseJoiningCommandTrigger => "CLOSE JOINING COMMAND",
                PlayerSessionJoinCommandTrigger => "JOIN COMMAND",
                PlayerSessionDefaultActorSelectionCommandTrigger => "DEFAULT ACTOR SELECTION COMMAND",
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
                PlayerSessionDefaultActorSelectionCommandTrigger command when
                    command.LastActorSelectionResult != null =>
                    nameof(PlayerActorSelectionResult),
                PlayerSessionLeaveCommandTrigger command when
                    command.LastLeaveResult != null =>
                    nameof(SessionPlayerLeaveResult),
                _ => "None"
            };
        }

        private bool HasLiveTargets()
        {
            if (target == null || targets == null || targets.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
