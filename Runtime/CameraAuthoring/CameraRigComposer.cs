using System;
using System.Collections.Generic;
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
    /// framing. Legacy authored target-source resolution remains available, while the
    /// explicit View-input seam receives already resolved Subject evidence.
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
        private const string DefaultSharedFollowTargetGroupObjectName =
            "Shared Follow Target Group";

        [Header("Camera Behavior")]
        [SerializeField]
        private CameraRigPresentationIntent presentationIntent =
            CameraRigPresentationIntent.Follow;

        [SerializeField]
        private CameraTargetSourceKind targetSourceKind =
            CameraTargetSourceKind.ExplicitTransform;

        [Tooltip(
            "Optional explicit component implementing ICameraTargetSource. " +
            "Any other MonoBehaviour is rejected by validation.")]
        [SerializeField]
        private MonoBehaviour targetSource;

        [SerializeField]
        private Transform explicitFollowTarget;

        [SerializeField]
        private Transform explicitLookAtTarget;

        [SerializeField]
        private CameraTargetRequirement followRequirement =
            CameraTargetRequirement.Required;

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

        [SerializeField, HideInInspector]
        private string appliedViewAssignmentContextId;

        [SerializeField, HideInInspector]
        private int appliedViewAssignmentRevision = -1;

        [SerializeField, HideInInspector]
        private string appliedViewAvailabilityContextId;

        [SerializeField, HideInInspector]
        private int appliedViewAvailabilityRevision = -1;

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
        private string lastTargetResolutionSummary;

        [SerializeField]
        private string lastMaterializationSummary;

        [SerializeField]
        private Transform lastResolvedFollowTarget;

        [SerializeField]
        private Transform lastResolvedLookAtTarget;

        public CameraRigPresentationIntent PresentationIntent =>
            presentationIntent;

        public CameraTargetSourceKind TargetSourceKind =>
            targetSourceKind;

        public MonoBehaviour TargetSourceBehaviour =>
            targetSource;

        public ICameraTargetSource TargetSource =>
            targetSource as ICameraTargetSource;

        public Transform ExplicitFollowTarget =>
            explicitFollowTarget;

        public Transform ExplicitLookAtTarget =>
            explicitLookAtTarget;

        public CameraTargetRequirement FollowRequirement =>
            followRequirement;

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
                    case CameraRigPresentationIntent.Fixed:
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

        public string LastTargetResolutionSummary =>
            lastTargetResolutionSummary.NormalizeText();

        public string LastMaterializationSummary =>
            lastMaterializationSummary.NormalizeText();

        public Transform LastResolvedFollowTarget =>
            lastResolvedFollowTarget;

        public Transform LastResolvedLookAtTarget =>
            lastResolvedLookAtTarget;

        public bool TryValidateForApply(
            out string issue)
        {
            issue = string.Empty;

            if (targetSource != null &&
                TargetSource == null)
            {
                issue =
                    $"Assigned Camera Target Source '{targetSource.GetType().FullName}' does not implement ICameraTargetSource.";
                return false;
            }

            if (targetSource == null &&
                targetSourceKind !=
                    CameraTargetSourceKind.ExplicitTransform)
            {
                issue =
                    $"CameraRigComposer requires a typed target-source component for source kind '{targetSourceKind}'.";
                return false;
            }

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

                    return true;

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

        public CameraTargetResolveResult ResolveCameraTargets(
            CameraTargetRequirement requestedFollowRequirement,
            CameraTargetRequirement requestedLookAtRequirement)
        {
            if (targetSource != null)
            {
                ICameraTargetSource provider =
                    TargetSource;

                if (provider == null)
                {
                    return CameraTargetResolveResult.Blocked(
                        new CameraTargetSourceDescriptor(
                            CameraTargetSourceKind.None,
                            targetSource,
                            string.Empty,
                            $"InvalidTargetSource:{targetSource.GetType().FullName}"),
                        "Assigned component does not implement ICameraTargetSource.",
                        "Camera rig target resolution was blocked by invalid target-source authoring.");
                }

                try
                {
                    return provider.ResolveCameraTargets(
                        requestedFollowRequirement,
                        requestedLookAtRequirement);
                }
                catch (Exception exception)
                {
                    return CameraTargetResolveResult.Blocked(
                        new CameraTargetSourceDescriptor(
                            provider.TargetSourceKind,
                            targetSource,
                            string.Empty,
                            provider.GetType().FullName),
                        $"Camera target source threw during resolution. {exception.Message}",
                        "Camera rig target resolution failed explicitly.",
                        CameraIssue.Blocking(
                            "camera.target-source.resolve-failed",
                            exception.Message));
                }
            }

            if (targetSourceKind !=
                CameraTargetSourceKind.ExplicitTransform)
            {
                return CameraTargetResolveResult.Blocked(
                    new CameraTargetSourceDescriptor(
                        targetSourceKind,
                        null,
                        string.Empty,
                        $"UnsupportedTargetSource:{targetSourceKind}"),
                    $"CameraRigComposer requires a typed provider for target source kind '{targetSourceKind}'.",
                    "Camera rig target resolution was blocked by unsupported source authoring.");
            }

            Transform explicitSourceTarget =
                requestedFollowRequirement !=
                CameraTargetRequirement.NotUsed
                    ? explicitFollowTarget
                    : explicitLookAtTarget;

            CameraTargetSourceDescriptor source =
                CameraTargetSourceDescriptor.ExplicitTransform(
                    explicitSourceTarget,
                    explicitSourceTarget != null
                        ? "ExplicitTransform"
                        : "ExplicitTransform:missing");

            var targets =
                new CameraResolvedTargets(
                    requestedFollowRequirement ==
                        CameraTargetRequirement.NotUsed
                            ? null
                            : explicitFollowTarget,
                    requestedLookAtRequirement ==
                        CameraTargetRequirement.NotUsed
                            ? null
                            : explicitLookAtTarget);

            return CameraTargetResolveResult.ValidateRequirements(
                source,
                targets,
                requestedFollowRequirement,
                requestedLookAtRequirement);
        }

        public CameraTargetResolveResult ResolveConfiguredCameraTargets()
        {
            return ResolveCameraTargets(
                EffectiveFollowRequirement,
                EffectiveLookAtRequirement);
        }

        /// <summary>
        /// Explicit CAMERA-026-C path for already resolved View input. This operation
        /// never reads or merges the legacy target-source authoring fields.
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

        /// <summary>
        /// Physically applies one current View input to the existing local Cinemachine
        /// Camera. This explicit operation never resolves or merges legacy target sources.
        /// </summary>
        public CameraViewPresentationApplyResult ApplyViewPresentation(
            CameraViewPresentationInput input,
            CameraViewAssignmentSnapshot currentSnapshot)
        {
            if (input == null || currentSnapshot == null || !input.IsValid)
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedInvalidInput,
                    input,
                    "View presentation apply requires valid input and its current logical snapshot.");
            }

            if (!input.IsCurrentFor(currentSnapshot) || IsOlderThanAppliedEvidence(input))
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedStaleInput,
                    input,
                    "Stale View presentation input cannot overwrite newer applied membership.");
            }

            if (PresentationIntent == CameraRigPresentationIntent.Follow &&
                !TryValidateSharedFollowSettings(out string settingsIssue))
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedInvalidSettings,
                    input,
                    settingsIssue);
            }

            if (cinemachineCamera == null)
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.RejectedMissingCinemachineCamera,
                    input,
                    "View presentation requires the Composer's existing materialized Cinemachine Camera.");
            }

            CameraViewTargetProjectionResult projection =
                ResolveViewPresentationTargets(input);

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededSharedFollow)
            {
                if (!TryEnsureSharedFollowProjection(out string ownershipIssue))
                {
                    return ViewApplyResult(
                        CameraViewPresentationApplyStatus.RejectedOwnershipConflict,
                        input,
                        ownershipIssue);
                }

                ReconcileSharedFollowMembers(input);
                ConfigureSharedFollowFraming();
                frameworkOwnedSharedFollowGroupFraming.enabled = true;
                cinemachineCamera.Follow = frameworkOwnedSharedFollowTargetGroup.transform;
                cinemachineCamera.LookAt =
                    EffectiveLookAtRequirement == CameraTargetRequirement.NotUsed
                        ? null
                        : frameworkOwnedSharedFollowTargetGroup.transform;
                RecordAppliedEvidence(input);
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededSharedFollow,
                    input,
                    $"Applied shared Follow presentation with '{input.SubjectCount}' ordered Subjects.");
            }

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededSingleSubject)
            {
                ClearOwnedSharedFollowProjection();
                cinemachineCamera.Follow = projection.Targets.FollowTarget;
                cinemachineCamera.LookAt = projection.Targets.LookAtTarget;
                RecordAppliedEvidence(input);
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededSingleSubject,
                    input,
                    "Applied direct single-Subject presentation to the existing Cinemachine Camera.");
            }

            ClearOwnedSharedFollowProjection();
            cinemachineCamera.Follow = null;
            cinemachineCamera.LookAt = null;
            RecordAppliedEvidence(input);

            if (projection.Status == CameraViewTargetProjectionStatus.SucceededNoTargets)
            {
                return ViewApplyResult(
                    CameraViewPresentationApplyStatus.SucceededFixedNoTargets,
                    input,
                    "Applied target-independent Fixed presentation.");
            }

            return ViewApplyResult(
                projection.Status == CameraViewTargetProjectionStatus.BlockedRequiredSubjectMissing
                    ? CameraViewPresentationApplyStatus.BlockedRequiredSubjectMissing
                    : CameraViewPresentationApplyStatus.BlockedUnsupportedPresentation,
                input,
                projection.BlockingIssue);
        }

        /// <summary>
        /// Clears only presentation state applied through the explicit View seam. Rig,
        /// Cinemachine Camera and Camera Output lifetime remain with their current owners.
        /// </summary>
        public CameraViewPresentationApplyResult ClearViewPresentation()
        {
            ClearOwnedSharedFollowProjection();
            if (cinemachineCamera != null)
            {
                cinemachineCamera.Follow = null;
                cinemachineCamera.LookAt = null;
            }

            appliedViewAssignmentContextId = string.Empty;
            appliedViewAssignmentRevision = -1;
            appliedViewAvailabilityContextId = string.Empty;
            appliedViewAvailabilityRevision = -1;
            return ViewApplyResult(
                CameraViewPresentationApplyStatus.SucceededCleared,
                null,
                "Cleared explicit View presentation without releasing the Composer, Cinemachine Camera or output.");
        }

        public CameraRigComposerDebugSnapshot CreateDebugSnapshot()
        {
            CameraTargetResolveResult resolution =
                ResolveConfiguredCameraTargets();

            CameraTargetSourceDescriptor source =
                resolution.Source;

            return new CameraRigComposerDebugSnapshot(
                presentationIntent,
                targetSource != null
                    ? resolution.Source.Kind
                    : targetSourceKind,
                source.LogicalSourceId,
                source.DiagnosticLabel,
                string.Empty,
                cinemachineCamera != null
                    ? cinemachineCamera.name.NormalizeText()
                    : string.Empty,
                lastResolvedFollowTarget != null
                    ? lastResolvedFollowTarget.name.NormalizeText()
                    : string.Empty,
                lastResolvedLookAtTarget != null
                    ? lastResolvedLookAtTarget.name.NormalizeText()
                    : string.Empty,
                lastApplyRebuildStatus.NormalizeText(),
                lastBlockingIssue.NormalizeText(),
                lastTargetResolutionSummary.NormalizeText(),
                lastMaterializationSummary.NormalizeText());
        }

        private static bool IsDefinedRequirement(
            CameraTargetRequirement requirement)
        {
            return requirement == CameraTargetRequirement.NotUsed ||
                   requirement == CameraTargetRequirement.Optional ||
                   requirement == CameraTargetRequirement.Required;
        }

        private bool TryEnsureSharedFollowProjection(out string issue)
        {
            issue = string.Empty;

            if (frameworkOwnedSharedFollowTargetGroup != null &&
                !IsChildOrSelf(frameworkOwnedSharedFollowTargetGroup.transform, transform))
            {
                issue = "Recorded shared Follow Target Group is outside this Composer rig.";
                return false;
            }

            CinemachineGroupFraming[] framings =
                cinemachineCamera.GetComponents<CinemachineGroupFraming>();
            for (int index = 0; index < framings.Length; index++)
            {
                if (framings[index] != frameworkOwnedSharedFollowGroupFraming)
                {
                    issue = "Author-owned or unproven Cinemachine Group Framing conflicts with shared Follow materialization.";
                    return false;
                }
            }

            if (frameworkOwnedSharedFollowGroupFraming != null &&
                frameworkOwnedSharedFollowGroupFraming.gameObject != cinemachineCamera.gameObject)
            {
                issue = "Recorded shared Follow Group Framing does not belong to this Composer's Cinemachine Camera.";
                return false;
            }

            if (frameworkOwnedSharedFollowTargetGroup == null)
            {
                var groupObject = new GameObject(DefaultSharedFollowTargetGroupObjectName);
                groupObject.transform.SetParent(transform, false);
                frameworkOwnedSharedFollowTargetGroup =
                    groupObject.AddComponent<CinemachineTargetGroup>();
            }

            if (frameworkOwnedSharedFollowGroupFraming == null)
            {
                frameworkOwnedSharedFollowGroupFraming =
                    cinemachineCamera.gameObject.AddComponent<CinemachineGroupFraming>();
            }

            return true;
        }

        private void ReconcileSharedFollowMembers(CameraViewPresentationInput input)
        {
            frameworkOwnedSharedFollowTargetGroup.Targets ??=
                new List<CinemachineTargetGroup.Target>();
            frameworkOwnedSharedFollowTargetGroup.Targets.Clear();
            for (int index = 0; index < input.SubjectCount; index++)
            {
                frameworkOwnedSharedFollowTargetGroup.Targets.Add(
                    new CinemachineTargetGroup.Target
                    {
                        Object = input.Subjects[index].Subject.Observation,
                        Weight = sharedFollowMemberWeight,
                        Radius = sharedFollowMemberRadius
                    });
            }

            frameworkOwnedSharedFollowTargetGroup.PositionMode =
                CinemachineTargetGroup.PositionModes.GroupCenter;
            frameworkOwnedSharedFollowTargetGroup.RotationMode =
                CinemachineTargetGroup.RotationModes.Manual;
            frameworkOwnedSharedFollowTargetGroup.UpdateMethod =
                CinemachineTargetGroup.UpdateMethods.LateUpdate;
        }

        private void ConfigureSharedFollowFraming()
        {
            frameworkOwnedSharedFollowGroupFraming.FramingMode =
                CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
            frameworkOwnedSharedFollowGroupFraming.SizeAdjustment =
                CinemachineGroupFraming.SizeAdjustmentModes.DollyThenZoom;
            frameworkOwnedSharedFollowGroupFraming.LateralAdjustment =
                CinemachineGroupFraming.LateralAdjustmentModes.ChangePosition;
            frameworkOwnedSharedFollowGroupFraming.FramingSize =
                sharedFollowFramingSize;
            frameworkOwnedSharedFollowGroupFraming.Damping =
                sharedFollowDamping;
            frameworkOwnedSharedFollowGroupFraming.FovRange =
                sharedFollowFovRange;
            frameworkOwnedSharedFollowGroupFraming.DollyRange =
                sharedFollowDollyRange;
            frameworkOwnedSharedFollowGroupFraming.OrthoSizeRange =
                sharedFollowOrthoSizeRange;
        }

        private void ClearOwnedSharedFollowProjection()
        {
            if (frameworkOwnedSharedFollowTargetGroup != null)
            {
                frameworkOwnedSharedFollowTargetGroup.Targets?.Clear();
            }

            if (frameworkOwnedSharedFollowGroupFraming != null)
            {
                frameworkOwnedSharedFollowGroupFraming.enabled = false;
            }
        }

        private bool IsOlderThanAppliedEvidence(CameraViewPresentationInput input)
        {
            if (!string.Equals(
                    appliedViewAssignmentContextId,
                    input.AssignmentContextId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return input.AssignmentRevision < appliedViewAssignmentRevision ||
                   (string.Equals(
                        appliedViewAvailabilityContextId,
                        input.AvailabilityContextId,
                        StringComparison.Ordinal) &&
                    input.AvailabilityRevision < appliedViewAvailabilityRevision);
        }

        private void RecordAppliedEvidence(CameraViewPresentationInput input)
        {
            appliedViewAssignmentContextId = input.AssignmentContextId;
            appliedViewAssignmentRevision = input.AssignmentRevision;
            appliedViewAvailabilityContextId = input.AvailabilityContextId;
            appliedViewAvailabilityRevision = input.AvailabilityRevision;
        }

        private CameraViewPresentationApplyResult ViewApplyResult(
            CameraViewPresentationApplyStatus status,
            CameraViewPresentationInput input,
            string diagnostic)
        {
            return new CameraViewPresentationApplyResult(
                status,
                input,
                cinemachineCamera,
                frameworkOwnedSharedFollowTargetGroup,
                frameworkOwnedSharedFollowGroupFraming,
                diagnostic);
        }

        private bool TryValidateSharedFollowSettings(out string issue)
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

        private static bool IsChildOrSelf(Transform candidate, Transform root)
        {
            return candidate != null && root != null &&
                   (candidate == root || candidate.IsChildOf(root));
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

        public void EditorSetApplyRebuildResult(
            string status,
            string blockingIssue,
            string targetResolutionSummary,
            string materializationSummary,
            Transform resolvedFollowTarget,
            Transform resolvedLookAtTarget)
        {
            lastApplyRebuildStatus =
                status.NormalizeText();

            lastBlockingIssue =
                blockingIssue.NormalizeText();

            lastTargetResolutionSummary =
                targetResolutionSummary.NormalizeText();

            lastMaterializationSummary =
                materializationSummary.NormalizeText();

            lastResolvedFollowTarget =
                resolvedFollowTarget;

            lastResolvedLookAtTarget =
                resolvedLookAtTarget;
        }

        private void Reset()
        {
            presentationIntent =
                CameraRigPresentationIntent.Follow;

            targetSourceKind =
                CameraTargetSourceKind.ExplicitTransform;

            followRequirement =
                CameraTargetRequirement.Required;

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
            appliedViewAssignmentContextId = string.Empty;
            appliedViewAssignmentRevision = -1;
            appliedViewAvailabilityContextId = string.Empty;
            appliedViewAvailabilityRevision = -1;
            materializationRevision = 0;
        }
#endif
    }
}
