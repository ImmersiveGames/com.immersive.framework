using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [CreateAssetMenu(fileName = "Third Person Camera Rig Behavior", menuName = "Immersive Framework/Camera/Rig Behaviors/Third Person")]
    public sealed class ThirdPersonCameraRigBehaviorDefinition : CameraRigBehaviorDefinition
    {
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.5f, -0.4f, 0f);
        [SerializeField] private float verticalArmLength = 0.4f;
        [SerializeField, Range(0f, 1f)] private float cameraSide = 1f;
        [SerializeField, Min(0f)] private float cameraDistance = 2f;
        [SerializeField] private Vector3 damping = new Vector3(0.1f, 0.5f, 0.3f);

        public override CameraRigPresentationIntent PresentationIntent => CameraRigPresentationIntent.ThirdPerson;
        public override CameraTargetRequirement FollowRequirement => CameraTargetRequirement.Required;
        public override CameraTargetRequirement LookAtRequirement => CameraTargetRequirement.NotUsed;
        public Vector3 ShoulderOffset => shoulderOffset;
        public float VerticalArmLength => verticalArmLength;
        public float CameraSide => cameraSide;
        public float CameraDistance => cameraDistance;
        public Vector3 Damping => damping;

        public override bool TryValidate(out string issue)
        {
            if (!IsFinite(shoulderOffset))
                return Invalid(nameof(shoulderOffset), shoulderOffset, "a finite Vector3", out issue);
            if (!IsFinite(verticalArmLength))
                return Invalid(nameof(verticalArmLength), verticalArmLength, "a finite value", out issue);
            if (!IsFinite(cameraSide) || cameraSide < 0f || cameraSide > 1f)
                return Invalid(nameof(cameraSide), cameraSide, "a finite value from 0 through 1", out issue);
            if (!IsFiniteNonNegative(cameraDistance))
                return Invalid(nameof(cameraDistance), cameraDistance, "a finite non-negative value", out issue);
            if (!IsFiniteNonNegative(damping))
                return Invalid(nameof(damping), damping, "a finite non-negative Vector3", out issue);

            issue = string.Empty;
            return true;
        }
    }
}
