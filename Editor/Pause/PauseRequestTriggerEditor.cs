using Immersive.Framework.Editor.Common;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Pause
{
    [CustomEditor(typeof(PauseRequestTrigger))]
    internal sealed class PauseRequestTriggerEditor : UnityEditor.Editor
    {
        private static readonly GUIContent ReasonLabel =
            new GUIContent(
                "Reason",
                "Optional diagnostic reason. When empty, the operation reason (pause.pause, pause.resume or pause.toggle) is used.");

        private SerializedProperty _reason;
        private bool _advanced;

        private void OnEnable() => _reason = serializedObject.FindProperty("reason");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var trigger = (PauseRequestTrigger)target;
            bool runtimeEvidence = Application.isPlaying && targets.Length == 1;

            FrameworkAuthoringInspectorGui.ProductHeader("Pause Request Trigger", string.Empty);

            FrameworkAuthoringInspectorGui.Section("Configuration");
            EditorGUILayout.PropertyField(_reason, ReasonLabel);
            DrawSuggestedReasonAction();

            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            FrameworkAuthoringInspectorGui.Status("Ready");

            if (runtimeEvidence)
            {
                DrawRuntimeStatus(trigger);
            }

            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced);
            if (_advanced)
            {
                DrawAdvanced(trigger, runtimeEvidence);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSuggestedReasonAction()
        {
            if (targets.Length != 1 ||
                _reason.hasMultipleDifferentValues ||
                !string.IsNullOrWhiteSpace(_reason.stringValue))
            {
                return;
            }

            if (GUILayout.Button("Use Suggested Reason"))
            {
                FrameworkAuthoringInspectorGui.ApplySuggestion(
                    serializedObject,
                    _reason,
                    FrameworkAuthoringSuggestionUtility.SuggestReason(target, "pause"),
                    "Suggest Pause Reason");
            }
        }

        private static void DrawRuntimeStatus(PauseRequestTrigger trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Runtime Status");
            EditorGUILayout.LabelField("Binding", trigger.ProductRequestBindingStatus);

            if (!trigger.HasPauseProductRequestBinding)
            {
                EditorGUILayout.HelpBox(
                    "Not bound to Pause. Place this trigger under roots processed by the Pause Scene Lifecycle composition.",
                    MessageType.Warning);
            }
        }

        private static void DrawAdvanced(PauseRequestTrigger trigger, bool runtimeEvidence)
        {
            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", nameof(PauseRequestTrigger));
            }

            if (!runtimeEvidence)
            {
                EditorGUI.indentLevel--;
                return;
            }

            FrameworkAuthoringInspectorGui.Section("Runtime Binding");
            EditorGUILayout.LabelField("Status", trigger.ProductRequestBindingStatus);
            EditorGUILayout.LabelField(
                "Pause State",
                trigger.HasPauseProductRequestBinding &&
                trigger.TryGetPauseSnapshot(out PauseSnapshot snapshot)
                    ? snapshot.State.ToString()
                    : "Unavailable");
            if (!string.IsNullOrWhiteSpace(trigger.ProductRequestBindingDiagnostic))
            {
                EditorGUILayout.LabelField(
                    trigger.ProductRequestBindingDiagnostic,
                    EditorStyles.wordWrappedMiniLabel);
            }

            FrameworkAuthoringInspectorGui.Section("Last Request");
            EditorGUILayout.LabelField("Outcome", trigger.LastOutcome.ToString());
            EditorGUILayout.LabelField("Reason", string.IsNullOrWhiteSpace(trigger.LastReason) ? "<none>" : trigger.LastReason);
            EditorGUILayout.LabelField("Pause Status", trigger.LastStatus.ToString());
            EditorGUILayout.LabelField("Previous State", trigger.LastPreviousState.ToString());
            EditorGUILayout.LabelField("Current State", trigger.LastCurrentState.ToString());
            EditorGUILayout.LabelField("Product Status", trigger.LastProductStatus);
            EditorGUILayout.LabelField("Execution Mode", trigger.LastExecutionMode);
            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                EditorGUILayout.LabelField(
                    trigger.LastMessage,
                    EditorStyles.wordWrappedMiniLabel);
            }

            FrameworkAuthoringInspectorGui.Section("Runtime Test");
            using (new EditorGUI.DisabledScope(!trigger.HasPauseProductRequestBinding))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pause"))
                {
                    trigger.RequestPause();
                }

                if (GUILayout.Button("Resume"))
                {
                    trigger.RequestResume();
                }

                if (GUILayout.Button("Toggle"))
                {
                    trigger.TogglePause();
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
