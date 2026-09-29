using Immersive.Framework.Camera;
using Immersive.Framework.Editor.CameraAuthoring;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Camera.Bindings
{
    [CustomEditor(typeof(CameraOutputAuthoring))]
    public sealed class CameraOutputAuthoringEditor : UnityEditor.Editor
    {
        private static readonly GUIContent UnityCameraLabel =
            new GUIContent(
                "Unity Camera",
                "Physical Unity Camera used by this persistent Camera Output.");

        private static readonly GUIContent CinemachineBrainLabel =
            new GUIContent(
                "Cinemachine Brain",
                "Cinemachine Brain that applies the Camera Rig currently presented by this output. It must be on the same GameObject as the Unity Camera.");

        private static readonly GUIContent FallbackCameraRigLabel =
            new GUIContent(
                "Fallback Camera Rig",
                "Explicit persistent technical coverage for this Output, prepared before any normal occurrence. It does not select or replace the active Assignment. Rig targets and framing are authored on CameraRigComposer, not here.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates this Camera Output configuration without initializing runtime services, creating components, discovering references or repairing the scene.");

        private static readonly GUIContent InitializeOnAwakeLabel =
            new GUIContent(
                "Initialize On Awake",
                "Initializes the Camera Output Session during Awake. Disable only when another explicit owner controls initialization timing.");

        private static readonly GUIContent LogDiagnosticsLabel =
            new GUIContent(
                "Log Diagnostics",
                "Emits non-error Camera Output diagnostics through the framework logger. Errors are still logged when this option is disabled.");

        private SerializedProperty _outputDefinition;
        private SerializedProperty _unityCamera;
        private SerializedProperty _cinemachineBrain;
        private SerializedProperty _fallbackCameraRig;
        private SerializedProperty _initializeOnAwake;
        private SerializedProperty _logDiagnostics;
        private SerializedProperty _lastStatus;
        private SerializedProperty _lastDiagnostic;

        private CameraOutputAuthoringValidationResult
            _lastValidationResult;
        private bool _validationOutdated;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            _outputDefinition = current.FindProperty("outputDefinition");
            _unityCamera = current.FindProperty("unityCamera");
            _cinemachineBrain = current.FindProperty("cinemachineBrain");
            _fallbackCameraRig = current.FindProperty("fallbackCameraRig");
            _initializeOnAwake = current.FindProperty("initializeOnAwake");
            _logDiagnostics = current.FindProperty("logDiagnostics");
            _lastStatus = current.FindProperty("lastStatus");
            _lastDiagnostic = current.FindProperty("lastDiagnostic");
        }

        public override void OnInspectorGUI()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            current.UpdateIfRequiredOrScript();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Camera Output",
                    "Configures one persistent physical Camera Output. Assignment and occurrence state are separate from physical rig authoring."),
                EditorStyles.boldLabel);

            CameraOutputReferenceGUI.DrawDefinitionReference(_outputDefinition, "Output Definition");
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            DrawConfiguration();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            DrawValidation();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            DrawAdvancedDebug();
            if (!IsCurrentTargetValid(inspectedTarget, current))
                return;

            bool modified = current.ApplyModifiedProperties();
            if (modified && _lastValidationResult != null)
            {
                _validationOutdated = true;
            }
        }

        private void DrawConfiguration()
        {
            DrawSection("Configuration");

            EditorGUILayout.PropertyField(
                _unityCamera,
                UnityCameraLabel);
            EditorGUILayout.PropertyField(
                _cinemachineBrain,
                CinemachineBrainLabel);
            EditorGUILayout.PropertyField(
                _fallbackCameraRig,
                FallbackCameraRigLabel);
        }

        private void DrawValidation()
        {
            DrawSection("Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        ValidateLabel,
                        GUILayout.Width(96f)))
                {
                    RunValidation();
                }

                GUILayout.Space(8f);
                EditorGUILayout.LabelField(
                    GetValidationStatus(),
                    EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
            }

            DrawFirstActionableValidationIssue();
        }

        private string GetValidationStatus()
        {
            if (_lastValidationResult == null)
            {
                return "Not Validated";
            }

            if (_validationOutdated)
            {
                return "Outdated";
            }

            if (_lastValidationResult.IsValid)
            {
                return "Ready";
            }

            return $"Needs Attention ({_lastValidationResult.BlockingIssueCount})";
        }

        private void DrawFirstActionableValidationIssue()
        {
            if (_lastValidationResult == null)
            {
                return;
            }

            if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Configuration changed after validation. Validate again before relying on the result.",
                    MessageType.Warning);
                return;
            }

            if (_lastValidationResult.IsValid ||
                _lastValidationResult.BlockingIssueCount == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                _lastValidationResult.BlockingIssues[0],
                MessageType.Error);
        }

        private void DrawRuntimeStatus()
        {
            DrawSection("Runtime Status");

            CameraOutputAuthoring binding =
                (CameraOutputAuthoring)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Initialized",
                        "True when this binding currently owns an initialized CameraOutputSession."),
                    binding != null && binding.IsInitialized);

                EditorGUILayout.PropertyField(
                    _lastStatus,
                    new GUIContent(
                        "Last Status",
                        "Most recent status recorded by Camera Output Session initialization or synchronization."));
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);

            _showAdvancedDebug = EditorGUILayout.Foldout(
                _showAdvancedDebug,
                new GUIContent(
                    "Advanced / Debug",
                    "Shows technical initialization options, runtime diagnostics and the complete validation report."),
                true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            EditorGUILayout.Space(5f);
            DrawTechnicalConfiguration();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Output ID", ((CameraOutputAuthoring)target).OutputIdText);

            EditorGUILayout.Space(5f);
            DrawRuntimeDiagnostics();

            EditorGUILayout.Space(5f);
            DrawValidationReport();

            EditorGUI.indentLevel--;
        }

        private void DrawTechnicalConfiguration()
        {
            EditorGUILayout.LabelField(
                "Technical Configuration",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(
                _initializeOnAwake,
                InitializeOnAwakeLabel);
            EditorGUILayout.PropertyField(
                _logDiagnostics,
                LogDiagnosticsLabel);
        }

        private void DrawRuntimeDiagnostics()
        {
            EditorGUILayout.LabelField(
                "Runtime Diagnostics",
                EditorStyles.miniBoldLabel);

            CameraOutputAuthoring binding =
                (CameraOutputAuthoring)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Initialized",
                        "Current runtime CameraOutputSession ownership state."),
                    binding != null && binding.IsInitialized);

                EditorGUILayout.PropertyField(
                    _lastStatus,
                    new GUIContent("Last Status"));

                EditorGUILayout.PropertyField(
                    _lastDiagnostic,
                    new GUIContent(
                        "Last Diagnostic",
                        "Most recent diagnostic recorded by this binding."));
            }
        }

        private void DrawValidationReport()
        {
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.LabelField(
                "Status",
                GetValidationStatus(),
                EditorStyles.miniLabel);

            if (_lastValidationResult == null)
            {
                return;
            }

            for (int index = 0;
                 index < _lastValidationResult.BlockingIssues.Count;
                 index++)
            {
                EditorGUILayout.LabelField(
                    $"{index + 1}. {_lastValidationResult.BlockingIssues[index]}",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void RunValidation()
        {
            UnityEngine.Object inspectedTarget = target;
            if (!TryGetValidSerializedObject(inspectedTarget, out SerializedObject current))
                return;

            current.ApplyModifiedProperties();
            if (!TryGetValidSerializedObject(inspectedTarget, out current))
                return;

            var binding = inspectedTarget as CameraOutputAuthoring;
            if (binding == null)
                return;

            CameraOutputAuthoringValidationResult result =
                CameraOutputAuthoringValidator.Validate(
                    binding);
            if (!TryGetValidSerializedObject(inspectedTarget, out current))
                return;

            _lastValidationResult = result;
            _validationOutdated = false;

            current.UpdateIfRequiredOrScript();
        }

        private bool TryGetValidSerializedObject(
            UnityEngine.Object inspectedTarget,
            out SerializedObject current)
        {
            current = null;
            if (this == null || inspectedTarget == null || target != inspectedTarget)
                return false;

            current = serializedObject;
            return current != null && current.targetObject == inspectedTarget;
        }

        private bool IsCurrentTargetValid(
            UnityEngine.Object inspectedTarget,
            SerializedObject current)
        {
            return current != null && inspectedTarget != null &&
                   target == inspectedTarget &&
                   current.targetObject == inspectedTarget;
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }

    }
}
