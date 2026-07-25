using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraRigComposer))]
    public sealed class CameraRigComposerEditor : UnityEditor.Editor
    {
        private enum TargetAuthoringMode
        {
            ExplicitTransforms = 0,
            TargetSourceComponent = 1
        }

        private SerializedProperty _recipe;
        private SerializedProperty _presentationIntent;
        private SerializedProperty _targetSourceKind;
        private SerializedProperty _targetSource;
        private SerializedProperty _explicitFollowTarget;
        private SerializedProperty _explicitLookAtTarget;
        private SerializedProperty _followRequirement;
        private SerializedProperty _lookAtRequirement;
        private SerializedProperty _followOffset;
        private SerializedProperty _cinemachineCamera;
        private SerializedProperty _createCinemachineCameraIfMissing;
        private SerializedProperty _cinemachineCameraObjectName;
        private SerializedProperty _logApplyRebuildDiagnostics;
        private SerializedProperty _lastApplyRebuildStatus;
        private SerializedProperty _lastBlockingIssue;
        private SerializedProperty _lastTargetResolutionSummary;
        private SerializedProperty _lastMaterializationSummary;
        private SerializedProperty _lastResolvedFollowTarget;
        private SerializedProperty _lastResolvedLookAtTarget;

        private CameraRigComposerApplyRebuildResult? _lastOperationResult;
        private bool _validationOutdated;
        private bool _showAdvancedConfiguration;
        private bool _showAdvancedDiagnostics;

        private void OnEnable()
        {
            _recipe = serializedObject.FindProperty("recipe");
            _presentationIntent =
                serializedObject.FindProperty("presentationIntent");
            _targetSourceKind =
                serializedObject.FindProperty("targetSourceKind");
            _targetSource = serializedObject.FindProperty("targetSource");
            _explicitFollowTarget =
                serializedObject.FindProperty("explicitFollowTarget");
            _explicitLookAtTarget =
                serializedObject.FindProperty("explicitLookAtTarget");
            _followRequirement =
                serializedObject.FindProperty("followRequirement");
            _lookAtRequirement =
                serializedObject.FindProperty("lookAtRequirement");
            _followOffset = serializedObject.FindProperty("followOffset");
            _cinemachineCamera =
                serializedObject.FindProperty("cinemachineCamera");
            _createCinemachineCameraIfMissing =
                serializedObject.FindProperty(
                    "createCinemachineCameraIfMissing");
            _cinemachineCameraObjectName =
                serializedObject.FindProperty(
                    "cinemachineCameraObjectName");
            _logApplyRebuildDiagnostics =
                serializedObject.FindProperty(
                    "logApplyRebuildDiagnostics");
            _lastApplyRebuildStatus =
                serializedObject.FindProperty("lastApplyRebuildStatus");
            _lastBlockingIssue =
                serializedObject.FindProperty("lastBlockingIssue");
            _lastTargetResolutionSummary =
                serializedObject.FindProperty("lastTargetResolutionSummary");
            _lastMaterializationSummary =
                serializedObject.FindProperty("lastMaterializationSummary");
            _lastResolvedFollowTarget =
                serializedObject.FindProperty("lastResolvedFollowTarget");
            _lastResolvedLookAtTarget =
                serializedObject.FindProperty("lastResolvedLookAtTarget");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            DrawHeader();

            EditorGUILayout.Space(6f);
            DrawRecipe();

            EditorGUILayout.Space(8f);
            DrawCameraBehavior();

            EditorGUILayout.Space(8f);
            DrawMaterialization();

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedConfiguration();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

            bool modified =
                serializedObject.ApplyModifiedProperties();
            if (modified &&
                _lastOperationResult.HasValue)
            {
                _validationOutdated = true;
            }
        }

        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Camera Rig Composer",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Authors and materializes one local Cinemachine Camera rig. It resolves explicit targets or one typed Target Source component, but it does not create a Unity Camera, Cinemachine Brain, Audio Listener or runtime Camera Output.",
                MessageType.Info);
        }

        private void DrawRecipe()
        {
            EditorGUILayout.LabelField(
                "Recipe",
                EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _recipe,
                new GUIContent(
                    "Camera Rig Recipe",
                    "Optional reusable defaults for this rig."));

            using (new EditorGUI.DisabledScope(
                       _recipe.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Recipe Defaults"))
                {
                    ApplyRecipeDefaults(false);
                }
            }

            EditorGUILayout.HelpBox(
                "Applying defaults is explicit. The Recipe never rebuilds the rig or changes scene objects automatically.",
                MessageType.None);
        }

        private void DrawCameraBehavior()
        {
            EditorGUILayout.LabelField(
                "Camera Behavior",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _presentationIntent,
                    new GUIContent(
                        "Presentation",
                        "Follow is the only presentation intent implemented by this composer."));
            }

            TargetAuthoringMode currentMode =
                ResolveTargetAuthoringMode();
            TargetAuthoringMode selectedMode =
                (TargetAuthoringMode)EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Target Mode",
                        "Use direct Transform references or a typed component implementing ICameraTargetSource."),
                    currentMode);

            if (selectedMode != currentMode)
            {
                SetTargetAuthoringMode(selectedMode);
            }

            if (selectedMode ==
                TargetAuthoringMode.ExplicitTransforms)
            {
                DrawExplicitTargets();
            }
            else
            {
                DrawTargetSourceComponent();
            }

            EditorGUILayout.PropertyField(
                _followRequirement,
                new GUIContent(
                    "Follow Target",
                    "Required blocks validation when missing. Optional allows a missing target. Not Used excludes Follow from the request."));
            EditorGUILayout.PropertyField(
                _lookAtRequirement,
                new GUIContent(
                    "Look At Target",
                    "Required blocks validation when missing. Optional allows a missing target. Not Used excludes Look At from the request."));
            EditorGUILayout.PropertyField(
                _followOffset,
                new GUIContent(
                    "Follow Offset",
                    "Offset applied to the materialized Cinemachine Follow component."));
        }

        private void DrawExplicitTargets()
        {
            EditorGUILayout.PropertyField(
                _explicitFollowTarget,
                new GUIContent("Follow Transform"));
            EditorGUILayout.PropertyField(
                _explicitLookAtTarget,
                new GUIContent("Look At Transform"));

            EditorGUILayout.HelpBox(
                "The rig resolves targets directly from these authored Transform references. Missing required references are reported only when Validate Configuration or Apply / Rebuild is pressed.",
                MessageType.None);
        }

        private void DrawTargetSourceComponent()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                _targetSource,
                new GUIContent(
                    "Target Source",
                    "Component implementing ICameraTargetSource."));
            bool changed = EditorGUI.EndChangeCheck();

            if (changed)
            {
                SyncSerializedTargetSourceKind();
            }

            EditorGUILayout.HelpBox(
                "Assign a typed component that supplies Follow and Look At targets. Type and target requirements are checked only by explicit validation or Apply / Rebuild.",
                MessageType.None);
        }

        private void DrawMaterialization()
        {
            EditorGUILayout.LabelField(
                "Materialization",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Apply / Rebuild materializes the configured Cinemachine rig idempotently. It does not create or repair the persistent Camera Output.",
                MessageType.None);

            if (GUILayout.Button("Apply / Rebuild Rig"))
            {
                RunApplyOrRebuild();
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (!_lastOperationResult.HasValue)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Run validation after configuring the Camera Rig Composer.",
                    MessageType.None);
            }
            else if (_validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the configuration changed.",
                    MessageType.Warning);
            }
            else if (_lastOperationResult.Value.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    "Ready — the last explicit validation or materialization operation succeeded.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Needs Attention — the last explicit operation found a blocking issue. Open Advanced / Diagnostics for details.",
                    MessageType.Error);
            }

            if (GUILayout.Button("Validate Configuration"))
            {
                RunValidation();
            }
        }

        private void DrawAdvancedConfiguration()
        {
            _showAdvancedConfiguration =
                EditorGUILayout.Foldout(
                    _showAdvancedConfiguration,
                    "Advanced Configuration",
                    true);

            if (!_showAdvancedConfiguration)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawTechnicalMaterializationConfiguration();

            EditorGUILayout.Space(6f);
            DrawRecipeOverwrite();

            EditorGUI.indentLevel--;
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

            DrawTechnicalEvidence();

            EditorGUILayout.Space(6f);
            DrawRuntimeEvidence();

            EditorGUILayout.Space(6f);
            DrawValidationReport();

            EditorGUI.indentLevel--;
        }

        private void DrawTechnicalMaterializationConfiguration()
        {
            EditorGUILayout.LabelField(
                "Technical Materialization",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These options control how Apply / Rebuild materializes the Cinemachine rig. They do not publish a Camera request or change the persistent Camera Output.",
                MessageType.None);

            EditorGUILayout.PropertyField(
                _cinemachineCamera,
                new GUIContent("Cinemachine Camera"));
            EditorGUILayout.PropertyField(
                _createCinemachineCameraIfMissing,
                new GUIContent("Create Camera If Missing"));
            EditorGUILayout.PropertyField(
                _cinemachineCameraObjectName,
                new GUIContent("Camera Object Name"));
            EditorGUILayout.PropertyField(
                _logApplyRebuildDiagnostics,
                new GUIContent("Log Apply / Rebuild Diagnostics"));
        }

        private void DrawTechnicalEvidence()
        {
            EditorGUILayout.LabelField(
                "Technical State",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _presentationIntent,
                    new GUIContent("Presentation Intent"));
                EditorGUILayout.PropertyField(
                    _targetSourceKind,
                    new GUIContent("Serialized Target Source Kind"));
            }
        }

        private void DrawRecipeOverwrite()
        {
            EditorGUILayout.LabelField(
                "Recipe Replacement",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Overwrite From Recipe replaces the current composer configuration with the assigned Recipe values. Scene materialization still requires Apply / Rebuild.",
                MessageType.Warning);

            using (new EditorGUI.DisabledScope(
                       _recipe.objectReferenceValue == null))
            {
                if (GUILayout.Button("Overwrite Configuration From Recipe"))
                {
                    bool confirmed =
                        EditorUtility.DisplayDialog(
                            "Overwrite Camera Rig Composer",
                            "Replace the current composer configuration with values from the assigned Camera Rig Recipe?",
                            "Overwrite",
                            "Cancel");

                    if (confirmed)
                    {
                        ApplyRecipeDefaults(true);
                    }
                }
            }
        }

        private void DrawRuntimeEvidence()
        {
            EditorGUILayout.LabelField(
                "Materialization Evidence",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Evidence below is updated only by Validate Configuration or Apply / Rebuild Rig. Opening this foldout does not resolve targets or inspect the rig.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _lastApplyRebuildStatus,
                    new GUIContent("Last Status"));
                EditorGUILayout.PropertyField(
                    _lastBlockingIssue,
                    new GUIContent("Last Blocking Issue"));
                EditorGUILayout.PropertyField(
                    _lastTargetResolutionSummary,
                    new GUIContent("Target Resolution"));
                EditorGUILayout.PropertyField(
                    _lastMaterializationSummary,
                    new GUIContent("Materialization Summary"));
                EditorGUILayout.PropertyField(
                    _lastResolvedFollowTarget,
                    new GUIContent("Resolved Follow Target"));
                EditorGUILayout.PropertyField(
                    _lastResolvedLookAtTarget,
                    new GUIContent("Resolved Look At Target"));
            }
        }

        private void DrawValidationReport()
        {
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel);

            if (!_lastOperationResult.HasValue)
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

            CameraRigComposerApplyRebuildResult result =
                _lastOperationResult.Value;

            if (result.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrWhiteSpace(
                        result.TargetResolutionSummary)
                        ? "No blocking issues were found."
                        : result.TargetResolutionSummary,
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(result.BlockingIssue)
                    ? "The Camera Rig Composer operation was blocked."
                    : result.BlockingIssue,
                MessageType.Error);

            if (!string.IsNullOrWhiteSpace(
                    result.TargetResolutionSummary))
            {
                EditorGUILayout.HelpBox(
                    result.TargetResolutionSummary,
                    MessageType.None);
            }
        }

        private TargetAuthoringMode ResolveTargetAuthoringMode()
        {
            CameraTargetSourceKind kind =
                (CameraTargetSourceKind)_targetSourceKind.intValue;

            return _targetSource.objectReferenceValue == null &&
                   kind == CameraTargetSourceKind.ExplicitTransform
                ? TargetAuthoringMode.ExplicitTransforms
                : TargetAuthoringMode.TargetSourceComponent;
        }

        private void SetTargetAuthoringMode(
            TargetAuthoringMode mode)
        {
            if (mode == TargetAuthoringMode.ExplicitTransforms)
            {
                _targetSource.objectReferenceValue = null;
                _targetSourceKind.intValue =
                    (int)CameraTargetSourceKind.ExplicitTransform;
                return;
            }

            if ((CameraTargetSourceKind)_targetSourceKind.intValue ==
                CameraTargetSourceKind.ExplicitTransform)
            {
                _targetSourceKind.intValue =
                    (int)CameraTargetSourceKind.None;
            }
        }

        private void SyncSerializedTargetSourceKind()
        {
            Object assigned =
                _targetSource.objectReferenceValue;

            if (assigned is ICameraTargetSource provider)
            {
                _targetSourceKind.intValue =
                    (int)provider.TargetSourceKind;
                return;
            }

            _targetSourceKind.intValue =
                (int)CameraTargetSourceKind.None;
        }

        private void RunValidation()
        {
            serializedObject.ApplyModifiedProperties();

            _lastOperationResult =
                CameraRigComposerApplyRebuildUtility.Validate(
                    (CameraRigComposer)target,
                    false);
            _validationOutdated = false;

            serializedObject.UpdateIfRequiredOrScript();
        }

        private void RunApplyOrRebuild()
        {
            serializedObject.ApplyModifiedProperties();

            _lastOperationResult =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                    (CameraRigComposer)target,
                    true,
                    true);
            _validationOutdated = false;

            serializedObject.UpdateIfRequiredOrScript();
        }

        private void ApplyRecipeDefaults(
            bool overwriteExisting)
        {
            serializedObject.ApplyModifiedProperties();

            var composer =
                (CameraRigComposer)target;
            Undo.RecordObject(
                composer,
                overwriteExisting
                    ? "Overwrite Camera Rig Composer From Recipe"
                    : "Apply Camera Rig Recipe Defaults");

            composer.EditorApplyRecipeDefaults(
                overwriteExisting,
                out _);
            EditorUtility.SetDirty(composer);

            if (_lastOperationResult.HasValue)
            {
                _validationOutdated = true;
            }

            serializedObject.UpdateIfRequiredOrScript();
        }
    }
}
