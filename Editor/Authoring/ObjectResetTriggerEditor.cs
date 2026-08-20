using Immersive.Framework.Editor.Common;
using Immersive.Framework.ObjectReset;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ObjectResetTrigger))]
    [CanEditMultipleObjects]
    internal sealed class ObjectResetTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _targetSubject;
        private SerializedProperty _reason;
        private SerializedProperty _allowNoParticipants;
        private SerializedProperty _stopOnFailure;
        private bool _advanced;

        private void OnEnable()
        {
            _targetSubject = serializedObject.FindProperty("targetSubject");
            _reason = serializedObject.FindProperty("reason");
            _allowNoParticipants = serializedObject.FindProperty("allowNoParticipants");
            _stopOnFailure = serializedObject.FindProperty("stopOnFailure");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FrameworkAuthoringInspectorGui.ProductHeader(
                "Object Reset Trigger",
                "Requests Reset for one authored Reset Subject.");

            FrameworkAuthoringInspectorGui.Section("Target / Intent");

            if (_targetSubject != null)
            {
                EditorGUILayout.PropertyField(
                    _targetSubject,
                    new GUIContent(
                        "Target Subject",
                        "Assign a UnityResetSubjectAdapter or provide an explicit ResetSubjectId text."),
                    includeChildren: true);
            }

            serializedObject.ApplyModifiedProperties();
            ObjectResetTargetAuthoringValidationResult targetValidation =
                ObjectResetTargetAuthoringValidator.Validate(_targetSubject);
            FrameworkAuthoringInspectorGui.IntentSummary(BuildIntentSummary(targetValidation));
            EditorGUILayout.HelpBox(
                targetValidation.HasAdapter
                    ? "An assigned Reset Subject Adapter is the authored target. A direct Reset Subject ID is optional and follows the runtime precedence shown below."
                    : "Use either an authored Reset Subject Adapter or a direct authored Reset Subject ID.",
                MessageType.None);

            FrameworkAuthoringInspectorGui.Section("Request Metadata");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", nameof(ObjectResetTrigger));
            }
            EditorGUILayout.PropertyField(
                _reason,
                new GUIContent(
                    "Reason",
                    "Optional diagnostics reason for this Object Reset request."));
            using (new EditorGUI.DisabledScope(targets.Length != 1 || !string.IsNullOrWhiteSpace(_reason.stringValue)))
            {
                if (GUILayout.Button("Use Suggested Reason"))
                {
                    FrameworkAuthoringInspectorGui.ApplySuggestion(
                        serializedObject,
                        _reason,
                        FrameworkAuthoringSuggestionUtility.SuggestReason(target, "reset.object"),
                        "Suggest Object Reset Reason");
                }
            }

            FrameworkAuthoringInspectorGui.Section("Execution Policy");
            EditorGUILayout.PropertyField(
                _allowNoParticipants,
                new GUIContent(
                    "Allow No Participants",
                    "When enabled, a selected ResetSubject with no participants succeeds as SucceededNoParticipants."));

            EditorGUILayout.PropertyField(
                _stopOnFailure,
                new GUIContent(
                    "Stop On Failure",
                    "Stops execution after the first blocking failure inside this single-subject request."));

            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            EditorGUILayout.HelpBox(
                targetValidation.IsValid
                    ? "Ready. " + targetValidation.Message
                    : "Incomplete. " + targetValidation.Message,
                targetValidation.IsValid ? MessageType.Info : MessageType.Error);

            DrawRuntimeResult();

            _advanced = FrameworkAuthoringInspectorGui.AdvancedFoldout(_advanced);
            if (_advanced && targets.Length == 1)
            {
                var trigger = (ObjectResetTrigger)target;
                EditorGUILayout.LabelField("Binding Diagnostic", trigger.ResetExecutionRuntimeBindingDiagnostic);
                DrawAdvancedTargetEvidence(trigger);
                EditorGUILayout.LabelField("Raw Last Result", trigger.HasLastResult ? trigger.LastResult.ToString() : "<none>");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRuntimeResult()
        {
            if (!Application.isPlaying || targets.Length != 1)
            {
                return;
            }

            var trigger = target as ObjectResetTrigger;
            if (trigger == null)
            {
                return;
            }

            FrameworkAuthoringInspectorGui.RuntimeBinding(
                trigger.ResetExecutionRuntimeBindingStatus,
                trigger.ResetExecutionRuntimeBindingDiagnostic,
                "Ensure this component is active under roots processed by the official Reset Scene Lifecycle composition.");
            DrawRuntimeTargetReadiness(trigger);
            FrameworkAuthoringInspectorGui.Section("Runtime Request Evidence");
            EditorGUILayout.LabelField("In Flight", trigger.IsRequestInFlight ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Phase", trigger.LastEventPhase.ToString());
            EditorGUILayout.LabelField("Last Outcome", trigger.LastOutcome.ToString());
            EditorGUILayout.LabelField("Last Result Status", trigger.LastResultStatus.ToString());

            if (!string.IsNullOrWhiteSpace(trigger.LastReason))
            {
                EditorGUILayout.LabelField("Last Reason", trigger.LastReason);
            }

            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                EditorGUILayout.HelpBox(trigger.LastMessage, ResolveRuntimeMessageType(trigger));
            }

            if (trigger.HasLastResult)
            {
                EditorGUILayout.LabelField("Participants", trigger.LastParticipantCount.ToString());
                EditorGUILayout.LabelField("Succeeded / Skipped / Failed", $"{trigger.LastSucceededParticipantCount} / {trigger.LastSkippedParticipantCount} / {trigger.LastFailedParticipantCount}");
                EditorGUILayout.LabelField("Blocking / Non-blocking Issues", $"{trigger.LastBlockingIssueCount} / {trigger.LastNonBlockingIssueCount}");
                EditorGUILayout.HelpBox(trigger.LastResultSummary, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(
                       !trigger.HasResetExecutionRuntimeBinding ||
                       trigger.IsRequestInFlight))
            {
                if (GUILayout.Button(
                        trigger.IsRequestInFlight
                            ? "Object Reset In Progress"
                            : "Request Object Reset"))
                {
                    trigger.RequestObjectReset();
                }
            }
        }

        private static MessageType ResolveRuntimeMessageType(ObjectResetTrigger trigger)
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

        private static string BuildIntentSummary(
            ObjectResetTargetAuthoringValidationResult validation)
        {
            switch (validation.Status)
            {
                case ObjectResetTargetAuthoringValidationStatus.ValidAdapterReference:
                    return "Reset the authored Reset Subject Adapter target.";
                case ObjectResetTargetAuthoringValidationStatus.ValidAuthoredSubjectId:
                    return "Reset the authored Reset Subject ID target.";
                default:
                    return "Choose one authored Reset Subject target.";
            }
        }

        private static void DrawRuntimeTargetReadiness(ObjectResetTrigger trigger)
        {
            if (trigger.TargetSubjectAdapter == null)
            {
                return;
            }

            FrameworkAuthoringInspectorGui.Section("Runtime Target Readiness");
            if (trigger.TargetSubjectAdapter.IsRegistered)
            {
                EditorGUILayout.LabelField("Runtime Registration", "Registered");
                return;
            }

            EditorGUILayout.LabelField("Runtime Readiness", "Waiting for runtime owner or registration");
            EditorGUILayout.HelpBox(
                "Authoring remains ready. The adapter will resolve its runtime Subject ID after Reset Scene Lifecycle binding and owner availability.",
                MessageType.Info);
        }

        private static void DrawAdvancedTargetEvidence(ObjectResetTrigger trigger)
        {
            if (trigger.TargetSubjectAdapter == null)
            {
                EditorGUILayout.LabelField("Target Source", "Direct authored Reset Subject ID");
                return;
            }

            var adapter = trigger.TargetSubjectAdapter;
            EditorGUILayout.LabelField("Target Source", "Reset Subject Adapter");
            EditorGUILayout.LabelField("Registration Binding", adapter.ResetRegistrationRuntimeBindingStatus);
            EditorGUILayout.LabelField("Scope", adapter.Scope.ToString());
            EditorGUILayout.LabelField("Registration", adapter.IsRegistered ? "Registered" : "Not registered");
            EditorGUILayout.LabelField(
                "Resolved Subject ID",
                adapter.SubjectId.IsValid ? adapter.SubjectId.StableText : "<not resolved>");
            EditorGUILayout.HelpBox(adapter.ResetRegistrationRuntimeBindingDiagnostic, MessageType.None);
        }
    }
}
