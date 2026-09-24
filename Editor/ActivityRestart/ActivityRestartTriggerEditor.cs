using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Editor.Common;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.ActivityRestart
{
    [CustomEditor(typeof(ActivityRestartTrigger))]
    internal sealed class ActivityRestartTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetActivity;
        private SerializedProperty _useCurrent;
        private SerializedProperty _requireCurrent;
        private SerializedProperty _reason;
        private SerializedProperty _resetSelection;
        private bool _advanced;

        private void OnEnable()
        {
            _targetActivity = serializedObject.FindProperty("targetActivity");
            _useCurrent = serializedObject.FindProperty("useCurrentActivityWhenTargetMissing");
            _requireCurrent = serializedObject.FindProperty("requireTargetActivityIsCurrent");
            _reason = serializedObject.FindProperty("reason");
            _resetSelection = serializedObject.FindProperty("resetSelection");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var trigger = (ActivityRestartTrigger)target;
            FrameworkAuthoringInspectorGui.ProductHeader("Activity Restart Trigger", "Resets selected Activity state, then performs Activity clear and reentry.");
            FrameworkAuthoringInspectorGui.IntentSummary(_targetActivity.objectReferenceValue == null ? "Restart the currently active Activity after resetting its Activity Subjects." : "Restart the selected Activity after resetting its configured Subjects.");
            FrameworkAuthoringInspectorGui.Section("Activity Target");
            EditorGUILayout.PropertyField(_targetActivity);
            EditorGUILayout.PropertyField(_useCurrent, new GUIContent("Use Current Activity When Target Missing"));
            EditorGUILayout.PropertyField(_requireCurrent, new GUIContent("Require Target Activity Is Current"));
            FrameworkAuthoringInspectorGui.Section("Reset Selection");
            EditorGUILayout.PropertyField(_resetSelection, true);
            FrameworkAuthoringInspectorGui.Section("Request Metadata");
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Source", nameof(ActivityRestartTrigger));
            EditorGUILayout.PropertyField(_reason);
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue)))
                if (GUILayout.Button("Use Suggested Reason")) FrameworkAuthoringInspectorGui.ApplySuggestion(serializedObject, _reason, FrameworkAuthoringSuggestionUtility.SuggestReason(target, "activity.restart"), "Suggest Activity Restart Reason");
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            EditorGUILayout.HelpBox(_targetActivity.objectReferenceValue == null && !_useCurrent.boolValue ? "Invalid: assign an Activity target or enable Use Current Activity When Target Missing." : "Ready.", _targetActivity.objectReferenceValue == null && !_useCurrent.boolValue ? MessageType.Error : MessageType.Info);
            if (Application.isPlaying && targets.Length == 1)
            {
                FrameworkAuthoringInspectorGui.RuntimeBinding(trigger.ActivityRestartRuntimeBindingStatus, trigger.ActivityRestartRuntimeBindingDiagnostic, "Ensure this component is active under roots processed by the official Activity Restart Scene Lifecycle composition.");
                FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
                EditorGUILayout.LabelField("Invocations", trigger.InvocationCount.ToString());
                EditorGUILayout.LabelField("Accepted / Rejected", trigger.AcceptedRequestCount + " / " + trigger.RejectedRequestCount);
                EditorGUILayout.LabelField("Request In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
                EditorGUILayout.LabelField("Last Status", trigger.LastResultStatus.ToString());
                if (!string.IsNullOrWhiteSpace(trigger.LastDiagnostic)) EditorGUILayout.HelpBox(trigger.LastDiagnostic, trigger.LastRequestFailed ? MessageType.Error : MessageType.Info);
                using (new EditorGUI.DisabledScope(!trigger.HasActivityRestartRuntimeBinding || trigger.IsRequestInFlight)) if (GUILayout.Button(trigger.IsRequestInFlight ? "Activity Restart In Progress" : "Request Activity Restart")) trigger.RequestActivityRestart();
            }
            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced);
            if (_advanced) { EditorGUILayout.LabelField("Raw Reset Result", trigger.HasLastResult ? trigger.LastResetExecutionResult.ToString() : "<none>"); EditorGUILayout.LabelField("Raw Activity Result", trigger.HasLastResult ? trigger.LastResult.ToString() : "<none>"); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
