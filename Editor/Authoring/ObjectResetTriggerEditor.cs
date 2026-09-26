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
        private static readonly GUIContent TargetSubjectLabel = new GUIContent(
            "Target Subject",
            "Assign a UnityResetSubjectAdapter or provide an explicit ResetSubjectId text. The adapter takes precedence when both are set.");
        private static readonly GUIContent ReasonLabel = new GUIContent(
            "Reason",
            "Optional diagnostics reason for this Object Reset request.");
        private static readonly GUIContent AllowNoParticipantsLabel = new GUIContent(
            "Allow No Participants",
            "When enabled, a selected ResetSubject with no participants succeeds as SucceededNoParticipants.");
        private static readonly GUIContent StopOnFailureLabel = new GUIContent(
            "Stop On Failure",
            "Stops execution after the first blocking failure inside this single-subject request.");

        private SerializedProperty _targetSubject;
        private SerializedProperty _reason;
        private SerializedProperty _allowNoParticipants;
        private SerializedProperty _stopOnFailure;
        private bool _showAdvanced;
        private bool _showDiagnostics;

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

            FrameworkAuthoringInspectorGui.ProductHeader("Object Reset Trigger", string.Empty);

            ObjectResetTargetAuthoringValidationResult targetValidation = DrawConfiguration();
            DrawConfigurationStatus(targetValidation);
            DrawAdvanced();
            DrawDiagnostics();

            serializedObject.ApplyModifiedProperties();
        }

        private ObjectResetTargetAuthoringValidationResult DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section("Configuration");

            if (_targetSubject != null)
            {
                EditorGUILayout.PropertyField(_targetSubject, TargetSubjectLabel, includeChildren: true);
            }

            EditorGUILayout.PropertyField(_reason, ReasonLabel);
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

            return ObjectResetTargetAuthoringValidator.Validate(_targetSubject);
        }

        private void DrawConfigurationStatus(ObjectResetTargetAuthoringValidationResult targetValidation)
        {
            FrameworkAuthoringInspectorGui.Section("Configuration Status");
            FrameworkAuthoringInspectorGui.Status(targetValidation.IsValid ? "Ready" : "Incomplete");
            if (!targetValidation.IsValid)
            {
                EditorGUILayout.HelpBox(targetValidation.Message, MessageType.Error);
            }

            if (Application.isPlaying && targets.Length == 1)
            {
                var trigger = (ObjectResetTrigger)target;
                EditorGUILayout.LabelField("Runtime", trigger.HasResetExecutionRuntimeBinding ? "Bound" : "Not bound");
            }
        }

        private void DrawAdvanced()
        {
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
            if (!_showAdvanced)
            {
                return;
            }

            EditorGUILayout.LabelField("Execution Policy", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_allowNoParticipants, AllowNoParticipantsLabel);
            EditorGUILayout.PropertyField(_stopOnFailure, StopOnFailureLabel);
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

            var trigger = (ObjectResetTrigger)target;

            FrameworkAuthoringInspectorGui.RuntimeBinding(
                trigger.ResetExecutionRuntimeBindingStatus,
                trigger.ResetExecutionRuntimeBindingDiagnostic,
                "Ensure this component is active under roots processed by the official Reset Scene Lifecycle composition.");

            FrameworkAuthoringInspectorGui.Section("Target Evidence");
            DrawTargetEvidence(trigger);

            if (!Application.isPlaying)
            {
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

            FrameworkAuthoringInspectorGui.Section("Runtime Test");
            using (new EditorGUI.DisabledScope(!trigger.HasResetExecutionRuntimeBinding || trigger.IsRequestInFlight))
            {
                if (GUILayout.Button(trigger.IsRequestInFlight ? "Object Reset In Progress" : "Request Object Reset"))
                {
                    trigger.RequestObjectReset();
                }
            }
        }

        private static void DrawTargetEvidence(ObjectResetTrigger trigger)
        {
            if (trigger.TargetSubjectAdapter == null)
            {
                EditorGUILayout.LabelField("Target Source", "Direct authored Reset Subject ID");
                return;
            }

            var adapter = trigger.TargetSubjectAdapter;
            EditorGUILayout.LabelField("Target Source", "Reset Subject Adapter");
            EditorGUILayout.LabelField("Scope", adapter.Scope.ToString());
            EditorGUILayout.LabelField("Registration Binding", adapter.ResetRegistrationRuntimeBindingStatus);
            EditorGUILayout.LabelField("Registration", adapter.IsRegistered ? "Registered" : "Not registered");
            EditorGUILayout.LabelField(
                "Resolved Subject ID",
                adapter.SubjectId.IsValid ? adapter.SubjectId.StableText : "Not resolved");
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
    }
}
