using Immersive.Framework.Camera;
using Immersive.Framework.Editor.CameraAuthoring;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Camera.Bindings
{
    [CustomEditor(typeof(CameraOutputSessionBinding))]
    public sealed class CameraOutputSessionBindingEditor : UnityEditor.Editor
    {
        private static readonly GUIContent UnityCameraLabel =
            new GUIContent(
                "Unity Camera",
                "Physical Unity Camera used by this persistent Camera Output.");

        private static readonly GUIContent CinemachineBrainLabel =
            new GUIContent(
                "Cinemachine Brain",
                "Cinemachine Brain that applies the Camera Rig currently presented by this output. It must be on the same GameObject as the Unity Camera.");

        private static readonly GUIContent DefaultCameraRigLabel =
            new GUIContent(
                "Default Camera Rig",
                "Explicit persistent Camera Rig presented when no normal Camera request wins or system presentation forces Default. Rig targets and framing are authored on CameraRigComposer, not here.");

        private static readonly GUIContent ValidateLabel =
            new GUIContent(
                "Validate",
                "Validates this Camera Output configuration without initializing runtime services, creating components, discovering references or repairing the scene.");

        private static readonly GUIContent OutputIdLabel =
            new GUIContent(
                "Camera Output ID",
                "Stable identity for this persistent Camera Output. Existing IDs are preserved and never replaced automatically.");

        private static readonly GUIContent InitializeOnAwakeLabel =
            new GUIContent(
                "Initialize On Awake",
                "Initializes the Camera Output Session during Awake. Disable only when another explicit owner controls initialization timing.");

        private static readonly GUIContent LogDiagnosticsLabel =
            new GUIContent(
                "Log Diagnostics",
                "Emits non-error Camera Output diagnostics through the framework logger. Errors are still logged when this option is disabled.");

        private SerializedProperty _outputId;
        private SerializedProperty _unityCamera;
        private SerializedProperty _cinemachineBrain;
        private SerializedProperty _defaultCameraRig;
        private SerializedProperty _initializeOnAwake;
        private SerializedProperty _logDiagnostics;
        private SerializedProperty _lastStatus;
        private SerializedProperty _lastDiagnostic;

        private CameraOutputSessionBindingAuthoringValidationResult
            _lastValidationResult;
        private bool _validationOutdated;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _outputId = serializedObject.FindProperty("outputId");
            _unityCamera = serializedObject.FindProperty("unityCamera");
            _cinemachineBrain = serializedObject.FindProperty("cinemachineBrain");
            _defaultCameraRig = serializedObject.FindProperty("defaultCameraRig");
            _initializeOnAwake = serializedObject.FindProperty("initializeOnAwake");
            _logDiagnostics = serializedObject.FindProperty("logDiagnostics");
            _lastStatus = serializedObject.FindProperty("lastStatus");
            _lastDiagnostic = serializedObject.FindProperty("lastDiagnostic");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.LabelField(
                new GUIContent(
                    "Camera Output",
                    "Configures one persistent physical Camera Output. Camera request arbitration and Camera Rig authoring remain separate authorities."),
                EditorStyles.boldLabel);

            DrawConfiguration();
            DrawValidation();

            if (Application.isPlaying)
            {
                DrawRuntimeStatus();
            }

            DrawAdvancedDebug();

            bool modified = serializedObject.ApplyModifiedProperties();
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
                _defaultCameraRig,
                DefaultCameraRigLabel);
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

            CameraOutputSessionBinding binding =
                (CameraOutputSessionBinding)target;

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
                    "Shows stable identity, technical initialization options, runtime diagnostics and the complete validation report."),
                true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawStableIdentity();

            EditorGUILayout.Space(5f);
            DrawTechnicalConfiguration();

            EditorGUILayout.Space(5f);
            DrawRuntimeDiagnostics();

            EditorGUILayout.Space(5f);
            DrawValidationReport();

            EditorGUI.indentLevel--;
        }

        private void DrawStableIdentity()
        {
            EditorGUILayout.LabelField(
                "Stable Identity",
                EditorStyles.miniBoldLabel);

            string id = _outputId != null
                ? _outputId.stringValue ?? string.Empty
                : string.Empty;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        OutputIdLabel,
                        id);
                }

                using (new EditorGUI.DisabledScope(
                           !string.IsNullOrWhiteSpace(id)))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Generate",
                                "Generates a stable Camera Output ID only when the field is empty."),
                            GUILayout.Width(72f)))
                    {
                        GenerateOutputId();
                    }
                }

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(id)))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Copy",
                                "Copies the current Camera Output ID to the clipboard."),
                            GUILayout.Width(48f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = id;
                    }
                }
            }
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

            CameraOutputSessionBinding binding =
                (CameraOutputSessionBinding)target;

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

        private void GenerateOutputId()
        {
            serializedObject.UpdateIfRequiredOrScript();
            if (HasText(_outputId))
            {
                return;
            }

            Undo.RecordObject(
                target,
                "Generate Camera Output ID");

            _outputId.stringValue =
                CameraAuthoringIdUtility.GenerateIdText();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);

            if (_lastValidationResult != null)
            {
                _validationOutdated = true;
            }

            serializedObject.UpdateIfRequiredOrScript();
        }

        private void RunValidation()
        {
            serializedObject.ApplyModifiedProperties();

            _lastValidationResult =
                CameraOutputSessionBindingAuthoringValidator.Validate(
                    (CameraOutputSessionBinding)target);
            _validationOutdated = false;

            serializedObject.UpdateIfRequiredOrScript();
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }

        private static bool HasText(
            SerializedProperty property)
        {
            return property != null &&
                   !string.IsNullOrWhiteSpace(property.stringValue);
        }
    }
}
