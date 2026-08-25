using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ActivityVisibilityRule))]
    [CanEditMultipleObjects]
    internal sealed class ActivityVisibilityRuleEditor : UnityEditor.Editor
    {
        private SerializedProperty _activities;
        private SerializedProperty _matchMode;
        private SerializedProperty _noActiveActivityPolicy;
        private FrameworkAuthoringValidationReport _validationReport;

        private void OnEnable()
        {
            _activities = serializedObject.FindProperty("activities");
            _matchMode = serializedObject.FindProperty("matchMode");
            _noActiveActivityPolicy = serializedObject.FindProperty("noActiveActivityPolicy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.LabelField("Activity Visibility Rule", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_activities, new GUIContent("Activities", "Activities evaluated by this visibility rule."), true);
            EditorGUILayout.PropertyField(_matchMode, new GUIContent("Match Mode", "Whether a listed Activity makes this GameObject visible or hidden."));
            EditorGUILayout.PropertyField(_noActiveActivityPolicy, new GUIContent("No Active Activity", "Visibility while no Activity is active."));
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed) _validationReport = null;
            DrawImmediateIssues();
            DrawValidation();
        }

        private void DrawImmediateIssues()
        {
            if (serializedObject.isEditingMultipleObjects || !(target is ActivityVisibilityRule rule)) return;
            ActivityVisibilityEvaluation evaluation = rule.EvaluateVisibility(null);
            if (!evaluation.IsValid)
                EditorGUILayout.HelpBox($"Invalid Activity visibility rule: {evaluation.DiagnosticReason}.", MessageType.Error);
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Configuration")) RunValidation();
                GUILayout.Space(8f);
                EditorGUILayout.LabelField(GetValidationStatus(), EditorStyles.miniBoldLabel, GUILayout.Width(92f));
            }
            if (_validationReport != null && (_validationReport.ErrorCount > 0 || _validationReport.WarningCount > 0))
                FrameworkAuthoringValidationGui.DrawIssues(_validationReport, false);
        }

        private void RunValidation()
        {
            _validationReport = new FrameworkAuthoringValidationReport();
            for (int index = 0; index < targets.Length; index++)
                _validationReport.AddRange(FrameworkAuthoringValidator.ValidateActivityVisibilityRule(targets[index] as ActivityVisibilityRule));
        }

        private string GetValidationStatus()
        {
            if (_validationReport == null) return "Not Validated";
            if (_validationReport.ErrorCount > 0) return "Invalid";
            return _validationReport.WarningCount > 0 ? "Warning" : "Valid";
        }
    }
}
