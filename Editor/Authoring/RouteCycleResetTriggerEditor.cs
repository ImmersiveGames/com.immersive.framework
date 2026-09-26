using Immersive.Framework.CycleReset;
using Immersive.Framework.Editor.Common;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(RouteCycleResetTrigger))]
    [CanEditMultipleObjects]
    internal sealed class RouteCycleResetTriggerEditor : UnityEditor.Editor
    {
        private static readonly GUIContent ReasonLabel = new GUIContent(
            "Reason",
            "Optional diagnostics reason. Keep it route/activity-cycle oriented. Avoid object/player/component wording; those reset levels are later phases.");

        private SerializedProperty _reason;
        private bool _showDiagnostics;

        private void OnEnable()
        {
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FrameworkAuthoringInspectorGui.ProductHeader("Route Cycle Reset Trigger", string.Empty);

            FrameworkAuthoringInspectorGui.Section("Configuration");
            EditorGUILayout.PropertyField(_reason, ReasonLabel);
            DrawReasonGuardrail(_reason);

            if (Application.isPlaying && targets.Length == 1)
            {
                var trigger = (RouteCycleResetTrigger)target;
                FrameworkAuthoringInspectorGui.RuntimeBinding(
                    trigger.RouteCycleResetRuntimeBindingStatus,
                    trigger.RouteCycleResetRuntimeBindingDiagnostic,
                    "Ensure this component is active under roots processed by the official Cycle Reset Scene Lifecycle composition.");
            }

            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDiagnostics()
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

            var trigger = target as RouteCycleResetTrigger;
            if (trigger == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime evidence is available in Play Mode.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
            EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString());
            if (!string.IsNullOrWhiteSpace(trigger.LastReason))
            {
                EditorGUILayout.LabelField("Last Reason", trigger.LastReason);
            }

            EditorGUILayout.HelpBox(trigger.LastResultSummary, ResolveRuntimeMessageType(trigger));
        }

        private static MessageType ResolveRuntimeMessageType(RouteCycleResetTrigger trigger)
        {
            if (trigger.LastRequestFailed)
            {
                return MessageType.Error;
            }

            if (trigger.LastRequestIgnored || trigger.LastResultCompletedWithWarnings)
            {
                return MessageType.Warning;
            }

            return MessageType.Info;
        }

        private static void DrawReasonGuardrail(SerializedProperty reason)
        {
            if (reason == null || reason.hasMultipleDifferentValues)
            {
                return;
            }

            var value = reason.stringValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (CycleResetTriggerAuthoringText.ContainsFutureResetVocabulary(value))
            {
                EditorGUILayout.HelpBox(
                    "The reason contains object/component/player/actor/pool/save/reload vocabulary. This trigger only requests Route Cycle Reset; use cycle-oriented wording to avoid confusing this with future local reset phases.",
                    MessageType.Warning);
            }
        }
    }
}
