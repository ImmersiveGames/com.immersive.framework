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

        private SerializedProperty presentationIntent;
        private SerializedProperty targetSourceKind;
        private SerializedProperty targetSource;
        private SerializedProperty explicitFollowTarget;
        private SerializedProperty explicitLookAtTarget;
        private SerializedProperty lookAtRequirement;
        private SerializedProperty followOffset;
        private SerializedProperty mountedPositionDamping;
        private SerializedProperty mountedRotationDamping;
        private SerializedProperty thirdPersonShoulderOffset;
        private SerializedProperty thirdPersonVerticalArmLength;
        private SerializedProperty thirdPersonCameraSide;
        private SerializedProperty thirdPersonCameraDistance;
        private SerializedProperty thirdPersonDamping;
        private SerializedProperty cinemachineCamera;
        private SerializedProperty materializedPresentationIntent;
        private SerializedProperty frameworkOwnedCinemachineCamera;
        private SerializedProperty frameworkOwnedPositionControl;
        private SerializedProperty frameworkOwnedRotationControl;
        private SerializedProperty materializationRevision;
        private SerializedProperty logApplyRebuildDiagnostics;
        private SerializedProperty lastApplyRebuildStatus;
        private SerializedProperty lastBlockingIssue;
        private SerializedProperty lastTargetResolutionSummary;
        private SerializedProperty lastMaterializationSummary;
        private SerializedProperty lastResolvedFollowTarget;
        private SerializedProperty lastResolvedLookAtTarget;

        private CameraRigComposerApplyRebuildResult? lastOperationResult;
        private bool lastOperationOutdated;
        private bool showAdvancedDebug;

        private void OnEnable()
        {
            presentationIntent =
                serializedObject.FindProperty("presentationIntent");
            targetSourceKind =
                serializedObject.FindProperty("targetSourceKind");
            targetSource =
                serializedObject.FindProperty("targetSource");
            explicitFollowTarget =
                serializedObject.FindProperty("explicitFollowTarget");
            explicitLookAtTarget =
                serializedObject.FindProperty("explicitLookAtTarget");
            lookAtRequirement =
                serializedObject.FindProperty("lookAtRequirement");
            followOffset =
                serializedObject.FindProperty("followOffset");
            mountedPositionDamping =
                serializedObject.FindProperty("mountedPositionDamping");
            mountedRotationDamping =
                serializedObject.FindProperty("mountedRotationDamping");
            thirdPersonShoulderOffset =
                serializedObject.FindProperty("thirdPersonShoulderOffset");
            thirdPersonVerticalArmLength =
                serializedObject.FindProperty("thirdPersonVerticalArmLength");
            thirdPersonCameraSide =
                serializedObject.FindProperty("thirdPersonCameraSide");
            thirdPersonCameraDistance =
                serializedObject.FindProperty("thirdPersonCameraDistance");
            thirdPersonDamping =
                serializedObject.FindProperty("thirdPersonDamping");
            cinemachineCamera =
                serializedObject.FindProperty("cinemachineCamera");
            materializedPresentationIntent =
                serializedObject.FindProperty("materializedPresentationIntent");
            frameworkOwnedCinemachineCamera =
                serializedObject.FindProperty("frameworkOwnedCinemachineCamera");
            frameworkOwnedPositionControl =
                serializedObject.FindProperty("frameworkOwnedPositionControl");
            frameworkOwnedRotationControl =
                serializedObject.FindProperty("frameworkOwnedRotationControl");
            materializationRevision =
                serializedObject.FindProperty("materializationRevision");
            logApplyRebuildDiagnostics =
                serializedObject.FindProperty("logApplyRebuildDiagnostics");
            lastApplyRebuildStatus =
                serializedObject.FindProperty("lastApplyRebuildStatus");
            lastBlockingIssue =
                serializedObject.FindProperty("lastBlockingIssue");
            lastTargetResolutionSummary =
                serializedObject.FindProperty("lastTargetResolutionSummary");
            lastMaterializationSummary =
                serializedObject.FindProperty("lastMaterializationSummary");
            lastResolvedFollowTarget =
                serializedObject.FindProperty("lastResolvedFollowTarget");
            lastResolvedLookAtTarget =
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
                lastOperationOutdated = true;
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
                presentationIntent,
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
                    lookAtRequirement,
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
                    explicitFollowTarget,
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
                    explicitLookAtTarget,
                    new GUIContent(
                        "Look At Target",
                        "Explicit target consumed by the model's supported rotation behavior."));
            }
        }

        private void DrawTargetSourceComponent()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                targetSource,
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
                followOffset,
                new GUIContent(
                    "Follow Offset",
                    "Camera offset used by the Framework-owned Cinemachine Follow Position Control."));
        }

        private void DrawMountedSettings()
        {
            EditorGUILayout.PropertyField(
                mountedPositionDamping,
                new GUIContent(
                    "Position Damping",
                    "Damping used by Cinemachine Hard Lock to Target. Zero is a hard positional lock."));

            EditorGUILayout.PropertyField(
                mountedRotationDamping,
                new GUIContent(
                    "Rotation Damping",
                    "Damping used by Cinemachine Rotate With Follow Target. Zero matches mount rotation immediately."));
        }

        private void DrawThirdPersonSettings()
        {
            EditorGUILayout.PropertyField(
                thirdPersonShoulderOffset,
                new GUIContent(
                    "Shoulder Offset"));

            EditorGUILayout.PropertyField(
                thirdPersonVerticalArmLength,
                new GUIContent(
                    "Vertical Arm Length"));

            EditorGUILayout.PropertyField(
                thirdPersonCameraSide,
                new GUIContent(
                    "Camera Side",
                    "0 = left shoulder, 1 = right shoulder, intermediate values blend between sides."));

            EditorGUILayout.PropertyField(
                thirdPersonCameraDistance,
                new GUIContent(
                    "Camera Distance"));

            EditorGUILayout.PropertyField(
                thirdPersonDamping,
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
                    explicitFollowTarget.objectReferenceValue == null)
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
                    explicitLookAtTarget.objectReferenceValue == null)
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
                lastApplyRebuildStatus != null
                    ? lastApplyRebuildStatus.stringValue ?? string.Empty
                    : string.Empty;

            if (!lastOperationResult.HasValue &&
                string.IsNullOrWhiteSpace(
                    persistedStatus))
            {
                return;
            }

            FrameworkAuthoringInspectorGui.Section(
                "Last Authoring Result");

            string status =
                lastOperationResult.HasValue
                    ? lastOperationResult.Value.Status
                    : persistedStatus;

            EditorGUILayout.LabelField(
                "Status",
                string.IsNullOrWhiteSpace(status)
                    ? "Not Recorded"
                    : status);

            if (lastOperationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "The recorded result predates the current configuration. Validate or Apply / Rebuild again.",
                    MessageType.Warning);
                return;
            }

            bool succeeded =
                lastOperationResult.HasValue
                    ? lastOperationResult.Value.Succeeded
                    : string.IsNullOrWhiteSpace(
                        lastBlockingIssue != null
                            ? lastBlockingIssue.stringValue
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
            showAdvancedDebug =
                EditorGUILayout.Foldout(
                    showAdvancedDebug,
                    "Advanced / Debug",
                    true);

            if (!showAdvancedDebug)
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
                lastOperationOutdated = true;
            }
        }

        private void DrawTechnicalReference()
        {
            FrameworkAuthoringInspectorGui.Section(
                "Technical Reference");

            EditorGUILayout.PropertyField(
                cinemachineCamera,
                new GUIContent(
                    "Cinemachine Camera",
                    "Explicit local Cinemachine Camera reference used by this Composer. A missing reference may be materialized locally by Apply / Rebuild."));

            EditorGUILayout.PropertyField(
                logApplyRebuildDiagnostics,
                new GUIContent(
                    "Log Apply / Rebuild Diagnostics"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    targetSourceKind,
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
                    materializedPresentationIntent,
                    new GUIContent(
                        "Materialized Presentation",
                        "Presentation recorded by the last successful Framework materialization."));

                EditorGUILayout.PropertyField(
                    frameworkOwnedCinemachineCamera,
                    new GUIContent(
                        "Owned Cinemachine Camera",
                        "Exact serialized reference proving that this Cinemachine Camera was materialized by the Framework."));

                EditorGUILayout.PropertyField(
                    frameworkOwnedPositionControl,
                    new GUIContent(
                        "Owned Position Control",
                        "Exact serialized provenance for the Framework-owned Cinemachine Body / Position control."));

                EditorGUILayout.PropertyField(
                    frameworkOwnedRotationControl,
                    new GUIContent(
                        "Owned Rotation Control",
                        "Exact serialized provenance for the Framework-owned Cinemachine Aim / Rotation control."));

                EditorGUILayout.PropertyField(
                    materializationRevision,
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
                frameworkOwnedPositionControl.objectReferenceValue as Component);
            DrawPipelineControl(
                "Rotation Control",
                aim,
                frameworkOwnedRotationControl.objectReferenceValue as Component);
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
                    lastApplyRebuildStatus,
                    new GUIContent(
                        "Last Status"));

                EditorGUILayout.PropertyField(
                    lastBlockingIssue,
                    new GUIContent(
                        "Last Blocking Issue"));

                EditorGUILayout.PropertyField(
                    lastTargetResolutionSummary,
                    new GUIContent(
                        "Target Resolution"));

                EditorGUILayout.PropertyField(
                    lastMaterializationSummary,
                    new GUIContent(
                        "Materialization Summary"));

                EditorGUILayout.PropertyField(
                    lastResolvedFollowTarget,
                    new GUIContent(
                        "Resolved Tracking Target"));

                EditorGUILayout.PropertyField(
                    lastResolvedLookAtTarget,
                    new GUIContent(
                        "Resolved Look At Target"));
            }

            if (lastOperationResult.HasValue &&
                !lastOperationOutdated &&
                !lastOperationResult.Value.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    lastOperationResult.Value.BlockingIssue,
                    MessageType.Error);
            }
        }

        private bool HasRecordedOperation()
        {
            return lastOperationResult.HasValue ||
                (lastApplyRebuildStatus != null &&
                 !string.IsNullOrWhiteSpace(
                     lastApplyRebuildStatus.stringValue));
        }

        private CameraRigPresentationIntent ResolvePresentationIntent()
        {
            return (CameraRigPresentationIntent)
                presentationIntent.intValue;
        }

        private CameraRigPresentationIntent ResolveMaterializedPresentationIntent()
        {
            return (CameraRigPresentationIntent)
                materializedPresentationIntent.intValue;
        }

        private CameraTargetRequirement ResolveEffectiveLookAtRequirement(
            CameraRigPresentationIntent presentation)
        {
            switch (presentation)
            {
                case CameraRigPresentationIntent.Fixed:
                case CameraRigPresentationIntent.Follow:
                    return (CameraTargetRequirement)
                        lookAtRequirement.intValue;

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
                cinemachineCamera.objectReferenceValue
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
                    targetSourceKind.intValue;

            return
                targetSource.objectReferenceValue == null &&
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
                targetSource.objectReferenceValue =
                    null;

                targetSourceKind.intValue =
                    (int)CameraTargetSourceKind
                        .ExplicitTransform;

                return;
            }

            if ((CameraTargetSourceKind)
                    targetSourceKind.intValue ==
                CameraTargetSourceKind.ExplicitTransform)
            {
                targetSourceKind.intValue =
                    (int)CameraTargetSourceKind.None;
            }
        }

        private void SyncSerializedTargetSourceKind()
        {
            Object assigned =
                targetSource.objectReferenceValue;

            if (assigned is
                ICameraTargetSource provider)
            {
                targetSourceKind.intValue =
                    (int)provider.TargetSourceKind;
                return;
            }

            targetSourceKind.intValue =
                (int)CameraTargetSourceKind.None;
        }

        private void RunValidation()
        {
            serializedObject.ApplyModifiedProperties();

            lastOperationResult =
                CameraRigComposerApplyRebuildUtility
                    .Validate(
                        (CameraRigComposer)target,
                        false);

            lastOperationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }

        private void RunApplyOrRebuild()
        {
            serializedObject.ApplyModifiedProperties();

            lastOperationResult =
                CameraRigComposerApplyRebuildUtility
                    .ApplyOrRebuild(
                        (CameraRigComposer)target,
                        true,
                        true);

            lastOperationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }
    }
}
