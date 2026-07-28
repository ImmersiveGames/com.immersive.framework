using Immersive.Framework.Editor.Common;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.GameFlow
{
    [CustomEditor(typeof(RouteRequestTrigger))]
    internal sealed class RouteRequestTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetRoute;
        private SerializedProperty _reason;
        private bool _advanced;
        private void OnEnable() { _targetRoute = serializedObject.FindProperty("targetRoute"); _reason = serializedObject.FindProperty("reason"); }
        public override void OnInspectorGUI()
        {
            serializedObject.Update(); var trigger = (RouteRequestTrigger)target;
            FrameworkAuthoringInspectorGui.ProductHeader("Route Request Trigger", "Requests navigation to one authored Route.");
            FrameworkAuthoringInspectorGui.IntentSummary(_targetRoute.objectReferenceValue == null ? "Choose a Route to request." : "Request the selected Route.");
            FrameworkAuthoringInspectorGui.Section("Target / Intent"); EditorGUILayout.PropertyField(_targetRoute, new GUIContent("Target Route"));
            FrameworkAuthoringInspectorGui.Section("Request Metadata"); using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Source", nameof(RouteRequestTrigger)); EditorGUILayout.PropertyField(_reason);
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue))) if (GUILayout.Button("Use Suggested Reason")) FrameworkAuthoringInspectorGui.ApplySuggestion(serializedObject, _reason, FrameworkAuthoringSuggestionUtility.SuggestReason(target, "route.request"), "Suggest Route Request Reason");
            FrameworkAuthoringInspectorGui.Section("Configuration Status"); EditorGUILayout.HelpBox(_targetRoute.objectReferenceValue == null ? "Incomplete: assign a Target Route." : "Ready.", _targetRoute.objectReferenceValue == null ? MessageType.Error : MessageType.Info);
            if (Application.isPlaying && targets.Length == 1) { FrameworkAuthoringInspectorGui.RuntimeBinding(trigger.RouteRuntimeBindingStatus, trigger.RouteRuntimeBindingDiagnostic, "Ensure this component is active under roots processed by the official Game Flow composition."); FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence"); EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No"); EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString()); if (!string.IsNullOrWhiteSpace(trigger.LastMessage)) EditorGUILayout.HelpBox(trigger.LastMessage, trigger.LastRequestFailed ? MessageType.Error : MessageType.Info); using (new EditorGUI.DisabledScope(!trigger.HasRouteRuntimeBinding || trigger.IsRequestInFlight)) if (GUILayout.Button("Request Route")) trigger.RequestRoute(); }
            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced); if (_advanced) EditorGUILayout.LabelField("Last Reason", trigger.LastReason);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
