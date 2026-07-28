using Immersive.Framework.Editor.Common;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.GameFlow
{
    [CustomEditor(typeof(ActivityRequestTrigger))]
    internal sealed class ActivityRequestTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetActivity;
        private SerializedProperty _reason;
        private bool _advanced;
        private void OnEnable() { _targetActivity = serializedObject.FindProperty("targetActivity"); _reason = serializedObject.FindProperty("reason"); }
        public override void OnInspectorGUI()
        {
            serializedObject.Update(); var trigger = (ActivityRequestTrigger)target;
            FrameworkAuthoringInspectorGui.ProductHeader("Activity Request Trigger", "Requests entry to one authored Activity, or clears the current Activity.");
            FrameworkAuthoringInspectorGui.IntentSummary(_targetActivity.objectReferenceValue == null ? "Choose an Activity to request, or use Clear Activity in Play Mode." : "Request the selected Activity.");
            FrameworkAuthoringInspectorGui.Section("Target / Intent"); EditorGUILayout.PropertyField(_targetActivity, new GUIContent("Target Activity"));
            FrameworkAuthoringInspectorGui.Section("Request Metadata"); using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Source", nameof(ActivityRequestTrigger)); EditorGUILayout.PropertyField(_reason);
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue))) if (GUILayout.Button("Use Suggested Reason")) FrameworkAuthoringInspectorGui.ApplySuggestion(serializedObject, _reason, FrameworkAuthoringSuggestionUtility.SuggestReason(target, "activity.request"), "Suggest Activity Request Reason");
            FrameworkAuthoringInspectorGui.Section("Configuration Status"); EditorGUILayout.HelpBox(_targetActivity.objectReferenceValue == null ? "Incomplete for Request Activity: assign a Target Activity. Clear Activity remains available in Play Mode." : "Ready.", _targetActivity.objectReferenceValue == null ? MessageType.Warning : MessageType.Info);
            if (Application.isPlaying && targets.Length == 1) { FrameworkAuthoringInspectorGui.RuntimeBinding(trigger.ActivityRuntimeBindingStatus, trigger.ActivityRuntimeBindingDiagnostic, "Ensure this component is active under roots processed by the official Game Flow composition."); FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence"); EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No"); EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString()); EditorGUILayout.LabelField("Last Operation", trigger.LastRequestClearedActivity ? "Clear Activity" : "Request Activity"); if (!string.IsNullOrWhiteSpace(trigger.LastMessage)) EditorGUILayout.HelpBox(trigger.LastMessage, trigger.LastRequestFailed ? MessageType.Error : MessageType.Info); using (new EditorGUI.DisabledScope(!trigger.HasActivityRuntimeBinding || trigger.IsRequestInFlight)) { if (GUILayout.Button("Request Activity")) trigger.RequestActivity(); if (GUILayout.Button("Clear Activity")) trigger.ClearActivity(); } }
            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced); if (_advanced) EditorGUILayout.LabelField("Last Reason", trigger.LastReason);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
