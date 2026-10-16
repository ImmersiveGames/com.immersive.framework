using System;
using Immersive.Framework.Camera;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Selectable follow behavior within the experimental Camera Rig authoring surface.")]
    public enum FollowCameraRigMode
    {
        FixedOffset = 0,
        ScreenComposer = 1
    }

    [CreateAssetMenu(fileName = "Follow Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Follow")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-027-C reusable Camera Rig behavior; IF-ADR-038 Session Camera use.")]
    public sealed class FollowCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        [SerializeField] private CameraTargetRequirement lookAtRequirement = CameraTargetRequirement.Optional;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 5f, -8f);
        [SerializeField] private FollowCameraRigMode followMode = FollowCameraRigMode.FixedOffset;
        [SerializeField, Min(0.01f)] private float cameraDistance = 10f;
        [SerializeField, Min(0f)] private float deadZoneDepth;
        [SerializeField] private ScreenComposerSettings composition = ScreenComposerSettings.Default;
        [SerializeField] private bool centerOnActivate = true;
        [SerializeField] private Vector3 targetOffset;
        [SerializeField] private Vector3 damping = Vector3.one;
        [SerializeField] private LookaheadSettings lookahead;
        [SerializeField, HideInInspector] private bool hasCapturedTrackerSettings;
        [SerializeField, HideInInspector] private TrackerSettings capturedTrackerSettings = TrackerSettings.Default;

        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.Follow;
        public override CameraTargetRequirement FollowRequirement => CameraTargetRequirement.Required;
        public override CameraTargetRequirement LookAtRequirement =>
            followMode == FollowCameraRigMode.ScreenComposer
                ? CameraTargetRequirement.NotUsed
                : lookAtRequirement;
        public Vector3 FollowOffset => followOffset;
        public FollowCameraRigMode FollowMode => followMode;
        public float CameraDistance => cameraDistance;
        public float DeadZoneDepth => deadZoneDepth;
        public ScreenComposerSettings Composition => composition;
        public bool CenterOnActivate => centerOnActivate;
        public Vector3 TargetOffset => targetOffset;
        public Vector3 Damping => damping;
        public LookaheadSettings Lookahead => lookahead;
        public bool HasCapturedTrackerSettings => hasCapturedTrackerSettings;
        public TrackerSettings CapturedTrackerSettings => capturedTrackerSettings;

        public override bool TryValidate(out string issue)
        {
            if (followMode != FollowCameraRigMode.FixedOffset &&
                followMode != FollowCameraRigMode.ScreenComposer)
                return Invalid(nameof(followMode), followMode, "FixedOffset or ScreenComposer", out issue);

            if (followMode == FollowCameraRigMode.FixedOffset)
            {
                if (!IsDefinedRequirement(lookAtRequirement))
                    return Invalid(nameof(lookAtRequirement), lookAtRequirement, "Not Used, Optional or Required", out issue);
                if (!IsFinite(followOffset))
                    return Invalid(nameof(followOffset), followOffset, "a finite Vector3", out issue);
                if (hasCapturedTrackerSettings &&
                    (!Enum.IsDefined(typeof(BindingMode), capturedTrackerSettings.BindingMode) ||
                     !Enum.IsDefined(typeof(AngularDampingMode), capturedTrackerSettings.AngularDampingMode) ||
                     !IsFiniteNonNegative(capturedTrackerSettings.PositionDamping) ||
                     !IsFiniteNonNegative(capturedTrackerSettings.RotationDamping) ||
                     !IsFiniteNonNegative(capturedTrackerSettings.QuaternionDamping)))
                    return Invalid(nameof(capturedTrackerSettings), capturedTrackerSettings.BindingMode, "valid Cinemachine tracker values", out issue);
            }
            else if (!TryValidateScreenComposerSettings(
                         cameraDistance,
                         deadZoneDepth,
                         composition,
                         targetOffset,
                         damping,
                         lookahead,
                         out issue))
            {
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public static bool TryValidateScreenComposerSettings(
            float cameraDistance,
            float deadZoneDepth,
            ScreenComposerSettings composition,
            Vector3 targetOffset,
            Vector3 damping,
            LookaheadSettings lookahead,
            out string issue)
        {
            if (!IsFinite(cameraDistance) || cameraDistance < 0.01f)
                return InvalidStatic(nameof(cameraDistance), cameraDistance, "a finite value of at least 0.01", out issue);
            if (!IsFinite(deadZoneDepth) || deadZoneDepth < 0f)
                return InvalidStatic(nameof(deadZoneDepth), deadZoneDepth, "a finite non-negative value", out issue);
            if (!IsFinite(composition.ScreenPosition) ||
                composition.ScreenPosition.x < -1.5f || composition.ScreenPosition.x > 1.5f ||
                composition.ScreenPosition.y < -1.5f || composition.ScreenPosition.y > 1.5f)
                return InvalidStatic(nameof(composition.ScreenPosition), composition.ScreenPosition, "a finite point within -1.5 through 1.5", out issue);
            if (!IsFinite(composition.DeadZone.Size) ||
                composition.DeadZone.Size.x < 0f || composition.DeadZone.Size.x > 2f ||
                composition.DeadZone.Size.y < 0f || composition.DeadZone.Size.y > 2f)
                return InvalidStatic(nameof(composition.DeadZone), composition.DeadZone.Size, "finite dimensions within 0 through 2", out issue);
            if (!IsFinite(composition.HardLimits.Size) ||
                composition.HardLimits.Size.x < composition.DeadZone.Size.x || composition.HardLimits.Size.x > 3f ||
                composition.HardLimits.Size.y < composition.DeadZone.Size.y || composition.HardLimits.Size.y > 3f ||
                !IsFinite(composition.HardLimits.Offset) ||
                composition.HardLimits.Offset.x < -1f || composition.HardLimits.Offset.x > 1f ||
                composition.HardLimits.Offset.y < -1f || composition.HardLimits.Offset.y > 1f)
                return InvalidStatic(nameof(composition.HardLimits), composition.HardLimits.Size, "finite limits no smaller than the Dead Zone, within 0 through 3, and offsets within -1 through 1", out issue);
            if (!IsFinite(targetOffset))
                return InvalidStatic(nameof(targetOffset), targetOffset, "a finite Vector3", out issue);
            if (!IsFiniteNonNegative(damping))
                return InvalidStatic(nameof(damping), damping, "a finite non-negative Vector3", out issue);
            if (!IsFinite(lookahead.Time) || lookahead.Time < 0f || lookahead.Time > 1f ||
                !IsFinite(lookahead.Smoothing) || lookahead.Smoothing < 0f || lookahead.Smoothing > 30f)
                return InvalidStatic(nameof(lookahead), lookahead.Time, "finite lookahead values within their Cinemachine ranges", out issue);

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void EditorCaptureFollowOffset(Vector3 value)
        {
            followOffset = value;
        }

        public void EditorCaptureTrackerSettings(TrackerSettings value)
        {
            capturedTrackerSettings = value;
            hasCapturedTrackerSettings = true;
        }

        public void EditorCaptureScreenComposerSettings(
            float capturedCameraDistance,
            float capturedDeadZoneDepth,
            ScreenComposerSettings capturedComposition,
            bool capturedCenterOnActivate,
            Vector3 capturedTargetOffset,
            Vector3 capturedDamping,
            LookaheadSettings capturedLookahead)
        {
            cameraDistance = capturedCameraDistance;
            deadZoneDepth = capturedDeadZoneDepth;
            composition = capturedComposition;
            centerOnActivate = capturedCenterOnActivate;
            targetOffset = capturedTargetOffset;
            damping = capturedDamping;
            lookahead = capturedLookahead;
        }
#endif

        private static bool InvalidStatic(string field, object value, string expectation, out string issue)
        {
            issue = $"Screen Composer Follow has invalid '{field}' value '{value}'. Expected {expectation}.";
            return false;
        }
    }
}
