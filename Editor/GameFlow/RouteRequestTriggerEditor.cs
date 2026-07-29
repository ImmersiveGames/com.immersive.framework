using System;
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
        private bool _showAdvanced;
        private bool _hasValidationResult;
        private string _validationMessage;
        private MessageType _validationMessageType;

        private void OnEnable()
        {
            _targetRoute = serializedObject.FindProperty("targetRoute");
            _reason = serializedObject.FindProperty("reason");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RouteRequestTrigger trigger = (RouteRequestTrigger)target;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                _targetRoute,
                new GUIContent(
                    "Target Route",
                    "Authored Route requested when RequestRoute is invoked."));
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Request Reason",
                    "Optional diagnostic reason. When empty, the target Route name is used."));
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
                "route.request");
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
                    "Suggest Route Request Reason");
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

            if (_targetRoute.hasMultipleDifferentValues)
            {
                _validationMessage = "Selected triggers use different Target Routes.";
                _validationMessageType = MessageType.Warning;
                return;
            }

            if (_targetRoute.objectReferenceValue == null)
            {
                _validationMessage = "Target Route is required.";
                _validationMessageType = MessageType.Error;
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
                _validationMessage = "Configuration is valid. The Target Route name will be used as the runtime reason.";
                _validationMessageType = MessageType.Info;
                return;
            }

            _validationMessage = "Configuration is valid.";
            _validationMessageType = MessageType.Info;
        }

        private void DrawAdvanced(RouteRequestTrigger trigger)
        {
            FrameworkAuthoringInspectorGui.Section("Advanced / Debug");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", nameof(RouteRequestTrigger));
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
                trigger.RouteRuntimeBindingStatus,
                trigger.RouteRuntimeBindingDiagnostic,
                "Ensure this component is active under roots processed by the official Game Flow composition.");

            FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
            EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString());

            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                EditorGUILayout.HelpBox(
                    trigger.LastMessage,
                    trigger.LastRequestFailed ? MessageType.Error : MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(
                       !trigger.HasRouteRuntimeBinding || trigger.IsRequestInFlight))
            {
                if (GUILayout.Button("Request Route"))
                {
                    trigger.RequestRoute();
                }
            }
        }
    }
}
