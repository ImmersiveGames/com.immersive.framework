using System;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Designer-facing authoring surface that owns one concrete Camera rig
    /// configuration and materializes one local Cinemachine Camera.
    ///
    /// The assigned Behavior definition owns reusable presentation intent and tuning.
    /// The Composer validates and materializes that intent on this concrete local rig.
    /// The explicit View-input seam receives resolved Subject evidence.
    ///
    /// It does not create or own a Unity Camera, CinemachineBrain, AudioListener
    /// or runtime Camera Output. It does not select an active camera or arbitrate
    /// Camera requests.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Rig Composer")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable per-Output Camera product surface for explicit Session 1..N topology; split-screen remains out of scope.")]
    public sealed class CameraRigComposer : MonoBehaviour
    {
        private const string DefaultCinemachineCameraObjectName =
            "Cinemachine Camera";

        [Header("Camera Behavior")]
        [SerializeField]
        private CameraRigBehaviorDefinition behaviorDefinition;

        [Header("Technical Materialization")]
        [SerializeField]
        private CinemachineCamera cinemachineCamera;

        // Durable provenance for editor materialization. These references are deliberately
        // hidden from the product surface: they prove ownership, but they are not authoring
        // controls. A component is Framework-owned only when its exact serialized reference
        // was recorded here at creation time.
        [SerializeField, HideInInspector]
        private CameraRigPresentationIntent materializedPresentationIntent =
            CameraRigPresentationIntent.Undefined;

        [SerializeField, HideInInspector]
        private CinemachineCamera frameworkOwnedCinemachineCamera;

        [SerializeField, HideInInspector]
        private Component frameworkOwnedPositionControl;

        [SerializeField, HideInInspector]
        private Component frameworkOwnedRotationControl;

        [SerializeField, HideInInspector]
        [FormerlySerializedAs("frameworkOwnedSharedFollowTargetGroup")]
        private CinemachineTargetGroup frameworkOwnedGroupTargetGroup;

        [SerializeField, HideInInspector]
        [FormerlySerializedAs("frameworkOwnedSharedFollowGroupFraming")]
        private CinemachineGroupFraming frameworkOwnedGroupFraming;

        private CameraViewPresentationAdapter _presentation;

        [SerializeField, HideInInspector]
        private int materializationRevision;

        [SerializeField]
        private bool logApplyRebuildDiagnostics = true;

        [Header("Debug")]
        [SerializeField]
        private string lastApplyRebuildStatus;

        [SerializeField]
        private string lastBlockingIssue;

        [SerializeField]
        private string lastMaterializationSummary;

        public CameraRigBehaviorDefinition BehaviorDefinition => behaviorDefinition;

        public CameraRigPresentationIntent PresentationIntent =>
            behaviorDefinition != null
                ? behaviorDefinition.PresentationIntent
                : CameraRigPresentationIntent.Undefined;

        public CameraTargetRequirement LookAtRequirement => EffectiveLookAtRequirement;
        public Vector3 FollowOffset => RequireFollowBehavior().FollowOffset;
        public Vector3 GroupFollowOffset => RequireGroupBehavior().FollowOffset;
        public float GroupMemberWeight => RequireGroupBehavior().MemberWeight;
        public float GroupMemberRadius => RequireGroupBehavior().MemberRadius;
        public float GroupFramingSize => RequireGroupBehavior().FramingSize;
        public float GroupDamping => RequireGroupBehavior().Damping;
        public Vector2 GroupFovRange => RequireGroupBehavior().FovRange;
        public Vector2 GroupDollyRange => RequireGroupBehavior().DollyRange;
        public Vector2 GroupOrthoSizeRange => RequireGroupBehavior().OrthoSizeRange;
        public float MountedPositionDamping => RequireMountedBehavior().PositionDamping;
        public float MountedRotationDamping => RequireMountedBehavior().RotationDamping;
        public Vector3 ThirdPersonShoulderOffset => RequireThirdPersonBehavior().ShoulderOffset;
        public float ThirdPersonVerticalArmLength => RequireThirdPersonBehavior().VerticalArmLength;
        public float ThirdPersonCameraSide => RequireThirdPersonBehavior().CameraSide;
        public float ThirdPersonCameraDistance => RequireThirdPersonBehavior().CameraDistance;
        public Vector3 ThirdPersonDamping => RequireThirdPersonBehavior().Damping;
        public CameraTargetRequirement EffectiveFollowRequirement =>
            behaviorDefinition != null ? behaviorDefinition.FollowRequirement : CameraTargetRequirement.NotUsed;
        public CameraTargetRequirement EffectiveLookAtRequirement =>
            behaviorDefinition != null ? behaviorDefinition.LookAtRequirement : CameraTargetRequirement.NotUsed;

        public CinemachineCamera CinemachineCamera =>
            cinemachineCamera;

        public CameraRigPresentationIntent MaterializedPresentationIntent =>
            materializedPresentationIntent;

        public CinemachineCamera FrameworkOwnedCinemachineCamera =>
            frameworkOwnedCinemachineCamera;

        public Component FrameworkOwnedPositionControl =>
            frameworkOwnedPositionControl;

        public Component FrameworkOwnedRotationControl =>
            frameworkOwnedRotationControl;

        public CinemachineTargetGroup FrameworkOwnedGroupTargetGroup =>
            frameworkOwnedGroupTargetGroup;

        public CinemachineGroupFraming FrameworkOwnedGroupFraming =>
            frameworkOwnedGroupFraming;

        public int MaterializationRevision =>
            materializationRevision;

        /// <summary>
        /// Apply / Rebuild always materializes a missing local Cinemachine Camera.
        /// This is a fixed Composer contract rather than designer policy.
        /// </summary>
        public bool CreateCinemachineCameraIfMissing =>
            true;

        public string CinemachineCameraObjectName =>
            DefaultCinemachineCameraObjectName;

        public bool LogApplyRebuildDiagnostics =>
            logApplyRebuildDiagnostics;

        public string LastApplyRebuildStatus =>
            lastApplyRebuildStatus.NormalizeText();

        public string LastBlockingIssue =>
            lastBlockingIssue.NormalizeText();

        public string LastMaterializationSummary =>
            lastMaterializationSummary.NormalizeText();

        public CameraViewPresentationApplyResult ApplyViewPresentation(
            CameraViewPresentationInput input, CameraViewAssignmentSnapshot currentSnapshot)
        {
            _presentation ??= new CameraViewPresentationAdapter(this);
            return _presentation.ApplyViewPresentation(input, currentSnapshot);
        }

        public CameraViewPresentationApplyResult ClearViewPresentation()
        {
            _presentation ??= new CameraViewPresentationAdapter(this);
            return _presentation.ClearViewPresentation();
        }

        public bool TryValidateForApply(
            out string issue)
        {
            if (behaviorDefinition == null)
            {
                issue = "CameraRigComposer requires a Camera Rig Behavior Definition.";
                return false;
            }

            if (behaviorDefinition.PresentationIntent != CameraRigPresentationIntent.Fixed &&
                behaviorDefinition.PresentationIntent != CameraRigPresentationIntent.Follow &&
                behaviorDefinition.PresentationIntent != CameraRigPresentationIntent.Mounted &&
                behaviorDefinition.PresentationIntent != CameraRigPresentationIntent.ThirdPerson &&
                behaviorDefinition.PresentationIntent != CameraRigPresentationIntent.Group)
            {
                issue = $"Camera Rig Behavior Definition '{behaviorDefinition.name}' has unsupported Presentation model '{behaviorDefinition.PresentationIntent}'.";
                return false;
            }

            if (behaviorDefinition.PresentationIntent == CameraRigPresentationIntent.Group &&
                !(behaviorDefinition is GroupCameraRigBehaviorDefinition))
            {
                issue = $"Camera Rig Behavior Definition '{behaviorDefinition.name}' must use GroupCameraRigBehaviorDefinition for Group presentation settings.";
                return false;
            }

            return behaviorDefinition.TryValidate(out issue);
        }

        /// <summary>
        /// Projects resolved View input according to this rig's presentation policy.
        /// </summary>
        public CameraViewTargetProjectionResult ResolveViewPresentationTargets(
            CameraViewPresentationInput input)
        {
            return CameraViewTargetProjector.Project(
                input,
                PresentationIntent,
                EffectiveFollowRequirement,
                EffectiveLookAtRequirement);
        }

        private FollowCameraRigBehaviorDefinition RequireFollowBehavior() =>
            behaviorDefinition as FollowCameraRigBehaviorDefinition ??
            throw new InvalidOperationException("The assigned Camera Rig Behavior Definition is not Follow.");

        private GroupCameraRigBehaviorDefinition RequireGroupBehavior() =>
            behaviorDefinition as GroupCameraRigBehaviorDefinition ??
            throw new InvalidOperationException("The assigned Camera Rig Behavior Definition is not Group.");

        private MountedCameraRigBehaviorDefinition RequireMountedBehavior() =>
            behaviorDefinition as MountedCameraRigBehaviorDefinition ??
            throw new InvalidOperationException("The assigned Camera Rig Behavior Definition is not Mounted.");

        private ThirdPersonCameraRigBehaviorDefinition RequireThirdPersonBehavior() =>
            behaviorDefinition as ThirdPersonCameraRigBehaviorDefinition ??
            throw new InvalidOperationException("The assigned Camera Rig Behavior Definition is not Third Person.");

#if UNITY_EDITOR
        public void EditorSetGeneratedReference(
            CinemachineCamera generatedCinemachineCamera)
        {
            if (cinemachineCamera == null)
            {
                cinemachineCamera =
                    generatedCinemachineCamera;
            }
        }

        public void EditorCommitMaterializationEvidence(
            CameraRigPresentationIntent materializedIntent,
            CinemachineCamera resolvedCinemachineCamera,
            bool cinemachineCameraFrameworkOwned,
            Component resolvedPositionControl,
            bool positionControlFrameworkOwned,
            Component resolvedRotationControl,
            bool rotationControlFrameworkOwned,
            int revision)
        {
            materializedPresentationIntent =
                materializedIntent;

            frameworkOwnedCinemachineCamera =
                cinemachineCameraFrameworkOwned
                    ? resolvedCinemachineCamera
                    : null;

            frameworkOwnedPositionControl =
                positionControlFrameworkOwned
                    ? resolvedPositionControl
                    : null;

            frameworkOwnedRotationControl =
                rotationControlFrameworkOwned
                    ? resolvedRotationControl
                    : null;

            materializationRevision =
                revision;
        }

        public void EditorSetApplyRebuildResult(string status, string blockingIssue, string materializationSummary)
        {
            lastApplyRebuildStatus = status.NormalizeText();
            lastBlockingIssue = blockingIssue.NormalizeText();
            lastMaterializationSummary = materializationSummary.NormalizeText();
        }

        public void EditorSetGroupMaterialization(CinemachineTargetGroup group, CinemachineGroupFraming framing)
        {
            frameworkOwnedGroupTargetGroup = group;
            frameworkOwnedGroupFraming = framing;
        }

        private void Reset()
        {
            behaviorDefinition = null;

            cinemachineCamera =
                GetComponentInChildren<CinemachineCamera>(
                    true);

            materializedPresentationIntent =
                CameraRigPresentationIntent.Undefined;
            frameworkOwnedCinemachineCamera = null;
            frameworkOwnedPositionControl = null;
            frameworkOwnedRotationControl = null;
            frameworkOwnedGroupTargetGroup = null;
            frameworkOwnedGroupFraming = null;
            materializationRevision = 0;
        }
#endif
    }
}
