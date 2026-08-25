using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Common;
using Unity.Cinemachine;
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

        private SerializedProperty _presentationIntent;
        private SerializedProperty _targetSourceKind;
        private SerializedProperty _targetSource;
        private SerializedProperty _explicitFollowTarget;
        private SerializedProperty _explicitLookAtTarget;
        private SerializedProperty _lookAtRequirement;
        private SerializedProperty _followOffset;
        private SerializedProperty _mountedPositionDamping;
        private SerializedProperty _mountedRotationDamping;
        private SerializedProperty _thirdPersonShoulderOffset;
        private SerializedProperty _thirdPersonVerticalArmLength;
        private SerializedProperty _thirdPersonCameraSide;
        private SerializedProperty _thirdPersonCameraDistance;
        private SerializedProperty _thirdPersonDamping;
        private SerializedProperty _cinemachineCamera;
        private SerializedProperty _materializedPresentationIntent;
        private SerializedProperty _frameworkOwnedCinemachineCamera;
        private SerializedProperty _frameworkOwnedPositionControl;
        private SerializedProperty _frameworkOwnedRotationControl;
        private SerializedProperty _materializationRevision;
        private SerializedProperty _logApplyRebuildDiagnostics;
        private SerializedProperty _lastApplyRebuildStatus;
        private SerializedProperty _lastBlockingIssue;
        private SerializedProperty _lastTargetResolutionSummary;
        private SerializedProperty _lastMaterializationSummary;
        private SerializedProperty _lastResolvedFollowTarget;
        private SerializedProperty _lastResolvedLookAtTarget;

        private CameraRigComposerApplyRebuildResult? _lastValidationResult;
        private CameraRigComposerApplyRebuildResult? _lastApplyResult;
        private bool _validationOutdated;
        private bool _materializationOutdated;
        private bool _showAdvancedDebug;

        private void OnEnable()
        {
            _presentationIntent =
                serializedObject.FindProperty("presentationIntent");
            _targetSourceKind =
                serializedObject.FindProperty("targetSourceKind");
            _targetSource =
                serializedObject.FindProperty("targetSource");
            _explicitFollowTarget =
                serializedObject.FindProperty("explicitFollowTarget");
            _explicitLookAtTarget =
                serializedObject.FindProperty("explicitLookAtTarget");
            _lookAtRequirement =
                serializedObject.FindProperty("lookAtRequirement");
            _followOffset =
                serializedObject.FindProperty("followOffset");
            _mountedPositionDamping =
                serializedObject.FindProperty("mountedPositionDamping");
            _mountedRotationDamping =
                serializedObject.FindProperty("mountedRotationDamping");
            _thirdPersonShoulderOffset =
                serializedObject.FindProperty("thirdPersonShoulderOffset");
            _thirdPersonVerticalArmLength =
                serializedObject.FindProperty("thirdPersonVerticalArmLength");
            _thirdPersonCameraSide =
                serializedObject.FindProperty("thirdPersonCameraSide");
            _thirdPersonCameraDistance =
                serializedObject.FindProperty("thirdPersonCameraDistance");
            _thirdPersonDamping =
                serializedObject.FindProperty("thirdPersonDamping");
            _cinemachineCamera =
                serializedObject.FindProperty("cinemachineCamera");
            _materializedPresentationIntent =
                serializedObject.FindProperty("materializedPresentationIntent");
            _frameworkOwnedCinemachineCamera =
                serializedObject.FindProperty("frameworkOwnedCinemachineCamera");
            _frameworkOwnedPositionControl =
                serializedObject.FindProperty("frameworkOwnedPositionControl");
            _frameworkOwnedRotationControl =
                serializedObject.FindProperty("frameworkOwnedRotationControl");
            _materializationRevision =
                serializedObject.FindProperty("materializationRevision");
            _logApplyRebuildDiagnostics =
                serializedObject.FindProperty("logApplyRebuildDiagnostics");
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

            DrawComposerHeader();

            EditorGUI.BeginChangeCheck();

            DrawPresentation();

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();

            DrawTargets(presentation);
            DrawModelSettings(presentation);

            bool authoringChanged =
                EditorGUI.EndChangeCheck();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (authoringChanged || modified)
            {
                MarkAuthoringChanged();
            }

            presentation = ResolvePresentationIntent();

            DrawMaterialization(presentation);
            DrawValidation();
            DrawAdvancedDebug();
        }

        private static void DrawComposerHeader()
        {
            EditorGUILayout.LabelField(
                new GUIContent(
                    "Camera Rig Composer",
                    "Authors one local gameplay Camera rig. Apply / Rebuild materializes the local Cinemachine Camera and Framework-owned pipeline controls. Camera Output selection and request arbitration are separate runtime authorities."),
                EditorStyles.boldLabel);
        }

        private void DrawPresentation()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Presentation");

            EditorGUILayout.PropertyField(
                _presentationIntent,
                new GUIContent(
                    "Model",
                    "Fixed preserves the local camera pose; Follow tracks one target with offset; Mounted locks to a Camera Mount and its rotation; Third Person tracks a rotating pivot using Cinemachine Third Person Follow."));
        }

        private void DrawTargets(
            CameraRigPresentationIntent presentation)
        {
            if (presentation ==
                CameraRigPresentationIntent.Undefined)
            {
                return;
            }

            FrameworkAuthoringInspectorGui.Section(
                "Targets");

            CameraTargetRequirement effectiveFollow =
                ResolveEffectiveFollowRequirement(
                    presentation);

            if (presentation == CameraRigPresentationIntent.Fixed ||
                presentation == CameraRigPresentationIntent.Follow)
            {
                EditorGUILayout.PropertyField(
                    _lookAtRequirement,
                    new GUIContent(
                        "Look At",
                        "Not Used disables the Look At role. Optional permits a missing target. Required blocks validation when missing."));
            }

            CameraTargetRequirement effectiveLookAt =
                ResolveEffectiveLookAtRequirement(
                    presentation);

            if (effectiveFollow == CameraTargetRequirement.NotUsed &&
                effectiveLookAt == CameraTargetRequirement.NotUsed)
            {
                return;
            }

            TargetAuthoringMode currentMode =
                ResolveTargetAuthoringMode();

            TargetAuthoringMode selectedMode =
                (TargetAuthoringMode)EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Target Mode",
                        "Use direct Transform references or one typed ICameraTargetSource component. No scene lookup is performed by materialization."),
                    currentMode);

            if (selectedMode != currentMode)
            {
                SetTargetAuthoringMode(
                    selectedMode);
            }

            if (selectedMode ==
                TargetAuthoringMode.ExplicitTransforms)
            {
                DrawExplicitTargets(
                    presentation,
                    effectiveFollow,
                    effectiveLookAt);
            }
            else
            {
                DrawTargetSourceComponent();
            }
        }

        private void DrawExplicitTargets(
            CameraRigPresentationIntent presentation,
            CameraTargetRequirement effectiveFollow,
            CameraTargetRequirement effectiveLookAt)
        {
            if (effectiveFollow !=
                CameraTargetRequirement.NotUsed)
            {
                string label =
                    presentation == CameraRigPresentationIntent.Mounted
                        ? "Camera Mount"
                        : presentation == CameraRigPresentationIntent.ThirdPerson
                            ? "Tracking Pivot"
                            : "Tracking Target";

                EditorGUILayout.PropertyField(
                    _explicitFollowTarget,
                    new GUIContent(
                        label,
                        "Explicit Transform used as the model's Tracking / Follow target."));
            }

            if ((presentation == CameraRigPresentationIntent.Fixed ||
                 presentation == CameraRigPresentationIntent.Follow) &&
                effectiveLookAt !=
                    CameraTargetRequirement.NotUsed)
            {
                EditorGUILayout.PropertyField(
                    _explicitLookAtTarget,
                    new GUIContent(
                        "Look At Target",
                        "Explicit target consumed by the model's supported rotation behavior."));
            }
        }

        private void DrawTargetSourceComponent()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                _targetSource,
                new GUIContent(
                    "Target Source",
                    "Component implementing ICameraTargetSource. The selected Presentation defines which target roles are requested."));

            if (EditorGUI.EndChangeCheck())
            {
                SyncSerializedTargetSourceKind();
            }
        }

        private void DrawModelSettings(
            CameraRigPresentationIntent presentation)
        {
            switch (presentation)
            {
                case CameraRigPresentationIntent.Follow:
                    FrameworkAuthoringInspectorGui.Section(
                        "Model Settings");
                    DrawFollowSettings();
                    break;

                case CameraRigPresentationIntent.Mounted:
                    FrameworkAuthoringInspectorGui.Section(
                        "Model Settings");
                    DrawMountedSettings();
                    break;

                case CameraRigPresentationIntent.ThirdPerson:
                    FrameworkAuthoringInspectorGui.Section(
                        "Model Settings");
                    DrawThirdPersonSettings();
                    break;
            }
        }

        private void DrawFollowSettings()
        {
            EditorGUILayout.PropertyField(
                _followOffset,
                new GUIContent(
                    "Follow Offset",
                    "Camera offset used by the Framework-owned Cinemachine Follow Position Control."));
        }

        private void DrawMountedSettings()
        {
            EditorGUILayout.PropertyField(
                _mountedPositionDamping,
                new GUIContent(
                    "Position Damping",
                    "Damping used by Cinemachine Hard Lock to Target. Zero is a hard positional lock."));

            EditorGUILayout.PropertyField(
                _mountedRotationDamping,
                new GUIContent(
                    "Rotation Damping",
                    "Damping used by Cinemachine Rotate With Follow Target. Zero matches mount rotation immediately."));
        }

        private void DrawThirdPersonSettings()
        {
            EditorGUILayout.PropertyField(
                _thirdPersonShoulderOffset,
                new GUIContent(
                    "Shoulder Offset"));

            EditorGUILayout.PropertyField(
                _thirdPersonVerticalArmLength,
                new GUIContent(
                    "Vertical Arm Length"));

            EditorGUILayout.PropertyField(
                _thirdPersonCameraSide,
                new GUIContent(
                    "Camera Side",
                    "0 = left shoulder, 1 = right shoulder, intermediate values blend between sides."));

            EditorGUILayout.PropertyField(
                _thirdPersonCameraDistance,
                new GUIContent(
                    "Camera Distance"));

            EditorGUILayout.PropertyField(
                _thirdPersonDamping,
                new GUIContent(
                    "Damping",
                    "Per-axis tracking damping applied by Cinemachine Third Person Follow."));
        }

        private void DrawMaterialization(
            CameraRigPresentationIntent presentation)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Materialization");

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();
            CameraRigPresentationIntent materialized =
                ResolveMaterializedPresentationIntent();

            string status =
                ResolveMaterializationStatus(
                    presentation,
                    materialized,
                    local);

            EditorGUILayout.LabelField(
                "Status",
                status);

            DrawMaterializationIssue(
                presentation,
                materialized,
                local);

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying ||
                       presentation ==
                           CameraRigPresentationIntent.Undefined))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Apply / Rebuild",
                            "Preflight and reconcile the local Cinemachine Camera and only Framework-owned Position / Rotation controls. Unknown external conflicts block before mutation.")))
                {
                    RunApplyOrRebuild();
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Authoring actions are unavailable in Play Mode.",
                    EditorStyles.miniLabel);
            }
        }

        private string ResolveMaterializationStatus(
            CameraRigPresentationIntent presentation,
            CameraRigPresentationIntent materialized,
            CinemachineCamera local)
        {
            if (_lastApplyResult.HasValue &&
                !_materializationOutdated &&
                !_lastApplyResult.Value.Succeeded)
            {
                return "Blocked";
            }

            if (presentation ==
                CameraRigPresentationIntent.Undefined)
            {
                return "Needs Setup";
            }

            if (local == null)
            {
                return "Not Materialized";
            }

            if (_materializationOutdated)
            {
                return "Needs Apply";
            }

            if (materialized ==
                CameraRigPresentationIntent.Undefined)
            {
                return "Needs Apply";
            }

            if (materialized != presentation)
            {
                return "Needs Rebuild";
            }

            return "Current";
        }

        private void DrawMaterializationIssue(
            CameraRigPresentationIntent presentation,
            CameraRigPresentationIntent materialized,
            CinemachineCamera local)
        {
            if (_lastApplyResult.HasValue &&
                !_materializationOutdated &&
                !_lastApplyResult.Value.Succeeded)
            {
                string issue =
                    _lastApplyResult.Value.BlockingIssue;

                if (!string.IsNullOrWhiteSpace(issue))
                {
                    EditorGUILayout.HelpBox(
                        FormatBlockingIssueForInspector(issue),
                        MessageType.Error);
                }

                return;
            }

            if (presentation ==
                CameraRigPresentationIntent.Undefined)
            {
                EditorGUILayout.HelpBox(
                    "Select a Presentation model before materializing this rig.",
                    MessageType.Info);
                return;
            }

            if (local == null)
            {
                return;
            }

            if (_materializationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Authored Camera Rig settings changed. Apply / Rebuild to reconcile the materialized rig.",
                    MessageType.Warning);
                return;
            }

            if (materialized ==
                CameraRigPresentationIntent.Undefined)
            {
                EditorGUILayout.HelpBox(
                    "A local Cinemachine Camera exists, but no Framework materialization is recorded. Apply / Rebuild to reconcile the rig.",
                    MessageType.Warning);
                return;
            }

            if (materialized != presentation)
            {
                EditorGUILayout.HelpBox(
                    $"Authored Presentation is '{presentation}' while materialized Presentation is '{materialized}'. Apply / Rebuild is required.",
                    MessageType.Warning);
            }
        }

        private void DrawValidation()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           Application.isPlaying))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Validate",
                                "Validates the authored Camera Rig configuration and target resolution without changing the Cinemachine pipeline."),
                            GUILayout.Width(96f)))
                    {
                        RunValidation();
                    }
                }

                GUILayout.Space(8f);

                EditorGUILayout.LabelField(
                    ResolveValidationStatus(),
                    EditorStyles.miniBoldLabel);

                GUILayout.FlexibleSpace();
            }

            if (_lastValidationResult.HasValue &&
                !_validationOutdated &&
                !_lastValidationResult.Value.Succeeded &&
                !string.IsNullOrWhiteSpace(
                    _lastValidationResult.Value.BlockingIssue))
            {
                EditorGUILayout.HelpBox(
                    FormatBlockingIssueForInspector(
                        _lastValidationResult.Value.BlockingIssue),
                    MessageType.Error);
            }
        }

        private string ResolveValidationStatus()
        {
            if (!_lastValidationResult.HasValue)
            {
                return "Not Validated";
            }

            if (_validationOutdated)
            {
                return "Outdated";
            }

            return _lastValidationResult.Value.Succeeded
                ? "Valid"
                : "Issue";
        }

        private static string FormatBlockingIssueForInspector(
            string issue)
        {
            if (string.IsNullOrWhiteSpace(issue))
            {
                return "The operation is blocked. Open Advanced / Debug for the technical diagnostic.";
            }

            const string rotationConflictPrefix =
                "cinemachine-rotation-control:external-or-unknown-conflict:";
            const string positionConflictPrefix =
                "cinemachine-position-control:external-or-unknown-conflict:";
            const string rotationDuplicatePrefix =
                "cinemachine-rotation-control:external-or-unknown-duplicate:";
            const string positionDuplicatePrefix =
                "cinemachine-position-control:external-or-unknown-duplicate:";

            if (issue.StartsWith(
                    rotationConflictPrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlConflict(
                    issue.Substring(rotationConflictPrefix.Length),
                    "Rotation");
            }

            if (issue.StartsWith(
                    positionConflictPrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlConflict(
                    issue.Substring(positionConflictPrefix.Length),
                    "Position");
            }

            if (issue.StartsWith(
                    rotationDuplicatePrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlDuplicate(
                    issue.Substring(rotationDuplicatePrefix.Length),
                    "Rotation");
            }

            if (issue.StartsWith(
                    positionDuplicatePrefix,
                    System.StringComparison.Ordinal))
            {
                return FormatExternalControlDuplicate(
                    issue.Substring(positionDuplicatePrefix.Length),
                    "Position");
            }

            if (issue ==
                "cinemachine-rotation-control:ownership-evidence-ambiguous")
            {
                return "Rotation Control ownership is ambiguous. The Framework cannot safely decide which control it owns. Review the recorded ownership and current pipeline in Advanced / Debug before applying.";
            }

            if (issue ==
                "cinemachine-position-control:ownership-evidence-ambiguous")
            {
                return "Position Control ownership is ambiguous. The Framework cannot safely decide which control it owns. Review the recorded ownership and current pipeline in Advanced / Debug before applying.";
            }

            if (issue == "cinemachine-follow:create-disabled")
            {
                return "The selected Presentation requires a Follow Position Control, but creation of that control is disabled. Review the Camera Rig configuration in Advanced / Debug.";
            }

            if (issue == "follow-target:required-missing" ||
                issue.EndsWith(
                    ":follow-target-required",
                    System.StringComparison.Ordinal))
            {
                return "A Tracking / Follow target is required for the selected Presentation. Assign the required target and try again.";
            }

            if (issue == "look-at-target:required-missing")
            {
                return "A Look At target is required for the selected Presentation. Assign the target and try again.";
            }

            if (issue.EndsWith(
                    ":look-at-not-supported",
                    System.StringComparison.Ordinal))
            {
                return "The selected Presentation does not support a Look At target. Remove that target or choose a compatible Presentation.";
            }

            if (issue.EndsWith(
                    ":settings-invalid",
                    System.StringComparison.Ordinal))
            {
                return "One or more settings for the selected Presentation are invalid. Review the Model Settings and try again.";
            }

            if (issue == "presentation:Undefined:not-supported")
            {
                return "Select a Presentation model before validating or materializing this rig.";
            }

            if (issue == "cinemachine-camera:missing")
            {
                return "No local Cinemachine Camera could be resolved or materialized for this rig. Review the Camera reference in Advanced / Debug.";
            }

            return "The operation is blocked by the current Camera Rig configuration. Open Advanced / Debug for the technical diagnostic.";
        }

        private static string FormatExternalControlConflict(
            string detail,
            string stageLabel)
        {
            const string desiredMarker = ":desired=";
            int desiredIndex =
                detail.IndexOf(
                    desiredMarker,
                    System.StringComparison.Ordinal);

            string existingType = desiredIndex >= 0
                ? detail.Substring(0, desiredIndex)
                : detail;
            string desired = desiredIndex >= 0
                ? detail.Substring(
                    desiredIndex + desiredMarker.Length)
                : string.Empty;

            string existingLabel =
                ResolveTechnicalTypeLabel(existingType);
            string desiredLabel =
                ResolveDesiredControlLabel(
                    desired,
                    stageLabel);

            return $"{stageLabel} Control conflict. This Camera already contains external or unrecognized '{existingLabel}'. The selected Presentation requires {desiredLabel}. Remove the conflicting control or review it in Advanced / Debug.";
        }

        private static string FormatExternalControlDuplicate(
            string detail,
            string stageLabel)
        {
            string existingLabel =
                ResolveTechnicalTypeLabel(detail);

            return $"{stageLabel} Control conflict. This Camera contains multiple external or unrecognized controls of type '{existingLabel}'. Resolve the duplicate controls before Apply / Rebuild. See Advanced / Debug for technical evidence.";
        }

        private static string ResolveTechnicalTypeLabel(
            string technicalType)
        {
            if (string.IsNullOrWhiteSpace(technicalType))
            {
                return "control";
            }

            int separator =
                technicalType.LastIndexOf('.');

            return separator >= 0 &&
                   separator < technicalType.Length - 1
                ? technicalType.Substring(separator + 1)
                : technicalType;
        }

        private static string ResolveDesiredControlLabel(
            string desired,
            string stageLabel)
        {
            switch (desired)
            {
                case "HardLookAt":
                    return "a Look At rotation control";

                case "RotateWithFollowTarget":
                    return "rotation that follows the Camera Mount";

                case "Follow":
                    return "a Follow position control";

                case "HardLockToTarget":
                    return "a hard-lock position control";

                case "ThirdPersonFollow":
                    return "a Third Person Follow position control";

                case "None":
                    return $"no {stageLabel.ToLowerInvariant()} control";

                default:
                    return $"a different {stageLabel.ToLowerInvariant()} control";
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(7f);

            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    new GUIContent(
                        "Advanced / Debug",
                        "Shows technical target contracts, Cinemachine materialization evidence, ownership provenance and the last recorded authoring operation."),
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawTargetContractEvidence();
            DrawMaterializationEvidence();
            DrawLastOperationEvidence();
            DrawDiagnostics();

            EditorGUI.indentLevel--;
        }

        private void DrawTargetContractEvidence()
        {
            EditorGUILayout.LabelField(
                "Target Contract",
                EditorStyles.miniBoldLabel);

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();
            CameraTargetRequirement effectiveFollow =
                ResolveEffectiveFollowRequirement(
                    presentation);
            CameraTargetRequirement effectiveLookAt =
                ResolveEffectiveLookAtRequirement(
                    presentation);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    ResolveFollowRoleLabel(presentation),
                    effectiveFollow.ToString());

                EditorGUILayout.TextField(
                    "Look At",
                    effectiveLookAt.ToString());

                EditorGUILayout.PropertyField(
                    _targetSourceKind,
                    new GUIContent(
                        "Target Source Kind"));

                EditorGUILayout.PropertyField(
                    _lastResolvedFollowTarget,
                    new GUIContent(
                        "Last Resolved Tracking Target"));

                EditorGUILayout.PropertyField(
                    _lastResolvedLookAtTarget,
                    new GUIContent(
                        "Last Resolved Look At Target"));
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawMaterializationEvidence()
        {
            EditorGUILayout.LabelField(
                "Materialization",
                EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                _cinemachineCamera,
                new GUIContent(
                    "Cinemachine Camera",
                    "Explicit local Cinemachine Camera reference used by this Composer. A missing reference may be materialized locally by Apply / Rebuild."));

            bool technicalAuthoringChanged =
                EditorGUI.EndChangeCheck();

            bool advancedModified =
                serializedObject.ApplyModifiedProperties();

            if (technicalAuthoringChanged)
            {
                MarkAuthoringChanged();
            }

            if (advancedModified)
            {
                serializedObject.UpdateIfRequiredOrScript();
            }

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _materializedPresentationIntent,
                    new GUIContent(
                        "Materialized Presentation"));

                EditorGUILayout.PropertyField(
                    _materializationRevision,
                    new GUIContent(
                        "Materialization Revision"));

                if (local != null)
                {
                    EditorGUILayout.Vector3Field(
                        "World Position",
                        local.transform.position);
                    EditorGUILayout.Vector3Field(
                        "World Rotation",
                        local.transform.eulerAngles);
                }

                EditorGUILayout.PropertyField(
                    _frameworkOwnedCinemachineCamera,
                    new GUIContent(
                        "Owned Cinemachine Camera"));
            }

            DrawPipelineEvidence(local);

            EditorGUILayout.Space(4f);
        }

        private void DrawPipelineEvidence(
            CinemachineCamera local)
        {
            if (local == null)
            {
                EditorGUILayout.LabelField(
                    "Pipeline",
                    "No local Cinemachine Camera");
                return;
            }

            CinemachineComponentBase body =
                local.GetCinemachineComponent(
                    CinemachineCore.Stage.Body);
            CinemachineComponentBase aim =
                local.GetCinemachineComponent(
                    CinemachineCore.Stage.Aim);

            DrawPipelineControl(
                "Position Control",
                body,
                _frameworkOwnedPositionControl.objectReferenceValue as Component);

            DrawPipelineControl(
                "Rotation Control",
                aim,
                _frameworkOwnedRotationControl.objectReferenceValue as Component);
        }

        private static void DrawPipelineControl(
            string label,
            Component current,
            Component recordedFrameworkOwned)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    label,
                    current,
                    typeof(Component),
                    true);
            }

            string ownership;

            if (current == null)
            {
                ownership = "None";
            }
            else if (current ==
                     recordedFrameworkOwned)
            {
                ownership = "Framework-owned";
            }
            else
            {
                ownership = "External / Unknown";
            }

            EditorGUILayout.LabelField(
                $"{label} Ownership",
                ownership,
                EditorStyles.miniLabel);
        }

        private void DrawLastOperationEvidence()
        {
            EditorGUILayout.LabelField(
                "Last Operation",
                EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _lastApplyRebuildStatus,
                    new GUIContent(
                        "Status"));

                EditorGUILayout.PropertyField(
                    _lastBlockingIssue,
                    new GUIContent(
                        "Blocking Issue"));

                EditorGUILayout.PropertyField(
                    _lastTargetResolutionSummary,
                    new GUIContent(
                        "Target Resolution"));

                EditorGUILayout.PropertyField(
                    _lastMaterializationSummary,
                    new GUIContent(
                        "Materialization Summary"));
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawDiagnostics()
        {
            EditorGUILayout.LabelField(
                "Diagnostics",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(
                _logApplyRebuildDiagnostics,
                new GUIContent(
                    "Log Apply / Rebuild Diagnostics"));

            serializedObject.ApplyModifiedProperties();
        }

        private void MarkAuthoringChanged()
        {
            _materializationOutdated = true;

            if (_lastValidationResult.HasValue)
            {
                _validationOutdated = true;
            }
        }

        private CameraRigPresentationIntent ResolvePresentationIntent()
        {
            return (CameraRigPresentationIntent)
                _presentationIntent.intValue;
        }

        private CameraRigPresentationIntent ResolveMaterializedPresentationIntent()
        {
            return (CameraRigPresentationIntent)
                _materializedPresentationIntent.intValue;
        }

        private CameraTargetRequirement ResolveEffectiveLookAtRequirement(
            CameraRigPresentationIntent presentation)
        {
            switch (presentation)
            {
                case CameraRigPresentationIntent.Fixed:
                case CameraRigPresentationIntent.Follow:
                    return (CameraTargetRequirement)
                        _lookAtRequirement.intValue;

                case CameraRigPresentationIntent.Mounted:
                case CameraRigPresentationIntent.ThirdPerson:
                case CameraRigPresentationIntent.Undefined:
                default:
                    return CameraTargetRequirement.NotUsed;
            }
        }

        private static CameraTargetRequirement ResolveEffectiveFollowRequirement(
            CameraRigPresentationIntent presentation)
        {
            switch (presentation)
            {
                case CameraRigPresentationIntent.Follow:
                case CameraRigPresentationIntent.Mounted:
                case CameraRigPresentationIntent.ThirdPerson:
                    return CameraTargetRequirement.Required;

                case CameraRigPresentationIntent.Fixed:
                case CameraRigPresentationIntent.Undefined:
                default:
                    return CameraTargetRequirement.NotUsed;
            }
        }

        private static string ResolveFollowRoleLabel(
            CameraRigPresentationIntent presentation)
        {
            switch (presentation)
            {
                case CameraRigPresentationIntent.Mounted:
                    return "Camera Mount";

                case CameraRigPresentationIntent.ThirdPerson:
                    return "Tracking Pivot";

                default:
                    return "Tracking / Follow";
            }
        }

        private CinemachineCamera ResolveLocalCinemachineCamera()
        {
            CameraRigComposer composer =
                (CameraRigComposer)target;

            CinemachineCamera assigned =
                _cinemachineCamera.objectReferenceValue
                    as CinemachineCamera;

            return assigned != null
                ? assigned
                : composer.GetComponentInChildren<
                    CinemachineCamera>(
                    true);
        }

        private TargetAuthoringMode ResolveTargetAuthoringMode()
        {
            CameraTargetSourceKind kind =
                (CameraTargetSourceKind)
                    _targetSourceKind.intValue;

            return
                _targetSource.objectReferenceValue == null &&
                kind ==
                    CameraTargetSourceKind.ExplicitTransform
                    ? TargetAuthoringMode.ExplicitTransforms
                    : TargetAuthoringMode.TargetSourceComponent;
        }

        private void SetTargetAuthoringMode(
            TargetAuthoringMode mode)
        {
            if (mode ==
                TargetAuthoringMode.ExplicitTransforms)
            {
                _targetSource.objectReferenceValue =
                    null;

                _targetSourceKind.intValue =
                    (int)CameraTargetSourceKind
                        .ExplicitTransform;

                return;
            }

            if ((CameraTargetSourceKind)
                    _targetSourceKind.intValue ==
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

            if (assigned is
                ICameraTargetSource provider)
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

            _lastValidationResult =
                CameraRigComposerApplyRebuildUtility
                    .Validate(
                        (CameraRigComposer)target,
                        false);

            _validationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }

        private void RunApplyOrRebuild()
        {
            serializedObject.ApplyModifiedProperties();

            _lastApplyResult =
                CameraRigComposerApplyRebuildUtility
                    .ApplyOrRebuild(
                        (CameraRigComposer)target,
                        true,
                        true);

            _materializationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }
    }
}
