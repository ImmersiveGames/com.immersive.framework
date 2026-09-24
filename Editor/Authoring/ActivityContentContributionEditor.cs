using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Editor.Common;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ActivityContentContribution))]
    [CanEditMultipleObjects]
    internal sealed class ActivityContentContributionEditor : UnityEditor.Editor
    {
        private SerializedProperty _activity;
        private SerializedProperty _localContentId;
        private SerializedProperty _requiredness;
        private FrameworkAuthoringValidationReport _validationReport;

        private void OnEnable()
        {
            _activity = serializedObject.FindProperty("activity");
            _localContentId = serializedObject.FindProperty("localContentId");
            _requiredness = serializedObject.FindProperty("requiredness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.LabelField("Activity Content Contribution", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_activity, new GUIContent("Activity", "Activity scope that owns this content boundary."));
            EditorGUILayout.PropertyField(_localContentId, new GUIContent("Local Content Id", "Stable explicit identity. GameObject names and hierarchy paths are diagnostics only."));
            DrawSuggestedIdentityAction();
            EditorGUILayout.PropertyField(_requiredness, new GUIContent("Requiredness", "Whether contribution consumers treat this boundary as required or optional."));
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed) _validationReport = null;
            DrawImmediateIssues();
            DrawValidation();
        }

        private void DrawSuggestedIdentityAction()
        {
            if (targets.Length != 1 || _localContentId == null || !string.IsNullOrWhiteSpace(_localContentId.stringValue)) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Use Suggested Id", "Creates an explicit local content identity from the selected GameObject."), EditorStyles.miniButton, GUILayout.Width(112f)))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(serializedObject, _localContentId, FrameworkAuthoringSuggestionUtility.SuggestIdentity(target, "activity.local-content"), "Suggest Activity Local Content Id");
                    _validationReport = null;
                }
            }
        }

        private void DrawImmediateIssues()
        {
            if (_activity != null && !_activity.hasMultipleDifferentValues && _activity.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Activity is missing.", MessageType.Error);
            if (_localContentId != null && !_localContentId.hasMultipleDifferentValues && string.IsNullOrWhiteSpace(_localContentId.stringValue))
                EditorGUILayout.HelpBox("Local Content Id is missing.", MessageType.Error);
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
                _validationReport.AddRange(FrameworkAuthoringValidator.ValidateActivityContentContribution(targets[index] as ActivityContentContribution));
        }

        private string GetValidationStatus()
        {
            if (_validationReport == null) return "Not Validated";
            if (_validationReport.ErrorCount > 0) return "Invalid";
            return _validationReport.WarningCount > 0 ? "Warning" : "Valid";
        }
    }
}
