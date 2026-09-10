using System;
using Immersive.Framework.Camera;
using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>
    /// Designer-facing authoring surface that owns one concrete Camera rig
    /// configuration and materializes one local Cinemachine Camera.
    ///
    /// The Composer is the presentation authority for Follow/Look At requirements and
    /// framing. The explicit View-input seam receives resolved Subject evidence.
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
        private CameraRigPresentationIntent presentationIntent =
            CameraRigPresentationIntent.Follow;

        [SerializeField]
        private CameraTargetRequirement lookAtRequirement =
            CameraTargetRequirement.Optional;

        [SerializeField]
        private Vector3 followOffset =
            new Vector3(0f, 5f, -8f);

        [Header("Shared Follow Settings")]
        [SerializeField, Min(0.0001f)]
        private float sharedFollowMemberWeight = 1f;

        [SerializeField, Min(0.0001f)]
        private float sharedFollowMemberRadius = 0.5f;

        [SerializeField, Range(0.01f, 2f)]
        private float sharedFollowFramingSize = 0.8f;

        [SerializeField, Range(0f, 20f)]
        private float sharedFollowDamping = 2f;

        [SerializeField]
        private Vector2 sharedFollowFovRange = new Vector2(1f, 100f);

        [SerializeField]
        private Vector2 sharedFollowDollyRange = new Vector2(-100f, 100f);

        [SerializeField]
        private Vector2 sharedFollowOrthoSizeRange = new Vector2(1f, 1000f);

        [Header("Mounted Settings")]
        [SerializeField, Min(0f)]
        private float mountedPositionDamping;

        [SerializeField, Min(0f)]
        private float mountedRotationDamping;

        [Header("Third Person Settings")]
        [SerializeField]
        private Vector3 thirdPersonShoulderOffset =
            new Vector3(0.5f, -0.4f, 0f);

        [SerializeField]
        private float thirdPersonVerticalArmLength = 0.4f;

        [SerializeField, Range(0f, 1f)]
        private float thirdPersonCameraSide = 1f;

        [SerializeField, Min(0f)]
        private float thirdPersonCameraDistance = 2f;

        [SerializeField]
        private Vector3 thirdPersonDamping =
            new Vector3(0.1f, 0.5f, 0.3f);

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
        private CinemachineTargetGroup frameworkOwnedSharedFollowTargetGroup;

        [SerializeField, HideInInspector]
        private CinemachineGroupFraming frameworkOwnedSharedFollowGroupFraming;

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

        public CameraRigPresentationIntent PresentationIntent =>
            presentationIntent;

        public CameraTargetRequirement LookAtRequirement =>
            lookAtRequirement;

        public Vector3 FollowOffset =>
            followOffset;

        public float SharedFollowMemberWeight => sharedFollowMemberWeight;
        public float SharedFollowMemberRadius => sharedFollowMemberRadius;
        public float SharedFollowFramingSize => sharedFollowFramingSize;
        public float SharedFollowDamping => sharedFollowDamping;
        public Vector2 SharedFollowFovRange => sharedFollowFovRange;
        public Vector2 SharedFollowDollyRange => sharedFollowDollyRange;
        public Vector2 SharedFollowOrthoSizeRange => sharedFollowOrthoSizeRange;

        public float MountedPositionDamping =>
            mountedPositionDamping;

        public float MountedRotationDamping =>
            mountedRotationDamping;

        public Vector3 ThirdPersonShoulderOffset =>
            thirdPersonShoulderOffset;

        public float ThirdPersonVerticalArmLength =>
            thirdPersonVerticalArmLength;

        public float ThirdPersonCameraSide =>
            thirdPersonCameraSide;

        public float ThirdPersonCameraDistance =>
            thirdPersonCameraDistance;

        public Vector3 ThirdPersonDamping =>
            thirdPersonDamping;

        public CameraTargetRequirement EffectiveFollowRequirement
        {
            get
            {
                switch (presentationIntent)
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
        }

        public CameraTargetRequirement EffectiveLookAtRequirement
        {
            get
            {
                switch (presentationIntent)
                {
                    case CameraRigPresentationIntent.Follow:
                        return lookAtRequirement;

                    case CameraRigPresentationIntent.Mounted:
                    case CameraRigPresentationIntent.ThirdPerson:
                    case CameraRigPresentationIntent.Undefined:
                    default:
                        return CameraTargetRequirement.NotUsed;
                }
            }
        }

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

        public CinemachineTargetGroup FrameworkOwnedSharedFollowTargetGroup =>
            frameworkOwnedSharedFollowTargetGroup;

        public CinemachineGroupFraming FrameworkOwnedSharedFollowGroupFraming =>
            frameworkOwnedSharedFollowGroupFraming;

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
            issue = string.Empty;

            if (!IsDefinedRequirement(EffectiveLookAtRequirement))
            {
                issue =
                    $"CameraRigComposer has invalid Look At requirement '{lookAtRequirement}' for presentation '{presentationIntent}'.";
                return false;
            }

            switch (presentationIntent)
            {
                case CameraRigPresentationIntent.Fixed:
                    return true;

                case CameraRigPresentationIntent.Follow:
                    if (!IsFinite(followOffset))
                    {
                        issue =
                            "Follow presentation requires a finite Follow Offset.";
                        return false;
                    }

                    return TryValidateSharedFollowSettings(out issue);

                case CameraRigPresentationIntent.Mounted:
                    if (!IsFiniteNonNegative(mountedPositionDamping) ||
                        !IsFiniteNonNegative(mountedRotationDamping))
                    {
                        issue =
                            "Mounted presentation damping values must be finite and non-negative.";
                        return false;
                    }

                    return true;

                case CameraRigPresentationIntent.ThirdPerson:
                    if (!IsFinite(thirdPersonShoulderOffset) ||
                        !IsFinite(thirdPersonVerticalArmLength) ||
                        !IsFinite(thirdPersonCameraSide) ||
                        thirdPersonCameraSide < 0f ||
                        thirdPersonCameraSide > 1f ||
                        !IsFiniteNonNegative(thirdPersonCameraDistance) ||
                        !IsFiniteNonNegative(thirdPersonDamping))
                    {
                        issue =
                            "Third Person presentation settings contain invalid, non-finite or out-of-range values.";
                        return false;
                    }

                    return true;

                case CameraRigPresentationIntent.Undefined:
                    issue =
                        "CameraRigComposer requires an explicit Presentation intent.";
                    return false;

                default:
                    issue =
                        $"CameraRigComposer does not support Presentation intent '{presentationIntent}'.";
                    return false;
            }
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

        private static bool IsDefinedRequirement(
            CameraTargetRequirement requirement)
        {
            return requirement == CameraTargetRequirement.NotUsed ||
                   requirement == CameraTargetRequirement.Optional ||
                   requirement == CameraTargetRequirement.Required;
        }

        internal bool TryValidateSharedFollowSettings(out string issue)
        {
            issue = string.Empty;
            if (!IsFinite(sharedFollowMemberWeight) || sharedFollowMemberWeight <= 0f ||
                !IsFinite(sharedFollowMemberRadius) || sharedFollowMemberRadius <= 0f ||
                !IsFinite(sharedFollowFramingSize) || sharedFollowFramingSize < 0.01f || sharedFollowFramingSize > 2f ||
                !IsFinite(sharedFollowDamping) || sharedFollowDamping < 0f || sharedFollowDamping > 20f ||
                !IsOrderedRange(sharedFollowFovRange, 1f, 179f) ||
                !IsOrderedRange(sharedFollowDollyRange, float.MinValue, float.MaxValue) ||
                !IsOrderedRange(sharedFollowOrthoSizeRange, 0.01f, float.MaxValue))
            {
                issue = "Shared Follow settings require finite positive Weight/Radius, valid Framing Size/Damping and ordered FOV, Dolly and Orthographic ranges.";
                return false;
            }

            return true;
        }

        private static bool IsOrderedRange(Vector2 range, float minimum, float maximum)
        {
            return IsFinite(range.x) && IsFinite(range.y) &&
                   range.x >= minimum && range.y <= maximum && range.x <= range.y;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return IsFinite(value) &&
                   value >= 0f;
        }

        private static bool IsFiniteNonNegative(Vector3 value)
        {
            return IsFinite(value) &&
                   value.x >= 0f &&
                   value.y >= 0f &&
                   value.z >= 0f;
        }

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

        public void EditorSetSharedFollowMaterialization(CinemachineTargetGroup group, CinemachineGroupFraming framing)
        {
            frameworkOwnedSharedFollowTargetGroup = group;
            frameworkOwnedSharedFollowGroupFraming = framing;
        }

        private void Reset()
        {
            presentationIntent =
                CameraRigPresentationIntent.Follow;

            lookAtRequirement =
                CameraTargetRequirement.Optional;

            followOffset =
                new Vector3(0f, 5f, -8f);

            sharedFollowMemberWeight = 1f;
            sharedFollowMemberRadius = 0.5f;
            sharedFollowFramingSize = 0.8f;
            sharedFollowDamping = 2f;
            sharedFollowFovRange = new Vector2(1f, 100f);
            sharedFollowDollyRange = new Vector2(-100f, 100f);
            sharedFollowOrthoSizeRange = new Vector2(1f, 1000f);

            mountedPositionDamping = 0f;
            mountedRotationDamping = 0f;

            thirdPersonShoulderOffset =
                new Vector3(0.5f, -0.4f, 0f);
            thirdPersonVerticalArmLength = 0.4f;
            thirdPersonCameraSide = 1f;
            thirdPersonCameraDistance = 2f;
            thirdPersonDamping =
                new Vector3(0.1f, 0.5f, 0.3f);

            cinemachineCamera =
                GetComponentInChildren<CinemachineCamera>(
                    true);

            materializedPresentationIntent =
                CameraRigPresentationIntent.Undefined;
            frameworkOwnedCinemachineCamera = null;
            frameworkOwnedPositionControl = null;
            frameworkOwnedRotationControl = null;
            frameworkOwnedSharedFollowTargetGroup = null;
            frameworkOwnedSharedFollowGroupFraming = null;
            materializationRevision = 0;
        }
#endif
    }
}
