using Immersive.Framework.Camera;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraOutputSessionBinding))]
    public sealed class CameraOutputSessionBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty outputId;
        private SerializedProperty unityCamera;
        private SerializedProperty cinemachineBrain;
        private SerializedProperty defaultCameraRig;
        private SerializedProperty initializeOnAwake;
        private SerializedProperty logDiagnostics;
        private SerializedProperty lastStatus;
        private SerializedProperty lastDiagnostic;

        private CameraOutputSessionBindingAuthoringValidationResult
            lastValidationResult;
        private bool validationOutdated;
        private bool showAdvancedDiagnostics;

        private void OnEnable()
        {
            outputId = serializedObject.FindProperty("outputId");
            unityCamera = serializedObject.FindProperty("unityCamera");
            cinemachineBrain = serializedObject.FindProperty("cinemachineBrain");
            defaultCameraRig = serializedObject.FindProperty("defaultCameraRig");
            initializeOnAwake = serializedObject.FindProperty("initializeOnAwake");
            logDiagnostics = serializedObject.FindProperty("logDiagnostics");
            lastStatus = serializedObject.FindProperty("lastStatus");
            lastDiagnostic = serializedObject.FindProperty("lastDiagnostic");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawInspectorHeader();

            EditorGUILayout.Space(6f);
            DrawCameraOutput();

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

            bool modified = serializedObject.ApplyModifiedProperties();
            if (modified && lastValidationResult != null)
            {
                validationOutdated = true;
            }
        }

        private static void DrawInspectorHeader()
        {
            EditorGUILayout.LabelField(
                "Camera Output",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Defines the persistent physical Camera output and its explicit Default Camera Rig. Camera arbitration may present a winning request, while the Default is used when no request wins or system presentation explicitly forces Default.",
                MessageType.Info);
        }

        private void DrawCameraOutput()
        {
            EditorGUILayout.LabelField(
                "Output Components",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                unityCamera,
                new GUIContent(
                    "Unity Camera",
                    "Physical Unity Camera used by the persistent output."));
            EditorGUILayout.PropertyField(
                cinemachineBrain,
                new GUIContent(
                    "Cinemachine Brain",
                    "Cinemachine Brain that applies the active virtual Camera rig."));
            EditorGUILayout.PropertyField(
                defaultCameraRig,
                new GUIContent(
                    "Default Camera Rig",
                    "Explicit persistent Camera Rig presented when no request wins or system presentation forces Default."));

            EditorGUILayout.HelpBox(
                "The Unity Camera and Cinemachine Brain must exist on the same GameObject. The Default Camera Rig is explicit authoring and is never discovered or synthesized automatically. Stable identity and technical settings remain under Advanced / Diagnostics.",
                MessageType.None);
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (lastValidationResult == null)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Run validation after configuring the Camera Output.",
                    MessageType.None);
            }
            else if (validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the configuration changed.",
                    MessageType.Warning);
            }
            else if (lastValidationResult.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Ready — no blocking Camera Output issues were found.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Needs Attention — {lastValidationResult.BlockingIssueCount} blocking issue(s) were found. Open Advanced / Diagnostics for details.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Validate Configuration"))
            {
                RunValidation();
            }
        }

        private void DrawAdvancedDiagnostics()
        {
            showAdvancedDiagnostics = EditorGUILayout.Foldout(
                showAdvancedDiagnostics,
                "Advanced / Diagnostics",
                true);

            if (!showAdvancedDiagnostics)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawIdentity();

            EditorGUILayout.Space(6f);
            DrawAdvancedConfiguration();

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
                "New components receive an ID when they are created. Existing IDs are preserved and are never replaced automatically.",
                MessageType.None);

            EditorGUILayout.LabelField(
                "Camera Output ID",
                EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        outputId.stringValue ?? string.Empty);
                }

                using (new EditorGUI.DisabledScope(HasText(outputId)))
                {
                    if (GUILayout.Button(
                            "Generate",
                            GUILayout.Width(72f)))
                    {
                        GenerateOutputId();
                    }
                }

                using (new EditorGUI.DisabledScope(!HasText(outputId)))
                {
                    if (GUILayout.Button(
                            "Copy",
                            GUILayout.Width(48f)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            outputId.stringValue;
                    }
                }
            }
        }

        private void DrawAdvancedConfiguration()
        {
            EditorGUILayout.LabelField(
                "Advanced Configuration",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                initializeOnAwake,
                new GUIContent(
                    "Initialize On Awake",
                    "Initialize the Camera Output Session during Awake."));
            EditorGUILayout.PropertyField(
                logDiagnostics,
                new GUIContent(
                    "Log Diagnostics",
                    "Emit non-error Camera Output diagnostics through the framework logger."));
        }

        private void DrawRuntimeDiagnostics()
        {
            EditorGUILayout.LabelField(
                "Runtime Diagnostics",
                EditorStyles.boldLabel);

            CameraOutputSessionBinding binding =
                (CameraOutputSessionBinding)target;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(
                    new GUIContent("Initialized"),
                    binding != null && binding.IsInitialized);
                EditorGUILayout.PropertyField(
                    lastStatus,
                    new GUIContent("Last Status"));
                EditorGUILayout.PropertyField(
                    lastDiagnostic,
                    new GUIContent("Last Diagnostic"));
            }
        }

        private void DrawValidationReport()
        {
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            if (lastValidationResult == null)
            {
                EditorGUILayout.HelpBox(
                    "No validation report is available.",
                    MessageType.None);
                return;
            }

            if (validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "This report is outdated. Run Validate Configuration again.",
                    MessageType.Warning);
            }

            if (lastValidationResult.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "No blocking issues were found.",
                    MessageType.Info);
                return;
            }

            foreach (string issue in lastValidationResult.BlockingIssues)
            {
                EditorGUILayout.HelpBox(
                    issue,
                    MessageType.Error);
            }
        }

        private void GenerateOutputId()
        {
            serializedObject.UpdateIfRequiredOrScript();
            if (HasText(outputId))
            {
                return;
            }

            Undo.RecordObject(
                target,
                "Generate Camera Output ID");
            outputId.stringValue =
                CameraAuthoringIdUtility.GenerateIdText();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            if (lastValidationResult != null)
            {
                validationOutdated = true;
            }
        }

        private void RunValidation()
        {
            serializedObject.ApplyModifiedProperties();

            lastValidationResult =
                CameraOutputSessionBindingAuthoringValidator.Validate(
                    (CameraOutputSessionBinding)target);
            validationOutdated = false;

            serializedObject.UpdateIfRequiredOrScript();
        }

        private static bool HasText(
            SerializedProperty property)
        {
            return property != null &&
                   !string.IsNullOrWhiteSpace(property.stringValue);
        }
    }
}
