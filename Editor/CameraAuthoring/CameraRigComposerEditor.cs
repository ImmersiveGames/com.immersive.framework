using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.Common;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    [CustomEditor(typeof(CameraRigComposer))]
    public sealed class CameraRigComposerEditor :
        UnityEditor.Editor
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

        private CameraRigComposerApplyRebuildResult? _lastOperationResult;
        private bool _lastOperationOutdated;
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
            DrawConfiguration();
            bool authoringChanged =
                EditorGUI.EndChangeCheck();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if ((authoringChanged || modified) &&
                HasRecordedOperation())
            {
                _lastOperationOutdated = true;
            }

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();

            DrawConfigurationStatus();

            DrawMaterializedState(presentation);
            DrawActions(presentation);
            DrawLastAuthoringResult();
            DrawAdvancedDebug();
        }

        private static void DrawComposerHeader()
        {
            EditorGUILayout.LabelField(
                new GUIContent(
                    "Camera Rig Composer",
                    "Authors one local gameplay Camera rig. Apply / Rebuild materializes only the local Cinemachine Camera and Framework-owned pipeline controls; Camera Output selection and request arbitration are separate runtime authorities."),
                EditorStyles.boldLabel);
        }

        private void DrawConfiguration()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Configuration");

            DrawPresentation();

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();

            EditorGUILayout.Space(5f);
            DrawTargets(presentation);

            EditorGUILayout.Space(5f);
            DrawModelSettings(presentation);
        }

        private void DrawPresentation()
        {
            EditorGUILayout.LabelField(
                "Presentation",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.PropertyField(
                _presentationIntent,
                new GUIContent(
                    "Model",
                    "Fixed preserves the local camera pose; Follow tracks one target with offset; Mounted locks to a Camera Mount and its rotation; Third Person tracks a rotating pivot using Cinemachine Third Person Follow. Presentation never decides runtime Camera Output arbitration."));

        }

        private void DrawTargets(
            CameraRigPresentationIntent presentation)
        {
            EditorGUILayout.LabelField(
                "Targets",
                EditorStyles.miniBoldLabel);

            if (presentation ==
                CameraRigPresentationIntent.Undefined)
            {
                return;
            }

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
                DrawTargetContract(
                    presentation,
                    effectiveFollow,
                    effectiveLookAt);
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

            EditorGUILayout.Space(3f);
            DrawTargetContract(
                presentation,
                effectiveFollow,
                effectiveLookAt);
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
                    "Component implementing ICameraTargetSource. The selected Presentation still defines which target roles are requested."));

            if (EditorGUI.EndChangeCheck())
            {
                SyncSerializedTargetSourceKind();
            }
        }

        private static void DrawTargetContract(
            CameraRigPresentationIntent presentation,
            CameraTargetRequirement effectiveFollow,
            CameraTargetRequirement effectiveLookAt)
        {
            EditorGUILayout.LabelField(
                "Effective Contract",
                EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    ResolveFollowRoleLabel(presentation),
                    effectiveFollow.ToString());
                EditorGUILayout.TextField(
                    "Look At",
                    effectiveLookAt.ToString());
            }
        }

        private void DrawModelSettings(
            CameraRigPresentationIntent presentation)
        {
            EditorGUILayout.LabelField(
                "Model Settings",
                EditorStyles.miniBoldLabel);

            switch (presentation)
            {
                case CameraRigPresentationIntent.Fixed:
                    DrawFixedSettings();
                    break;

                case CameraRigPresentationIntent.Follow:
                    DrawFollowSettings();
                    break;

                case CameraRigPresentationIntent.Mounted:
                    DrawMountedSettings();
                    break;

                case CameraRigPresentationIntent.ThirdPerson:
                    DrawThirdPersonSettings();
                    break;
            }
        }

        private void DrawFixedSettings()
        {
            CinemachineCamera local =
                ResolveLocalCinemachineCamera();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Pose Camera",
                        "Fixed uses the authored Transform pose of the local Cinemachine Camera. Apply / Rebuild preserves that pose and does not create a Position Control."),
                    local,
                    typeof(CinemachineCamera),
                    true);

                if (local != null)
                {
                    EditorGUILayout.Vector3Field(
                        "World Position",
                        local.transform.position);
                    EditorGUILayout.Vector3Field(
                        "World Rotation",
                        local.transform.eulerAngles);
                }
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

        private void DrawConfigurationStatus()
        {
            CameraRigComposer composer =
                (CameraRigComposer)target;

            FrameworkAuthoringInspectorGui.Section(
                "Configuration Status");

            if (!composer.TryValidateForApply(
                    out string diagnostic))
            {
                EditorGUILayout.LabelField(
                    "Status",
                    "Needs Attention");

                EditorGUILayout.HelpBox(
                    diagnostic,
                    MessageType.Error);
                return;
            }

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();
            CameraTargetRequirement effectiveFollow =
                ResolveEffectiveFollowRequirement(
                    presentation);
            CameraTargetRequirement effectiveLookAt =
                ResolveEffectiveLookAtRequirement(
                    presentation);

            if (ResolveTargetAuthoringMode() ==
                TargetAuthoringMode.ExplicitTransforms)
            {
                if (effectiveFollow ==
                        CameraTargetRequirement.Required &&
                    _explicitFollowTarget.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField(
                        "Status",
                        "Needs Attention");

                    EditorGUILayout.HelpBox(
                        $"{ResolveFollowRoleLabel(presentation)} is required for the selected Presentation.",
                        MessageType.Error);
                    return;
                }

                if (effectiveLookAt ==
                        CameraTargetRequirement.Required &&
                    _explicitLookAtTarget.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField(
                        "Status",
                        "Needs Attention");

                    EditorGUILayout.HelpBox(
                        "Look At Target is required for the selected Presentation.",
                        MessageType.Error);
                    return;
                }

                EditorGUILayout.LabelField(
                    "Status",
                    "Ready");
                return;
            }

            EditorGUILayout.LabelField(
                "Status",
                "Ready for Validation");
        }

        private void DrawMaterializedState(
            CameraRigPresentationIntent presentation)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Materialized State");

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();
            CameraRigPresentationIntent materialized =
                ResolveMaterializedPresentationIntent();
            string status;
            string warning = string.Empty;

            if (local == null)
            {
                status = "Not Materialized";
            }
            else if (materialized ==
                     CameraRigPresentationIntent.Undefined)
            {
                status = "Local Camera Present";
            }
            else if (presentation ==
                     CameraRigPresentationIntent.Undefined)
            {
                status = "Authored Configuration Incomplete";
                warning =
                    $"A local rig is materialized as '{materialized}', but the authored Presentation is Undefined.";
            }
            else if (materialized != presentation)
            {
                status = "Stale — Rebuild Required";
                warning =
                    $"Authored Presentation is '{presentation}' while materialized Presentation is '{materialized}'. Apply / Rebuild is required.";
            }
            else
            {
                status = "Current";
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(
                    "Status",
                    status);

                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Cinemachine Camera",
                        "Local Cinemachine Camera resolved or materialized for this Composer. No Unity Camera, Brain, Audio Listener or persistent Camera Output is created here."),
                    local,
                    typeof(CinemachineCamera),
                    true);

                EditorGUILayout.TextField(
                    "Materialized Presentation",
                    materialized.ToString());
            }

            if (!string.IsNullOrWhiteSpace(warning))
            {
                EditorGUILayout.HelpBox(
                    warning,
                    MessageType.Warning);
            }
        }

        private void DrawActions(
            CameraRigPresentationIntent presentation)
        {
            FrameworkAuthoringInspectorGui.Section(
                "Actions");

            using (new EditorGUI.DisabledScope(
                       Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Validate Configuration",
                            "Validate authored Camera Rig configuration and target resolution without materializing or changing the Cinemachine pipeline.")))
                {
                    RunValidation();
                }

                using (new EditorGUI.DisabledScope(
                           presentation ==
                           CameraRigPresentationIntent.Undefined))
                {
                    if (GUILayout.Button(
                            new GUIContent(
                                "Apply / Rebuild Rig",
                                "Preflight and reconcile the local Cinemachine Camera and only Framework-owned Position / Rotation controls. Unknown external conflicts block before mutation.")))
                    {
                        RunApplyOrRebuild();
                    }
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Authoring actions are unavailable in Play Mode.",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawLastAuthoringResult()
        {
            string persistedStatus =
                _lastApplyRebuildStatus != null
                    ? _lastApplyRebuildStatus.stringValue ?? string.Empty
                    : string.Empty;

            if (!_lastOperationResult.HasValue &&
                string.IsNullOrWhiteSpace(
                    persistedStatus))
            {
                return;
            }

            FrameworkAuthoringInspectorGui.Section(
                "Last Authoring Result");

            string status =
                _lastOperationResult.HasValue
                    ? _lastOperationResult.Value.Status
                    : persistedStatus;

            EditorGUILayout.LabelField(
                "Status",
                string.IsNullOrWhiteSpace(status)
                    ? "Not Recorded"
                    : status);

            if (_lastOperationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "The recorded result predates the current configuration. Validate or Apply / Rebuild again.",
                    MessageType.Warning);
                return;
            }

            bool succeeded =
                _lastOperationResult.HasValue
                    ? _lastOperationResult.Value.Succeeded
                    : string.IsNullOrWhiteSpace(
                        _lastBlockingIssue != null
                            ? _lastBlockingIssue.stringValue
                            : string.Empty);

            if (!succeeded)
            {
                EditorGUILayout.HelpBox(
                    "The last authoring operation found a blocking issue. See Advanced / Debug for evidence.",
                    MessageType.Error);
            }
        }

        private void DrawAdvancedDebug()
        {
            EditorGUILayout.Space(6f);
            _showAdvancedDebug =
                EditorGUILayout.Foldout(
                    _showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (!_showAdvancedDebug)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            DrawTechnicalReference();
            bool advancedChanged =
                EditorGUI.EndChangeCheck();

            DrawOwnershipEvidence();
            DrawPipelineEvidence();
            DrawLastOperationEvidence();

            bool advancedModified =
                serializedObject.ApplyModifiedProperties();

            if ((advancedChanged || advancedModified) &&
                HasRecordedOperation())
            {
                _lastOperationOutdated = true;
            }
        }

        private void DrawTechnicalReference()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Technical Reference");

            EditorGUILayout.PropertyField(
                _cinemachineCamera,
                new GUIContent(
                    "Cinemachine Camera",
                    "Explicit local Cinemachine Camera reference used by this Composer. A missing reference may be materialized locally by Apply / Rebuild."));

            EditorGUILayout.PropertyField(
                _logApplyRebuildDiagnostics,
                new GUIContent(
                    "Log Apply / Rebuild Diagnostics"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _targetSourceKind,
                    new GUIContent(
                        "Resolved Target Source Kind"));
            }
        }

        private void DrawOwnershipEvidence()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Materialization Provenance");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _materializedPresentationIntent,
                    new GUIContent(
                        "Materialized Presentation",
                        "Presentation recorded by the last successful Framework materialization."));

                EditorGUILayout.PropertyField(
                    _frameworkOwnedCinemachineCamera,
                    new GUIContent(
                        "Owned Cinemachine Camera",
                        "Exact serialized reference proving that this Cinemachine Camera was materialized by the Framework."));

                EditorGUILayout.PropertyField(
                    _frameworkOwnedPositionControl,
                    new GUIContent(
                        "Owned Position Control",
                        "Exact serialized provenance for the Framework-owned Cinemachine Body / Position control."));

                EditorGUILayout.PropertyField(
                    _frameworkOwnedRotationControl,
                    new GUIContent(
                        "Owned Rotation Control",
                        "Exact serialized provenance for the Framework-owned Cinemachine Aim / Rotation control."));

                EditorGUILayout.PropertyField(
                    _materializationRevision,
                    new GUIContent(
                        "Materialization Revision",
                        "Revision counter advanced by successful materialization."));
            }
        }

        private void DrawPipelineEvidence()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Current Cinemachine Pipeline");

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();

            if (local == null)
            {
                EditorGUILayout.LabelField(
                    "Status",
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
                ownership);

            if (current != null &&
                current != recordedFrameworkOwned)
            {
                EditorGUILayout.HelpBox(
                    $"{label} is External / Unknown. Apply / Rebuild will not replace it unless ownership is proven.",
                    MessageType.Warning);
            }
        }

        private void DrawLastOperationEvidence()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Last Operation Evidence");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    _lastApplyRebuildStatus,
                    new GUIContent(
                        "Last Status"));

                EditorGUILayout.PropertyField(
                    _lastBlockingIssue,
                    new GUIContent(
                        "Last Blocking Issue"));

                EditorGUILayout.PropertyField(
                    _lastTargetResolutionSummary,
                    new GUIContent(
                        "Target Resolution"));

                EditorGUILayout.PropertyField(
                    _lastMaterializationSummary,
                    new GUIContent(
                        "Materialization Summary"));

                EditorGUILayout.PropertyField(
                    _lastResolvedFollowTarget,
                    new GUIContent(
                        "Resolved Tracking Target"));

                EditorGUILayout.PropertyField(
                    _lastResolvedLookAtTarget,
                    new GUIContent(
                        "Resolved Look At Target"));
            }

            if (_lastOperationResult.HasValue &&
                !_lastOperationOutdated &&
                !_lastOperationResult.Value.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    _lastOperationResult.Value.BlockingIssue,
                    MessageType.Error);
            }
        }

        private bool HasRecordedOperation()
        {
            return _lastOperationResult.HasValue ||
                (_lastApplyRebuildStatus != null &&
                 !string.IsNullOrWhiteSpace(
                     _lastApplyRebuildStatus.stringValue));
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

            _lastOperationResult =
                CameraRigComposerApplyRebuildUtility
                    .Validate(
                        (CameraRigComposer)target,
                        false);

            _lastOperationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }

        private void RunApplyOrRebuild()
        {
            serializedObject.ApplyModifiedProperties();

            _lastOperationResult =
                CameraRigComposerApplyRebuildUtility
                    .ApplyOrRebuild(
                        (CameraRigComposer)target,
                        true,
                        true);

            _lastOperationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }
    }
}
