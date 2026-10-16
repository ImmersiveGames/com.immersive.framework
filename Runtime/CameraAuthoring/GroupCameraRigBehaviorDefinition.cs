using System;
using Immersive.Framework.Camera;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "Group Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Group")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-027-C reusable Camera Rig behavior; IF-ADR-038 Session Camera use.")]
    public sealed class GroupCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        [SerializeField] private CameraTargetRequirement followRequirement = CameraTargetRequirement.Required;
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 5f, -8f);
        [SerializeField, Min(0.0001f)] private float memberWeight = 1f;
        [SerializeField, Min(0.0001f)] private float memberRadius = 0.5f;
        [SerializeField, Range(0.01f, 2f)] private float framingSize = 0.8f;
        [SerializeField, Range(0f, 20f)] private float damping = 2f;
        [SerializeField] private Vector2 fovRange = new Vector2(1f, 100f);
        [SerializeField] private Vector2 dollyRange = new Vector2(-100f, 100f);
        [SerializeField] private Vector2 orthoSizeRange = new Vector2(1f, 1000f);
        [SerializeField] private CinemachineGroupFraming.FramingModes framingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
        [SerializeField] private Vector2 centerOffset;
        [SerializeField] private CinemachineGroupFraming.SizeAdjustmentModes sizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyThenZoom;
        [SerializeField] private CinemachineGroupFraming.LateralAdjustmentModes lateralAdjustment = CinemachineGroupFraming.LateralAdjustmentModes.ChangePosition;
        [SerializeField, HideInInspector] private bool hasCapturedTrackerSettings;
        [SerializeField, HideInInspector] private TrackerSettings capturedTrackerSettings = TrackerSettings.Default;

        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.Group;
        public override CameraTargetRequirement FollowRequirement => followRequirement;
        public override CameraTargetRequirement LookAtRequirement => lookAtRequirement;
        public Vector3 FollowOffset => followOffset;
        public float MemberWeight => memberWeight;
        public float MemberRadius => memberRadius;
        public float FramingSize => framingSize;
        public float Damping => damping;
        public Vector2 FovRange => fovRange;
        public Vector2 DollyRange => dollyRange;
        public Vector2 OrthoSizeRange => orthoSizeRange;
        public CinemachineGroupFraming.FramingModes FramingMode => framingMode;
        public Vector2 CenterOffset => centerOffset;
        public CinemachineGroupFraming.SizeAdjustmentModes SizeAdjustment => sizeAdjustment;
        public CinemachineGroupFraming.LateralAdjustmentModes LateralAdjustment => lateralAdjustment;
        public bool HasCapturedTrackerSettings => hasCapturedTrackerSettings;
        public TrackerSettings CapturedTrackerSettings => capturedTrackerSettings;

        public override bool TryValidate(out string issue)
        {
            if (!IsDefinedRequirement(followRequirement))
                return Invalid(nameof(followRequirement), followRequirement, "Not Used, Optional or Required", out issue);
            if (!IsDefinedRequirement(lookAtRequirement))
                return Invalid(nameof(lookAtRequirement), lookAtRequirement, "Not Used, Optional or Required", out issue);
            if (!IsFinite(followOffset))
                return Invalid(nameof(followOffset), followOffset, "a finite Vector3", out issue);
            if (!IsFinite(memberWeight) || memberWeight <= 0f)
                return Invalid(nameof(memberWeight), memberWeight, "a finite value greater than zero", out issue);
            if (!IsFinite(memberRadius) || memberRadius <= 0f)
                return Invalid(nameof(memberRadius), memberRadius, "a finite value greater than zero", out issue);
            if (!IsFinite(framingSize) || framingSize < 0.01f || framingSize > 2f)
                return Invalid(nameof(framingSize), framingSize, "a finite value from 0.01 through 2", out issue);
            if (!IsFinite(damping) || damping < 0f || damping > 20f)
                return Invalid(nameof(damping), damping, "a finite value from 0 through 20", out issue);
            if (!IsOrderedRange(fovRange, 1f, 179f))
                return Invalid(nameof(fovRange), fovRange, "an ordered finite range within 1 through 179", out issue);
            if (!IsOrderedRange(dollyRange, float.MinValue, float.MaxValue))
                return Invalid(nameof(dollyRange), dollyRange, "an ordered finite range", out issue);
            if (!IsOrderedRange(orthoSizeRange, 0.01f, float.MaxValue))
                return Invalid(nameof(orthoSizeRange), orthoSizeRange, "an ordered finite range starting at 0.01 or greater", out issue);
            if (!IsFinite(centerOffset))
                return Invalid(nameof(centerOffset), centerOffset, "a finite Vector2", out issue);
            if (!Enum.IsDefined(typeof(CinemachineGroupFraming.FramingModes), framingMode) ||
                !Enum.IsDefined(typeof(CinemachineGroupFraming.SizeAdjustmentModes), sizeAdjustment) ||
                !Enum.IsDefined(typeof(CinemachineGroupFraming.LateralAdjustmentModes), lateralAdjustment))
                return Invalid("framingMode/sizeAdjustment/lateralAdjustment", $"{framingMode}/{sizeAdjustment}/{lateralAdjustment}", "supported Cinemachine Group Framing modes", out issue);
            if (hasCapturedTrackerSettings &&
                (!Enum.IsDefined(typeof(BindingMode), capturedTrackerSettings.BindingMode) ||
                 !Enum.IsDefined(typeof(AngularDampingMode), capturedTrackerSettings.AngularDampingMode) ||
                 !IsFiniteNonNegative(capturedTrackerSettings.PositionDamping) ||
                 !IsFiniteNonNegative(capturedTrackerSettings.RotationDamping) ||
                 !IsFiniteNonNegative(capturedTrackerSettings.QuaternionDamping)))
                return Invalid(nameof(capturedTrackerSettings), capturedTrackerSettings.BindingMode, "valid Cinemachine tracker values", out issue);

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void EditorCaptureSettings(
            Vector3 capturedFollowOffset,
            float capturedMemberWeight,
            float capturedMemberRadius,
            float capturedFramingSize,
            float capturedDamping,
            Vector2 capturedFovRange,
            Vector2 capturedDollyRange,
            Vector2 capturedOrthoSizeRange,
            CinemachineGroupFraming.FramingModes capturedFramingMode,
            Vector2 capturedCenterOffset,
            CinemachineGroupFraming.SizeAdjustmentModes capturedSizeAdjustment,
            CinemachineGroupFraming.LateralAdjustmentModes capturedLateralAdjustment,
            TrackerSettings capturedTrackerSettings)
        {
            followOffset = capturedFollowOffset;
            memberWeight = capturedMemberWeight;
            memberRadius = capturedMemberRadius;
            framingSize = capturedFramingSize;
            damping = capturedDamping;
            fovRange = capturedFovRange;
            dollyRange = capturedDollyRange;
            orthoSizeRange = capturedOrthoSizeRange;
            framingMode = capturedFramingMode;
            centerOffset = capturedCenterOffset;
            sizeAdjustment = capturedSizeAdjustment;
            lateralAdjustment = capturedLateralAdjustment;
            this.capturedTrackerSettings = capturedTrackerSettings;
            hasCapturedTrackerSettings = true;
        }
#endif
    }
}
