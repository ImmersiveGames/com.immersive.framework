using Immersive.Framework.Editor.Common;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Pause
{
    [CustomEditor(typeof(PauseRequestTrigger))]
    internal sealed class PauseRequestTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _reason;
        private bool _advanced;

        private void OnEnable() => _reason = serializedObject.FindProperty("reason");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var trigger = (PauseRequestTrigger)target;
            FrameworkAuthoringInspectorGui.ProductHeader("Pause Request Trigger", "Requests logical Pause, Resume or Toggle through the current Pause runtime authority.");
            FrameworkAuthoringInspectorGui.IntentSummary("Pause gameplay through the current Pause runtime authority.");
            FrameworkAuthoringInspectorGui.Section("Request Metadata");
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextField("Source", nameof(PauseRequestTrigger));
            EditorGUILayout.PropertyField(_reason, new GUIContent("Reason"));
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue)))
            {
                if (GUILayout.Button("Use Suggested Reason"))
                    FrameworkAuthoringInspectorGui.ApplySuggestion(serializedObject, _reason, FrameworkAuthoringSuggestionUtility.SuggestReason(target, "pause"), "Suggest Pause Reason");
            }
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(_reason.stringValue) ? "Ready. A public Pause action supplies its operation reason when this optional diagnostic reason is empty." : "Ready.", MessageType.Info);
            if (Application.isPlaying && targets.Length == 1)
            {
                FrameworkAuthoringInspectorGui.RuntimeBinding(trigger.ProductRequestBindingStatus, trigger.ProductRequestBindingDiagnostic, "Ensure this component is active under roots processed by the official Pause Scene Lifecycle composition.");
                FrameworkAuthoringInspectorGui.Section("Effective Pause Evidence");
                EditorGUILayout.LabelField("Requested State", trigger.LastStatus.ToString());
                EditorGUILayout.LabelField("Effective State", trigger.LastCurrentState.ToString());
                EditorGUILayout.LabelField("Product Status", trigger.LastProductStatus);
                EditorGUILayout.LabelField("Execution Mode", trigger.LastExecutionMode);
                if (!string.IsNullOrWhiteSpace(trigger.LastMessage)) EditorGUILayout.HelpBox(trigger.LastMessage, trigger.LastRequestFailed ? MessageType.Error : MessageType.Info);
                using (new EditorGUI.DisabledScope(!trigger.HasPauseProductRequestBinding))
                {
                    if (GUILayout.Button("Request Pause")) trigger.RequestPause();
                    if (GUILayout.Button("Request Resume")) trigger.RequestResume();
                    if (GUILayout.Button("Request Toggle Pause")) trigger.TogglePause();
                }
            }
            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced);
            if (_advanced) EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString());
            serializedObject.ApplyModifiedProperties();
        }
    }
}
