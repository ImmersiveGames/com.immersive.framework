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
        private bool _showAdvanced;
        private bool _hasValidationResult;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _targetActivity = serializedObject.FindProperty("targetActivity");
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ActivityRequestTrigger trigger = (ActivityRequestTrigger)target;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                _targetActivity,
                new GUIContent(
                    "Target Activity",
                    "Authored Activity requested when RequestActivity is invoked."));
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Request Reason",
                    "Optional diagnostic reason. When empty, the target Activity name is used."));
            bool authoringChanged = EditorGUI.EndChangeCheck();

            if (authoringChanged)
            {
                _hasValidationResult = false;
            }

            DrawSuggestedReasonAction();
            DrawPrimaryActions();

            if (_hasValidationResult)
            {
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);
            }

            if (_showAdvanced)
            {
                DrawAdvanced(trigger);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSuggestedReasonAction()
        {
            if (targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue))
            {
                return;
            }

            string suggestedReason = FrameworkAuthoringSuggestionUtility.SuggestReason(
                target,
                "activity.request");
            if (string.IsNullOrWhiteSpace(suggestedReason))
            {
                return;
            }

            if (GUILayout.Button("Use Suggested Reason"))
            {
                FrameworkAuthoringInspectorGui.ApplySuggestion(
                    serializedObject,
                    _reason,
                    suggestedReason,
                    "Suggest Activity Request Reason");
                _hasValidationResult = false;
            }
        }

        private void DrawPrimaryActions()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    ValidateConfiguration();
                }

                string advancedLabel = _showAdvanced
                    ? "Hide Advanced / Debug"
                    : "Advanced / Debug";
                if (GUILayout.Button(advancedLabel))
                {
                    _showAdvanced = !_showAdvanced;
                }
            }
        }

        private void ValidateConfiguration()
        {
            _hasValidationResult = true;

            if (_targetActivity.hasMultipleDifferentValues)
            {
                _validationMessage = "Selected triggers use different Target Activities.";
                _validationMessageType = MessageType.Warning;
                return;
            }

            if (_targetActivity.objectReferenceValue == null)
            {
                _validationMessage = "Target Activity is not assigned. Request Activity will fail, but Clear Activity remains available in Play Mode.";
                _validationMessageType = MessageType.Warning;
                return;
            }

            if (_reason.hasMultipleDifferentValues)
            {
                _validationMessage = "Configuration is valid. Selected triggers use different Request Reasons.";
                _validationMessageType = MessageType.Info;
                return;
            }

            if (string.IsNullOrWhiteSpace(_reason.stringValue))
            {
                _validationMessage = "Configuration is valid. The Target Activity name will be used as the runtime reason.";
                _validationMessageType = MessageType.Info;
                return;
            }

            _validationMessage = "Configuration is valid.";
            _validationMessageType = MessageType.Info;
        }

        private void DrawAdvanced(ActivityRequestTrigger trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Advanced / Debug");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", nameof(ActivityRequestTrigger));
            }

            if (!string.IsNullOrWhiteSpace(trigger.LastReason))
            {
                EditorGUILayout.LabelField("Last Reason", trigger.LastReason);
            }

            if (!Application.isPlaying || targets.Length != 1)
            {
                EditorGUILayout.HelpBox(
                    "Runtime evidence is available in Play Mode for a single selected trigger.",
                    MessageType.None);
                return;
            }

            FrameworkAuthoringInspectorGui.RuntimeBinding(
                trigger.ActivityRuntimeBindingStatus,
                trigger.ActivityRuntimeBindingDiagnostic,
                "Ensure this component is active under roots processed by the official Game Flow composition.");

            FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
            EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString());
            EditorGUILayout.LabelField(
                "Last Operation",
                trigger.LastRequestClearedActivity ? "Clear Activity" : "Request Activity");

            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                EditorGUILayout.HelpBox(
                    trigger.LastMessage,
                    trigger.LastRequestFailed ? MessageType.Error : MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(
                       !trigger.HasActivityRuntimeBinding || trigger.IsRequestInFlight))
            {
                if (GUILayout.Button("Request Activity"))
                {
                    trigger.RequestActivity();
                }

                if (GUILayout.Button("Clear Activity"))
                {
                    trigger.ClearActivity();
                }
            }
        }
    }
}
