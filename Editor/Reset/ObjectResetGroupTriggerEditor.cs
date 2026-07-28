using Immersive.Framework.Editor.Common;
using Immersive.Framework.ObjectReset;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Reset.Editor
{
    [CustomEditor(typeof(ObjectResetGroupTrigger))]
    internal sealed class ObjectResetGroupTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _groupId;
        private SerializedProperty _reason;
        private SerializedProperty _selection;
        private SerializedProperty _mode;
        private SerializedProperty _explicitSubjects;
        private bool _advanced;

        private void OnEnable()
        {
            _groupId = serializedObject.FindProperty("groupId");
            _reason = serializedObject.FindProperty("reason");
            _selection = serializedObject.FindProperty("selection");
            _mode = _selection?.FindPropertyRelative("mode");
            _explicitSubjects = _selection?.FindPropertyRelative("explicitSubjects");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var trigger = (ObjectResetGroupTrigger)target;

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Object Reset Group Trigger",
                "Requests one Reset operation for a configured selection of Reset Subjects.");
            FrameworkAuthoringInspectorGui.IntentSummary(BuildIntent());

            FrameworkAuthoringInspectorGui.Section("Identity");
            EditorGUILayout.PropertyField(_groupId, new GUIContent("Group ID"));
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_groupId.stringValue)))
            {
                if (GUILayout.Button("Generate ID"))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(serializedObject, _groupId,
                        FrameworkAuthoringSuggestionUtility.SuggestIdentity(target, "reset.group"), "Generate Reset Group ID");
                }
            }
            EditorGUILayout.HelpBox("The Group ID is stable authoring identity. It is not Source or Reason.", MessageType.None);

            FrameworkAuthoringInspectorGui.Section("Reset Selection");
            if (_selection == null)
            {
                EditorGUILayout.HelpBox("Invalid: the current ObjectResetGroupTrigger contract has no Reset Selection.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.PropertyField(_mode, new GUIContent("Selection Mode"));
                if (_mode != null && _mode.enumValueIndex == 0)
                {
                    EditorGUILayout.PropertyField(_explicitSubjects, new GUIContent("Explicit Subjects"), true);
                    EditorGUILayout.LabelField("Configured Subjects", _explicitSubjects.arraySize.ToString());
                }
                EditorGUILayout.PropertyField(_selection.FindPropertyRelative("allowNoSubjects"));
                EditorGUILayout.PropertyField(_selection.FindPropertyRelative("allowNoParticipants"));
                EditorGUILayout.PropertyField(_selection.FindPropertyRelative("stopOnFailure"));
            }

            FrameworkAuthoringInspectorGui.Section("Request Metadata");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", nameof(ObjectResetGroupTrigger));
            }
            EditorGUILayout.PropertyField(_reason, new GUIContent("Reason"));
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue)))
            {
                if (GUILayout.Button("Use Suggested Reason"))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(serializedObject, _reason,
                        FrameworkAuthoringSuggestionUtility.SuggestReason(target, "reset.group"), "Suggest Reset Group Reason");
                }
            }

            DrawConfigurationStatus();
            if (Application.isPlaying && targets.Length == 1)
            {
                FrameworkAuthoringInspectorGui.RuntimeBinding(trigger.ResetSelectionExecutionRuntimeBindingStatus,
                    trigger.ResetSelectionExecutionRuntimeBindingDiagnostic,
                    "Ensure this component is active under roots processed by the official Reset Scene Lifecycle composition.");
                DrawRuntimeEvidence(trigger);
                using (new EditorGUI.DisabledScope(!trigger.HasResetSelectionExecutionRuntimeBinding || trigger.IsRequestInFlight))
                {
                    if (GUILayout.Button(trigger.IsRequestInFlight ? "Group Reset In Progress" : "Request Group Reset"))
                    {
                        trigger.RequestObjectResetGroup();
                    }
                }
            }

            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced);
            if (_advanced)
            {
                EditorGUILayout.LabelField("Resolved Group ID", trigger.ResolvedGroupId);
                EditorGUILayout.LabelField("Resolved Reason", trigger.ResolvedReason);
                EditorGUILayout.LabelField("Raw Selection", trigger.Selection != null ? trigger.Selection.ToString() : "<missing>");
                EditorGUILayout.LabelField("Last Result", trigger.HasLastResult ? trigger.LastResult.ToString() : "<none>");
            }
            serializedObject.ApplyModifiedProperties();
        }

        private string BuildIntent()
        {
            if (_mode == null)
            {
                return "Configure a Reset Subject selection.";
            }

            if (_mode.enumValueIndex == 0)
            {
                return $"Reset {_explicitSubjects.arraySize} explicitly selected Subject(s) as one group.";
            }

            return "Reset Subjects resolved by " + _mode.enumDisplayNames[_mode.enumValueIndex] + ".";
        }

        private void DrawConfigurationStatus()
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            if (string.IsNullOrWhiteSpace(_groupId.stringValue))
            {
                EditorGUILayout.HelpBox("Incomplete: Group ID is empty. Generate or enter a stable Group ID.", MessageType.Error);
            }
            else if (_selection == null)
            {
                EditorGUILayout.HelpBox("Invalid: Reset Selection is missing from the serialized component contract.", MessageType.Error);
            }
            else if (_mode != null && _mode.enumValueIndex == 0 && _explicitSubjects.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Incomplete: Explicit Subjects is selected, but no Subject references are configured. Add at least one UnityResetSubjectAdapter reference.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Ready. Runtime subject registration remains a Play Mode concern.", MessageType.Info);
            }
        }

        private static void DrawRuntimeEvidence(ObjectResetGroupTrigger trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
            EditorGUILayout.LabelField("Request In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Status", trigger.LastResultStatus.ToString());
            EditorGUILayout.LabelField("Last Target Count", trigger.LastTargetCount.ToString());
            EditorGUILayout.LabelField("Participants", trigger.LastParticipantCount.ToString());
            EditorGUILayout.LabelField("Failed Participants", trigger.LastFailedParticipantCount.ToString());
            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                EditorGUILayout.HelpBox(trigger.LastMessage, trigger.LastRequestFailed ? MessageType.Error : MessageType.Info);
            }
        }
    }
}
