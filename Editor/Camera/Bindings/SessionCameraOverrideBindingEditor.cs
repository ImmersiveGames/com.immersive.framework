using Immersive.Framework.Camera;
using Immersive.Framework.Editor.CameraAuthoring;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Camera.Bindings
{
    [CustomEditor(typeof(SessionCameraOverrideBinding))]
    public sealed class SessionCameraOverrideBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _persistentOutputSession;
        private SerializedProperty _scopeId;
        private SerializedProperty _requestId;
        private SerializedProperty _rigComposer;
        private SerializedProperty _targetSource;
        private SerializedProperty _precedence;
        private SerializedProperty _tieBreakerId;
        private SerializedProperty _logDiagnostics;
        private SerializedProperty _overrideActive;
        private SerializedProperty _ownerActive;
        private SerializedProperty _lastStatus;
        private SerializedProperty _lastDiagnostic;

        private SessionCameraOverrideAuthoringValidationResult
            _lastValidationResult;
        private bool _validationOutdated;
        private bool _showAdvancedDiagnostics;

        private void OnEnable()
        {
            _persistentOutputSession =
                serializedObject.FindProperty("persistentOutputSession");
            _scopeId = serializedObject.FindProperty("scopeId");
            _requestId = serializedObject.FindProperty("requestId");
            _rigComposer = serializedObject.FindProperty("rigComposer");
            _targetSource = serializedObject.FindProperty("targetSource");
            _precedence = serializedObject.FindProperty("precedence");
            _tieBreakerId = serializedObject.FindProperty("tieBreakerId");
            _logDiagnostics = serializedObject.FindProperty("logDiagnostics");
            _overrideActive = serializedObject.FindProperty("overrideActive");
            _ownerActive = serializedObject.FindProperty("ownerActive");
            _lastStatus = serializedObject.FindProperty("lastStatus");
            _lastDiagnostic = serializedObject.FindProperty("lastDiagnostic");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawInspectorHeader();

            EditorGUILayout.Space(6f);
            DrawCameraRequest();

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

            bool modified =
                serializedObject.ApplyModifiedProperties();
            if (modified &&
                _lastValidationResult != null)
            {
                _validationOutdated = true;
            }
        }

        private static void DrawInspectorHeader()
        {
            EditorGUILayout.LabelField(
                "Session Camera Override",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Defines one Session-scoped Camera request. It references an existing Camera Output, Camera Rig and Target; it does not create or select those objects automatically.",
                MessageType.Info);
        }

        private void DrawCameraRequest()
        {
            EditorGUILayout.LabelField(
                "Camera Request",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                _persistentOutputSession,
                new GUIContent(
                    "Camera Output",
                    "Persistent Camera Output Session Binding that receives this request."));
            EditorGUILayout.PropertyField(
                _rigComposer,
                new GUIContent(
                    "Camera Rig",
                    "Camera Rig Composer published by this Session request."));
            EditorGUILayout.PropertyField(
                _targetSource,
                new GUIContent(
                    "Target",
                    "Explicit Transform used as the request target source."));
            EditorGUILayout.PropertyField(
                _precedence,
                new GUIContent(
                    "Priority",
                    "Higher precedence wins Camera arbitration. Equal values are resolved by the stable Tie Breaker ID."));

            EditorGUILayout.HelpBox(
                "Configure the output, rig and target first. Stable request identities and runtime evidence remain under Advanced / Diagnostics.",
                MessageType.None);
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (_lastValidationResult == null)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Run validation after configuring the Session Camera Override.",
                    MessageType.None);
            }
            else if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the configuration changed.",
                    MessageType.Warning);
            }
            else if (_lastValidationResult.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Ready — no blocking Session Camera Override issues were found.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Needs Attention — {_lastValidationResult.BlockingIssueCount} blocking issue(s) were found. Open Advanced / Diagnostics for details.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Validate Configuration"))
            {
                RunValidation();
            }
        }

        private void DrawAdvancedDiagnostics()
        {
            _showAdvancedDiagnostics =
                EditorGUILayout.Foldout(
                    _showAdvancedDiagnostics,
                    "Advanced / Diagnostics",
                    true);

            if (!_showAdvancedDiagnostics)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawIdentity();

            EditorGUILayout.Space(6f);
            DrawRuntimeDiagnostics();

            EditorGUILayout.Space(6f);
            DrawValidationReport();

            EditorGUI.indentLevel--;
        }

        private void DrawIdentity()
        {
            EditorGUILayout.LabelField(
                "Stable Identity",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "IDs are generated only when requested. Existing values are preserved and never replaced automatically.",
                MessageType.None);

            DrawIdField("Session Scope ID", _scopeId);
            DrawIdField("Camera Request ID", _requestId);
            DrawIdField("Tie Breaker ID", _tieBreakerId);

            bool hasAllIds =
                HasText(_scopeId) &&
                HasText(_requestId) &&
                HasText(_tieBreakerId);

            using (new EditorGUI.DisabledScope(hasAllIds))
            {
                if (GUILayout.Button("Generate Missing IDs"))
                {
                    GenerateMissingIds();
                }
            }
        }

        private static void DrawIdField(
            string label,
            SerializedProperty property)
        {
            EditorGUILayout.LabelField(
                label,
                EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        property.stringValue ?? string.Empty);
                }

                using (new EditorGUI.DisabledScope(HasText(property)))
                {
                    if (GUILayout.Button(
                            "Generate",
                            GUILayout.Width(72f)))
                    {
                        property.stringValue =
                            CameraAuthoringIdUtility.GenerateIdText();
                    }
                }

                using (new EditorGUI.DisabledScope(!HasText(property)))
                {
                    if (GUILayout.Button(
                            "Copy",
                            GUILayout.Width(48f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            property.stringValue;
                    }
                }
            }
        }

        private void GenerateMissingIds()
        {
            Undo.RecordObject(
                target,
                "Generate Session Camera Override IDs");

            GenerateIfMissing(_scopeId);
            GenerateIfMissing(_requestId);
            GenerateIfMissing(_tieBreakerId);

            if (_lastValidationResult != null)
            {
                _validationOutdated = true;
            }
        }

        private static void GenerateIfMissing(
            SerializedProperty property)
        {
            if (!HasText(property))
            {
                property.stringValue =
                    CameraAuthoringIdUtility.GenerateIdText();
            }
        }

        private void DrawRuntimeDiagnostics()
        {
            EditorGUILayout.LabelField(
                "Runtime Diagnostics",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _logDiagnostics,
                new GUIContent("Log Diagnostics"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _ownerActive,
                    new GUIContent("Session Scope Available"));
                EditorGUILayout.PropertyField(
                    _overrideActive,
                    new GUIContent("Override Published"));
                EditorGUILayout.PropertyField(
                    _lastStatus,
                    new GUIContent("Last Status"));
                EditorGUILayout.PropertyField(
                    _lastDiagnostic,
                    new GUIContent("Last Diagnostic"));
            }
        }

        private void DrawValidationReport()
        {
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            if (_lastValidationResult == null)
            {
                EditorGUILayout.HelpBox(
                    "No validation report is available.",
                    MessageType.None);
                return;
            }

            if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "This report is outdated. Run Validate Configuration again.",
                    MessageType.Warning);
            }

            if (_lastValidationResult.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "No blocking issues were found.",
                    MessageType.Info);
                return;
            }

            foreach (string issue in
                     _lastValidationResult.BlockingIssues)
            {
                EditorGUILayout.HelpBox(
                    issue,
                    MessageType.Error);
            }
        }

        private void RunValidation()
        {
            serializedObject.ApplyModifiedProperties();

            _lastValidationResult =
                SessionCameraOverrideAuthoringValidator.Validate(
                    (SessionCameraOverrideBinding)target);
            _validationOutdated = false;

            serializedObject.UpdateIfRequiredOrScript();
        }

        private static bool HasText(
            SerializedProperty property)
        {
            return property != null &&
                   !string.IsNullOrWhiteSpace(
                       property.stringValue);
        }
    }
}
