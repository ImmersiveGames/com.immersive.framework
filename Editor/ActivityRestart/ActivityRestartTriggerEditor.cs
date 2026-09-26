using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Reset;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.ActivityRestart
{
    [CustomEditor(typeof(ActivityRestartTrigger))]
    internal sealed class ActivityRestartTriggerEditor : UnityEditor.Editor
    {
        private static readonly GUIContent UseCurrentLabel = new GUIContent(
            "Use Current When Target Missing",
            "When Target Activity is empty, restart the currently active Activity instead of failing.");
        private static readonly GUIContent RequireCurrentLabel = new GUIContent(
            "Require Target Is Current",
            "Rejects the request if Target Activity is assigned but is not the currently active Activity.");
        private static readonly GUIContent ReasonLabel = new GUIContent(
            "Reason",
            "Optional diagnostics reason for this Activity Restart request.");

        private SerializedProperty _targetActivity;
        private SerializedProperty _useCurrent;
        private SerializedProperty _requireCurrent;
        private SerializedProperty _reason;
        private SerializedProperty _resetSelection;
        private bool _showAdvanced;
        private bool _showDiagnostics;

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

            FrameworkAuthoringInspectorGui.ProductHeader("Activity Restart Trigger", string.Empty);

            DrawConfiguration();
            DrawConfigurationStatus(trigger);
            DrawAdvanced();
            DrawDiagnostics(trigger);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section("Activity Target");
            EditorGUILayout.PropertyField(_targetActivity);
            EditorGUILayout.PropertyField(_useCurrent, UseCurrentLabel);
            EditorGUILayout.PropertyField(_requireCurrent, RequireCurrentLabel);

            FrameworkAuthoringInspectorGui.Section("Reset Selection");
            if (_resetSelection == null)
            {
                EditorGUILayout.HelpBox(
                    "Invalid: the current ActivityRestartTrigger contract has no Reset Selection.",
                    MessageType.Error);
            }
            else
            {
                ResetSelectionConfigEditorGui.DrawSelection(_resetSelection);
            }

            FrameworkAuthoringInspectorGui.Section("Reason");
            EditorGUILayout.PropertyField(_reason, ReasonLabel);
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue)))
            {
                if (GUILayout.Button("Use Suggested Reason"))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(
                        serializedObject,
                        _reason,
                        FrameworkAuthoringSuggestionUtility.SuggestReason(target, "activity.restart"),
                        "Suggest Activity Restart Reason");
                }
            }
        }

        private void DrawConfigurationStatus(ActivityRestartTrigger trigger)
        {
            bool hasTarget = _targetActivity.objectReferenceValue != null || _useCurrent.boolValue;

            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            FrameworkAuthoringInspectorGui.Status(hasTarget ? "Ready" : "Incomplete");
            if (!hasTarget)
            {
                EditorGUILayout.HelpBox(
                    "Assign a Target Activity or enable Use Current When Target Missing.",
                    MessageType.Error);
            }

            if (Application.isPlaying && targets.Length == 1)
            {
                EditorGUILayout.LabelField("Runtime", trigger.HasActivityRestartRuntimeBinding ? "Bound" : "Not bound");
            }
        }

        private void DrawAdvanced()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (!_showAdvanced || _resetSelection == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Reset Selection Details", EditorStyles.miniBoldLabel);
            ResetSelectionConfigEditorGui.DrawAdvanced(_resetSelection);
        }

        private void DrawDiagnostics(ActivityRestartTrigger trigger)
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

            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime evidence is available in Play Mode.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            FrameworkAuthoringInspectorGui.RuntimeBinding(
                trigger.ActivityRestartRuntimeBindingStatus,
                trigger.ActivityRestartRuntimeBindingDiagnostic,
                "Ensure this component is active under roots processed by the official Activity Restart Scene Lifecycle composition.");

            FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
            EditorGUILayout.LabelField("Invocations", trigger.InvocationCount.ToString());
            EditorGUILayout.LabelField("Accepted / Rejected", trigger.AcceptedRequestCount + " / " + trigger.RejectedRequestCount);
            EditorGUILayout.LabelField("Request In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.HelpBox(trigger.LastResultSummary, trigger.LastRequestFailed ? MessageType.Error : MessageType.Info);

            FrameworkAuthoringInspectorGui.Section("Runtime Test");
            using (new EditorGUI.DisabledScope(!trigger.HasActivityRestartRuntimeBinding || trigger.IsRequestInFlight))
            {
                if (GUILayout.Button(trigger.IsRequestInFlight ? "Activity Restart In Progress" : "Request Activity Restart"))
                {
                    trigger.RequestActivityRestart();
                }
            }
        }
    }
}
