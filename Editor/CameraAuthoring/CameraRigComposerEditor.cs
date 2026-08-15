using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
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
        private bool validationOutdated;
        private bool showAdvancedDiagnostics;

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

            DrawInspectorHeader();

            EditorGUILayout.Space(8f);
            DrawPresentation();

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();

            EditorGUILayout.Space(8f);
            DrawTargets(presentation);

            EditorGUILayout.Space(8f);
            DrawModelSettings(presentation);

            EditorGUILayout.Space(8f);
            DrawMaterialization(presentation);

            EditorGUILayout.Space(8f);
            DrawValidation();

            EditorGUILayout.Space(8f);
            DrawAdvancedDiagnostics();

            bool modified =
                serializedObject.ApplyModifiedProperties();

            if (modified &&
                lastOperationResult.HasValue)
            {
                validationOutdated = true;
            }
        }

        private static void DrawInspectorHeader()
        {
            EditorGUILayout.LabelField(
                "Camera Rig Composer",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Choose one Presentation model for this local rig. The model defines target roles and the supported Cinemachine pipeline. Camera Output selection and request arbitration remain separate runtime authorities.",
                MessageType.Info);

            EditorGUILayout.HelpBox(
                "Apply / Rebuild only materializes the local Cinemachine Camera and Framework-owned pipeline controls. It never creates a Unity Camera, Cinemachine Brain, Audio Listener or persistent Camera Output.",
                MessageType.None);
        }

        private void DrawPresentation()
        {
            EditorGUILayout.LabelField(
                "Presentation",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                presentationIntent,
                new GUIContent(
                    "Model",
                    "Fixed, Follow, Mounted and Third Person are local rig presentation models. They never decide which rig wins runtime Camera Output arbitration."));

            CameraRigPresentationIntent presentation =
                ResolvePresentationIntent();

            switch (presentation)
            {
                case CameraRigPresentationIntent.Fixed:
                    EditorGUILayout.HelpBox(
                        "Fixed preserves the authored Cinemachine Camera Transform. It has no procedural Position Control. Look At may optionally supply rotation behavior.",
                        MessageType.None);
                    break;

                case CameraRigPresentationIntent.Follow:
                    EditorGUILayout.HelpBox(
                        "Follow maintains an authored offset from one required Tracking target. Optional/required Look At materializes a supported rotation control.",
                        MessageType.None);
                    break;

                case CameraRigPresentationIntent.Mounted:
                    EditorGUILayout.HelpBox(
                        "Mounted hard-locks position to one required Camera Mount / Tracking target and matches that target's rotation. Gameplay owns movement/rotation of the mount.",
                        MessageType.None);
                    break;

                case CameraRigPresentationIntent.ThirdPerson:
                    EditorGUILayout.HelpBox(
                        "Third Person uses one required rotating Tracking target/pivot and Cinemachine Third Person Follow framing. Gameplay owns input that rotates the target.",
                        MessageType.None);
                    break;

                case CameraRigPresentationIntent.Undefined:
                default:
                    EditorGUILayout.HelpBox(
                        "Choose an explicit Presentation model before validation or Apply / Rebuild.",
                        MessageType.Error);
                    break;
            }
        }

        private void DrawTargets(
            CameraRigPresentationIntent presentation)
        {
            EditorGUILayout.LabelField(
                "Targets",
                EditorStyles.boldLabel);

            if (presentation ==
                CameraRigPresentationIntent.Undefined)
            {
                EditorGUILayout.HelpBox(
                    "Target controls become available after a Presentation model is selected.",
                    MessageType.None);
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
                EditorGUILayout.HelpBox(
                    "This Presentation currently requires no target source.",
                    MessageType.None);
                EditorGUILayout.Space(4f);
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
                        "Use direct Transform references or one typed ICameraTargetSource component. Presentation materializers never perform scene lookup."),
                    currentMode);

            if (selectedMode != currentMode)
            {
                SetTargetAuthoringMode(selectedMode);
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

            EditorGUILayout.Space(4f);
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
                        "Explicit Transform used as the model's Tracking/Follow target."));
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

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    targetSourceKind,
                    new GUIContent(
                        "Resolved Source Kind"));
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

            EditorGUILayout.LabelField(
                ResolveFollowRoleLabel(presentation),
                effectiveFollow.ToString());
            EditorGUILayout.LabelField(
                "Look At",
                effectiveLookAt.ToString());
        }

        private void DrawModelSettings(
            CameraRigPresentationIntent presentation)
        {
            EditorGUILayout.LabelField(
                "Model Settings",
                EditorStyles.boldLabel);

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

                case CameraRigPresentationIntent.Undefined:
                default:
                    EditorGUILayout.HelpBox(
                        "No model settings are available until Presentation is selected.",
                        MessageType.None);
                    break;
            }
        }

        private void DrawFixedSettings()
        {
            EditorGUILayout.HelpBox(
                "Pose is authored directly on the local Cinemachine Camera Transform in the Scene. Apply / Rebuild preserves that pose and does not create a Position Control.",
                MessageType.Info);

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();

            if (local == null)
            {
                EditorGUILayout.HelpBox(
                    "Apply / Rebuild will create the local Cinemachine Camera. Position it in the Scene after creation, then subsequent rebuilds will preserve the authored pose.",
                    MessageType.None);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
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

        private void DrawMaterialization(
            CameraRigPresentationIntent presentation)
        {
            EditorGUILayout.LabelField(
                "Materialization",
                EditorStyles.boldLabel);

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();

            if (local == null)
            {
                EditorGUILayout.HelpBox(
                    "No local Cinemachine Camera exists. Apply / Rebuild Rig will create one automatically.",
                    MessageType.Info);
            }
            else if (presentation ==
                     CameraRigPresentationIntent.Fixed)
            {
                EditorGUILayout.HelpBox(
                    $"Cinemachine Camera '{local.name}' will be reused. Its authored Transform pose is preserved while owned pipeline controls are reconciled.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Cinemachine Camera '{local.name}' will be reused. Apply / Rebuild reconciles only Framework-owned Body/Aim controls and blocks on unknown incompatible controls.",
                    MessageType.None);
            }

            if (presentation !=
                CameraRigPresentationIntent.Undefined &&
                ResolveMaterializedPresentationIntent() !=
                    CameraRigPresentationIntent.Undefined &&
                ResolveMaterializedPresentationIntent() !=
                    presentation)
            {
                EditorGUILayout.HelpBox(
                    $"Presentation switch detected: materialized '{ResolveMaterializedPresentationIntent()}' -> authored '{presentation}'. Only controls proven Framework-owned may be replaced.",
                    MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(
                       presentation ==
                       CameraRigPresentationIntent.Undefined))
            {
                if (GUILayout.Button(
                        "Apply / Rebuild Rig"))
                {
                    RunApplyOrRebuild();
                }
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.LabelField(
                "Validation",
                EditorStyles.boldLabel);

            if (!lastOperationResult.HasValue)
            {
                EditorGUILayout.HelpBox(
                    "Not validated. Configure the rig and run validation.",
                    MessageType.None);
            }
            else if (validationOutdated)
            {
                EditorGUILayout.HelpBox(
                    "Validation result is outdated because the configuration changed.",
                    MessageType.Warning);
            }
            else if (lastOperationResult.Value.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    "Ready — the last validation or materialization operation succeeded.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Needs Attention — the last operation found a blocking issue. Open Advanced / Diagnostics.",
                    MessageType.Error);
            }

            if (GUILayout.Button(
                    "Validate Configuration"))
            {
                RunValidation();
            }
        }

        private void DrawAdvancedDiagnostics()
        {
            showAdvancedDiagnostics =
                EditorGUILayout.Foldout(
                    showAdvancedDiagnostics,
                    "Advanced / Diagnostics",
                    true);

            if (!showAdvancedDiagnostics)
            {
                return;
            }

            EditorGUI.indentLevel++;

            DrawTechnicalReference();
            EditorGUILayout.Space(6f);
            DrawOwnershipEvidence();
            EditorGUILayout.Space(6f);
            DrawPipelineEvidence();
            EditorGUILayout.Space(6f);
            DrawLastOperationEvidence();

            if (lastOperationResult.HasValue &&
                !validationOutdated &&
                !lastOperationResult.Value.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    lastOperationResult.Value.BlockingIssue,
                    MessageType.Error);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawTechnicalReference()
        {
            EditorGUILayout.LabelField(
                "Technical Reference",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                cinemachineCamera,
                new GUIContent(
                    "Cinemachine Camera",
                    "Explicit local Cinemachine Camera reference used by this Composer. A missing reference may be materialized locally by Apply / Rebuild."));

            EditorGUILayout.PropertyField(
                logApplyRebuildDiagnostics,
                new GUIContent(
                    "Log Apply / Rebuild Diagnostics"));
        }

        private void DrawOwnershipEvidence()
        {
            EditorGUILayout.LabelField(
                "Persistent Materialization Provenance",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    materializedPresentationIntent,
                    new GUIContent(
                        "Materialized Presentation"));

                EditorGUILayout.PropertyField(
                    frameworkOwnedCinemachineCamera,
                    new GUIContent(
                        "Owned Cinemachine Camera"));

                EditorGUILayout.PropertyField(
                    frameworkOwnedPositionControl,
                    new GUIContent(
                        "Owned Position Control"));

                EditorGUILayout.PropertyField(
                    frameworkOwnedRotationControl,
                    new GUIContent(
                        "Owned Rotation Control"));

                EditorGUILayout.PropertyField(
                    materializationRevision,
                    new GUIContent(
                        "Materialization Revision"));
            }

            EditorGUILayout.HelpBox(
                "Ownership is exact-reference provenance. A component that merely exists on the same Cinemachine Camera is External / Unknown unless this Composer recorded that exact reference when it materialized the component.",
                MessageType.None);
        }

        private void DrawPipelineEvidence()
        {
            EditorGUILayout.LabelField(
                "Current Cinemachine Pipeline",
                EditorStyles.boldLabel);

            CinemachineCamera local =
                ResolveLocalCinemachineCamera();

            if (local == null)
            {
                EditorGUILayout.HelpBox(
                    "No local Cinemachine Camera is available for pipeline inspection.",
                    MessageType.None);
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
            MessageType messageType;

            if (current == null)
            {
                ownership = "None";
                messageType = MessageType.None;
            }
            else if (current ==
                     recordedFrameworkOwned)
            {
                ownership = "Framework-owned";
                messageType = MessageType.Info;
            }
            else
            {
                ownership = "External / Unknown";
                messageType = MessageType.Warning;
            }

            EditorGUILayout.HelpBox(
                $"{label}: {ownership}.",
                messageType);
        }

        private void DrawLastOperationEvidence()
        {
            EditorGUILayout.LabelField(
                "Last Operation",
                EditorStyles.boldLabel);

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
            var composer =
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
                targetSource.objectReferenceValue ==
                    null &&
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

            validationOutdated = false;

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

            validationOutdated = false;

            serializedObject
                .UpdateIfRequiredOrScript();
        }
    }
}
