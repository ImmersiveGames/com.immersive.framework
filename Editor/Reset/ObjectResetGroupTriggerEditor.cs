using Immersive.Framework.Editor.Common;
using Immersive.Framework.ObjectReset;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Reset
{
    [CustomEditor(typeof(ObjectResetGroupTrigger))]
    internal sealed class ObjectResetGroupTriggerEditor : UnityEditor.Editor
    {
        private static readonly GUIContent GroupIdLabel = new GUIContent(
            "Group ID",
            "Stable authoring identity for this group request. It is not Source or Reason.");
        private static readonly GUIContent ReasonLabel = new GUIContent(
            "Reason",
            "Optional diagnostics reason for this Object Reset Group request.");

        private SerializedProperty _groupId;
        private SerializedProperty _reason;
        private SerializedProperty _selection;
        private bool _showAdvanced;
        private bool _showDiagnostics;

        private void OnEnable()
        {
            _groupId = serializedObject.FindProperty("groupId");
            _reason = serializedObject.FindProperty("reason");
            _selection = serializedObject.FindProperty("selection");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var trigger = (ObjectResetGroupTrigger)target;

            FrameworkAuthoringInspectorGui.ProductHeader("Object Reset Group Trigger", string.Empty);

            DrawConfiguration();
            DrawConfigurationStatus(trigger);
            DrawAdvanced();
            DrawDiagnostics(trigger);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section("Configuration");

            EditorGUILayout.PropertyField(_groupId, GroupIdLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_groupId.stringValue)))
            {
                if (GUILayout.Button("Generate ID"))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(
                        serializedObject,
                        _groupId,
                        FrameworkAuthoringSuggestionUtility.SuggestIdentity(target, "reset.group"),
                        "Generate Reset Group ID");
                }
            }

            if (_selection == null)
            {
                EditorGUILayout.HelpBox(
                    "Invalid: the current ObjectResetGroupTrigger contract has no Reset Selection.",
                    MessageType.Error);
            }
            else
            {
                ResetSelectionConfigEditorGui.DrawSelection(_selection);
            }

            EditorGUILayout.PropertyField(_reason, ReasonLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue)))
            {
                if (GUILayout.Button("Use Suggested Reason"))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(
                        serializedObject,
                        _reason,
                        FrameworkAuthoringSuggestionUtility.SuggestReason(target, "reset.group"),
                        "Suggest Reset Group Reason");
                }
            }
        }

        private void DrawConfigurationStatus(ObjectResetGroupTrigger trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");

            bool ready = !string.IsNullOrWhiteSpace(_groupId.stringValue) &&
                _selection != null &&
                !(ResetSelectionConfigEditorGui.IsExplicitSelectionEmpty(_selection) &&
                  !ResetSelectionConfigEditorGui.AllowsNoSubjects(_selection));
            FrameworkAuthoringInspectorGui.Status(ready ? "Ready" : "Incomplete");

            if (!ready)
            {
                string message = string.IsNullOrWhiteSpace(_groupId.stringValue)
                    ? "Group ID is empty. Generate or enter a stable Group ID."
                    : _selection == null
                        ? "Reset Selection is missing from the serialized component contract."
                        : "Explicit Subjects is selected, but no Subject references are configured and Allow No Subjects is disabled.";
                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }

            if (Application.isPlaying && targets.Length == 1)
            {
                EditorGUILayout.LabelField(
                    "Runtime",
                    trigger.HasResetSelectionExecutionRuntimeBinding ? "Bound" : "Not bound");
            }
        }

        private void DrawAdvanced()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (!_showAdvanced || _selection == null)
            {
                return;
            }

            ResetSelectionConfigEditorGui.DrawAdvanced(_selection);
        }

        private void DrawDiagnostics(ObjectResetGroupTrigger trigger)
        {
            _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics, "Diagnostics", true);
            if (!_showDiagnostics)
            {
                return;
            }

            if (targets.Length != 1)
            {
                EditorGUILayout.HelpBox("Diagnostics are shown for single-object selection only.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Resolved Group ID", trigger.ResolvedGroupId);
            EditorGUILayout.LabelField("Resolved Reason", trigger.ResolvedReason);
            EditorGUILayout.LabelField("Raw Selection", trigger.Selection != null ? trigger.Selection.ToString() : "<missing>");

            if (!Application.isPlaying)
            {
                return;
            }

            FrameworkAuthoringInspectorGui.RuntimeBinding(
                trigger.ResetSelectionExecutionRuntimeBindingStatus,
                trigger.ResetSelectionExecutionRuntimeBindingDiagnostic,
                "Ensure this component is active under roots processed by the official Reset Scene Lifecycle composition.");

            FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
            EditorGUILayout.LabelField("Request In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Target Count", trigger.LastTargetCount.ToString());
            EditorGUILayout.HelpBox(
                trigger.LastResultSummary,
                trigger.LastRequestFailed ? MessageType.Error : trigger.LastRequestIgnored ? MessageType.Warning : MessageType.Info);

            FrameworkAuthoringInspectorGui.Section("Runtime Test");
            using (new EditorGUI.DisabledScope(!trigger.HasResetSelectionExecutionRuntimeBinding || trigger.IsRequestInFlight))
            {
                if (GUILayout.Button(trigger.IsRequestInFlight ? "Group Reset In Progress" : "Request Group Reset"))
                {
                    trigger.RequestObjectResetGroup();
                }
            }
        }
    }
}
